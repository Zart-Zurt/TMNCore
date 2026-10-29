const fs = require("fs");
const path = require("path");
const zlib = require("zlib");
const { execSync } = require("child_process");

function resolveNodeSsh()
{
  try
  {
    const mod = require("node-ssh");
    return mod.NodeSSH || mod;
  }
  catch (e) {}

  const globalRoots = [];
  try
  {
    const globalNpm = execSync("npm root -g", { encoding: "utf8" }).trim();
    if (globalNpm)
    {
      globalRoots.push(globalNpm);
    }
  }
  catch (e) {}

  const userProfile = process.env.USERPROFILE || process.env.HOME || "";
  if (userProfile)
  {
    globalRoots.push(path.join(userProfile, "AppData", "Roaming", "npm", "node_modules"));
  }

  for (const root of globalRoots)
  {
    const target = path.join(root, "node-ssh");
    if (fs.existsSync(target))
    {
      try
      {
        const mod = require(target);
        return mod.NodeSSH || mod;
      }
      catch (e) {}
    }
  }

  console.log("[Penta Deploy] Installing node-ssh globally...");
  execSync("npm install -g node-ssh", { stdio: "inherit" });

  for (const root of globalRoots)
  {
    const target = path.join(root, "node-ssh");
    if (fs.existsSync(target))
    {
      const mod = require(target);
      return mod.NodeSSH || mod;
    }
  }

  const fallback = require("node-ssh");
  return fallback.NodeSSH || fallback;
}

function normalizeBuildCompression(sourceDir)
{
  const buildDir = path.join(sourceDir, "Build");
  if (!fs.existsSync(buildDir))
  {
    return;
  }

  const files = fs.readdirSync(buildDir);
  let converted = false;

  for (const file of files)
  {
    const filePath = path.join(buildDir, file);

    if (file.endsWith(".br"))
    {
      const rawName = file.slice(0, -3);
      const destPath = path.join(buildDir, rawName);
      if (!fs.existsSync(destPath))
      {
        console.log(`[Penta Deploy] Decompressing Brotli: ${file} -> ${rawName}`);
        const buffer = fs.readFileSync(filePath);
        fs.writeFileSync(destPath, zlib.brotliDecompressSync(buffer));
        converted = true;
      }
    }
    else if (file.endsWith(".gz"))
    {
      const rawName = file.slice(0, -3);
      const destPath = path.join(buildDir, rawName);
      if (!fs.existsSync(destPath))
      {
        console.log(`[Penta Deploy] Decompressing Gzip: ${file} -> ${rawName}`);
        const buffer = fs.readFileSync(filePath);
        fs.writeFileSync(destPath, zlib.gunzipSync(buffer));
        converted = true;
      }
    }
  }

  const indexPath = path.join(sourceDir, "index.html");
  if (converted && fs.existsSync(indexPath))
  {
    let html = fs.readFileSync(indexPath, "utf8");
    html = html.replace(/\.data\.(br|gz)/g, ".data");
    html = html.replace(/\.framework\.js\.(br|gz)/g, ".framework.js");
    html = html.replace(/\.wasm\.(br|gz)/g, ".wasm");
    fs.writeFileSync(indexPath, html, "utf8");
    console.log("[Penta Deploy] Adjusted index.html to uncompressed asset paths.");
  }
}

async function run()
{
  const [,, sourceDir, slug, cleanVersion, host, portStr, username, password, remoteBasePath] = process.argv;

  if (!sourceDir || !slug || !cleanVersion || !host || !username)
  {
    console.error("Usage: node penta_ssh_sync.cjs <sourceDir> <slug> <cleanVersion> <host> <port> <username> <password> <remoteBasePath>");
    process.exit(1);
  }

  if (!fs.existsSync(sourceDir))
  {
    throw new Error(`Source directory does not exist: ${sourceDir}`);
  }

  const port = parseInt(portStr || "22", 10);
  const basePath = remoteBasePath || "/root/penta";

  normalizeBuildCompression(sourceDir);

  const NodeSSH = resolveNodeSsh();
  const ssh = new NodeSSH();

  const tempArchiveName = `proto_${slug}_${cleanVersion}_${Date.now()}.tar.gz`;
  const tempArchivePath = path.join(path.dirname(sourceDir), tempArchiveName);

  try
  {
    console.log(`[Penta Deploy] Creating archive: ${tempArchivePath}`);
    execSync(`tar -czf "${tempArchivePath}" -C "${sourceDir}" .`, { stdio: "inherit" });

    console.log(`[Penta Deploy] Connecting to SSH ${username}@${host}:${port}...`);
    const connectOptions = {
      host: host,
      port: port,
      username: username,
      readyTimeout: 30000
    };

    if (password && password.length > 0)
    {
      connectOptions.password = password;
    }

    const defaultKeyPath = path.join(process.env.USERPROFILE || process.env.HOME || "", ".ssh", "id_rsa");
    if (!connectOptions.password && fs.existsSync(defaultKeyPath))
    {
      connectOptions.privateKeyPath = defaultKeyPath;
    }

    await ssh.connect(connectOptions);
    console.log("[Penta Deploy] Connected via SSH.");

    const remoteDestDir = `${basePath}/public/prototypes/${slug}/${cleanVersion}`;
    const remoteArchivePath = `${basePath}/${tempArchiveName}`;

    console.log(`[Penta Deploy] Uploading archive to ${remoteArchivePath}...`);
    await ssh.putFile(tempArchivePath, remoteArchivePath);

    console.log(`[Penta Deploy] Extracting to remote directory ${remoteDestDir}...`);
    let result = await ssh.execCommand(`mkdir -p ${remoteDestDir} && tar -xzf ${remoteArchivePath} -C ${remoteDestDir} && rm -f ${remoteArchivePath}`);
    if (result.code !== 0 && result.code !== null)
    {
      throw new Error(`Remote extraction failed: ${result.stderr || result.stdout}`);
    }

    console.log("[Penta Deploy] Synchronizing to penta-web Docker container...");
    const dockerCommands = [
      `docker exec penta-web mkdir -p /app/public/prototypes/${slug}/${cleanVersion}`,
      `docker cp ${remoteDestDir}/. penta-web:/app/public/prototypes/${slug}/${cleanVersion}/`,
      `docker exec penta-web chmod -R 755 /app/public/prototypes/${slug}/${cleanVersion}`,
      "docker restart penta-web"
    ];

    for (const cmd of dockerCommands)
    {
      result = await ssh.execCommand(cmd);
      if (result.code !== 0 && result.code !== null)
      {
        console.warn(`[Penta Deploy] Warning on container step (${cmd}): ${result.stderr || result.stdout}`);
      }
    }

    ssh.dispose();
    console.log("[Penta Deploy] Remote SSH synchronization finished successfully!");
  }
  finally
  {
    if (fs.existsSync(tempArchivePath))
    {
      try { fs.unlinkSync(tempArchivePath); } catch (e) {}
    }
  }
}

run()
  .then(() => process.exit(0))
  .catch((err) =>
  {
    console.error("[Penta Deploy Error]", err.message || err);
    process.exit(1);
  });
