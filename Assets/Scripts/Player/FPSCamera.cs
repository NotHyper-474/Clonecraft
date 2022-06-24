using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCamera : MonoBehaviour
{
	[SerializeField] private float sensitivity = 2f;
	[SerializeField] private float MinPitch = -90f;
	[SerializeField] private float MaxPitch = 90f;
	
	private Vector3 eulerRot;
	
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float yaw = Input.GetAxisRaw("Mouse X") * sensitivity;
		float pitch = -Input.GetAxisRaw("Mouse Y") * sensitivity;
		
		eulerRot.x += pitch;
		eulerRot.x = Mathf.Clamp(eulerRot.x, MinPitch, MaxPitch);
		eulerRot.y += yaw;
		
		transform.localRotation = Quaternion.Euler(new Vector3(eulerRot.x, 0f));
		transform.parent.localRotation = Quaternion.Euler(new Vector3(0f,eulerRot.y));
    }
}
