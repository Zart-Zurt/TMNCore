using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Core.Editor
{
    public class CorePackageUpdaterWindow : EditorWindow
    {
        private const string PackageId = "com.tmn.core";
        private const string GitRepositoryUrl = "https://github.com/Zart-Zurt/TMNCore.git#main";
        private const string LocalPackageUrl = "file:../../TMN-Core";

        private static AddRequest _addRequest;
        private static ListRequest _listRequest;

        private string _targetTagOrBranch = "main";
        private string _installedVersion = "Loading...";
        private string _installedSource = "Unknown";
        private string _installedHash = string.Empty;
        private string _statusMessage = "Ready";
        private MessageType _statusMessageType = MessageType.Info;
        private bool _isBusy;

        public static void ShowWindow()
        {
            var centerType = System.Type.GetType("Penta.Editor.ZZEngineControlCenterWindow, Penta.Editor")
                          ?? System.Type.GetType("Penta.Editor.PentaCoreWindow, Penta.Editor");
            if (centerType != null)
            {
                var method = centerType.GetMethod("OpenControlCenter", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, null);
                    return;
                }
            }

            var window = GetWindow<CorePackageUpdaterWindow>("TMN Core Updater");
            window.minSize = new Vector2(400, 330);
            window.Show();
        }

        public static void QuickUpdateToLatest()
        {
            var overviewType = System.Type.GetType("Penta.Editor.ZZEnginePackagesOverviewView, Penta.Editor");
            if (overviewType != null)
            {
                var method = overviewType.GetMethod("QuickUpdateAllPackagesStatic", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, null);
                    return;
                }
            }

            if (_addRequest != null && !_addRequest.IsCompleted)
            {
                EditorUtility.DisplayDialog("TMN Core Updater", "An update request is already in progress.", "OK");
                return;
            }

            var currentHash = GetCurrentInstalledGitHashStatic();
            if (TryGetRemoteCommitHash(GitRepositoryUrl, "main", out var remoteHash))
            {
                if (!string.IsNullOrEmpty(currentHash) &&
                    (currentHash.Equals(remoteHash, System.StringComparison.OrdinalIgnoreCase) ||
                     remoteHash.StartsWith(currentHash, System.StringComparison.OrdinalIgnoreCase) ||
                     currentHash.StartsWith(remoteHash, System.StringComparison.OrdinalIgnoreCase)))
                {
                    var shortHash = remoteHash.Length > 7 ? remoteHash.Substring(0, 7) : remoteHash;
                    var force = EditorUtility.DisplayDialog(
                        "TMN Core Updater",
                        $"TMN Core is already at the latest commit!\n\nInstalled commit: {shortHash}\nRemote commit: {shortHash}\n\nDo you want to force re-download anyway?",
                        "Force Re-download",
                        "Cancel");

                    if (!force)
                    {
                        return;
                    }
                }
            }

            ClearPackageLocks(PackageId);
            _addRequest = Client.Add(GitRepositoryUrl);
            EditorApplication.update += MonitorQuickAddRequest;
            EditorUtility.DisplayProgressBar("TMN Core Updater", "Fetching latest Core from GitHub...", 0.5f);
        }

        private static void MonitorQuickAddRequest()
        {
            if (_addRequest == null || !_addRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= MonitorQuickAddRequest;
            EditorUtility.ClearProgressBar();

            if (_addRequest.Status == StatusCode.Success)
            {
                EditorUtility.DisplayDialog("TMN Core Updater", "TMN Core package successfully updated to latest version!", "OK");
            }
            else
            {
                var error = _addRequest.Error != null ? _addRequest.Error.message : "Unknown error";
                EditorUtility.DisplayDialog("TMN Core Updater", "Failed to update package: " + error, "OK");
            }

            _addRequest = null;
        }

        private void OnEnable()
        {
            RefreshInstalledPackageInfo();
        }

        private void Update()
        {
            if (_listRequest != null && _listRequest.IsCompleted)
            {
                ProcessListRequest();
            }

            if (_addRequest != null && _addRequest.IsCompleted)
            {
                ProcessAddRequest();
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawPackageStatus();
            DrawActions();
            DrawStatusMessage();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("TMN Core Package Updater", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Manage and synchronize the TMN Core package across projects.", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);
        }

        private void DrawPackageStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Package:", PackageId, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Installed Version:", _installedVersion);
            EditorGUILayout.LabelField("Source:", _installedSource);

            if (!string.IsNullOrEmpty(_installedHash))
            {
                var shortHash = _installedHash.Length > 7 ? _installedHash.Substring(0, 7) : _installedHash;
                EditorGUILayout.LabelField("Git Hash:", shortHash);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawActions()
        {
            EditorGUI.BeginDisabledGroup(_isBusy);

            if (GUILayout.Button("Update to Latest (main branch)", GUILayout.Height(32)))
            {
                var currentHash = GetCurrentInstalledGitHash();
                if (TryGetRemoteCommitHash(GitRepositoryUrl, "main", out var remoteHash))
                {
                    if (!string.IsNullOrEmpty(currentHash) &&
                        (currentHash.Equals(remoteHash, System.StringComparison.OrdinalIgnoreCase) ||
                         remoteHash.StartsWith(currentHash, System.StringComparison.OrdinalIgnoreCase) ||
                         currentHash.StartsWith(remoteHash, System.StringComparison.OrdinalIgnoreCase)))
                    {
                        var shortHash = remoteHash.Length > 7 ? remoteHash.Substring(0, 7) : remoteHash;
                        var force = EditorUtility.DisplayDialog(
                            "TMN Core Updater",
                            $"TMN Core is already at the latest commit!\n\nInstalled commit: {shortHash}\nRemote commit: {shortHash}\n\nDo you want to force re-download anyway?",
                            "Force Re-download",
                            "Cancel");

                        if (!force)
                        {
                            SetStatus($"Already up to date with origin/main ({shortHash}).", MessageType.Info);
                            return;
                        }
                    }
                }

                StartUpdate(GitRepositoryUrl);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            _targetTagOrBranch = EditorGUILayout.TextField("Tag / Branch:", _targetTagOrBranch);
            if (GUILayout.Button("Update to Tag", GUILayout.Width(110)))
            {
                if (string.IsNullOrWhiteSpace(_targetTagOrBranch))
                {
                    SetStatus("Please specify a valid tag or branch.", MessageType.Warning);
                }
                else
                {
                    var branchOrTag = _targetTagOrBranch.Trim();
                    var currentHash = GetCurrentInstalledGitHash();
                    if (TryGetRemoteCommitHash(GitRepositoryUrl, branchOrTag, out var remoteHash))
                    {
                        if (!string.IsNullOrEmpty(currentHash) &&
                            (currentHash.Equals(remoteHash, System.StringComparison.OrdinalIgnoreCase) ||
                             remoteHash.StartsWith(currentHash, System.StringComparison.OrdinalIgnoreCase) ||
                             currentHash.StartsWith(remoteHash, System.StringComparison.OrdinalIgnoreCase)))
                        {
                            var shortHash = remoteHash.Length > 7 ? remoteHash.Substring(0, 7) : remoteHash;
                            var force = EditorUtility.DisplayDialog(
                                "TMN Core Updater",
                                $"TMN Core is already at the target commit for '{branchOrTag}'!\n\nInstalled commit: {shortHash}\n\nDo you want to force re-download anyway?",
                                "Force Re-download",
                                "Cancel");

                            if (!force)
                            {
                                SetStatus($"Already at latest commit for {branchOrTag} ({shortHash}).", MessageType.Info);
                                return;
                            }
                        }
                    }

                    StartUpdate(GitRepositoryUrl.Split('#')[0] + "#" + branchOrTag);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Switch to Local Mode"))
            {
                StartUpdate(LocalPackageUrl);
            }

            if (GUILayout.Button("Refresh Info"))
            {
                RefreshInstalledPackageInfo();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Force Clear Lock & Resolve"))
            {
                ForceClearLockAndResolve();
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(10);
        }

        private void DrawStatusMessage()
        {
            if (_isBusy)
            {
                EditorGUILayout.HelpBox("Operation in progress, please wait...", MessageType.Info);
            }
            else if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusMessageType);
            }
        }

        private void StartUpdate(string targetIdentifier)
        {
            _isBusy = true;
            if (targetIdentifier.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            {
                ClearPackageLocks(PackageId);
            }

            SetStatus("Updating package from: " + targetIdentifier, MessageType.Info);
            _addRequest = Client.Add(targetIdentifier);
        }

        private void RefreshInstalledPackageInfo()
        {
            _isBusy = true;
            _installedVersion = "Checking...";
            _installedSource = "Checking...";
            _installedHash = string.Empty;
            _listRequest = Client.List(true);
        }

        private void ProcessListRequest()
        {
            _isBusy = false;

            if (_listRequest.Status == StatusCode.Success)
            {
                var found = false;
                foreach (var package in _listRequest.Result)
                {
                    if (package.name == PackageId)
                    {
                        found = true;
                        _installedVersion = package.version;
                        _installedSource = package.source.ToString();
                        _installedHash = package.git != null ? package.git.hash : string.Empty;
                        SetStatus("Package info updated.", MessageType.Info);
                        break;
                    }
                }

                if (!found)
                {
                    _installedVersion = "Not Installed";
                    _installedSource = "None";
                    SetStatus("Package " + PackageId + " is not installed in this project.", MessageType.Warning);
                }
            }
            else
            {
                var error = _listRequest.Error != null ? _listRequest.Error.message : "Unknown error";
                SetStatus("Failed to query packages: " + error, MessageType.Error);
            }

            _listRequest = null;
            Repaint();
        }

        private void ProcessAddRequest()
        {
            _isBusy = false;

            if (_addRequest.Status == StatusCode.Success)
            {
                SetStatus("Package successfully updated!", MessageType.Info);
                RefreshInstalledPackageInfo();
            }
            else
            {
                var error = _addRequest.Error != null ? _addRequest.Error.message : "Unknown error";
                SetStatus("Failed to update package: " + error, MessageType.Error);
            }

            _addRequest = null;
            Repaint();
        }

        private void ForceClearLockAndResolve()
        {
            _isBusy = true;
            ClearPackageLocks(PackageId);
            SetStatus("Resolving packages...", MessageType.Info);
            Client.Resolve();
            RefreshInstalledPackageInfo();
        }

        private void SetStatus(string message, MessageType messageType)
        {
            _statusMessage = message;
            _statusMessageType = messageType;
            Repaint();
        }

        private string GetCurrentInstalledGitHash()
        {
            if (!string.IsNullOrEmpty(_installedHash))
            {
                return _installedHash;
            }

            return GetCurrentInstalledGitHashStatic();
        }

        private static string GetCurrentInstalledGitHashStatic()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/" + PackageId);
            if (info != null && info.git != null && !string.IsNullOrEmpty(info.git.hash))
            {
                return info.git.hash;
            }

            return string.Empty;
        }

        private static bool TryGetRemoteCommitHash(string repoUrl, string branchOrTag, out string remoteHash)
        {
            remoteHash = string.Empty;
            try
            {
                var target = string.IsNullOrWhiteSpace(branchOrTag) ? "main" : branchOrTag.Trim();
                var cleanRepoUrl = repoUrl;
                var hashIndex = cleanRepoUrl.IndexOf('#');
                if (hashIndex >= 0)
                {
                    cleanRepoUrl = cleanRepoUrl.Substring(0, hashIndex);
                }

                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "git";
                process.StartInfo.Arguments = $"ls-remote \"{cleanRepoUrl}\" refs/heads/{target} refs/tags/{target}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);

                if (!string.IsNullOrWhiteSpace(output))
                {
                    var lines = output.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 0)
                    {
                        var parts = lines[0].Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0 && parts[0].Length >= 7)
                        {
                            remoteHash = parts[0];
                            return true;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[TMNCore] Failed to query remote git hash: {ex.Message}");
            }

            return false;
        }

        private static void ClearPackageLocks(params string[] packageIds)
        {
            try
            {
                var lockFilePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", "packages-lock.json"));
                if (!File.Exists(lockFilePath))
                {
                    return;
                }

                var text = File.ReadAllText(lockFilePath);
                var modified = false;

                foreach (var id in packageIds)
                {
                    var targetKey = $"\"{id}\"";
                    var startIndex = text.IndexOf(targetKey, System.StringComparison.Ordinal);
                    if (startIndex < 0)
                    {
                        continue;
                    }

                    var openBraceIndex = text.IndexOf('{', startIndex);
                    if (openBraceIndex < 0)
                    {
                        continue;
                    }

                    var depth = 1;
                    var endIndex = openBraceIndex + 1;
                    while (endIndex < text.Length && depth > 0)
                    {
                        if (text[endIndex] == '{')
                        {
                            depth++;
                        }
                        else if (text[endIndex] == '}')
                        {
                            depth--;
                        }

                        endIndex++;
                    }

                    if (endIndex < text.Length && text[endIndex] == ',')
                    {
                        endIndex++;
                    }
                    else
                    {
                        var prevIndex = startIndex - 1;
                        while (prevIndex >= 0 && char.IsWhiteSpace(text[prevIndex]))
                        {
                            prevIndex--;
                        }

                        if (prevIndex >= 0 && text[prevIndex] == ',')
                        {
                            startIndex = prevIndex;
                        }
                    }

                    text = text.Remove(startIndex, endIndex - startIndex);
                    modified = true;
                }

                if (modified)
                {
                    File.WriteAllText(lockFilePath, text);
                    AssetDatabase.Refresh();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[TMNCore] Failed to clear lock entries: {ex.Message}");
            }
        }
    }
}
