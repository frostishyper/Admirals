using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// -- SCRIPT --
// Basic Hover Effect(s) Script for Image/Sprite based UI Elements -Frostishyper
// Needs to be attached to the UI Element with an Image Component and a child TextMeshPro Text Component -Frostishyper
// Else it this isnt for you -Frostishyper
public class HoverEffectScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image Img;
    private TMPro.TMP_Text Labeltext;
    private Sprite NeutralImage;
    public Sprite HoverImage;
    private Color HoverColor = Color.yellow;
    private Color DefaultColor = Color.white;

    void Start()
    {
        Img = GetComponent<UnityEngine.UI.Image>();
        NeutralImage = Img.sprite;
        Labeltext = GetComponentInChildren<TMPro.TMP_Text>();
    }


    // When Hoevered Over
    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        Img.sprite = HoverImage;
        Labeltext.color = HoverColor;
    }
    // When Not Hovered Over
    public void OnPointerExit(PointerEventData pointerEventData)
    {
        Img.sprite = NeutralImage;
        Labeltext.color = DefaultColor;
    }
}
