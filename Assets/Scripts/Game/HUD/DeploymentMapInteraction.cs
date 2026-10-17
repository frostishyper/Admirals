using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;


[DisallowMultipleComponent]
public sealed class DeploymentMapInteraction : MonoBehaviour
{
    private enum DragSource
    {
        None = 0,
        Card = 1,
        Map = 2
    }


    [Header("Game")]
    [SerializeField]
    private GameMaster _GameMaster;

    [SerializeField]
    private HumanGameplayInput _HumanGameplayInput;

    [SerializeField]
    private UnitPresentationCatalog _UnitPresentationCatalog;

    [SerializeField]
    private Camera _GameplayCamera;


    [Header("HUD")]
    [SerializeField]
    private GameplayHUD _GameplayHUD;

    [SerializeField]
    private DeploymentTrayUI _DeploymentTray;

    [SerializeField]
    private DeploymentTileMarkerPresenter _TileMarkerPresenter;


    [Header("Tentative Unit Views")]
    [SerializeField]
    private Color _LegalGhostColor =
        new Color(1f, 1f, 1f, 0.8f);

    [SerializeField]
    private Color _IllegalGhostColor =
        new Color(1f, 1f, 1f, 0.35f);

    [SerializeField]
    private Color _PlacedViewColor =
        Color.white;

    [SerializeField]
    private Color _SelectedViewColor =
        new Color(1f, 1f, 1f, 0.8f);


    private readonly Dictionary<string, UnitView> _TentativeViews =
        new Dictionary<string, UnitView>();

    private readonly List<RaycastResult> _UIRaycastResults =
        new List<RaycastResult>();


    private GameParticipantAPI.ParticipantAccess _ParticipantAccess;

    private RuntimeParticipant _Participant;

    private GameDeployment.ParticipantView _Plan;


    private DragSource _DragSource =
        DragSource.None;

    private DeploymentUnitCard _DraggedCard;

    private GameDeployment.PlacementView _DraggedPlacement;

    private UnitView _DraggedView;


    private Vector2Int _OriginalMovementPivotCoordinate;

    private UnitFacing _OriginalFacing;

    private Vector2Int _GrabbedTileOffset;

    private Vector2Int _DragCandidateMovementPivotCoordinate;

    private UnitFacing _DragFacing;


    private bool _DragCandidateLegal;

    private string _DragValidationMessage =
        string.Empty;


    private bool _Initialized;

    private bool _HUDButtonsBound;


    public bool Initialized =>
        _Initialized;


    private IEnumerator Start()
    {
        if (!ValidateReferences())
        {
            yield break;
        }


        while (!_GameMaster.InitializationComplete)
        {
            if (!_GameMaster.enabled)
            {
                Debug.LogError(
                    "Deployment Map Interaction could not initialize because GameMaster initialization failed.",
                    this
                );

                yield break;
            }


            yield return null;
        }


        Initialize();
    }


    private void OnEnable()
    {
        if (_Initialized)
        {
            BindHUDButtons();
        }
    }


    private void Update()
    {
        if (!_Initialized ||
            _GameMaster.CurrentPhase != GameplayPhase.Deployment ||
            _Plan == null ||
            _Plan.IsLocked)
        {
            return;
        }


        if (_DragSource == DragSource.Card)
        {
            return;
        }


        Mouse Mouse =
            UnityEngine.InputSystem.Mouse.current;


        if (Mouse == null)
        {
            return;
        }


        if (_DragSource == DragSource.Map)
        {
            UpdateMapDrag(Mouse);

            return;
        }


        if (!Mouse.leftButton.wasPressedThisFrame ||
            IsPointerOverUI())
        {
            return;
        }


        Vector2 ScreenPosition =
            Mouse.position.ReadValue();


        if (!TryGetPointerCoordinate(
                ScreenPosition,
                out Vector2Int PointerCoordinate))
        {
            ClearSelection();

            return;
        }


        if (!TryFindPlacementAtCoordinate(
                PointerCoordinate,
                out GameDeployment.PlacementView Placement))
        {
            ClearSelection();

            return;
        }


        BeginMapDrag(
            Placement,
            PointerCoordinate
        );
    }


