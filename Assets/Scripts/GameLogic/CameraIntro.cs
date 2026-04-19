using System.Collections;
using UnityEngine;

public class CameraIntro : MonoBehaviour
{
    private Camera cam;

    [Header("Zoom Settings")]
    public float zoomedInSize = 2f;    // Close to the Kratt
    public float targetSize = 5.4f;   // Your normal play size
    public float duration = 2.5f;     // How long the zoom takes

    [Header("Focus Target")]
    public Transform playerTransform; // Drag your Kratt here

    void Start()
    {
        cam = GetComponent<Camera>();

        // Start zoomed in and centered on player
        cam.orthographicSize = zoomedInSize;

        // If your camera follows the player, this just handles the zoom.
        // If not, we can lerp the position too.
        StartCoroutine(ExecuteIntro());
    }

    IEnumerator ExecuteIntro()
    {
        float elapsed = 0;

        // Use a nice "Ease Out" curve so it starts fast and slows down
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Smoothly move from zoomedIn to targetSize
            // (1 - (1 - t) * (1 - t)) is a simple Ease-Out formula
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            cam.orthographicSize = Mathf.Lerp(zoomedInSize, targetSize, easeT);

            yield return null;
        }

        cam.orthographicSize = targetSize;
    }
}