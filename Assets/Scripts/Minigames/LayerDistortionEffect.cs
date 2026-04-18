using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Warps a UI Image's vertices at runtime to create a stretching/pulling effect.
/// Attach to SkyLayer, EarthLayer, UnderworldLayer Images.
/// Call SetDistortion() to animate, Reset() to snap back.
/// </summary>
[RequireComponent(typeof(Image))]
public class LayerDistortionEffect : BaseMeshEffect
{
    // ── Public state set by ConnectLayers ─────────────────────────────
    [HideInInspector] public float distortionAmount = 0f; // 0 = none, 1 = full
    [HideInInspector] public bool pullDown = false; // which edge to warp
    [HideInInspector] public bool pullUp = false;

    [Header("Warp Settings")]
    [SerializeField] private float maxStretch = 60f;  // max pixel stretch at seam edge
    [SerializeField] private float wobbleAmount = 12f;  // lateral wobble magnitude
    [SerializeField] private float wobbleSpeed = 4f;   // wobble frequency

    private float _wobbleTime;

    private void Update()
    {
        if (distortionAmount > 0.001f)
        {
            _wobbleTime = Mathf.Repeat(_wobbleTime + Time.deltaTime * wobbleSpeed, Mathf.PI * 2f);
            graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || distortionAmount < 0.001f) return;

        int count = vh.currentVertCount;
        if (count == 0) return;

        // UI Image quads have 4 verts: 0=BL, 1=TL, 2=TR, 3=BR
        // (May be sliced — iterate all and warp by normalized Y position)
        UIVertex vert = new UIVertex();

        Rect r = graphic.rectTransform.rect;

        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vert, i);

            // Normalized position within the rect (0=bottom, 1=top)
            float normY = Mathf.InverseLerp(r.yMin, r.yMax, vert.position.y);

            float stretch = 0f;
            float wobble = Mathf.Sin(_wobbleTime + vert.position.x * 0.03f) * wobbleAmount
                            * distortionAmount;

            if (pullDown)
            {
                // Bottom edge pulls downward — stretch increases toward bottom
                stretch = -maxStretch * (1f - normY) * distortionAmount;
            }

            if (pullUp)
            {
                // Top edge pulls upward — stretch increases toward top
                stretch = maxStretch * normY * distortionAmount;
            }

            vert.position.y += stretch;
            vert.position.x += wobble * (pullDown ? (1f - normY) : normY);

            vh.SetUIVertex(vert, i);
        }
    }

    public void Reset()
    {
        distortionAmount = 0f;
        pullDown = false;
        pullUp = false;
        graphic.SetVerticesDirty();
    }
}