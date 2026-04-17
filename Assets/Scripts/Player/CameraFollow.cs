using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Setup")]
    public Transform target; // Drag your Player here

    [Header("Follow Settings")]
    public float smoothSpeed = 10f; // Higher is snappier, lower is more "floaty"

    private Vector3 offset;

    void Start()
    {
        // Automatically calculate the offset based on where you positioned the camera in the editor
        if (target != null)
        {
            offset = transform.position - target.position;
        }
        else
        {
            Debug.LogWarning("CameraFollow script needs a Target! Please assign your Player in the inspector.");
        }
    }

    // We use LateUpdate for cameras so it moves AFTER the player has finished moving in FixedUpdate/Update.
    // This prevents nasty camera jittering.
    void LateUpdate()
    {
        if (target == null) return;

        // The position the camera wants to be at
        Vector3 desiredPosition = target.position + offset;

        // Smoothly move the camera towards the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}