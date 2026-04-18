using UnityEngine;

/// <summary>
/// Attach to a flat disc/ring prefab placed in the scene.
/// Pulses on show, disappears on hide. Zero dependencies.
/// </summary>
public class NavMarker : MonoBehaviour
{
    [Header("Pulse")]
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseAmount = 0.1f; // ±10% scale oscillation

    private Vector3 _baseScale;

    private void Awake()
    {
        _baseScale = transform.localScale;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        // Gentle breathing pulse so it reads clearly on the floor
        float t = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = _baseScale * t;
    }

    public void ShowAt(Vector3 worldPos)
    {
        transform.position = worldPos + Vector3.up * 0.02f; // just above floor to avoid z-fight
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);
}