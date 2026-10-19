using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[DisallowMultipleComponent]
public sealed class DeploymentUnitCard :
    MonoBehaviour,
    IInitializePotentialDragHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IScrollHandler
{
    private enum PointerDragMode
    {
        None = 0,
        Pending = 1,
        TrayScroll = 2,
        Deployment = 3
    }


    [Header("Presentation")]
    [SerializeField]
    private Image _UnitPreview;

    [SerializeField]
    private TMP_Text _UnitNameText;

    [SerializeField]
    private TMP_Text _CountText;


    private GameDeployment.PlacementView _Placement;

    private DeploymentTrayUI _DragOwner;

    private ScrollRect _ParentScrollRect;


    private PointerDragMode _PointerDragMode =
        PointerDragMode.None;


    public GameDeployment.PlacementView Placement =>
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
        GameDeployment.PlacementView Placement,
        UnitPresentationCatalog PresentationCatalog,
        DeploymentTrayUI DragOwner
    )
    {
        _Placement =
            Placement;

        _DragOwner =
            DragOwner;

        _ParentScrollRect =
            GetComponentInParent<ScrollRect>();


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


    public void OnInitializePotentialDrag(
        PointerEventData EventData
    )
    {
        _PointerDragMode =
            PointerDragMode.None;


        if (_ParentScrollRect != null &&
            EventData != null)
        {
            _ParentScrollRect.OnInitializePotentialDrag(
                EventData
            );
        }
    }


    public void OnBeginDrag(
        PointerEventData EventData
    )
    {
        if (EventData == null)
        {
            return;
        }


        _PointerDragMode =
            PointerDragMode.Pending;


        ResolvePendingDragMode(
            EventData
        );
    }


    public void OnDrag(
        PointerEventData EventData
    )
    {
        if (EventData == null)
        {
            return;
        }


        if (_PointerDragMode ==
            PointerDragMode.Pending)
        {
            ResolvePendingDragMode(
                EventData
            );
        }


        switch (_PointerDragMode)
        {
            case PointerDragMode.TrayScroll:

                if (_ParentScrollRect != null)
                {
                    _ParentScrollRect.OnDrag(
                        EventData
                    );
                }

                break;


            case PointerDragMode.Deployment:

                if (_DragOwner != null)
                {
                    _DragOwner.UpdateCardDrag(
                        this,
                        EventData
                    );
                }

                break;
        }
    }


    public void OnEndDrag(
        PointerEventData EventData
    )
    {
        if (EventData == null)
        {
            _PointerDragMode =
                PointerDragMode.None;

            return;
        }


        switch (_PointerDragMode)
        {
            case PointerDragMode.TrayScroll:

                if (_ParentScrollRect != null)
                {
                    _ParentScrollRect.OnEndDrag(
                        EventData
                    );
                }

                break;


            case PointerDragMode.Deployment:

                if (_DragOwner != null)
                {
                    _DragOwner.EndCardDrag(
                        this,
                        EventData
                    );
                }

                break;
        }


        _PointerDragMode =
            PointerDragMode.None;
    }


    public void OnScroll(
        PointerEventData EventData
    )
    {
        if (_ParentScrollRect == null ||
            EventData == null)
        {
            return;
        }


        _ParentScrollRect.OnScroll(
            EventData
        );


        EventData.Use();
    }


    private void ResolvePendingDragMode(
        PointerEventData EventData
    )
    {
        if (_PointerDragMode !=
                PointerDragMode.Pending ||
            EventData == null)
        {
            return;
        }


        float HorizontalMovement =
            Mathf.Abs(
                EventData.delta.x
            );

        float VerticalMovement =
            Mathf.Abs(
                EventData.delta.y
            );


        if (Mathf.Approximately(
                HorizontalMovement +
                    VerticalMovement,
                0f))
        {
            return;
        }


        bool CanBeginDeployment =
            _DragOwner != null &&
            _Placement != null &&
            !_Placement.IsPlaced;

        bool IsUpwardDeploymentGesture =
            CanBeginDeployment &&
            EventData.delta.y > 0f &&
            VerticalMovement >
                HorizontalMovement;


        if (IsUpwardDeploymentGesture)
        {
            _PointerDragMode =
                PointerDragMode.Deployment;


            _DragOwner.BeginCardDrag(
                this,
                EventData
            );

            return;
        }


        if (_ParentScrollRect == null)
        {
            _PointerDragMode =
                PointerDragMode.None;

            return;
        }


        _PointerDragMode =
            PointerDragMode.TrayScroll;


        _ParentScrollRect.OnBeginDrag(
            EventData
        );
    }


    private void OnDisable()
    {
        _PointerDragMode =
            PointerDragMode.None;
    }
}
