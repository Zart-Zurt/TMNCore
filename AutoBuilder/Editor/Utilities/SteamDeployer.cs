using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AutoBuilder.Editor
{
    public static class SteamDeployer
    {
        private const uint BmClick = 0x00F5;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public static CancellationTokenSource StartWindowAutoCloser()
        {
            var cts = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var hwnd = FindWindow(null, "Opening file failed");
                    if (hwnd != IntPtr.Zero)
                    {
                        var btn = FindWindowExRecursive(hwnd, IntPtr.Zero, "Button", "Try Again");
                        if (btn == IntPtr.Zero)
                        {
                            btn = FindWindowExRecursive(hwnd, IntPtr.Zero, "Button", "&Try Again");
                        }
                        if (btn == IntPtr.Zero)
                        {
                            btn = FindWindowExRecursive(hwnd, IntPtr.Zero, "Button", "Try &Again");
                        }

                        if (btn != IntPtr.Zero)
                        {
                            SendMessage(btn, BmClick, IntPtr.Zero, IntPtr.Zero);
                            Debug.Log("[AutoBuilder] Automatically clicked 'Try Again' on build error dialog.");
                        }
                    }

                    Thread.Sleep(500);
                }
            });

            return cts;
        }

        private static IntPtr FindWindowExRecursive(IntPtr parent, IntPtr childAfter, string className, string windowName)
        {
            var result = FindWindowEx(parent, childAfter, className, windowName);
            if (result != IntPtr.Zero)
            {
                return result;
            }

            var child = FindWindowEx(parent, IntPtr.Zero, null, null);
            while (child != IntPtr.Zero)
            {
                var found = FindWindowExRecursive(child, IntPtr.Zero, className, windowName);
                if (found != IntPtr.Zero)
                {
                    return found;
                }
                child = FindWindowEx(parent, child, null, null);
            }

            return IntPtr.Zero;
        }

        public static bool ValidateSteamSdkPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var contentPath = Path.Combine(path, "tools", "ContentBuilder", "content");
            return Directory.Exists(contentPath);
        }

        public static void UploadToSteam(string sdkPath, uint appId, string version, string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Debug.LogError("[AutoBuilder] Please enter your Steam username and password.");
                return;
            }

            sdkPath = Path.GetFullPath(sdkPath);

            var contentBuilderDir = Path.Combine(sdkPath, "tools", "ContentBuilder");
            var scriptsDir = Path.Combine(contentBuilderDir, "scripts");
            var builderDir = Path.Combine(contentBuilderDir, "builder");

            if (!Directory.Exists(scriptsDir))
            {
                Directory.CreateDirectory(scriptsDir);
            }

            var depotId = appId + 1;
            var appVdfPath = Path.Combine(scriptsDir, $"app_build_{appId}.vdf");
            var depotVdfPath = Path.Combine(scriptsDir, $"depot_build_{depotId}.vdf");

            var outputDir = Path.Combine(contentBuilderDir, "output");
            var contentDir = Path.Combine(contentBuilderDir, "content");

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            GenerateDepotBuildVdf(depotVdfPath, depotId, contentDir);
            GenerateAppBuildVdf(appVdfPath, appId, depotId, depotVdfPath, outputDir, version);

            var steamCmdExe = FindSteamCmdExecutable(sdkPath);
            if (string.IsNullOrEmpty(steamCmdExe) || !File.Exists(steamCmdExe))
            {
                Debug.LogError($"[AutoBuilder] steamcmd.exe not found! Please check the SDK path: {sdkPath}");
                return;
            }

            Debug.Log("[AutoBuilder] Uploading to Steam via steamcmd...");

            var args = $"+login \"{username}\" \"{password}\" +run_app_build \"{appVdfPath}\" +quit";

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var startInfo = new ProcessStartInfo(steamCmdExe, args)
                    {
                        WorkingDirectory = Path.GetDirectoryName(steamCmdExe),
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };

                    using (var process = new Process { StartInfo = startInfo })
                    {
                        process.Start();
                        process.WaitForExit();

                        var exitCode = process.ExitCode;
                        if (exitCode == 0)
                        {
                            Debug.Log("[AutoBuilder] Steam upload completed successfully!");
                        }
                        else if (exitCode == 5)
                        {
                            Debug.LogError("[AutoBuilder] steamcmd exited with code: 5 (Authentication / Timeout). Please click 'Login SteamCMD' button to authenticate Steam Guard 2FA.");
                        }
                        else
                        {
                            Debug.LogError($"[AutoBuilder] steamcmd exited with code: {exitCode}.");
                        }
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[AutoBuilder] steamcmd failed to start: {exception.Message}");
                }
            });
        }

        public static void LoginSteamCmd(string sdkPath, string username, string password)
        {
            var steamCmdExe = FindSteamCmdExecutable(sdkPath);
            if (string.IsNullOrEmpty(steamCmdExe) || !File.Exists(steamCmdExe))
            {
                Debug.LogError($"[AutoBuilder] steamcmd not found! Check SDK path: {sdkPath}");
                return;
            }

            try
            {
                var absoluteExePath = Path.GetFullPath(steamCmdExe);
                var workingDirectory = Path.GetDirectoryName(absoluteExePath);
                ProcessStartInfo startInfo;

                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    var cmdArgs = $"/k \"cd /d \"{workingDirectory}\" && \"{absoluteExePath}\" +login \"{username}\" \"{password}\"\"";
                    startInfo = new ProcessStartInfo("cmd.exe", cmdArgs)
                    {
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                }
                else
                {
                    var args = $"+login \"{username}\" \"{password}\"";
                    startInfo = new ProcessStartInfo(absoluteExePath, args)
                    {
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                }

                Debug.Log($"[AutoBuilder] Launching SteamCMD login window: {absoluteExePath}");
                Process.Start(startInfo);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AutoBuilder] Failed to launch steamcmd: {exception.Message}");
            }
        }

        public static void OpenSteamPipeGui(string sdkPath)
        {
            var guiPath = GetSteamPipeGuiPath(sdkPath);
            if (string.IsNullOrEmpty(guiPath) || !File.Exists(guiPath))
            {
                Debug.LogError($"[AutoBuilder] SteamPipeGUI.exe not found under: {sdkPath}");
                return;
            }

            try
            {
                var absoluteGuiPath = Path.GetFullPath(guiPath);
                var startInfo = new ProcessStartInfo(absoluteGuiPath)
                {
                    WorkingDirectory = Path.GetDirectoryName(absoluteGuiPath),
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AutoBuilder] Failed to launch SteamPipeGUI: {exception.Message}");
            }
        }

        public static void WriteLocalSteamAppIdFile(uint appId, bool isLocal)
        {
            try
            {
                var projectRootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var filePath = Path.Combine(projectRootPath, "steam_appid.txt");

                if (!isLocal)
                {
                    File.WriteAllText(filePath, appId.ToString());
                    Debug.Log($"[AutoBuilder] Created/Updated local steam_appid.txt in project root with AppID: {appId}");
                }
                else
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Debug.Log("[AutoBuilder] Deleted local steam_appid.txt in project root for Local platform.");
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AutoBuilder] Failed to write local steam_appid.txt: {exception.Message}");
            }
        }

        public static bool UpdateAppIdInScript(uint activeAppId)
        {
            var guids = AssetDatabase.FindAssets("AppIdHolder t:Script");
            if (guids.Length == 0)
            {
                Debug.LogError("[AutoBuilder] AppIdHolder.cs not found!");
                return false;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var content = File.ReadAllText(path);

            var pattern = @"return\s+\d+\s*;";
            var replacement = $"return {activeAppId};";

            if (Regex.IsMatch(content, pattern))
            {
                var newContent = Regex.Replace(content, pattern, replacement);
                if (newContent != content)
                {
                    File.WriteAllText(path, newContent);
                    AssetDatabase.ImportAsset(path);
                    Debug.Log($"[AutoBuilder] AppIdHolder.cs updated. Active ID: {activeAppId}");
                    return true;
                }
            }

            return false;
        }

        private static string FindSteamCmdExecutable(string sdkPath)
        {
            var contentBuilderDir = Path.Combine(sdkPath, "tools", "ContentBuilder");
            var builderDir = Path.Combine(contentBuilderDir, "builder");
            var steamCmdExe = Path.Combine(builderDir, "steamcmd.exe");

            if (!File.Exists(steamCmdExe))
            {
                steamCmdExe = Path.Combine(sdkPath, "tools", "builder", "steamcmd.exe");
                if (!File.Exists(steamCmdExe))
                {
                    steamCmdExe = Path.Combine(contentBuilderDir, "builder_osx", "steamcmd");
                }
            }

            return steamCmdExe;
        }

        private static string GetSteamPipeGuiPath(string sdkPath)
        {
            var toolsDir = Path.Combine(sdkPath, "tools");
            var contentBuilderDir = Path.Combine(toolsDir, "ContentBuilder");
            var possiblePaths = new[]
            {
                Path.Combine(toolsDir, "SteamPipeGUI", "SteamPipeGUI.exe"),
                Path.Combine(contentBuilderDir, "builder_ui", "SteamPipeGUI.exe"),
                Path.Combine(contentBuilderDir, "SteamPipeGUI.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static void GenerateAppBuildVdf(string path, uint appId, uint depotId, string depotVdfPath, string outputDir, string version)
        {
            var content = $@"
""appbuild""
{{
	""appid"" ""{appId}""
	""desc"" ""{version}""
	""buildoutput"" ""{outputDir}""
	""contentroot"" """"
	""preview"" ""0""
	""local""	""""
	""depots""
	{{
		""{depotId}""	""{depotVdfPath}""
	}}
}}";
            File.WriteAllText(path, content.Trim());
        }

        private static void GenerateDepotBuildVdf(string path, uint depotId, string contentDir)
        {
            var content = $@"
""DepotBuildConfig""
{{
	""DepotID"" ""{depotId}""
	""contentroot"" ""{contentDir}""
	""FileMapping""
	{{
		""LocalPath"" ""*""
		""DepotPath"" "".""
		""recursive"" ""1""
	}}
	""FileExclusion"" ""*.pdb""
	""FileExclusion"" ""steam_appid.txt""
}}";
            File.WriteAllText(path, content.Trim());
        }
    }
}