    public bool Initialize()
    {
        if (_Initialized)
        {
            RefreshPresentation();

            return true;
        }


        if (!ValidateReferences())
        {
            return false;
        }


        if (!_HumanGameplayInput.Initialize())
        {
            Debug.LogError(
                "Deployment Map Interaction could not initialize the local Human Gameplay Input context.",
                this
            );

            return false;
        }


        if (!ReferenceEquals(
                _HumanGameplayInput.GameMaster,
                _GameMaster))
        {
            Debug.LogError(
                "Deployment Map Interaction and Human Gameplay Input reference different GameMaster instances.",
                this
            );

            return false;
        }


        _ParticipantAccess =
            _HumanGameplayInput.ParticipantAccess;

        _Participant =
            _ParticipantAccess?.Participant;


        if (_ParticipantAccess == null ||
            !_ParticipantAccess.TryGetDeploymentView(
                out _Plan))
        {
            Debug.LogError(
                "Deployment Map Interaction could not read the local participant's Deployment view.",
                this
            );

            return false;
        }


        BindHUDButtons();


        if (!_TileMarkerPresenter.Initialize(
                _ParticipantAccess,
                _Plan))
        {
            Debug.LogError(
                "Deployment Map Interaction could not initialize the Deployment tile markers.",
                this
            );

            UnbindHUDButtons();

            return false;
        }


        if (!_DeploymentTray.Initialize(
                _Plan,
                this))
        {
            Debug.LogError(
                "Deployment Map Interaction could not initialize the Deployment tray.",
                this
            );

            UnbindHUDButtons();

            return false;
        }


        _GameplayHUD.SetPlayerName(
            _Participant.Participant.DisplayName
        );

        _GameplayHUD.ShowDeploymentPhase();


        _Initialized =
            true;


        _HumanGameplayInput.ParticipantChanged +=
            HandleParticipantChanged;


        _ParticipantAccess.SnapshotChanged +=
            HandleSnapshotChanged;


        SynchronizeTentativeViews();
        RefreshPresentation();

        return true;
    }


    private void HandleParticipantChanged()
    {
        if (!_Initialized ||
            _HumanGameplayInput?.ParticipantAccess == null ||
            ReferenceEquals(
                _ParticipantAccess,
                _HumanGameplayInput.ParticipantAccess))
            return;

        CancelActiveDrag(false);
        DestroyAllTentativeViews();


        if (_ParticipantAccess != null)
        {
            _ParticipantAccess.SnapshotChanged -=
                HandleSnapshotChanged;
        }

        _ParticipantAccess = _HumanGameplayInput.ParticipantAccess;
        _Participant = _ParticipantAccess.Participant;


        _ParticipantAccess.SnapshotChanged +=
            HandleSnapshotChanged;

        if (!_ParticipantAccess.TryGetDeploymentView(out _Plan) ||
            !_TileMarkerPresenter.Initialize(_ParticipantAccess, _Plan) ||
            !_DeploymentTray.Initialize(_Plan, this))
        {
            Debug.LogError(
                "Deployment HUD could not rebind its participant context.",
                this);
            return;
        }

        _GameplayHUD.SetPlayerName(_ParticipantAccess.DisplayName);
        SynchronizeTentativeViews();
        RefreshPresentation();
    }


    private void HandleSnapshotChanged()
    {
        if (_Initialized)
        {
            RefreshPresentation();
        }
    }


    internal void BeginCardDrag(
        DeploymentUnitCard Card,
        PointerEventData EventData
    )
    {
        CancelActiveDrag(true);


        if (!_Initialized ||
            Card == null ||
            Card.Placement == null ||
            Card.Placement.IsPlaced)
        {
            return;
        }


        GameDeployment.PlacementView Placement =
            Card.Placement;


        if (!TryCreateTentativeView(
                Placement,
                out UnitView NewView))
        {
            return;
        }


        UnitFacing InitialFacing;


        if (Placement.HasFacing)
        {
            InitialFacing =
                Placement.Facing;
        }
        else if (!_ParticipantAccess.TryGetInitialDeploymentFacing(
                out InitialFacing))
        {
            Debug.LogError(
                "Deployment drag could not derive an initial facing from the authored Deployment zones.",
                this
            );

            Destroy(NewView.gameObject);

            return;
        }


        ClearSelection();


        _DragSource =
            DragSource.Card;

        _DraggedCard =
            Card;

        _DraggedPlacement =
            Placement;

        _DraggedView =
            NewView;

        _DragFacing =
            InitialFacing;

        _DragCandidateLegal =
            false;

        _DragValidationMessage =
            string.Empty;


        _DraggedView.name =
            $"Tentative_{Placement.ID}";

        _DraggedView.SetFacing(
            _DragFacing
        );

        _DraggedView.gameObject.SetActive(
            false
        );


        UpdateCardDrag(
            Card,
            EventData
        );
    }


