using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerFPSController : MonoBehaviour
{
	[SerializeField] private float walkSpeed = 6f;
	[SerializeField] private float runSpeed = 9f;
	[SerializeField] private float jumpSpeed = 10f;

	public CharacterController Controller { get; private set; }

	private Vector2 input;
	private bool isGrounded;
	private bool jump;
	private float verticalVelocity;
	private bool toChunk;
	private float jumpCooldown;

	// Start is called before the first frame update
	void Start()
	{
		Controller = GetComponent<CharacterController>();
	}
	
	void LateUpdate()
	{
		if (!toChunk)
		{
			if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit))
			{
				// Controller doesn't like it when we set the position directly
				Controller.enabled = false;
				transform.position = hit.point + Vector3.up;
				Controller.enabled = toChunk = true;
			}
		}	
	}

	void Update()
	{
		float hor = Input.GetAxis("Horizontal");
		float ver = Input.GetAxis("Vertical");
		var spd = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
		input.x = hor * spd;
		input.y = ver * spd;
		if (Input.GetButtonDown("Jump")) jump = true;
	}

	void FixedUpdate()
	{
		Vector3 moveAxis = transform.right * input.x + transform.forward * input.y;

		// apply gravity always, to let us track down ramps properly
		
		isGrounded = Controller.isGrounded;
		if (isGrounded)
			verticalVelocity = -1f;

		// NOTE: deltaTime in FixedUpdate is perfectly fine as Unity takes care of that for us
		jumpCooldown -= Time.deltaTime;

		verticalVelocity += Physics.gravity.y * Time.deltaTime;

		// allow jump as long as the player is on the ground
		if (jump && isGrounded)
		{
			// Physics dynamics formula for calculating jump up velocity based on height and gravity
			verticalVelocity += Mathf.Sqrt(jumpSpeed * 2f * -Physics.gravity.y);
			verticalVelocity = Mathf.Min(verticalVelocity, 100f);
			jump = false;
		}
		

		// inject Y velocity before we use it
		moveAxis.y = verticalVelocity;

		// Auto jump
		Ray blockCheck = new Ray(new Vector3(transform.position.x - 0.5f, transform.position.y + 0.01f, transform.position.z + 0.1f), transform.forward);
		if (input.y > 0f && Physics.Raycast(blockCheck, 1.1f) && jumpCooldown <= 0f)
		{
			jumpCooldown = 0.5f;
			jump = true;
		}

		Controller.Move(moveAxis * Time.deltaTime);
	}
}
