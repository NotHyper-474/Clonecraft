using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCamera : MonoBehaviour
{
#pragma warning disable CS0618 // Convenience
	[SerializeField] private float m_Sensitivity = 2f;
	[SerializeField] private float m_MinPitch = -90f;
	[SerializeField] private float m_MaxPitch = 90f;
	[SerializeField] private bool m_LockMouse = true;
	
	private Vector3 eulerRot;
	
    // Start is called before the first frame update
    void Start()
    {
		Screen.lockCursor = m_LockMouse;
	}

    // Update is called once per frame
    void Update()
    {
        float yaw = Input.GetAxisRaw("Mouse X") * m_Sensitivity;
		float pitch = -Input.GetAxisRaw("Mouse Y") * m_Sensitivity;
		
		eulerRot.x += pitch;
		eulerRot.x = Mathf.Clamp(eulerRot.x, m_MinPitch, m_MaxPitch);
		eulerRot.y += yaw;
		
		transform.localRotation = Quaternion.Euler(new Vector3(eulerRot.x, 0f));
		transform.parent.localRotation = Quaternion.Euler(new Vector3(0f,eulerRot.y));

		if (Input.GetButtonDown("Cancel"))
		{
			m_LockMouse = !m_LockMouse;
			Screen.lockCursor = m_LockMouse;
		}
    }
}
#pragma warning restore CS0618