    internal void UpdateCardDrag(
        DeploymentUnitCard Card,
        PointerEventData EventData
    )
    {
        if (_DragSource != DragSource.Card ||
            !ReferenceEquals(Card, _DraggedCard) ||
            _DraggedPlacement == null ||
            _DraggedView == null ||
            EventData == null)
        {
            return;
        }


        _DragCandidateLegal =
            false;

        _DragValidationMessage =
            string.Empty;


        if (IsPointerOverBlockingUI(EventData) ||
            !TryGetPointerCoordinate(
                EventData.position,
                out Vector2Int PointerCoordinate))
        {
            _DraggedView.gameObject.SetActive(
                false
            );

            _TileMarkerPresenter.ClearDragPreview();

            return;
        }


        _DragCandidateMovementPivotCoordinate =
            PointerCoordinate;


        PositionView(
            _DraggedView,
            _DraggedPlacement.Unit,
            _DragCandidateMovementPivotCoordinate,
            _DragFacing
        );


        _DragCandidateLegal =
            _ParticipantAccess.ValidateDeploymentPlacement(
                _DraggedPlacement.ID,
                _DragCandidateMovementPivotCoordinate,
                _DragFacing,
                out _DragValidationMessage
            );


        SetViewColor(
            _DraggedView,
            _DragCandidateLegal
                ? _LegalGhostColor
                : _IllegalGhostColor
        );


        _TileMarkerPresenter.ShowDragPreview(
            _DraggedPlacement.ID,
            _DraggedPlacement.Unit,
            _DragCandidateMovementPivotCoordinate,
            _DragFacing,
            _DragCandidateLegal
        );
    }


    internal void EndCardDrag(
        DeploymentUnitCard Card,
        PointerEventData EventData
    )
    {
        if (_DragSource != DragSource.Card ||
            !ReferenceEquals(Card, _DraggedCard) ||
            _DraggedPlacement == null ||
            _DraggedView == null)
        {
            return;
        }


        if (EventData == null)
        {
            _DragCandidateLegal =
                false;
        }
        else
        {
            UpdateCardDrag(
                Card,
                EventData
            );
        }


        string SubmissionMessage =
            string.Empty;


        bool Submitted =
            _DragCandidateLegal &&
            _ParticipantAccess.TrySubmitDeploymentPlacement(
                _DraggedPlacement.ID,
                _DragCandidateMovementPivotCoordinate,
                _DragFacing,
                out SubmissionMessage
            );


        if (!Submitted)
        {
            ReportRejectedInput(
                !string.IsNullOrWhiteSpace(SubmissionMessage)
                    ? SubmissionMessage
                    : _DragValidationMessage
            );

            CancelActiveDrag(false);
            RefreshPresentation();

            return;
        }


        string FleetEntryID =
            _DraggedPlacement.ID;

        UnitView PlacedView =
            _DraggedView;


        if (_TentativeViews.TryGetValue(
                FleetEntryID,
                out UnitView ExistingView) &&
            ExistingView != null &&
            ExistingView != PlacedView)
        {
            Destroy(ExistingView.gameObject);
        }


        _TentativeViews[FleetEntryID] =
            PlacedView;


        ClearActiveDragState();
        RefreshPresentation();
    }


    private bool ValidateReferences()
    {
        if (_GameMaster == null)
        {
            Debug.LogError(
                "Deployment Map Interaction has no GameMaster.",
                this
            );

            return false;
        }


        if (_HumanGameplayInput == null)
        {
            Debug.LogError(
                "Deployment Map Interaction has no Human Gameplay Input context.",
                this
            );

            return false;
        }


        if (_UnitPresentationCatalog == null)
        {
            Debug.LogError(
                "Deployment Map Interaction has no Unit Presentation Catalog.",
                this
            );

            return false;
        }


        if (_GameplayCamera == null)
        {
            Debug.LogError(
                "Deployment Map Interaction has no Gameplay Camera.",
                this
            );

            return false;
        }


        if (_GameplayHUD == null)
        {
            Debug.LogError(
                "Deployment Map Interaction has no Gameplay HUD.",
                this
            );

            return false;
        }


        if (_DeploymentTray == null)
        {
            Debug.LogError(
                "Deployment Map Interaction has no Deployment Tray.",
                this
            );

            return false;
        }


        if (_TileMarkerPresenter == null)
        {
            Debug.LogError(
                "Deployment Map Interaction has no Deployment Tile Marker Presenter.",
                this
            );

            return false;
        }


        return true;
    }


