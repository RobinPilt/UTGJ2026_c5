using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ObjectiveTrigger : MonoBehaviour
{
    [SerializeField] private GameObject visualIndicator;

    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
        SetActive(false); // always start inactive
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State != GameState.Navigation) return;

        GameManager.Instance.TransitionTo(GameState.Minigame);
    }

    public void PlaceAt(Vector3 position)
    {
        transform.position = position;
        SetActive(true);
    }

    public void Deactivate() => SetActive(false);

    private void SetActive(bool on)
    {
        // Lazy init in case RoomManager.Awake fires before our Awake
        if (_collider == null)
            _collider = GetComponent<Collider>();

        if (_collider != null)
            _collider.enabled = on;

        if (visualIndicator != null)
            visualIndicator.SetActive(on);
    }
}