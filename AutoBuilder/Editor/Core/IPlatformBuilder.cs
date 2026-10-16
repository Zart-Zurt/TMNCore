using UnityEditor;
using UnityEditor.Build.Reporting;

namespace AutoBuilder.Editor
{
    public interface IPlatformBuilder
    {
        TargetPlatform Platform { get; }
        BuildTarget Target { get; }
        BuildTargetGroup TargetGroup { get; }
        string DisplayName { get; }
        bool IsHostSupported { get; }
        string HostUnsupportedReason { get; }

        void Initialize(AutoBuilderContext context);
        void DrawSettingsGUI();
        void PreBuild(AutoBuilderContext context);
        BuildPlayerOptions ConfigureBuild(AutoBuilderContext context);
        void PostBuild(AutoBuilderContext context, BuildReport report);
        void SaveSettings();
        void LoadSettings();
    }
}
