using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// Attach to the Card_Portrait root
public class CardPortraitHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Main Sprite
    [Header("Portrait")]
    [SerializeField] private Image PortraitImage;
    [SerializeField] private Sprite OffSprite;
    [SerializeField] private Sprite OnSprite;
    
    // NamePlate Sprite
    [Header("Name Plate")]
    [SerializeField] private Image PlateImage;
    
    // Label Text
    [Header("Label")]
    [SerializeField] private TMP_Text Label;
    private Color LabelOffColor = Color.white;
    private Color LabelOnColor = Color.yellow;

    private void Awake()
    {
        ApplyState(false);
    }
    
    // On Hover
    public void OnPointerEnter(PointerEventData eventData) => ApplyState(true);
    public void OnPointerExit(PointerEventData eventData) => ApplyState(false);
    
    private void ApplyState(bool active)
    {
        if (PortraitImage != null)
        {
            PortraitImage.sprite = active ? OnSprite : OffSprite;
        }

        if (Label != null)
        {
            Label.color = active ? LabelOnColor : LabelOffColor;
        }
    }
}