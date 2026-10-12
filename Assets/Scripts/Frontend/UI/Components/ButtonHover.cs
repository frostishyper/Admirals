using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Attach To BTN_Action Root GameObject
public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Root Button Image
    [SerializeField] private Image _ButtonImage;

    // Child Label Text
    [SerializeField] private TMP_Text _ButtonText;

    // Default & Hover Sprites
    [SerializeField] private Sprite _DefaultSprite;
    [SerializeField] private Sprite _HoverSprite;

    // Default & Hover Text Colors
    [SerializeField] private Color _DefaultTextColor = Color.white;
    [SerializeField] private Color _HoverTextColor = Color.green;


    // Called By Unity When Pointer Enters This UI Element
    public void OnPointerEnter(PointerEventData eventData)
    {
        _ButtonImage.sprite = _HoverSprite;
        _ButtonText.color = _HoverTextColor;
    }


    // Called By Unity When Pointer Leaves This UI Element
    public void OnPointerExit(PointerEventData eventData)
    {
        _ButtonImage.sprite = _DefaultSprite;
        _ButtonText.color = _DefaultTextColor;
    }
}