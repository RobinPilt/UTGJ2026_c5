using UnityEngine;
using TMPro;

public class GlowingTextPulse : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    [Header("Settings")]
    public float minGlow = 0.4f;
    public float maxGlow = 1.0f;
    public float speed = 2.0f;

    private Material instancedMaterial;

    void Start()
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshProUGUI>();
        // Create an instance of the material so we don't change all text in the game
        instancedMaterial = new Material(textMesh.fontSharedMaterial);
        textMesh.fontSharedMaterial = instancedMaterial;
    }

    void Update()
    {
        // PingPong creates a 0 to 1 value that bounces back and forth
        float t = Mathf.PingPong(Time.time * speed, 1.0f);
        // Lerp between min and max softness/brightness
        float currentGlow = Mathf.Lerp(minGlow, maxGlow, t);
        // We modify the "Softness" of the Underlay to make it pulse
        instancedMaterial.SetFloat("_UnderlaySoftness", currentGlow);
    }
}