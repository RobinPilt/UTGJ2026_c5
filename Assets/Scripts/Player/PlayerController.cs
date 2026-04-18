using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Feel")]
    [SerializeField] private float moveForce = 18f;  // how hard we push
    [SerializeField] private float maxSpeed = 7f;   // top speed cap
    [SerializeField] private float linearDrag = 9f;   // high = snappy stop
    [SerializeField] private float ballRadius = 0.5f; // used for roll math

    [Header("References")]
    [SerializeField] private Transform ballVisual;  // child mesh — rotates for rolling
    [SerializeField] private Camera orthoCam;    // assign your ortho camera

    private Rigidbody _rb;
    private bool _inputEnabled = true;

    // ── Lifecycle ───────────────────────────────────────────────────
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Freeze rotation on the physics body — we spin the visual manually
        // so the ball rolls smoothly without fighting the physics solver.
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.linearDamping = linearDrag;
        _rb.angularDamping = 0f;
        _rb.interpolation = RigidbodyInterpolation.Interpolate; // smooth camera follow

        if (orthoCam == null) orthoCam = Camera.main;
    }

    private void OnEnable()
    {
        // Auto-subscribe to GameManager events if present
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNavigationBegin.AddListener(EnableInput);
            GameManager.Instance.OnMinigameBegin.AddListener(DisableInput);
            GameManager.Instance.OnDialogueBegin.AddListener(DisableInput);
            GameManager.Instance.OnRoomReloadBegin.AddListener(DisableInput);
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNavigationBegin.RemoveListener(EnableInput);
            GameManager.Instance.OnMinigameBegin.RemoveListener(DisableInput);
            GameManager.Instance.OnDialogueBegin.RemoveListener(DisableInput);
            GameManager.Instance.OnRoomReloadBegin.RemoveListener(DisableInput);
        }
    }

    // ── Physics ─────────────────────────────────────────────────────
    private void FixedUpdate()
    {
        if (!_inputEnabled) return;

        Vector2 raw = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (raw == Vector2.zero) return;

        Vector3 moveDir = GetCameraRelativeDirection(raw);

        // Only add force if we're under max speed in that direction
        float speedAlongDir = Vector3.Dot(_rb.linearVelocity, moveDir);
        if (speedAlongDir < maxSpeed)
            _rb.AddForce(moveDir * moveForce, ForceMode.Force);
    }

    // ── Visuals ─────────────────────────────────────────────────────
    private void Update()
    {
        if (ballVisual == null) return;
        RollBallVisual();
    }

    /// <summary>
    /// Spins the mesh so it looks like the ball is genuinely rolling.
    /// The axis is perpendicular to the velocity (cross product with up),
    /// and speed is derived from arc length = v / circumference * 360°.
    /// </summary>
    private void RollBallVisual()
    {
        Vector3 flatVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        if (flatVelocity.magnitude < 0.05f) return;

        Vector3 rollAxis = Vector3.Cross(Vector3.up, flatVelocity).normalized;
        float rollDeg = (flatVelocity.magnitude / (2f * Mathf.PI * ballRadius)) * 360f;

        ballVisual.Rotate(rollAxis, rollDeg * Time.deltaTime, Space.World);
    }

    // ── Camera-relative input ────────────────────────────────────────
    /// <summary>
    /// Converts raw WASD input into world-space direction relative to the
    /// orthographic camera's orientation. This means "up" always moves the
    /// ball away from the camera regardless of camera angle.
    /// </summary>
    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        Vector3 forward = orthoCam.transform.forward;
        Vector3 right = orthoCam.transform.right;

        // Flatten to horizontal plane so we don't push into/out of the ground
        forward.y = 0f; forward.Normalize();
        right.y = 0f; right.Normalize();

        return (forward * input.y + right * input.x).normalized;
    }

    // ── Public API ───────────────────────────────────────────────────
    public void EnableInput() => SetInput(true);
    public void DisableInput() => SetInput(false);

    private void SetInput(bool on)
    {
        _inputEnabled = on;
        if (!on)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }
}