    private void BindHUDButtons()
    {
        if (_HUDButtonsBound ||
            _GameplayHUD == null)
        {
            return;
        }


        Button ResetButton =
            _GameplayHUD.ResetButton;

        Button ConfirmButton =
            _GameplayHUD.ConfirmButton;

        Button RotateLeftButton =
            _GameplayHUD.RotateLeftButton;

        Button RotateRightButton =
            _GameplayHUD.RotateRightButton;


        if (ResetButton != null)
        {
            ResetButton.onClick.AddListener(
                HandleReset
            );
        }


        if (ConfirmButton != null)
        {
            ConfirmButton.onClick.AddListener(
                HandleConfirm
            );
        }


        if (RotateLeftButton != null)
        {
            RotateLeftButton.onClick.AddListener(
                HandleRotateLeft
            );
        }


        if (RotateRightButton != null)
        {
            RotateRightButton.onClick.AddListener(
                HandleRotateRight
            );
        }


        _HUDButtonsBound =
            true;
    }


    private void UnbindHUDButtons()
    {
        if (!_HUDButtonsBound ||
            _GameplayHUD == null)
        {
            return;
        }


        Button ResetButton =
            _GameplayHUD.ResetButton;

        Button ConfirmButton =
            _GameplayHUD.ConfirmButton;

        Button RotateLeftButton =
            _GameplayHUD.RotateLeftButton;

        Button RotateRightButton =
            _GameplayHUD.RotateRightButton;


        if (ResetButton != null)
        {
            ResetButton.onClick.RemoveListener(
                HandleReset
            );
        }


        if (ConfirmButton != null)
        {
            ConfirmButton.onClick.RemoveListener(
                HandleConfirm
            );
        }


        if (RotateLeftButton != null)
        {
            RotateLeftButton.onClick.RemoveListener(
                HandleRotateLeft
            );
        }


        if (RotateRightButton != null)
        {
            RotateRightButton.onClick.RemoveListener(
                HandleRotateRight
            );
        }


        _HUDButtonsBound =
            false;
    }


    private void BeginMapDrag(
        GameDeployment.PlacementView Placement,
        Vector2Int GrabbedCoordinate
    )
    {
        if (Placement == null ||
            !Placement.IsPlaced ||
            !Placement.HasFacing)
        {
            return;
        }


        CancelActiveDrag(true);


        if (!_ParticipantAccess.TrySelectDeploymentPlacement(
                Placement.ID,
                out string SelectionMessage))
        {
            ReportRejectedInput(
                SelectionMessage
            );

            return;
        }


        if (!_TentativeViews.TryGetValue(
                Placement.ID,
                out UnitView View) ||
            View == null)
        {
            if (!TryCreateTentativeView(
                    Placement,
                    out View))
            {
                return;
            }


            _TentativeViews[Placement.ID] =
                View;
        }


        _DragSource =
            DragSource.Map;

        _DraggedPlacement =
            Placement;

        _DraggedView =
            View;

        _OriginalMovementPivotCoordinate =
            Placement.MovementPivotCoordinate;

        _OriginalFacing =
            Placement.Facing;

        _GrabbedTileOffset =
            GrabbedCoordinate -
            Placement.MovementPivotCoordinate;

        _DragCandidateMovementPivotCoordinate =
            Placement.MovementPivotCoordinate;

        _DragFacing =
            Placement.Facing;

        _DragCandidateLegal =
            true;

        _DragValidationMessage =
            string.Empty;


        PositionView(
            _DraggedView,
            _DraggedPlacement.Unit,
            _OriginalMovementPivotCoordinate,
            _OriginalFacing
        );


        RefreshPresentation();
    }


