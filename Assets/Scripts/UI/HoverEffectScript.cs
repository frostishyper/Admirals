using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// -- SCRIPT --
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

    void Update()
    {
        
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
