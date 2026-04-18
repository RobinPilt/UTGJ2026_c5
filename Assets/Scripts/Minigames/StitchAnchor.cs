using UnityEngine;
using UnityEngine.EventSystems;

public class StitchAnchor : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public void OnPointerDown(PointerEventData e) =>
        ConnectLayers.Instance.OnStitchPointerDown(GetComponent<RectTransform>());

    public void OnPointerUp(PointerEventData e) =>
        ConnectLayers.Instance.OnStitchPointerUp(e.position);

    public void OnDrag(PointerEventData e) =>
        ConnectLayers.Instance.OnDrag(e.position);
}