    private void UpdateMapDrag(
        Mouse Mouse
    )
    {
        if (_DraggedPlacement == null ||
            _DraggedView == null)
        {
            CancelActiveDrag(true);
            RefreshPresentation();

            return;
        }


        Vector2 ScreenPosition =
            Mouse.position.ReadValue();


        _DragCandidateLegal =
            false;

        _DragValidationMessage =
            string.Empty;


        if (IsPointerOverUI() ||
            !TryGetPointerCoordinate(
                ScreenPosition,
                out Vector2Int PointerCoordinate))
        {
            PositionView(
                _DraggedView,
                _DraggedPlacement.Unit,
                _OriginalMovementPivotCoordinate,
                _OriginalFacing
            );

            SetViewColor(
                _DraggedView,
                _SelectedViewColor
            );

            _TileMarkerPresenter.ClearDragPreview();
        }
        else
        {
            _DragCandidateMovementPivotCoordinate =
                PointerCoordinate -
                _GrabbedTileOffset;


            PositionView(
                _DraggedView,
                _DraggedPlacement.Unit,
                _DragCandidateMovementPivotCoordinate,
                _DragFacing
            );


            _DragCandidateLegal =
                _ParticipantAccess.ValidateDeploymentPlacement(
                    _DraggedPlacement.ID,
                    _DragCandidateMovementPivotCoordinate,
                    _DragFacing,
                    out _DragValidationMessage
                );


            SetViewColor(
                _DraggedView,
                _DragCandidateLegal
                    ? _LegalGhostColor
                    : _IllegalGhostColor
            );


            _TileMarkerPresenter.ShowDragPreview(
                _DraggedPlacement.ID,
                _DraggedPlacement.Unit,
                _DragCandidateMovementPivotCoordinate,
                _DragFacing,
                _DragCandidateLegal
            );
        }


        if (Mouse.leftButton.wasReleasedThisFrame)
        {
            CompleteMapDrag();
        }
    }


    private void CompleteMapDrag()
    {
        if (_DraggedPlacement == null ||
            _DraggedView == null)
        {
            CancelActiveDrag(true);
            RefreshPresentation();

            return;
        }


        string SubmissionMessage =
            string.Empty;


        bool Submitted =
            _DragCandidateLegal &&
            _ParticipantAccess.TrySubmitDeploymentPlacement(
                _DraggedPlacement.ID,
                _DragCandidateMovementPivotCoordinate,
                _DragFacing,
                out SubmissionMessage
            );


        if (Submitted)
        {
            ClearActiveDragState();
            RefreshPresentation();

            return;
        }


        ReportRejectedInput(
            !string.IsNullOrWhiteSpace(SubmissionMessage)
                ? SubmissionMessage
                : _DragValidationMessage
        );


        CancelActiveDrag(true);
        RefreshPresentation();
    }


    private bool TryFindPlacementAtCoordinate(
        Vector2Int Coordinate,
        out GameDeployment.PlacementView FoundPlacement
    )
    {
        FoundPlacement =
            null;


        if (_Plan == null)
        {
            return false;
        }


        IReadOnlyList<GameDeployment.PlacementView> Placements =
            _Plan.Placements;


        for (int PlacementIndex = 0;
            PlacementIndex < Placements.Count;
            PlacementIndex++)
        {
            GameDeployment.PlacementView Placement =
                Placements[PlacementIndex];


            if (Placement == null ||
                !Placement.IsPlaced ||
                !Placement.HasFacing)
            {
                continue;
            }


            if (!_ParticipantAccess.TryGetDeploymentPlacementTiles(
                    Placement.ID,
                    out RuntimeUnitTilePlacement[] PlacementTiles,
                    out _))
            {
                continue;
            }


            for (int TileIndex = 0;
                TileIndex < PlacementTiles.Length;
                TileIndex++)
            {
                if (PlacementTiles[TileIndex].MapCoordinate !=
                    Coordinate)
                {
                    continue;
                }


                FoundPlacement =
                    Placement;

                return true;
            }
        }


        return false;
    }


    private void HandleRotateLeft()
    {
        if (!_Initialized ||
            _Plan == null ||
            _Plan.SelectedPlacement == null)
        {
            return;
        }


        CancelActiveDrag(true);


        GameDeployment.PlacementView SelectedPlacement =
            _Plan.SelectedPlacement;


        if (!_ParticipantAccess.TryRotateDeploymentPlacementCounterClockwise(
                SelectedPlacement.ID,
                out string ValidationMessage))
        {
            ReportRejectedInput(
                ValidationMessage
            );
        }


        RefreshPresentation();
    }


