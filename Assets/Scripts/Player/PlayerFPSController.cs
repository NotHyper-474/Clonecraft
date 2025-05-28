using Minecraft;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerFPSController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float jumpSpeed = 10f;
    [SerializeField] private float gravityMultiplier = 10f;

    public CharacterController Controller { get; private set; }

    private Vector2 input;
    private bool isGrounded;
    private bool jump;
    private float jumpCooldown;
    private float verticalVelocity;
    private bool toChunk;

    // Start is called before the first frame update
    private void Start()
    {
        Controller = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        if (toChunk) return;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit))
        {
            // Controller doesn't like it when we set the position directly
            Controller.enabled = false;
            transform.position = hit.point + Vector3.up;
            Controller.enabled = toChunk = true;
        }
    }

    private void Update()
    {
        float hor = Input.GetAxis("Horizontal");
        float ver = Input.GetAxis("Vertical");
        var spd = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        input.x = hor * spd;
        input.y = ver * spd;
        if (Input.GetButtonDown("Jump")) jump = true;
    }

    private void FixedUpdate()
    {
        if (!toChunk) return;
        Vector3 moveAxis = transform.right * input.x + transform.forward * input.y;

        // apply gravity always, to let us track down ramps properly

        isGrounded = Controller.isGrounded;
        if (isGrounded)
            verticalVelocity = -1f;

        // NOTE: deltaTime in FixedUpdate is perfectly fine as Unity takes care of that for us
        jumpCooldown -= Time.deltaTime;

        verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;

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
        if (input.y > 0f && jumpCooldown <= 0f)
        {
            Ray blockCheck =
                new Ray(
                    new Vector3(transform.position.x - 0.5f, transform.position.y + 0.01f, transform.position.z + 0.1f),
                    transform.forward);
            var point = TerrainManager.Instance.RaycastTerrainMesh(blockCheck, 0.1f, 1f)?.Point;

            if (point.HasValue)
            {
                var hasBlockOnTop = !TerrainManager.Instance
                    .GetBlockAt(point.Value + 0.5f * TerrainManager.Instance.TerrainConfig.blockSize * Vector3.up).IsEmpty();
                if (!hasBlockOnTop)
                {
                    jumpCooldown = 0.5f;
                    jump = true;
                }
            }
        }

        Controller.Move(moveAxis * Time.deltaTime);
    }
}