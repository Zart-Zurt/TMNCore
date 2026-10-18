using System;
using System.IO;
using Core.Events;
using Sirenix.OdinInspector;
using TMNLibrary.Singleton;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Inputs
{
    [DefaultExecutionOrder(-90)]
    [AddComponentMenu("Core/Input/Input Manager")]
    public class InputManager : Singleton<InputManager>
    {
        [SerializeField] private InputActionAsset inputActions;

        private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;

        public InputAction MoveAction { get; private set; }
        public InputAction LookAction { get; private set; }
        public InputAction JumpAction { get; private set; }
        public InputAction SprintAction { get; private set; }
        public InputAction InteractAction { get; private set; }
        public InputAction CarryAction { get; private set; }
        public InputAction PauseAction { get; private set; }
        public string CurrentDeviceType { get; private set; } = "KeyboardMouse";

        protected override void Awake()
        {
            base.Awake();

            if (inputActions == null)
            {
                Debug.LogError("InputManager: InputActionAsset is not assigned.");
                return;
            }

            LoadBindingOverridesFromFile();

            var playerMap = inputActions.FindActionMap("Player");
            if (playerMap != null)
            {
                MoveAction = playerMap.FindAction("Move");
                LookAction = playerMap.FindAction("Look");
                JumpAction = playerMap.FindAction("Jump");
                SprintAction = playerMap.FindAction("Sprint");
                InteractAction = playerMap.FindAction("Interact");
                CarryAction = playerMap.FindAction("Carry");
                PauseAction = playerMap.FindAction("Pause");

                playerMap.Enable();
            }
            else
            {
                Debug.LogError("InputManager: 'Player' action map not found in InputActionAsset.");
            }

            InputSystem.onActionChange += OnActionChange;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            InputSystem.onActionChange -= OnActionChange;

            if (_rebindingOperation != null)
            {
                _rebindingOperation.Dispose();
                _rebindingOperation = null;
            }
        }

        private void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed)
            {
                return;
            }

            if (obj is InputAction action && action.activeControl != null)
            {
                UpdateDeviceType(action.activeControl.device);
            }
        }

        private void UpdateDeviceType(InputDevice device)
        {
            if (device == null)
            {
                return;
            }

            var newDeviceType = device switch
            {
                Gamepad => "Gamepad",
                Keyboard or Mouse => "KeyboardMouse",
                _ => CurrentDeviceType
            };

            if (newDeviceType != CurrentDeviceType)
            {
                CurrentDeviceType = newDeviceType;
                EventBus.Raise(new InputDeviceChangedEvent
                {
                    DeviceType = CurrentDeviceType
                });
            }
        }

        public void StartRebind(string actionName, int bindingIndex = 0, Action onComplete = null, Action onCancel = null)
        {
            if (inputActions == null)
            {
                return;
            }

            var action = inputActions.FindAction(actionName);
            if (action == null)
            {
                Debug.LogWarning($"InputManager: Action '{actionName}' not found for rebinding.");
                return;
            }

            _rebindingOperation?.Cancel();
            _rebindingOperation?.Dispose();

            action.Disable();

            _rebindingOperation = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(op =>
                {
                    op.Dispose();
                    _rebindingOperation = null;
                    action.Enable();
                    SaveBindingOverridesToFile();
                    EventBus.Raise(new InputBindingChangedEvent
                    {
                        ActionName = actionName,
                        DisplayString = action.GetBindingDisplayString(bindingIndex)
                    });
                    onComplete?.Invoke();
                })
                .OnCancel(op =>
                {
                    op.Dispose();
                    _rebindingOperation = null;
                    action.Enable();
                    onCancel?.Invoke();
                });

            _rebindingOperation.Start();
        }

        public void CancelRebind()
        {
            _rebindingOperation?.Cancel();
        }

        public void ResetAllBindings()
        {
            if (inputActions == null)
            {
                return;
            }

            inputActions.RemoveAllBindingOverrides();
            SaveBindingOverridesToFile();
        }

        public string GetBindingDisplayString(string actionName, int bindingIndex = 0)
        {
            if (inputActions == null)
            {
                return string.Empty;
            }

            var action = inputActions.FindAction(actionName);
            return action != null ? action.GetBindingDisplayString(bindingIndex) : string.Empty;
        }

        public void SaveBindingOverridesToFile()
        {
            if (inputActions == null)
            {
                return;
            }

            var json = inputActions.SaveBindingOverridesAsJson();
            var path = Path.Combine(Application.persistentDataPath, "input_bindings.json");
            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }

        public void LoadBindingOverridesFromFile()
        {
            if (inputActions == null)
            {
                return;
            }

            var path = Path.Combine(Application.persistentDataPath, "input_bindings.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                inputActions.LoadBindingOverridesFromJson(json);
            }
        }
    }
}