    private void HandleRotateRight()
    {
        if (!_Initialized ||
            _Plan == null ||
            _Plan.SelectedPlacement == null)
        {
            return;
        }


        CancelActiveDrag(true);


        GameDeployment.PlacementView SelectedPlacement =
            _Plan.SelectedPlacement;


        if (!_ParticipantAccess.TryRotateDeploymentPlacementClockwise(
                SelectedPlacement.ID,
                out string ValidationMessage))
        {
            ReportRejectedInput(
                ValidationMessage
            );
        }


        RefreshPresentation();
    }


    private void HandleReset()
    {
        if (!_Initialized)
        {
            return;
        }


        CancelActiveDrag(true);


        if (!_ParticipantAccess.TryResetDeployment(
                out string ValidationMessage))
        {
            ReportRejectedInput(
                ValidationMessage
            );

            RefreshPresentation();

            return;
        }


        DestroyAllTentativeViews();
        _DeploymentTray.ResetScroll();
        RefreshPresentation();
    }


    private void HandleConfirm()
    {
        if (!_Initialized ||
            _Plan == null ||
            _Plan.IsLocked)
        {
            return;
        }


        CancelActiveDrag(
            true
        );


        if (!_ParticipantAccess.TryConfirmDeployment(
                out string ValidationMessage))
        {
            ReportRejectedInput(
                ValidationMessage
            );
        }


        RefreshPresentation();
    }


    private void ClearSelection()
    {
        if (!_Initialized ||
            _Participant == null ||
            _Plan == null ||
            _Plan.SelectedPlacement == null)
        {
            return;
        }


        if (!_ParticipantAccess.TryClearDeploymentSelection(
                out string ValidationMessage))
        {
            ReportRejectedInput(
                ValidationMessage
            );
        }


        RefreshPresentation();
    }


    private void SynchronizeTentativeViews()
    {
        if (_Plan == null)
        {
            return;
        }


        IReadOnlyList<GameDeployment.PlacementView> Placements =
            _Plan.Placements;


        for (int Index = 0;
            Index < Placements.Count;
            Index++)
        {
            GameDeployment.PlacementView Placement =
                Placements[Index];


            if (Placement == null ||
                !Placement.IsPlaced ||
                !Placement.HasFacing)
            {
                continue;
            }


            if (_TentativeViews.TryGetValue(
                    Placement.ID,
                    out UnitView ExistingView) &&
                ExistingView != null)
            {
                PositionView(
                    ExistingView,
                    Placement.Unit,
                    Placement.MovementPivotCoordinate,
                    Placement.Facing
                );

                continue;
            }


            if (!TryCreateTentativeView(
                    Placement,
                    out UnitView NewView))
            {
                continue;
            }


            _TentativeViews[Placement.ID] =
                NewView;


            PositionView(
                NewView,
                Placement.Unit,
                Placement.MovementPivotCoordinate,
                Placement.Facing
            );
        }
    }


    private bool TryCreateTentativeView(
        GameDeployment.PlacementView Placement,
        out UnitView View
    )
    {
        View =
            null;


        if (Placement == null ||
            Placement.Unit == null)
        {
            return false;
        }


        if (!_UnitPresentationCatalog.TryGetPrefab(
                Placement.Unit,
                out UnitView UnitPrefab))
        {
            Debug.LogError(
                $"No Unit presentation prefab is registered for '{Placement.Unit.Name}'.",
                this
            );

            return false;
        }


        if (_GameMaster.MapAuthoring == null ||
            _GameMaster.MapAuthoring.Units == null)
        {
            Debug.LogError(
                "Deployment presentation cannot find the runtime Map Units root.",
                this
            );

            return false;
        }


        View =
            Instantiate(
                UnitPrefab,
                _GameMaster.MapAuthoring.Units
            );


        if (View == null)
        {
            return false;
        }


        View.name =
            $"Tentative_{Placement.ID}";


        return true;
    }


