using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AutoBuilder.Editor
{
    public static class ScriptingDefinesManager
    {
        public static bool ApplyCommonDefines(BuildTargetGroup targetGroup, bool enableCheats, bool disableRemoteData, string platformDefine)
        {
            var definesChanged = false;
            var hasSRDebugger = IsSRDebuggerPresent();

            if (hasSRDebugger)
            {
                try
                {
                    var type = FindTypeInAppDomain("SRDebugger.Editor.SRDebugEditor");
                    if (type != null)
                    {
                        var field = type.GetField("IsEnabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (field != null)
                        {
                            var isEnabled = (bool)field.GetValue(null);
                            if (isEnabled != enableCheats)
                            {
                                var method = type.GetMethod("SetEnabled", new[] { typeof(bool) });
                                if (method != null)
                                {
                                    method.Invoke(null, new object[] { enableCheats });
                                    definesChanged = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[AutoBuilder] Failed to toggle SRDebugger state: {exception.Message}");
                }
            }

            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            var defineList = new List<string>(defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            if (enableCheats)
            {
                if (!defineList.Contains("ENABLE_CHEATS"))
                {
                    defineList.Add("ENABLE_CHEATS");
                    definesChanged = true;
                }
            }
            else
            {
                if (defineList.Contains("ENABLE_CHEATS"))
                {
                    defineList.Remove("ENABLE_CHEATS");
                    definesChanged = true;
                }
            }

            var allPlatformDefines = new[] { "STEAM_BUILD", "ANDROID_BUILD", "IOS_BUILD", "WEBGL_BUILD" };
            foreach (var def in allPlatformDefines)
            {
                if (def == platformDefine)
                {
                    if (!defineList.Contains(def))
                    {
                        defineList.Add(def);
                        definesChanged = true;
                    }
                }
                else
                {
                    if (defineList.Contains(def))
                    {
                        defineList.Remove(def);
                        definesChanged = true;
                    }
                }
            }

            if (disableRemoteData)
            {
                if (!defineList.Contains("DISABLE_REMOTE_DATA"))
                {
                    defineList.Add("DISABLE_REMOTE_DATA");
                    definesChanged = true;
                }
            }
            else
            {
                if (defineList.Contains("DISABLE_REMOTE_DATA"))
                {
                    defineList.Remove("DISABLE_REMOTE_DATA");
                    definesChanged = true;
                }
            }

            if (!hasSRDebugger)
            {
                if (!defineList.Contains("DISABLE_SRDEBUGGER"))
                {
                    defineList.Add("DISABLE_SRDEBUGGER");
                    definesChanged = true;
                }
            }

            var newDefines = string.Join(";", defineList);
            if (newDefines != defines)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newDefines);
                AssetDatabase.SaveAssets();
                definesChanged = true;
            }

            return definesChanged;
        }

        private static bool IsSRDebuggerPresent()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType("SRDebug") != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static Type FindTypeInAppDomain(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
