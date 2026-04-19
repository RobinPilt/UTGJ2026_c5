using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to a RawImage sitting behind each menu button.
/// Animates the blob shader and reacts to hover state.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class MenuButtonBlobController : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Material")]
    [SerializeField] private Material blobMaterial;

    [Header("Hover")]
    [SerializeField] private float hoverTransitionSpeed = 6f;

    [Header("Link to MenuButton — optional")]
    [Tooltip("If assigned, blob also reacts to keyboard/controller selection.")]
    [SerializeField] private GameObject linkedButton;

    private RawImage _image;
    private Material _instance;
    private float _targetHover = 0f;
    private float _currentHover = 0f;

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void Awake()
    {
        _image = GetComponent<RawImage>();
        _instance = Instantiate(blobMaterial);
        _image.material = _instance;
        _image.color = Color.white; // don't tint — shader handles color
        _image.raycastTarget = false;  // let clicks pass through to button
    }

    private void Update()
    {
        _instance.SetFloat("_Time2", Time.time);

        // Also react to EventSystem selection for keyboard/controller
        if (linkedButton != null)
        {
            bool selected = EventSystem.current.currentSelectedGameObject == linkedButton;
            if (selected) _targetHover = 1f;
        }

        // Smooth hover transition
        _currentHover = Mathf.MoveTowards(
            _currentHover, _targetHover,
            hoverTransitionSpeed * Time.deltaTime
        );
        _instance.SetFloat("_HoverGlow", _currentHover);

        // Reset target each frame — re-set by events
        _targetHover = 0f;
    }

    private void OnDestroy()
    {
        if (_instance != null) Destroy(_instance);
    }

    // ── Hover events ──────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData e) => _targetHover = 1f;
    public void OnPointerExit(PointerEventData e) => _targetHover = 0f;
}