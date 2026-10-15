using TMPro;
using UnityEngine;
using UnityEngine.UI;


[DisallowMultipleComponent]
public sealed class DeploymentUnitCard :
    MonoBehaviour
{
    [Header("Presentation")]
    [SerializeField]
    private Image _UnitPreview;

    [SerializeField]
    private TMP_Text _UnitNameText;

    [SerializeField]
    private TMP_Text _CountText;


    private GameDeployment.Placement _Placement;


    public GameDeployment.Placement Placement =>
        _Placement;


    public string FleetEntryID
    {
        get
        {
            if (_Placement == null)
            {
                return string.Empty;
            }


            return _Placement.ID;
        }
    }


    public void Bind(
        GameDeployment.Placement Placement,
        UnitPresentationCatalog PresentationCatalog
    )
    {
        _Placement =
            Placement;


        if (_Placement == null ||
            _Placement.Unit == null)
        {
            Debug.LogError(
                "Deployment Unit Card received no valid Placement.",
                this
            );


            gameObject.SetActive(
                false
            );


            return;
        }


        if (PresentationCatalog == null)
        {
            Debug.LogError(
                $"Deployment Unit Card '{name}' received no Unit Presentation Catalog.",
                this
            );


            gameObject.SetActive(
                false
            );


            return;
        }


        if (!PresentationCatalog.TryGetPrefab(
                _Placement.Unit,
                out UnitView UnitPrefab))
        {
            Debug.LogError(
                $"No Unit presentation prefab is registered for '{_Placement.Unit.Name}'.",
                this
            );


            gameObject.SetActive(
                false
            );


            return;
        }


        SpriteRenderer SpriteRenderer =
            UnitPrefab.GetComponent<SpriteRenderer>();


        if (SpriteRenderer == null ||
            SpriteRenderer.sprite == null)
        {
            Debug.LogError(
                $"Unit presentation prefab '{UnitPrefab.name}' has no SpriteRenderer base sprite.",
                this
            );


            gameObject.SetActive(
                false
            );


            return;
        }


        if (_UnitPreview != null)
        {
            _UnitPreview.sprite =
                SpriteRenderer.sprite;


            _UnitPreview.preserveAspect =
                true;
        }


        if (_UnitNameText != null)
        {
            _UnitNameText.text =
                _Placement.Unit.Name;
        }


        if (_CountText != null)
        {
            // One Deployment card represents one top-level Fleet Entry.
            _CountText.text =
                "x1";
        }


        gameObject.name =
            $"DeploymentUnitCard_{_Placement.ID}";


        if (transform is RectTransform CardRectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                CardRectTransform
            );
        }
    }
}