using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCamera : MonoBehaviour
{
#pragma warning disable CS0618 // Convenience
	[SerializeField] private float m_Sensitivity = 2f;
	[SerializeField] private float m_MinPitch = -90f;
	[SerializeField] private float m_MaxPitch = 90f;
	[SerializeField] private bool m_LockMouse = true;
	[SerializeField] private bool m_InvertY = true;
	
	private Vector3 eulerRot;

	private InputAction _lookAction;
	private InputAction _cancelAction;
	
    // Start is called before the first frame update
    void Start()
    {
		Screen.lockCursor = m_LockMouse;
		_lookAction = InputSystem.actions.FindAction("Look");
		_cancelAction = InputSystem.actions.FindAction("Cancel");
	}

    // Update is called once per frame
    void Update()
    {
	    var input = _lookAction.ReadValue<Vector2>();
        float yaw = input.x * m_Sensitivity;
		float pitch = input.y * m_Sensitivity;
		if (m_InvertY) pitch = -pitch;
		
		eulerRot.x += pitch;
		eulerRot.x = Mathf.Clamp(eulerRot.x, m_MinPitch, m_MaxPitch);
		eulerRot.y += yaw;
		
		transform.localRotation = Quaternion.Euler(new Vector3(eulerRot.x, 0f));
		transform.parent.localRotation = Quaternion.Euler(new Vector3(0f,eulerRot.y));

		if (_cancelAction.WasPressedThisFrame())
		{
			m_LockMouse = !m_LockMouse;
			Screen.lockCursor = m_LockMouse;
		}
    }
}
#pragma warning restore CS0618