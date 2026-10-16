using UnityEditor;
using UnityEngine;

namespace AutoBuilder.Editor
{
    public class AutoBuilderWindow : EditorWindow
    {
        private AutoBuilderView _view;
        private Vector2 _scrollPosition;

        public static void OpenWindow()
        {
            var window = GetWindow<AutoBuilderWindow>("Auto Builder");
            window.minSize = new Vector2(480, 560);
            window.Show();
        }

        private void OnEnable()
        {
            _view = new AutoBuilderView();
            _view.Initialize();
        }

        private void OnDisable()
        {
            _view?.SaveSettings();
        }

        private void OnGUI()
        {
            if (_view == null)
            {
                _view = new AutoBuilderView();
                _view.Initialize();
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            _view.DrawGUI();
            EditorGUILayout.EndScrollView();
        }
    }
}