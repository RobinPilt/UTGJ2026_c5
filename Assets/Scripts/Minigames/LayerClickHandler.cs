using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to each layer Image (Sky, Earth, Underworld).
/// Forwards clicks to ConnectLayers during the selection phase.
/// </summary>
public class LayerClickHandler : MonoBehaviour, IPointerClickHandler
{
    public ConnectLayers.LayerType layerType;

    public void OnPointerClick(PointerEventData e)
    {
        ConnectLayers.Instance?.OnLayerClicked(layerType);
    }
}