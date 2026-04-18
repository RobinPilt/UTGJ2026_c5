using UnityEngine;

/// <summary>
/// Place on any invisible trigger collider in the scene.
/// When Kratt enters during Navigation, fires the Minigame transition.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ObjectiveTrigger : MonoBehaviour
{
    [Tooltip("Optional: glow or animate this object to hint at the goal.")]
    [SerializeField] private GameObject visualIndicator;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State != GameState.Navigation) return;

        GameManager.Instance.TransitionTo(GameState.Minigame);
    }

    // Called by RoomManager to reposition between rounds
    public void PlaceAt(Vector3 position)
    {
        transform.position = position;
        if (visualIndicator != null)
            visualIndicator.SetActive(true);
    }

    public void Deactivate()
    {
        if (visualIndicator != null)
            visualIndicator.SetActive(false);
    }
}