    private void PositionView(
        UnitView View,
        Definition_Unit Unit,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing
    )
    {
        if (View == null ||
            _GameMaster.GameMap == null)
        {
            return;
        }


        Vector3 WorldPosition =
            _GameMaster.GameMap.CoordinateToWorldCenter(
                MovementPivotCoordinate
            );


        if (UnitFootprintGeometry.TryGetVisualCenterOffset(
                Unit,
                Facing,
                out Vector2 VisualCenterOffset,
                out string ValidationMessage))
        {
            WorldPosition +=
                _GameMaster.GameMap.CoordinateOffsetToWorld(
                    VisualCenterOffset
                );
        }
        else
        {
            Debug.LogError(
                $"Could not position Unit presentation: {ValidationMessage}",
                View
            );
        }


        View.transform.position =
            WorldPosition;

        View.SetFacing(
            Facing
        );

        View.gameObject.SetActive(
            true
        );
    }


    private void RefreshPresentation()
    {
        if (!_Initialized ||
            _Plan == null)
        {
            return;
        }


        if (_GameMaster.CurrentPhase !=
                GameplayPhase.Deployment)
        {
            FinishDeploymentPresentation();

            return;
        }


        _GameplayHUD.ShowDeploymentPhase();


        SynchronizeTentativeViews();
        ApplyTentativeViewColors();
        _DeploymentTray.Refresh();


        _GameplayHUD.SetDeploymentPlacedCount(
            _Plan.PlacedCount,
            _Plan.TotalCount
        );


        bool HasSelection =
            _Plan.SelectedPlacement != null &&
            _Plan.SelectedPlacement.IsPlaced;

        bool HasTentativePlacements =
            _Plan.PlacedCount > 0;


        bool CanAcceptInput =
            !_Plan.IsLocked &&
            _GameMaster.CurrentPhase ==
                GameplayPhase.Deployment;


        _GameplayHUD.SetDeploymentControlsAvailable(
            CanAcceptInput &&
                HasTentativePlacements,
            CanAcceptInput &&
                _Plan.AllPlaced,
            CanAcceptInput &&
                HasSelection
        );
    }


    private void FinishDeploymentPresentation()
    {
        CancelActiveDrag(
            true
        );


        _TileMarkerPresenter.ClearMarkers();


        foreach (KeyValuePair<string, UnitView> Pair
            in _TentativeViews)
        {
            SetViewColor(
                Pair.Value,
                _PlacedViewColor
            );
        }


        SynchronizeCommittedViewStates();


        _GameplayHUD.SetDeploymentControlsAvailable(
            false,
            false,
            false
        );


        _GameplayHUD.ShowMovementPhase();
    }


    private void SynchronizeCommittedViewStates()
    {
        if (_ParticipantAccess == null ||
            !_ParticipantAccess.TryGetSnapshot(
                out ParticipantGameSnapshot Snapshot,
                out _))
        {
            return;
        }


        for (int i = 0;
            i < Snapshot.OwnUnits.Length;
            i++)
        {
            ParticipantUnitSnapshot Unit =
                Snapshot.OwnUnits[i];


            if (Unit == null ||
                string.IsNullOrWhiteSpace(
                    Unit.FleetEntryID) ||
                !_TentativeViews.TryGetValue(
                    Unit.FleetEntryID,
                    out UnitView View) ||
                View == null)
            {
                continue;
            }


            View.SetFacing(
                Unit.Facing
            );

            View.SetTags(
                Unit.Tags
            );
        }
    }


    private void ApplyTentativeViewColors()
    {
        if (_Plan == null)
        {
            return;
        }


        foreach (KeyValuePair<string, UnitView> Pair
            in _TentativeViews)
        {
            UnitView View =
                Pair.Value;


            if (View == null ||
                !_Plan.TryGetPlacement(
                    Pair.Key,
                    out GameDeployment.PlacementView Placement) ||
                !Placement.IsPlaced)
            {
                continue;
            }


            PositionView(
                View,
                Placement.Unit,
                Placement.MovementPivotCoordinate,
                Placement.Facing
            );


            bool IsSelected =
                ReferenceEquals(
                    Placement,
                    _Plan.SelectedPlacement
                );


            SetViewColor(
                View,
                IsSelected
                    ? _SelectedViewColor
                    : _PlacedViewColor
            );
        }
    }


    private static void SetViewColor(
        UnitView View,
        Color Color
    )
    {
        if (View == null)
        {
            return;
        }


        SpriteRenderer SpriteRenderer =
            View.GetComponent<SpriteRenderer>();


        if (SpriteRenderer != null)
        {
            SpriteRenderer.color =
                Color;
        }
    }


