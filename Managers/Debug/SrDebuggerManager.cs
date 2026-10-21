using System;
using Sirenix.OdinInspector;
using TMNLibrary.Singleton;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Core.Debugging
{
    [AddComponentMenu("Core/Debug/SR Debugger Manager")]
    public class SrDebuggerManager : Singleton<SrDebuggerManager>
    {
        [SerializeField] private string triggerSequence = "debug";
        [SerializeField] private float bufferTimeout = 2.0f;

        private string _inputBuffer = string.Empty;
        private float _lastInputTime;

#if ENABLE_INPUT_SYSTEM
        private bool _isSubscribed;

        private void OnEnable()
        {
            SubscribeInputSystem();
        }

        private void OnDisable()
        {
            UnsubscribeInputSystem();
        }

        private void SubscribeInputSystem()
        {
            if (_isSubscribed)
            {
                return;
            }

            if (Keyboard.current != null)
            {
                Keyboard.current.onTextInput += ProcessInputChar;
                _isSubscribed = true;
            }
        }

        private void UnsubscribeInputSystem()
        {
            if (!_isSubscribed)
            {
                return;
            }

            if (Keyboard.current != null)
            {
                Keyboard.current.onTextInput -= ProcessInputChar;
            }

            _isSubscribed = false;
        }
#endif

        private void Update()
        {
            CheckTimeout();

#if ENABLE_INPUT_SYSTEM
            if (!_isSubscribed && Keyboard.current != null)
            {
                SubscribeInputSystem();
            }
#endif

#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                return;
            }
#endif
            var input = UnityEngine.Input.inputString;
            if (!string.IsNullOrEmpty(input))
            {
                for (var i = 0; i < input.Length; i++)
                {
                    ProcessInputChar(input[i]);
                }
            }
#endif
        }

        private void ProcessInputChar(char character)
        {
            if (char.IsControl(character))
            {
                return;
            }

            var currentTime = Time.unscaledTime;
            if (currentTime - _lastInputTime > bufferTimeout)
            {
                _inputBuffer = string.Empty;
            }

            _lastInputTime = currentTime;
            _inputBuffer += character;

            if (string.IsNullOrEmpty(triggerSequence))
            {
                return;
            }

            if (_inputBuffer.EndsWith(triggerSequence, StringComparison.OrdinalIgnoreCase))
            {
                _inputBuffer = string.Empty;
                ToggleDebugPanel();
            }
            else if (_inputBuffer.Length > triggerSequence.Length * 2)
            {
                _inputBuffer = _inputBuffer.Substring(_inputBuffer.Length - triggerSequence.Length);
            }
        }

        private void CheckTimeout()
        {
            if (_inputBuffer.Length > 0 && Time.unscaledTime - _lastInputTime > bufferTimeout)
            {
                _inputBuffer = string.Empty;
            }
        }

        public void ToggleDebugPanel()
        {
#if !DISABLE_SRDEBUGGER
            if (SRDebug.Instance == null)
            {
                return;
            }

            if (SRDebug.Instance.IsDebugPanelVisible)
            {
                SRDebug.Instance.HideDebugPanel();
            }
            else
            {
                SRDebug.Instance.ShowDebugPanel();
            }
#endif
        }
    }
}
