using UnityEngine;
using UnityEngine.InputSystem;

namespace Clonecraft.Player
{
    public class FPSCamera : MonoBehaviour
    {
#pragma warning disable CS0618 // Convenience
        [SerializeField] private float sensitivity = 2f;
        [SerializeField] private float minPitch = -90f;
        [SerializeField] private float maxPitch = 90f;
        [SerializeField] private bool lockMouse = true;
        [SerializeField] private bool invertY = true;

        private Vector3 _eulerRot;

        private InputAction _lookAction;
        private InputAction _cancelAction;

        private void Start()
        {
            Screen.lockCursor = lockMouse;
            _lookAction = InputSystem.actions.FindAction("Look");
            _cancelAction = InputSystem.actions.FindAction("Cancel");
        }

        private void Update()
        {
            var input = _lookAction.ReadValue<Vector2>();
            var yaw = input.x * sensitivity;
            var pitch = input.y * sensitivity;
            if (invertY) pitch = -pitch;

            _eulerRot.x += pitch;
            _eulerRot.x = Mathf.Clamp(_eulerRot.x, minPitch, maxPitch);
            _eulerRot.y += yaw;

            transform.localRotation = Quaternion.Euler(Vector3.right * _eulerRot.x);
            transform.parent.localRotation = Quaternion.Euler(Vector3.up * _eulerRot.y);

            if (_cancelAction.WasPressedThisFrame())
            {
                lockMouse = !lockMouse;
                Screen.lockCursor = lockMouse;
            }
        }
    }
}
#pragma warning restore CS0618