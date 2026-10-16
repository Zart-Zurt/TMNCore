using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AutoBuilder.Editor
{
    public static class ProcessRunner
    {
        public static int Run(string executable, string arguments, string workingDirectory = "", bool createNoWindow = true)
        {
            try
            {
                var startInfo = new ProcessStartInfo(executable, arguments)
                {
                    WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? Path.GetDirectoryName(executable) : workingDirectory,
                    UseShellExecute = !createNoWindow,
                    CreateNoWindow = createNoWindow
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return -1;
                    }

                    process.WaitForExit();
                    return process.ExitCode;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AutoBuilder] Failed to run process '{executable}': {exception.Message}");
                return -1;
            }
        }

        public static void RunDetached(string executable, string arguments, string workingDirectory = "")
        {
            try
            {
                var startInfo = new ProcessStartInfo(executable, arguments)
                {
                    WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? Path.GetDirectoryName(executable) : workingDirectory,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process.Start(startInfo);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AutoBuilder] Failed to launch detached process '{executable}': {exception.Message}");
            }
        }

        public static bool CreateZipWithTar(string sourceFolder, string targetZipPath)
        {
            try
            {
                var parentDir = Path.GetDirectoryName(sourceFolder);
                var folderName = Path.GetFileName(sourceFolder);
                var args = $"-a -c -f \"{targetZipPath}\" -C \"{parentDir}\" \"{folderName}\"";

                var code = Run("tar.exe", args, parentDir, true);
                return code == 0;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AutoBuilder] Tar compression failed: {exception.Message}");
                return false;
            }
        }

        public static void KillRunningProcesses(string baseName)
        {
            var cleanName = baseName.Replace("_v", "").Replace("_", "").Trim();

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    var name = process.ProcessName;
                    if ((!string.IsNullOrEmpty(baseName) && name.StartsWith(baseName, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(cleanName) && name.Contains(cleanName, StringComparison.OrdinalIgnoreCase)) ||
                        name.Contains("PokeFood", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("WindowsPlayer", StringComparison.OrdinalIgnoreCase))
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                }
                catch
                {
                }
            }
        }

        public static void DeleteDirectoryWithRetries(string dirPath, int maxRetries = 10, int delayMs = 200)
        {
            for (var i = 0; i < maxRetries; i++)
            {
                try
                {
                    if (Directory.Exists(dirPath))
                    {
                        Directory.Delete(dirPath, true);
                    }

                    return;
                }
                catch (IOException)
                {
                    if (i == maxRetries - 1)
                    {
                        throw;
                    }

                    Thread.Sleep(delayMs);
                }
            }
        }

        public static void DeleteFileWithRetries(string filePath, int maxRetries = 10, int delayMs = 200)
        {
            for (var i = 0; i < maxRetries; i++)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    return;
                }
                catch (IOException)
                {
                    if (i == maxRetries - 1)
                    {
                        throw;
                    }

                    Thread.Sleep(delayMs);
                }
            }
        }

        public static void CopyDirectory(string sourceDir, string destinationDir, string excludeFileName = "")
        {
            var dir = new DirectoryInfo(sourceDir);
            var dirs = dir.GetDirectories();

            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            foreach (var file in dir.GetFiles())
            {
                if (!string.IsNullOrEmpty(excludeFileName) && file.Name.Equals(excludeFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            foreach (var subDir in dirs)
            {
                var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, excludeFileName);
            }
        }
    }
}
