using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Clonecraft.Player
{
    public class FlyCamera : MonoBehaviour
    {
        [SerializeField] private float WalkSpeed = 6f;
        [SerializeField] private float RunSpeed = 9f;
        [SerializeField] private float Sensitivity = 2f;

        [SerializeField] private float MinVerticalClamp = -90f;
        [SerializeField] private float MaxVerticalClamp = 90f;
        [SerializeField] private bool MouseLocked;

        private Vector3 eulerAngles;

        private void Start()
        {
            eulerAngles = transform.localEulerAngles;
        }

        private void Update()
        {
            var (x, y) = (Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            var z = Input.GetKey(KeyCode.E) ? 1f : Input.GetKey(KeyCode.Q) ? -1f : 0f;
            var speed = (Input.GetKey(KeyCode.LeftShift) ? RunSpeed : WalkSpeed) * Time.deltaTime;

            transform.position += transform.forward * (y * speed) +
                                  transform.right * (x * speed) +
                                  transform.up * (z * speed);

            // Camera
            var h = Input.GetAxisRaw("Mouse X") * Sensitivity;
            var v = -Input.GetAxisRaw("Mouse Y") * Sensitivity;

            eulerAngles += new Vector3(v, h);
            eulerAngles.x = Mathf.Clamp(eulerAngles.x, MinVerticalClamp, MaxVerticalClamp);

            transform.localRotation = Quaternion.Euler(eulerAngles);

            if (Input.GetButtonDown("Cancel"))
            {
                MouseLocked = !MouseLocked;
                SetMouse(MouseLocked);
            }
        }

        public void SetMouse(bool locked)
        {
            Cursor.visible = !locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}