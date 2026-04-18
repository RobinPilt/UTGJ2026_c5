using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Feel")]
    [SerializeField] private float maxSpeed = 7f;
    [SerializeField] private float steeringForce = 22f;   // snappiness of direction changes
    [SerializeField] private float linearDrag = 6f;
    [SerializeField] private float slowRadius = 2.5f;  // begin decelerating within this distance
    [SerializeField] private float stopRadius = 0.22f; // snap-stop threshold
    [SerializeField] private float ballRadius = 0.5f;  // used for rolling visual math

    [Header("Click Detection")]
    [SerializeField] private LayerMask groundLayer;  // assign "Ground" layer in Inspector
    [SerializeField] private Camera orthoCam;

    [Header("References")]
    [SerializeField] private Transform ballVisual;   // child mesh that spins visually
    [SerializeField] private NavMarker navMarker;    // scene instance of the marker prefab

    private Rigidbody _rb;
    private Vector3 _targetPos;
    private bool _hasTarget;
    private bool _inputEnabled;

    // ── Lifecycle ───────────────────────────────────────────────────
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.linearDamping = linearDrag;
        _rb.angularDamping = 0f;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (orthoCam == null) orthoCam = Camera.main;

        _inputEnabled = false; // locked until GameManager reaches Navigation state
    }

    // Moved from OnEnable → Start so GameManager.Instance is guaranteed to exist
    private void Start()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnNavigationBegin.AddListener(EnableInput);
        GameManager.Instance.OnMinigameBegin.AddListener(DisableInput);
        GameManager.Instance.OnDialogueBegin.AddListener(DisableInput);
        GameManager.Instance.OnRoomReloadBegin.AddListener(DisableInput);
    }

    private void OnDestroy()
    {
        // OnDestroy is safer than OnDisable for unsubscribing singletons
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnNavigationBegin.RemoveListener(EnableInput);
        GameManager.Instance.OnMinigameBegin.RemoveListener(DisableInput);
        GameManager.Instance.OnDialogueBegin.RemoveListener(DisableInput);
        GameManager.Instance.OnRoomReloadBegin.RemoveListener(DisableInput);
    }

    // ── Per-frame ────────────────────────────────────────────────────
    private void Update()
    {
        if (!_inputEnabled) return;
        HandleClick();
        RollBallVisual();
    }

    private void FixedUpdate()
    {
        if (!_inputEnabled || !_hasTarget) return;
        SeekTarget();
    }

    // ── Click → raycast → set target ────────────────────────────────
    private void HandleClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = orthoCam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayer)) return;

        _targetPos = hit.point;
        _targetPos.y = transform.position.y; // stay on the horizontal plane
        _hasTarget = true;
        navMarker?.ShowAt(_targetPos);
    }

    // ── Arrival steering ─────────────────────────────────────────────
    private void SeekTarget()
    {
        Vector3 toTarget = _targetPos - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        if (dist <= stopRadius)
        {
            _rb.linearVelocity = Vector3.zero;
            _hasTarget = false;
            navMarker?.Hide();
            return;
        }

        // Scale desired speed down linearly inside slowRadius
        float speedScale = Mathf.Clamp01(dist / slowRadius);
        Vector3 desiredVelocity = toTarget.normalized * (maxSpeed * speedScale);

        // Steering force = gap between where we want to be and where we are
        Vector3 flatCurrent = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        Vector3 steering = desiredVelocity - flatCurrent;

        _rb.AddForce(steering * steeringForce, ForceMode.Force);
    }

    // ── Rolling visual ───────────────────────────────────────────────
    private void RollBallVisual()
    {
        if (ballVisual == null) return;

        Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        if (flatVel.magnitude < 0.05f) return;

        Vector3 rollAxis = Vector3.Cross(Vector3.up, flatVel).normalized;
        float rollDeg = (flatVel.magnitude / (2f * Mathf.PI * ballRadius)) * 360f;
        ballVisual.Rotate(rollAxis, rollDeg * Time.deltaTime, Space.World);
    }

    // ── Public API ───────────────────────────────────────────────────
    public void EnableInput() => SetInput(true);
    public void DisableInput() => SetInput(false);

    private void SetInput(bool on)
    {
        _inputEnabled = on;
        _hasTarget = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        if (!on) navMarker?.Hide();
    }
}