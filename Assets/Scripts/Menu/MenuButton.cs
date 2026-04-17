using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI buttonText;
    public Color normalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public Color hoverColor = Color.white;

    private Material instancedMat;
    private bool isHovered = false;

    void Start()
    {
        if (buttonText != null)
        {
            instancedMat = new Material(buttonText.fontSharedMaterial);
            buttonText.fontSharedMaterial = instancedMat;
            buttonText.color = normalColor;
        }
    }

    void Update()
    {
        // Check if THIS button is the one currently selected in the EventSystem
        isHovered = (EventSystem.current.currentSelectedGameObject == this.gameObject);

        buttonText.color = isHovered ? hoverColor : normalColor;

        /* Pulse stuff

        // Apply the Global Pulse to the glow if hovered
        if (isHovered && instancedMat != null)
        {
            // Use the exact same math as the markers
            float pulse = Mathf.Lerp(0.1f, 0.8f, MenuController.GlobalPulseValue);
            instancedMat.SetFloat("_UnderlaySoftness", pulse);
        }
        else if (instancedMat != null)
        {
            instancedMat.SetFloat("_UnderlaySoftness", 0f);
        }
        */
    }

    // Marker stuff NOT IN USE

    public void OnPointerEnter(PointerEventData eventData)
    {
        // THIS is what makes the markers move on HOVER
        EventSystem.current.SetSelectedGameObject(this.gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Optional: Deselect if you want markers to vanish when not hovering anything
        // EventSystem.current.SetSelectedGameObject(null);
    }
}