    private bool TryGetPointerCoordinate(
        Vector2 ScreenPosition,
        out Vector2Int Coordinate
    )
    {
        Coordinate =
            default;


        if (!TryGetPointerWorldPosition(
                ScreenPosition,
                out Vector3 WorldPosition) ||
            _GameMaster.GameMap == null)
        {
            return false;
        }


        Coordinate =
            _GameMaster.GameMap.WorldToCoordinate(
                WorldPosition
            );


        return true;
    }


    private bool TryGetPointerWorldPosition(
        Vector2 ScreenPosition,
        out Vector3 WorldPosition
    )
    {
        WorldPosition =
            Vector3.zero;


        if (_GameplayCamera == null ||
            _GameMaster.MapAuthoring == null ||
            _GameMaster.MapAuthoring.GridReference == null)
        {
            return false;
        }


        float MapPlaneZ =
            _GameMaster
                .MapAuthoring
                .GridReference
                .transform
                .position
                .z;


        Plane MapPlane =
            new Plane(
                Vector3.forward,
                new Vector3(
                    0f,
                    0f,
                    MapPlaneZ
                )
            );


        Ray PointerRay =
            _GameplayCamera.ScreenPointToRay(
                ScreenPosition
            );


        if (!MapPlane.Raycast(
                PointerRay,
                out float Distance))
        {
            return false;
        }


        WorldPosition =
            PointerRay.GetPoint(
                Distance
            );


        return true;
    }


    private static bool IsPointerOverUI()
    {
        return
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject();
    }


    private bool IsPointerOverBlockingUI(
        PointerEventData EventData
    )
    {
        if (EventSystem.current == null ||
            EventData == null)
        {
            return false;
        }


        _UIRaycastResults.Clear();


        EventSystem.current.RaycastAll(
            EventData,
            _UIRaycastResults
        );


        for (int Index = 0;
            Index < _UIRaycastResults.Count;
            Index++)
        {
            if (_UIRaycastResults[Index].module
                is GraphicRaycaster)
            {
                return true;
            }
        }


        return false;
    }


    private void CancelActiveDrag(
        bool RestoreExistingView
    )
    {
        if (_DragSource == DragSource.Card &&
            _DraggedView != null)
        {
            Destroy(
                _DraggedView.gameObject
            );
        }
        else if (_DragSource == DragSource.Map &&
                 RestoreExistingView &&
                 _DraggedView != null)
        {
            PositionView(
                _DraggedView,
                _DraggedPlacement.Unit,
                _OriginalMovementPivotCoordinate,
                _OriginalFacing
            );
        }


        ClearActiveDragState();
    }


    private void ClearActiveDragState()
    {
        if (_TileMarkerPresenter != null)
        {
            _TileMarkerPresenter.ClearDragPreview();
        }


        _DragSource =
            DragSource.None;

        _DraggedCard =
            null;

        _DraggedPlacement =
            null;

        _DraggedView =
            null;

        _OriginalMovementPivotCoordinate =
            default;

        _OriginalFacing =
            default;

        _GrabbedTileOffset =
            default;

        _DragCandidateMovementPivotCoordinate =
            default;

        _DragFacing =
            default;

        _DragCandidateLegal =
            false;

        _DragValidationMessage =
            string.Empty;
    }


    private void DestroyAllTentativeViews()
    {
        foreach (KeyValuePair<string, UnitView> Pair
            in _TentativeViews)
        {
            if (Pair.Value != null)
            {
                Destroy(
                    Pair.Value.gameObject
                );
            }
        }


        _TentativeViews.Clear();
    }


    private void ReportRejectedInput(
        string ValidationMessage
    )
    {
        if (string.IsNullOrWhiteSpace(
                ValidationMessage))
        {
            return;
        }


        Debug.Log(
            $"Deployment input rejected: {ValidationMessage}",
            this
        );
    }


    private void OnDisable()
    {
        CancelActiveDrag(true);
        UnbindHUDButtons();
    }


    private void OnDestroy()
    {
        if (_HumanGameplayInput != null)
            _HumanGameplayInput.ParticipantChanged -=
                HandleParticipantChanged;


        if (_ParticipantAccess != null)
        {
            _ParticipantAccess.SnapshotChanged -=
                HandleSnapshotChanged;
        }


        if (_TileMarkerPresenter != null)
        {
            _TileMarkerPresenter.ClearDragPreview();
        }


        UnbindHUDButtons();
    }
}



