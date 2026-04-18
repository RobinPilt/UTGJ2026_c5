using UnityEngine;
using UnityEngine.EventSystems;

public class LayerClickHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    public ConnectLayers.LayerType layerType;

    public void OnPointerClick(PointerEventData e) =>
        ConnectLayers.Instance?.OnLayerClicked(layerType);

    public void OnPointerEnter(PointerEventData e) =>
        ConnectLayers.Instance?.OnLayerHoverEnter(layerType);
}
