using UnityEngine;

/// <summary>
/// Attach to empty GameObjects in the scene to mark valid prop positions.
/// No logic here — RoomManager reads the transform.
/// </summary>
public class PropSlot : MonoBehaviour
{
#if UNITY_EDITOR
    // Visible in Scene view so you can place them accurately
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
        Gizmos.DrawSphere(transform.position, 0.25f);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.25f);
    }
#endif
}