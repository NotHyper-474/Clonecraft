using UnityEngine;
using UnityEngine.InputSystem;

namespace Clonecraft.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerFPSController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float runSpeed = 9f;
        [SerializeField] private float jumpSpeed = 10f;
        [SerializeField] private float gravityMultiplier = 1f;
        [SerializeField] private float terminalVelocity = 50f;
        [SerializeField] private float jumpGraceTime = 0.1f;

        public CharacterController Controller { get; private set; }

        private Vector2 _input;
        private bool _jumpRequested;
        private float _jumpCooldown;
        private float _jumpGraceTimer;
        private Vector3 _gravitalVelocity;
        private bool _wasGrounded;
        private bool _toChunk;

        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;

        private void Start()
        {
            Controller = GetComponent<CharacterController>();
            _moveAction = InputSystem.actions.FindAction("Move");
            _sprintAction = InputSystem.actions.FindAction("Sprint");
            _jumpAction = InputSystem.actions.FindAction("Jump");
        }

        private void LateUpdate()
        {
            if (_toChunk) return;
            if (Physics.Raycast(transform.position, Vector3.down, out var hit))
            {
                Controller.enabled = false;
                transform.position = hit.point + Vector3.up;
                Controller.enabled = _toChunk = true;
            }
        }

        private void Update()
        {
            var spd = _sprintAction.IsPressed() ? runSpeed : walkSpeed;
            _input = _moveAction.ReadValue<Vector2>() * spd;
            if (_jumpAction.WasPressedThisFrame()) _jumpRequested = true;
        }

        private void FixedUpdate()
        {
            if (!_toChunk) return;
            var moveAxis = transform.right * _input.x + transform.forward * _input.y;

            // allow jump as long as the player is on the ground
            if (Controller.isGrounded)
            {
                // Keep adding a tiny force to force the controller to slide down slopes
                _gravitalVelocity = Physics.gravity.normalized;

                if (!_wasGrounded)
                {
                    _jumpGraceTimer = jumpGraceTime;
                }

                // Auto jump
                if (!_jumpRequested && _input.y > 0f && _jumpCooldown <= 0f)
                {
                    Ray blockCheck =
                        new Ray(
                            new Vector3(transform.position.x, transform.position.y + 0.01f,
                                transform.position.z + 0.1f),
                            transform.forward);
                    var point = TerrainManager.RaycastTerrainMesh(blockCheck, 0.1f, 1f)?.Point;

                    if (point.HasValue)
                    {
                        var hasBlockOnTop = !TerrainManager.Instance
                            .GetBlockAt(point.Value +
                                        0.5f * TerrainManager.Instance.TerrainConfig.blockSize * Vector3.up)
                            .IsEmpty();
                        if (!hasBlockOnTop)
                        {
                            _jumpCooldown = 0.5f;
                            _jumpRequested = true;
                        }
                    }
                }
            }
            else
            {
                _gravitalVelocity += Physics.gravity * (gravityMultiplier * Time.fixedDeltaTime);
                _gravitalVelocity = Vector3.Max(Vector3.one * -terminalVelocity, _gravitalVelocity);

                _jumpGraceTimer -= Time.deltaTime;
                if (_jumpRequested && _jumpGraceTimer <= 0f)
                {
                    _jumpRequested = false;
                }
            }

            if (_jumpRequested && (_wasGrounded || _jumpGraceTimer > 0f))
            {
                var jumpForce = transform.up * Mathf.Sqrt(jumpSpeed * 2f * -Physics.gravity.y);
                _gravitalVelocity = jumpForce;
                _jumpRequested = false;
                _jumpGraceTimer = 0f;
            }

            _wasGrounded = Controller.isGrounded;

            if (_jumpCooldown > 0f)
            {
                // NOTE: deltaTime in FixedUpdate is perfectly fine as Unity takes care of that for us
                _jumpCooldown -= Time.deltaTime;
            }

            moveAxis += _gravitalVelocity;
            _jumpRequested = false;

            Controller.Move(moveAxis * Time.fixedDeltaTime);
        }
    }
}