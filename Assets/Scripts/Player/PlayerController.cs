using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 14f;

    [Header("Jump")]
    public float jumpHeight = 3.2f;
    public int maxJumps = 2;

    [Header("Gravity")]
    public float gravity = -28f;
    public float fallMultiplier = 2.2f;

    [Header("Air Control")]
    // How fast the player can accelerate toward a new direction while airborne.
    // Higher = snappier steering; lower = floatier. Does not cap total air speed,
    // so a dash-jump carries full dash momentum and strafing gradually redirects it.
    public float airAcceleration = 40f;

    [Header("Dash")]
    public float dashSpeed = 22f;
    public float dashDuration = 0.30f;
    public float dashCooldown = 1f;

    [Header("Crouch / Slide")]
    public float crouchHeight = 1f;
    public float crouchSpeed = 7f;
    public float slideDrag = 14f;
    public float slideMinSpeed = 2f;
    public float slideSteerStrength = 8f;
    public float cameraLerpSpeed = 12f;

    [Header("B-hop")]
    public float bhopWindow = 0.01f;

    [Header("Grapple")]
    public KeyCode grappleKey = KeyCode.E;
    public float grappleMaxDistance = 30f;
    public LayerMask grappleLayer;
    public float grapplePullSpeed = 20f;
    public float grappleArrivalDistance = 2f;
    public float grappleExitSpeed = 14f;
    public float grappleCooldown = 0.5f;
    public Material grappleLineMaterial;
    public float grappleLineRightOffset = 0.35f;
    public float grappleLineDownOffset = 0.2f;

    private CharacterController _cc;
    private Vector3 _velocity;          // vertical only
    private Vector3 _horizontalVelocity; // horizontal only — persists through air
    private int _jumpsRemaining;
    private float _standHeight;

    private Transform _cameraHolder;
    private float _standCameraY;
    private float _targetCameraY;

    private Vector3 _dashDirection;
    private float _dashTimer;
    private float _dashCooldownTimer;

    private enum MoveState { Standing, Crouching, Sliding }
    private MoveState _moveState = MoveState.Standing;
    private Vector3 _slideVelocity;

    private bool _isGrappling;
    private Vector3 _grappleAnchor;
    private LineRenderer _grappleLine;
    private float _grappleCooldownTimer;
    private Camera _camera;

    private bool    _wasGrounded;
    private float   _bhopTimer;
    private Vector3 _bhopVelocity;

    public bool IsDashing => _dashTimer > 0f;
    public Vector3 DashDirection => _dashDirection;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _standHeight = _cc.height;

        _cameraHolder = transform.Find("CameraHolder");
        _standCameraY = _cameraHolder.localPosition.y;
        _targetCameraY = _standCameraY;
        _camera = Camera.main;

        _grappleLine = gameObject.AddComponent<LineRenderer>();
        _grappleLine.positionCount = 2;
        _grappleLine.startWidth = 0.05f;
        _grappleLine.endWidth = 0.02f;
        _grappleLine.useWorldSpace = true;
        if (grappleLineMaterial != null)
            _grappleLine.material = grappleLineMaterial;
        else
        {
            _grappleLine.material = new Material(Shader.Find("Sprites/Default"));
            _grappleLine.startColor = new Color(0.35f, 0.35f, 0.35f);
            _grappleLine.endColor   = new Color(0.35f, 0.35f, 0.35f);
        }
        _grappleLine.enabled = false;
    }

    void Update()
    {
        bool grounded = _cc.isGrounded;

        if (grounded && _velocity.y < 0f)
        {
            _velocity.y = -2f;
            _jumpsRemaining = maxJumps;
        }

        if (grounded && !_wasGrounded)
        {
            _bhopTimer    = bhopWindow;
            _bhopVelocity = _horizontalVelocity;
        }
        _bhopTimer = Mathf.Max(0f, _bhopTimer - Time.deltaTime);

        if (!grounded && _wasGrounded)
            _jumpsRemaining = Mathf.Min(_jumpsRemaining, maxJumps - 1);
        _wasGrounded = grounded;

        HandleGrapple();
        HandleDash();
        HandleCrouchSlide(grounded);
        HandleHorizontalMovement(grounded);
        HandleJump();
        HandleGravity();
        UpdateCameraHeight();
    }

    void HandleDash()
    {
        bool wasDashing = IsDashing;
        _dashTimer = Mathf.Max(0f, _dashTimer - Time.deltaTime);
        _dashCooldownTimer = Mathf.Max(0f, _dashCooldownTimer - Time.deltaTime);

        // Dash expired while airborne — inject momentum into the horizontal velocity
        // so the air-accel system carries it forward naturally
        if (wasDashing && !IsDashing && !_cc.isGrounded)
            _horizontalVelocity = _dashDirection * dashSpeed;

        if (Input.GetKeyDown(KeyCode.LeftShift) && _dashCooldownTimer == 0f && !IsDashing)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            Vector3 dir = transform.right * x + transform.forward * z;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            else dir.Normalize();

            _dashDirection = dir;
            _dashTimer = dashDuration;
            _dashCooldownTimer = dashCooldown;
        }
    }

    void HandleGrapple()
    {
        _grappleCooldownTimer = Mathf.Max(0f, _grappleCooldownTimer - Time.deltaTime);

        if (Input.GetKeyDown(grappleKey) && !_isGrappling && _grappleCooldownTimer == 0f)
        {
            Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, grappleMaxDistance, grappleLayer))
            {
                _grappleAnchor = hit.point;
                _isGrappling = true;
                _dashTimer = 0f;
                _slideVelocity = Vector3.zero;
                _grappleLine.enabled = true;
            }
        }

        if (Input.GetKeyUp(grappleKey) && _isGrappling)
            DetachGrapple();

        if (_isGrappling)
        {
            if ((_grappleAnchor - transform.position).magnitude <= grappleArrivalDistance)
            {
                DetachGrapple();
                return;
            }
            Vector3 ropeStart = _cameraHolder.position
                + _camera.transform.right * grappleLineRightOffset
                - _camera.transform.up * grappleLineDownOffset;
            _grappleLine.SetPosition(0, ropeStart);
            _grappleLine.SetPosition(1, _grappleAnchor);
        }
    }

    void DetachGrapple()
    {
        Vector3 dir = (_grappleAnchor - transform.position).normalized;
        _horizontalVelocity = new Vector3(dir.x, 0f, dir.z) * grappleExitSpeed;
        _velocity.y = dir.y * grappleExitSpeed;
        _isGrappling = false;
        _grappleLine.enabled = false;
        _grappleCooldownTimer = grappleCooldown;
    }

    void HandleCrouchSlide(bool grounded)
    {
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl);

        switch (_moveState)
        {
            case MoveState.Standing:
                if (ctrlHeld && grounded)
                {
                    if (IsDashing)
                    {
                        _slideVelocity = _dashDirection * dashSpeed;
                        _dashTimer = 0f;
                        EnterCrouched();
                        _moveState = MoveState.Sliding;
                    }
                    else if (_horizontalVelocity.magnitude > slideMinSpeed)
                    {
                        // Landing with momentum (e.g. dash-jump) — carry it into the slide
                        _slideVelocity = _horizontalVelocity;
                        EnterCrouched();
                        _moveState = MoveState.Sliding;
                    }
                    else
                    {
                        float x = Input.GetAxisRaw("Horizontal");
                        float z = Input.GetAxisRaw("Vertical");
                        bool hasInput = x != 0f || z != 0f;
                        EnterCrouched();

                        if (hasInput)
                        {
                            Vector3 dir = (transform.right * x + transform.forward * z).normalized;
                            _slideVelocity = dir * moveSpeed;
                            _moveState = MoveState.Sliding;
                        }
                        else
                        {
                            _moveState = MoveState.Crouching;
                        }
                    }
                }
                break;

            case MoveState.Crouching:
                if (!ctrlHeld && CanStand())
                {
                    ExitCrouched();
                    _moveState = MoveState.Standing;
                }
                break;

            case MoveState.Sliding:
                float speed = _slideVelocity.magnitude - slideDrag * Time.deltaTime;

                if (speed <= slideMinSpeed || (!ctrlHeld && CanStand()))
                {
                    _slideVelocity = Vector3.zero;

                    if (ctrlHeld || !CanStand())
                        _moveState = MoveState.Crouching;
                    else
                    {
                        ExitCrouched();
                        _moveState = MoveState.Standing;
                    }
                }
                else
                {
                    _slideVelocity = _slideVelocity.normalized * speed;

                    float steerInput = Input.GetAxisRaw("Horizontal");
                    if (steerInput != 0f)
                    {
                        _slideVelocity += transform.right * (steerInput * slideSteerStrength * Time.deltaTime);
                        _slideVelocity = _slideVelocity.normalized * speed;
                    }
                }
                break;
        }
    }

    void HandleHorizontalMovement(bool grounded)
    {
        if (_isGrappling)
        {
            Vector3 dir = (_grappleAnchor - transform.position).normalized;
            _cc.Move(dir * grapplePullSpeed * Time.deltaTime);
            return;
        }

        if (_moveState == MoveState.Sliding)
        {
            _cc.Move(_slideVelocity * Time.deltaTime);
            return;
        }

        if (IsDashing)
        {
            _horizontalVelocity = _dashDirection * dashSpeed;
            _cc.Move(_horizontalVelocity * Time.deltaTime);
            return;
        }

        float ix = Input.GetAxisRaw("Horizontal");
        float iz = Input.GetAxisRaw("Vertical");
        Vector3 wishDir = transform.right * ix + transform.forward * iz;
        if (wishDir.magnitude > 1f) wishDir.Normalize();

        if (grounded)
        {
            // Snappy ground movement — instant direction change
            float moveSpd = _moveState == MoveState.Crouching ? crouchSpeed : moveSpeed;
            _horizontalVelocity = wishDir * moveSpd;
        }
        else
        {
            // Speed cap is whatever we're already going (preserves dash momentum)
            // or moveSpeed if we're slower — strafing can never push total speed higher.
            float speedCap = Mathf.Max(moveSpeed, _horizontalVelocity.magnitude);

            float currentSpeedAlongWish = Vector3.Dot(_horizontalVelocity, wishDir);
            float addSpeed = Mathf.Clamp(moveSpeed - currentSpeedAlongWish, 0f, airAcceleration * Time.deltaTime);
            _horizontalVelocity += wishDir * addSpeed;

            if (_horizontalVelocity.magnitude > speedCap)
                _horizontalVelocity = _horizontalVelocity.normalized * speedCap;
        }

        _cc.Move(_horizontalVelocity * Time.deltaTime);
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && _jumpsRemaining > 0)
        {
            if (_isGrappling)
                DetachGrapple();

            if (_moveState != MoveState.Standing && CanStand())
            {
                ExitCrouched();
                _moveState = MoveState.Standing;
            }

            // Jumping during a dash injects full dash momentum into horizontal velocity;
            // the air-accel system then carries it and allows gradual steering from there
            if (IsDashing)
            {
                _horizontalVelocity = _dashDirection * dashSpeed;
                _dashTimer = 0f;
            }

            if (_bhopTimer > 0f && _bhopVelocity.magnitude > moveSpeed)
                _horizontalVelocity = _bhopVelocity;
            _bhopTimer = 0f;
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _jumpsRemaining--;
        }
    }

    void HandleGravity()
    {
        if (_isGrappling)
        {
            _velocity.y = 0f;
            return;
        }
        float appliedGravity = (_velocity.y < 0f) ? gravity * fallMultiplier : gravity;
        _velocity.y += appliedGravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    void UpdateCameraHeight()
    {
        Vector3 pos = _cameraHolder.localPosition;
        pos.y = Mathf.Lerp(pos.y, _targetCameraY, cameraLerpSpeed * Time.deltaTime);
        _cameraHolder.localPosition = pos;
    }

    void EnterCrouched()
    {
        _cc.height = crouchHeight;
        _cc.center = Vector3.up * (crouchHeight / 2f);

        float eyeOffsetFromTop = _standHeight - _standCameraY;
        _targetCameraY = crouchHeight - eyeOffsetFromTop;
    }

    void ExitCrouched()
    {
        _cc.height = _standHeight;
        _cc.center = Vector3.up * (_standHeight / 2f);
        _targetCameraY = _standCameraY;
    }

    bool CanStand()
    {
        Vector3 origin = transform.position + Vector3.up * (crouchHeight / 2f);
        return !Physics.SphereCast(origin, _cc.radius * 0.9f, Vector3.up, out _, _standHeight - crouchHeight);
    }
}
