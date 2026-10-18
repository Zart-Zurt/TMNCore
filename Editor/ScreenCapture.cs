using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Core.Editor
{
    public class EditorScreenCapture : EditorWindow
    {
        [MenuItem("Tools/Capture Screenshot")]
        private static void Capture()
        {
            const string screenshotPath = "Screenshots/";
            var filename = screenshotPath + "Screenshot_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";

            if (!Directory.Exists(screenshotPath))
            {
                Directory.CreateDirectory(screenshotPath);
            }

            ScreenCapture.CaptureScreenshot(filename);
            Debug.Log("[Editor] Screenshot saved to: " + filename);
            EditorUtility.RevealInFinder(filename);
        }
    }
}