using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerFPSController : MonoBehaviour
{
	[SerializeField] private float walkSpeed = 6f;
	[SerializeField] private float runSpeed = 9f;
	[SerializeField] private float jumpSpeed = 10f;

	public CharacterController Controller { get; private set; }

	private bool jumping;
	private bool jump;
	private float verticalVelocity;

	// Start is called before the first frame update
	void Start()
	{
		Controller = GetComponent<CharacterController>();
	}

	// Update is called once per frame
	void Update()
	{
		float hor = Input.GetAxis("Horizontal");
		float ver = Input.GetAxis("Vertical");
		var spd = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

		Vector3 moveAxis = transform.right * hor + transform.forward * ver;
		moveAxis *= spd;

		Ray blockCheck = new Ray(new Vector3(transform.position.x - 0.5f, transform.position.y + 0.01f, transform.position.z + 0.1f), transform.forward);
		if (ver > 0f && Physics.Raycast(blockCheck, 1.1f))
		{
			jump = true;
		}

		// apply gravity always, to let us track down ramps properly
		verticalVelocity += Physics.gravity.y * Time.deltaTime;

		// allow jump as long as the player is on the ground
		if ((jump || Input.GetButtonDown("Jump")) && !jumping)
		{
			// Physics dynamics formula for calculating jump up velocity based on height and gravity
			verticalVelocity += Mathf.Sqrt(jumpSpeed * 2f * -Physics.gravity.y);
			jump = false;
		}
		else {
			jumping = !Controller.isGrounded;
			if (!jumping)
				verticalVelocity = -2f;
		}
		// inject Y velocity before we use it
		moveAxis.y = verticalVelocity;

		Controller.Move(moveAxis * Time.deltaTime);
	}
}
