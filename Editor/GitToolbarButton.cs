using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
namespace Core.Editor
{
    [InitializeOnLoad]
    public static class GitToolbarButton
    {
        private const double CheckInterval = 60f;
        private static string _buttonText = "Git: ...";
        private static string _tooltip = "Checking git status...";

        private static int _commitsBehind;
        private static bool _isProcessing;
        private static double _lastCheckTime;
        private static string _currentBranch = "main";
        private static string _workingDirectory;
        private static ToolbarButton _toolbarButton;

        static GitToolbarButton()
        {
            EditorApplication.delayCall += Initialize;
            EditorApplication.update += OnUpdate;
        }

        private static void Initialize()
        {
            _workingDirectory = Application.dataPath.Replace("/Assets", "").Replace("\\Assets", "");
            _ = CheckGitStatusAsync();
        }

        private static void OnUpdate()
        {
            EnsureToolbarButton();

            if (!_isProcessing && EditorApplication.timeSinceStartup - _lastCheckTime > CheckInterval)
            {
                _ = CheckGitStatusAsync();
            }
        }

        private static void EnsureToolbarButton()
        {
            if (_toolbarButton != null && _toolbarButton.panel != null)
            {
                return;
            }

            var toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
            if (toolbarType == null)
            {
                return;
            }

            var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            if (toolbars.Length == 0)
            {
                return;
            }

            var toolbar = toolbars[0];
            var root = toolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(toolbar) as VisualElement;

            if (root == null)
            {
                var property = toolbarType.GetProperty("viewVisualTree", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null)
                {
                    root = property.GetValue(toolbar) as VisualElement;
                }
            }

            if (root == null)
            {
                return;
            }

            var container = root.Q("ToolbarZoneRightAlign") ?? root.Q("unity-toolbar-right") ?? root;

            if (_toolbarButton == null)
            {
                _toolbarButton = new ToolbarButton(OnBoxClicked)
                {
                    text = _buttonText,
                    tooltip = _tooltip,
                    style =
                    {
                        paddingLeft = 10,
                        paddingRight = 10
                    }
                };
            }

            if (!container.Contains(_toolbarButton))
            {
                container.Add(_toolbarButton);
            }
        }

        private static void OnBoxClicked()
        {
            if (_isProcessing)
            {
                return;
            }

            if (_commitsBehind > 0)
            {
                _ = PullGitAsync();
            }
            else
            {
                _ = CheckGitStatusAsync();
            }
        }

        private static async Task CheckGitStatusAsync()
        {
            if (_isProcessing)
            {
                return;
            }

            _isProcessing = true;
            _lastCheckTime = EditorApplication.timeSinceStartup;

            UpdateButtonUI("Git: Checking...", "Querying repository status...");

            _currentBranch = await RunGitCommandAsync("rev-parse --abbrev-ref HEAD");
            _currentBranch = _currentBranch?.Trim() ?? "";

            if (string.IsNullOrEmpty(_currentBranch) || _currentBranch.StartsWith("Error"))
            {
                UpdateButtonUI("Git: No Repo", "No git repository found.");
                _isProcessing = false;
                return;
            }

            await RunGitCommandAsync("fetch origin " + _currentBranch);
            var result = await RunGitCommandAsync($"rev-list --right-only --count HEAD...origin/{_currentBranch}");

            if (int.TryParse(result?.Trim() ?? "0", out var count))
            {
                _commitsBehind = count;
            }

            if (_commitsBehind > 0)
            {
                UpdateButtonUI($"Pull ({_commitsBehind})", $"{_commitsBehind} new commits on {_currentBranch}.");
            }
            else
            {
                UpdateButtonUI($"{_currentBranch}", "Repository is up to date.");
            }

            _isProcessing = false;
        }

        private static async Task PullGitAsync()
        {
            _isProcessing = true;
            UpdateButtonUI("Pulling...", "Downloading updates...");
            var result = await RunGitCommandAsync($"pull origin {_currentBranch}");
            Debug.Log("[Git] Pull Result:\n" + result);
            await CheckGitStatusAsync();
        }

        private static void UpdateButtonUI(string text, string tooltip)
        {
            _buttonText = text;
            _tooltip = tooltip;

            if (_toolbarButton != null)
            {
                _toolbarButton.text = _buttonText;
                _toolbarButton.tooltip = _tooltip;
            }
        }

        private static Task<string> RunGitCommandAsync(string arguments)
        {
            var tcs = new TaskCompletionSource<string>();
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = _workingDirectory
            };

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            var output = "";
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    output += e.Data + "\n";
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    output += e.Data + "\n";
                }
            };
            process.Exited += (_, _) =>
            {
                process.WaitForExit();
                tcs.SetResult(output);
                process.Dispose();
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch
            {
                tcs.SetResult("Error");
            }

            return tcs.Task;
        }
    }
}
#endif