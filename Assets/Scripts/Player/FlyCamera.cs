using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    [SerializeField]
    private float m_WalkSpeed = 6f;
    [SerializeField]
    private float m_RunSpeed = 9f;
	[SerializeField]
	private float m_Sensitivity = 2f;

	[SerializeField]
	private float m_MinVerticalClamp = -90f;
	[SerializeField]
	private float m_MaxVerticalClamp = 90f;
	[SerializeField]
	private bool m_MouseLocked;

	private Vector3 eulerAngles;

	private void Start()
	{
		eulerAngles = transform.localEulerAngles;
	}

	private void Update()
	{
		float x, y, z;
		x = Input.GetAxis("Horizontal");
		y = Input.GetAxis("Vertical");
		z = Input.GetKey(KeyCode.E) ? 1 : Input.GetKey(KeyCode.Q) ? -1 : 0;
		float speed = Input.GetKey(KeyCode.LeftShift) ? m_RunSpeed : m_WalkSpeed;

		
		transform.position += transform.forward * (y * speed) * Time.deltaTime +
							  transform.right * (x * speed) * Time.deltaTime +
							  transform.up * (z * speed) * Time.deltaTime;

		// Camera
		float h = Input.GetAxisRaw("Mouse X") * m_Sensitivity;
		float v = -Input.GetAxisRaw("Mouse Y") * m_Sensitivity;

		eulerAngles += new Vector3(v, h);
		eulerAngles.x = Mathf.Clamp(eulerAngles.x, m_MinVerticalClamp, m_MaxVerticalClamp);

		transform.localRotation = Quaternion.Euler(eulerAngles);

		if(Input.GetButtonDown("Cancel"))
		{
			m_MouseLocked = !m_MouseLocked;
			SetMouse(m_MouseLocked);
		}
	}

	public void SetMouse(bool locked)
	{
		Cursor.visible = !locked;
		Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
	}
}
