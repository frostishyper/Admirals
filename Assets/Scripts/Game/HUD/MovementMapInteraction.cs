using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;


// Human Movement-phase translator. Tentative state is presentation-only;
// only Confirm submits gameplay data through ParticipantAccess.
[DisallowMultipleComponent]
[RequireComponent(typeof(HumanGameplayInput))]
[RequireComponent(typeof(ParticipantUnitPresenter))]
public sealed class MovementMapInteraction : MonoBehaviour
{
    [Header("Game")]
    [SerializeField]
    private GameMaster _GameMaster;

    [SerializeField]
    private HumanGameplayInput _HumanGameplayInput;

    [SerializeField]
    private ParticipantUnitPresenter _UnitPresenter;

    [SerializeField]
    private UnitPresentationCatalog _UnitPresentationCatalog;

    [SerializeField]
    private Camera _GameplayCamera;


    [Header("HUD")]
    [SerializeField]
    private GameplayHUD _GameplayHUD;


    [Header("Movement Markers")]
    [SerializeField]
    private TileBase _MovementAreaTile;

    [SerializeField]
    private TileBase _LegalDestinationTile;

    [SerializeField]
    private TileBase _IllegalDestinationTile;


    [Header("Tentative Unit View")]
    [SerializeField]
    private Color _LegalPreviewColor =
        new Color(1f, 1f, 1f, 0.8f);

    [SerializeField]
    private Color _IllegalPreviewColor =
        new Color(1f, 1f, 1f, 0.35f);


    private readonly HashSet<Vector2Int> _MovementArea =
        new HashSet<Vector2Int>();

    private readonly HashSet<Vector3Int> _PaintedCells =
        new HashSet<Vector3Int>();

    private readonly List<Vector3Int> _CellsToClear =
        new List<Vector3Int>();


    private GameParticipantAPI.ParticipantAccess _ParticipantAccess;
    private ParticipantGameSnapshot _Snapshot;
    private ParticipantUnitSnapshot _SelectedUnit;
    private Definition_Unit _SelectedDefinition;
    private Tilemap _TileMarkers;

    private Vector2Int _CandidatePivot;
    private UnitFacing _CandidateFacing;
    private bool _CandidateLegal;
    private bool _MineTargeting;
    private bool _UnitDeploymentTargeting;
    private int _SelectedSystemSlotIndex = -1;
    private int _SelectedCarriedUnitIndex = -1;
    private string _DeploymentPreviewDefinitionID = string.Empty;
    private bool _Initialized;
    private bool _ButtonsBound;
    private float _NextTimerRefreshTime;


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
                    "Movement Map Interaction could not initialize because GameMaster initialization failed.",
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
            BindButtons();
        }
    }


    private void Update()
    {
        if (!_Initialized ||
            (_GameMaster.CurrentPhase != GameplayPhase.Movement &&
             _GameMaster.CurrentPhase != GameplayPhase.PreBattle) ||
            IsMovementPlaybackLocked())
        {
            return;
        }


        HandleKeyboardInput();
        HandlePointerInput();


        if (Time.unscaledTime >=
            _NextTimerRefreshTime)
        {
            _NextTimerRefreshTime =
                Time.unscaledTime + 0.1f;

            RefreshTimerPresentation();
        }
    }


    public bool Initialize()
    {
        if (_Initialized)
        {
            RefreshFromSnapshot();
            return true;
        }


        if (!ValidateReferences() ||
            !_HumanGameplayInput.Initialize() ||
            !ReferenceEquals(
                _HumanGameplayInput.GameMaster,
                _GameMaster) ||
            _GameMaster.MapAuthoring == null ||
            _GameMaster.MapAuthoring.TileMarkers == null)
        {
            Debug.LogError(
                "Movement Map Interaction could not resolve its participant or runtime map references.",
                this
            );

            return false;
        }


        _TileMarkers =
            _GameMaster.MapAuthoring.TileMarkers;

        _Initialized =
            true;

        _HumanGameplayInput.ParticipantChanged +=
            HandleParticipantChanged;

        BindParticipant();
        BindButtons();
        RefreshFromSnapshot();

        return true;
    }


    private void BindParticipant()
    {
        if (_ParticipantAccess != null)
        {
            _ParticipantAccess.SnapshotChanged -=
                HandleSnapshotChanged;
        }


        _ParticipantAccess =
            _HumanGameplayInput.ParticipantAccess;


        if (_ParticipantAccess != null)
        {
            _ParticipantAccess.SnapshotChanged +=
                HandleSnapshotChanged;
        }
    }


    private void HandleParticipantChanged()
    {
        ClearSelection();
        BindParticipant();
        RefreshFromSnapshot();
    }


    private void HandleSnapshotChanged()
    {
        RefreshFromSnapshot();
    }


    private void RefreshTimerPresentation()
    {
        if (_GameMaster.CurrentPhase == GameplayPhase.PreBattle)
        {
            if (_ParticipantAccess != null &&
                _ParticipantAccess.TryGetPreBattleView(
                    out GamePreBattle.ParticipantView PreBattle) &&
                PreBattle != null)
            {
                _GameplayHUD.SetPreBattleStatus(
                    PreBattle.ActionsCommitted,
                    PreBattle.SecondsRemaining,
                    PreBattle.CanAct
                );
            }

            return;
        }


        if (_ParticipantAccess == null ||
            !_ParticipantAccess.TryGetMovementView(
                out GameMovement.ParticipantView Movement) ||
            Movement == null)
        {
            return;
        }


        _GameplayHUD.SetMovementStatus(
            Movement.MovesCommitted,
            Movement.MovementLimit,
            Movement.SecondsRemaining,
            Movement.CanAct
        );
    }


    private void RefreshFromSnapshot()
    {
        string ValidationMessage =
            string.Empty;


        if (!_Initialized ||
            _ParticipantAccess == null ||
            !_ParticipantAccess.TryGetSnapshot(
                out _Snapshot,
                out ValidationMessage))
        {
            if (!string.IsNullOrWhiteSpace(
                    ValidationMessage))
            {
                Debug.LogError(
                    $"Movement interaction could not read its participant snapshot: {ValidationMessage}",
                    this
                );
            }

            return;
        }


        bool IsMovement =
            _Snapshot.CurrentPhase == GameplayPhase.Movement &&
            _Snapshot.Movement != null;

        bool IsPreBattle =
            _Snapshot.CurrentPhase == GameplayPhase.PreBattle &&
            _Snapshot.PreBattle != null;


        if (!IsMovement && !IsPreBattle)
        {
            ClearSelection();
            return;
        }


        if (IsPreBattle)
        {
            _GameplayHUD.ShowPreBattlePhase();
            _GameplayHUD.SetPlayerName(_ParticipantAccess.DisplayName);
            _GameplayHUD.SetPreBattleStatus(
                _Snapshot.PreBattle.ActionsCommitted,
                _Snapshot.PreBattle.SecondsRemaining,
                _Snapshot.PreBattle.CanAct
            );


            if (!_Snapshot.PreBattle.CanAct)
            {
                ClearSelection();
                return;
            }


            if (_SelectedUnit == null)
            {
                _GameplayHUD.SetPreBattleSelectionVisible(false, false);
                return;
            }


            if (!TryFindOwnUnit(
                    _SelectedUnit.RuntimeUnitID,
                    out ParticipantUnitSnapshot PreBattleUnit))
            {
                ClearSelection();
                return;
            }


            _SelectedUnit = PreBattleUnit;
            RefreshSelectedUnitDetails();
            _GameplayHUD.SetPreBattleSelectionVisible(
                true,
                _MineTargeting || _UnitDeploymentTargeting,
                _UnitDeploymentTargeting
            );

            if (_MineTargeting || _UnitDeploymentTargeting)
                RefreshCandidatePresentation();

            return;
        }


        _GameplayHUD.ShowMovementPhase();
        _GameplayHUD.SetPlayerName(
            _ParticipantAccess.DisplayName
        );

        _GameplayHUD.SetMovementStatus(
            _Snapshot.Movement.MovesCommitted,
            _Snapshot.Movement.MovementLimit,
            _Snapshot.Movement.SecondsRemaining,
            _Snapshot.Movement.CanAct
        );


        if (!_Snapshot.Movement.CanAct)
        {
            ClearSelection();
            return;
        }


        if (_SelectedUnit == null)
        {
            _GameplayHUD.SetMovementSelectionVisible(false);
            return;
        }


        if (!TryFindOwnUnit(
                _SelectedUnit.RuntimeUnitID,
                out ParticipantUnitSnapshot CurrentUnit) ||
            HasMoved(
                _Snapshot.Movement,
                CurrentUnit.RuntimeUnitID))
        {
            ClearSelection();
            return;
        }


        _SelectedUnit =
            CurrentUnit;

        RefreshSelectedUnitDetails();

        RefreshCandidatePresentation();
    }


    private void HandlePointerInput()
    {
        Mouse Mouse =
            UnityEngine.InputSystem.Mouse.current;


        if (Mouse == null)
        {
            return;
        }


        if (Mouse.rightButton.wasPressedThisFrame)
        {
            ClearSelection();
            return;
        }


        if (!Mouse.leftButton.wasPressedThisFrame ||
            IsPointerOverUI() ||
            !TryGetPointerCoordinate(
                Mouse.position.ReadValue(),
                out Vector2Int Coordinate))
        {
            return;
        }


        if (TryFindOwnUnitAtCoordinate(
                Coordinate,
                out ParticipantUnitSnapshot Unit))
        {
            SelectUnit(Unit);
            return;
        }


        if (_MineTargeting)
        {
            TrySetMineCandidate(Coordinate);
            return;
        }


        if (_UnitDeploymentTargeting)
        {
            TrySetUnitDeploymentCandidate(Coordinate);
            return;
        }


        if (_SelectedUnit != null)
        {
            TrySetCandidate(
                Coordinate,
                _CandidateFacing
            );
        }
    }


    private void HandleKeyboardInput()
    {
        Keyboard Keyboard =
            UnityEngine.InputSystem.Keyboard.current;


        if (Keyboard == null)
        {
            return;
        }


        if (_GameMaster.CurrentPhase == GameplayPhase.PreBattle)
        {
            if (Keyboard.escapeKey.wasPressedThisFrame)
                ClearSelection();
            else if (Keyboard.enterKey.wasPressedThisFrame)
                ConfirmMovement();
            else if (Keyboard.qKey.wasPressedThisFrame)
                RotateCandidate(false);
            else if (Keyboard.eKey.wasPressedThisFrame)
                RotateCandidate(true);

            return;
        }


        if (Keyboard.escapeKey.wasPressedThisFrame)
            ClearSelection();
        else if (Keyboard.enterKey.wasPressedThisFrame)
            ConfirmMovement();
        else if (Keyboard.upArrowKey.wasPressedThisFrame)
            NudgeCandidate(Vector2Int.up);
        else if (Keyboard.downArrowKey.wasPressedThisFrame)
            NudgeCandidate(Vector2Int.down);
        else if (Keyboard.leftArrowKey.wasPressedThisFrame)
            NudgeCandidate(Vector2Int.left);
        else if (Keyboard.rightArrowKey.wasPressedThisFrame)
            NudgeCandidate(Vector2Int.right);
        else if (Keyboard.qKey.wasPressedThisFrame)
            RotateCandidate(false);
        else if (Keyboard.eKey.wasPressedThisFrame)
            RotateCandidate(true);
    }


    private void SelectUnit(
        ParticipantUnitSnapshot Unit
    )
    {
        if (_Snapshot?.PreBattle != null &&
            _Snapshot.CurrentPhase == GameplayPhase.PreBattle)
        {
            SelectPreBattleUnit(Unit);
            return;
        }


        if (_Snapshot?.Movement == null ||
            !_Snapshot.Movement.CanAct ||
            Unit == null ||
            Unit.LifeState != RuntimeUnitLifeState.Active ||
            !Unit.IsOnMap ||
            HasMoved(
                _Snapshot.Movement,
                Unit.RuntimeUnitID))
        {
            return;
        }


        string ValidationMessage =
            string.Empty;

        Vector2Int[] Area =
            Array.Empty<Vector2Int>();


        if (!_UnitPresentationCatalog.TryGetPresentation(
                Unit.DefinitionID,
                out Definition_Unit Definition,
                out _) ||
            !_ParticipantAccess.TryGetMovementArea(
                Unit.RuntimeUnitID,
                out Area,
                out ValidationMessage))
        {
            ReportRejectedInput(ValidationMessage);
            return;
        }


        ClearSelection();

        _SelectedUnit =
            Unit;

        _SelectedDefinition =
            Definition;

        _CandidatePivot =
            Unit.MovementPivotCoordinate;

        _CandidateFacing =
            Unit.Facing;

        _MovementArea.Clear();


        for (int i = 0; i < Area.Length; i++)
        {
            _MovementArea.Add(
                Area[i]
            );
        }


        RefreshSelectedUnitDetails();


        RefreshCandidatePresentation();
    }


    private void SelectPreBattleUnit(
        ParticipantUnitSnapshot Unit
    )
    {
        if (_Snapshot?.PreBattle == null ||
            !_Snapshot.PreBattle.CanAct ||
            Unit == null ||
            Unit.LifeState != RuntimeUnitLifeState.Active ||
            !Unit.IsOnMap ||
            !_UnitPresentationCatalog.TryGetPresentation(
                Unit.DefinitionID,
                out Definition_Unit Definition,
                out _))
        {
            return;
        }


        ClearSelection();
        _SelectedUnit = Unit;
        _SelectedDefinition = Definition;
        _CandidatePivot = Unit.MovementPivotCoordinate;
        _CandidateFacing = Unit.Facing;
        RefreshSelectedUnitDetails();
        _GameplayHUD.SetPreBattleSelectionVisible(true, false);
    }


    private void BeginPreBattleSystemTargeting(
        int SystemSlotIndex,
        int CarriedUnitIndex)
    {
        if (CarriedUnitIndex >= 0)
            BeginUnitDeploymentTargeting(SystemSlotIndex, CarriedUnitIndex);
        else
            BeginMineTargeting(SystemSlotIndex);
    }


    private void BeginMineTargeting(int SystemSlotIndex)
    {
        string ValidationMessage = string.Empty;


        if (_SelectedUnit == null ||
            _ParticipantAccess == null ||
            !_ParticipantAccess.TryGetMineDeploymentArea(
                _SelectedUnit.RuntimeUnitID,
                SystemSlotIndex,
                out Vector2Int[] Area,
                out ValidationMessage))
        {
            ReportRejectedInput(ValidationMessage);
            return;
        }


        _MovementArea.Clear();
        for (int i = 0; i < Area.Length; i++)
            _MovementArea.Add(Area[i]);

        _SelectedSystemSlotIndex = SystemSlotIndex;
        _MineTargeting = true;
        _UnitDeploymentTargeting = false;
        _SelectedCarriedUnitIndex = -1;
        _DeploymentPreviewDefinitionID = string.Empty;
        _CandidateLegal = false;

        if (Area.Length > 0)
        {
            _CandidatePivot = Area[0];
            RefreshCandidatePresentation();
        }
        else
        {
            PaintMarkers();
            _GameplayHUD.SetPreBattleSelectionVisible(true, true);
        }
    }


    private void BeginUnitDeploymentTargeting(
        int SystemSlotIndex,
        int CarriedUnitIndex)
    {
        string ValidationMessage = string.Empty;


        if (_SelectedUnit == null ||
            _ParticipantAccess == null ||
            !TryGetCarriedUnitDefinitionID(
                SystemSlotIndex,
                CarriedUnitIndex,
                out string DefinitionID) ||
            !_ParticipantAccess.TryGetUnitDeploymentArea(
                _SelectedUnit.RuntimeUnitID,
                SystemSlotIndex,
                CarriedUnitIndex,
                _SelectedUnit.Facing,
                out Vector2Int[] Area,
                out ValidationMessage))
        {
            ReportRejectedInput(
                string.IsNullOrWhiteSpace(ValidationMessage)
                    ? "The carried Unit could not be selected for deployment."
                    : ValidationMessage
            );
            return;
        }


        _MovementArea.Clear();
        for (int i = 0; i < Area.Length; i++)
            _MovementArea.Add(Area[i]);

        _SelectedSystemSlotIndex = SystemSlotIndex;
        _SelectedCarriedUnitIndex = CarriedUnitIndex;
        _DeploymentPreviewDefinitionID = DefinitionID;
        _MineTargeting = false;
        _UnitDeploymentTargeting = true;
        _CandidateFacing = _SelectedUnit.Facing;
        _CandidateLegal = false;


        if (Area.Length > 0)
        {
            _CandidatePivot = Area[0];
            RefreshCandidatePresentation();
        }
        else
        {
            PaintMarkers();
            _GameplayHUD.SetPreBattleSelectionVisible(true, true, true);
        }
    }


    private void TrySetMineCandidate(Vector2Int Coordinate)
    {
        if (!_MineTargeting ||
            !_MovementArea.Contains(Coordinate))
        {
            return;
        }

        _CandidatePivot = Coordinate;
        RefreshCandidatePresentation();
    }


    private void TrySetUnitDeploymentCandidate(Vector2Int Coordinate)
    {
        if (!_UnitDeploymentTargeting ||
            !_MovementArea.Contains(Coordinate))
        {
            return;
        }

        _CandidatePivot = Coordinate;
        RefreshCandidatePresentation();
    }


    private void NudgeCandidate(
        Vector2Int Offset
    )
    {
        if (_SelectedUnit != null &&
            !IsMovementPlaybackLocked())
        {
            TrySetCandidate(
                _CandidatePivot + Offset,
                _CandidateFacing
            );
        }
    }


    private void RotateCandidate(
        bool Clockwise
    )
    {
        if (_SelectedUnit == null)
        {
            return;
        }


        if (IsMovementPlaybackLocked())
        {
            return;
        }


        int Facing =
            (int)_CandidateFacing;

        UnitFacing Rotated =
            (UnitFacing)((Facing + (Clockwise ? 1 : 3)) % 4);


        if (_GameMaster.CurrentPhase == GameplayPhase.PreBattle)
        {
            if (!_UnitDeploymentTargeting)
                return;

            if (!_ParticipantAccess.TryGetUnitDeploymentArea(
                    _SelectedUnit.RuntimeUnitID,
                    _SelectedSystemSlotIndex,
                    _SelectedCarriedUnitIndex,
                    Rotated,
                    out Vector2Int[] Area,
                    out string ValidationMessage))
            {
                if (!string.IsNullOrWhiteSpace(ValidationMessage))
                    ReportRejectedInput(ValidationMessage);
                return;
            }


            _MovementArea.Clear();
            for (int i = 0; i < Area.Length; i++)
                _MovementArea.Add(Area[i]);

            _CandidateFacing = Rotated;
            if (!_MovementArea.Contains(_CandidatePivot) && Area.Length > 0)
                _CandidatePivot = Area[0];

            RefreshCandidatePresentation();
            return;
        }

        TrySetCandidate(
            _CandidatePivot,
            Rotated
        );
    }


    private void TrySetCandidate(
        Vector2Int Pivot,
        UnitFacing Facing
    )
    {
        if (IsMovementPlaybackLocked() ||
            _SelectedDefinition == null ||
            !UnitFootprintGeometry.TryBuildPlacement(
                _SelectedDefinition,
                Pivot,
                Facing,
                out RuntimeUnitTilePlacement[] Placements,
                out _))
        {
            return;
        }


        bool IntersectsManeuverArea =
            false;


        for (int i = 0; i < Placements.Length; i++)
        {
            if (_MovementArea.Contains(
                    Placements[i].MapCoordinate))
            {
                IntersectsManeuverArea =
                    true;

                break;
            }
        }


        // Preview may extend beyond the maneuver area, but never lose contact
        // with it. Confirm remains stricter and requires full legal placement.
        if (!IntersectsManeuverArea)
        {
            return;
        }


        _CandidatePivot =
            Pivot;

        _CandidateFacing =
            Facing;

        RefreshCandidatePresentation();
    }


    private void RefreshCandidatePresentation()
    {
        if (_SelectedUnit == null ||
            _SelectedDefinition == null)
        {
            return;
        }


        if (_GameMaster.CurrentPhase == GameplayPhase.PreBattle)
        {
            if (_MineTargeting)
            {
                _CandidateLegal =
                _ParticipantAccess.ValidateMineDeployment(
                    _SelectedUnit.RuntimeUnitID,
                    _SelectedSystemSlotIndex,
                    _CandidatePivot,
                    out _
                );
            }
            else if (_UnitDeploymentTargeting)
            {
                _CandidateLegal =
                    _ParticipantAccess.ValidateUnitDeployment(
                        _SelectedUnit.RuntimeUnitID,
                        _SelectedSystemSlotIndex,
                        _SelectedCarriedUnitIndex,
                        _CandidatePivot,
                        _CandidateFacing,
                        out _
                    );

                _UnitPresenter.TryShowDeploymentPreview(
                    _DeploymentPreviewDefinitionID,
                    _CandidatePivot,
                    _CandidateFacing,
                    _CandidateLegal
                        ? _LegalPreviewColor
                        : _IllegalPreviewColor
                );
            }
            else
            {
                _CandidateLegal = false;
            }

            PaintMarkers();
            _GameplayHUD.SetPreBattleSelectionVisible(
                true,
                _MineTargeting || _UnitDeploymentTargeting,
                _UnitDeploymentTargeting
            );
            _GameplayHUD.SetMovementSelectionControls(
                _UnitDeploymentTargeting,
                _CandidateLegal
            );
            return;
        }


        _CandidateLegal =
            _ParticipantAccess.ValidateMovement(
                _SelectedUnit.RuntimeUnitID,
                _CandidatePivot,
                _CandidateFacing,
                out GameMovement.MovementPlan Plan,
                out _
            ) &&
            Plan != null;

        _UnitPresenter.TryShowOwnUnitPreview(
            _SelectedUnit.RuntimeUnitID,
            _CandidatePivot,
            _CandidateFacing,
            _CandidateLegal
                ? _LegalPreviewColor
                : _IllegalPreviewColor
        );

        PaintMarkers();

        _GameplayHUD.SetMovementSelectionVisible(true);
        _GameplayHUD.SetMovementSelectionControls(
            true,
            _CandidateLegal
        );
    }


    private void ConfirmMovement()
    {
        if (_GameMaster.CurrentPhase == GameplayPhase.PreBattle)
        {
            if (_MineTargeting)
                ConfirmMineDeployment();
            else if (_UnitDeploymentTargeting)
                ConfirmUnitDeployment();
            return;
        }


        if (_SelectedUnit == null ||
            !_CandidateLegal ||
            IsMovementPlaybackLocked())
        {
            return;
        }


        string RuntimeUnitID =
            _SelectedUnit.RuntimeUnitID;

        _UnitPresenter.ClearOwnUnitPreview(
            RuntimeUnitID
        );


        if (!_ParticipantAccess.TryCommitMovement(
                RuntimeUnitID,
                _CandidatePivot,
                _CandidateFacing,
                out GameMovement.CommitResult Result,
                out string ValidationMessage))
        {
            ReportRejectedInput(ValidationMessage);
            RefreshCandidatePresentation();
            return;
        }


        Debug.Log(
            $"Movement resolved with {Result.Outcome} at {Result.FinalMovementPivotCoordinate}, facing {Result.FinalFacing}.",
            this
        );

        ClearSelection();
    }


    private void ConfirmMineDeployment()
    {
        if (_SelectedUnit == null ||
            !_MineTargeting ||
            !_CandidateLegal ||
            _ParticipantAccess == null)
        {
            return;
        }


        if (!_ParticipantAccess.TryDeployMine(
                _SelectedUnit.RuntimeUnitID,
                _SelectedSystemSlotIndex,
                _CandidatePivot,
                out string ValidationMessage))
        {
            ReportRejectedInput(ValidationMessage);
            RefreshCandidatePresentation();
            return;
        }


        ClearSelection();
    }


    private void ConfirmUnitDeployment()
    {
        if (_SelectedUnit == null ||
            !_UnitDeploymentTargeting ||
            !_CandidateLegal ||
            _ParticipantAccess == null)
        {
            return;
        }


        if (!_ParticipantAccess.TryDeployCarriedUnit(
                _SelectedUnit.RuntimeUnitID,
                _SelectedSystemSlotIndex,
                _SelectedCarriedUnitIndex,
                _CandidatePivot,
                _CandidateFacing,
                out string ValidationMessage))
        {
            ReportRejectedInput(ValidationMessage);
            RefreshCandidatePresentation();
            return;
        }


        ClearSelection();
    }


    private void EndMovement()
    {
        if (_Snapshot?.CurrentPhase == GameplayPhase.PreBattle)
        {
            if (_Snapshot.PreBattle == null ||
                !_Snapshot.PreBattle.CanAct)
            {
                return;
            }

            ClearSelection();

            if (!_ParticipantAccess.TryEndPreBattle(
                    out string PreBattleMessage))
            {
                ReportRejectedInput(PreBattleMessage);
            }

            return;
        }


        if (_Snapshot?.Movement == null ||
            !_Snapshot.Movement.CanAct ||
            IsMovementPlaybackLocked())
        {
            return;
        }


        ClearSelection();


        if (!_ParticipantAccess.TryEndMovement(
                out string ValidationMessage))
        {
            ReportRejectedInput(ValidationMessage);
        }
    }


    private void ClearSelection()
    {
        if (_SelectedUnit != null &&
            _UnitPresenter != null)
        {
            _UnitPresenter.ClearOwnUnitPreview(
                _SelectedUnit.RuntimeUnitID
            );
        }


        if (_UnitPresenter != null)
            _UnitPresenter.ClearDeploymentPreview();


        _SelectedUnit =
            null;

        _SelectedDefinition =
            null;

        _CandidateLegal =
            false;

        _MineTargeting = false;
        _UnitDeploymentTargeting = false;
        _SelectedSystemSlotIndex = -1;
        _SelectedCarriedUnitIndex = -1;
        _DeploymentPreviewDefinitionID = string.Empty;

        _MovementArea.Clear();
        ClearMarkers();


        if (_GameplayHUD != null)
        {
            _GameplayHUD.ClearMovementUnitDetails();
            _GameplayHUD.SetMovementSelectionVisible(false);
            _GameplayHUD.SetPreBattleSelectionVisible(false, false);
            _GameplayHUD.SetMovementSelectionControls(false, false);
        }
    }


    private void RefreshSelectedUnitDetails()
    {
        if (_GameplayHUD == null ||
            _SelectedUnit == null ||
            _SelectedDefinition == null)
        {
            return;
        }

        bool CanAct =
            (_Snapshot?.Movement != null &&
             _Snapshot.Movement.CanAct) ||
            (_Snapshot?.PreBattle != null &&
             _Snapshot.PreBattle.CanAct);

        GameplayPhase Phase =
            _Snapshot != null
                ? _Snapshot.CurrentPhase
                : GameplayPhase.None;

        _GameplayHUD.BindMovementUnitDetails(
            _SelectedUnit,
            _SelectedDefinition,
            _UnitPresentationCatalog,
            Phase,
            CanAct,
            BeginPreBattleSystemTargeting,
            TryUseSelectedAbility
        );
    }


    private void TryUseSelectedAbility(
        int AbilitySlotIndex)
    {
        if (_SelectedUnit == null ||
            _ParticipantAccess == null)
        {
            return;
        }

        if (!_ParticipantAccess.TryUseAbility(
                _SelectedUnit.RuntimeUnitID,
                AbilitySlotIndex,
                out string ValidationMessage))
        {
            ReportRejectedInput(ValidationMessage);
        }
    }


    private bool TryGetCarriedUnitDefinitionID(
        int SystemSlotIndex,
        int CarriedUnitIndex,
        out string DefinitionID
    )
    {
        DefinitionID = string.Empty;

        if (_SelectedUnit?.Systems == null)
            return false;

        for (int i = 0; i < _SelectedUnit.Systems.Length; i++)
        {
            ParticipantSystemSnapshot System = _SelectedUnit.Systems[i];

            if (System == null ||
                System.SlotIndex != SystemSlotIndex ||
                System.CarriedUnits == null)
            {
                continue;
            }

            for (int j = 0; j < System.CarriedUnits.Length; j++)
            {
                ParticipantCarriedUnitSnapshot Carried = System.CarriedUnits[j];

                if (Carried != null &&
                    Carried.CarriedUnitIndex == CarriedUnitIndex &&
                    Carried.State == RuntimeCarriedUnitState.Carried &&
                    !string.IsNullOrWhiteSpace(Carried.DefinitionID))
                {
                    DefinitionID = Carried.DefinitionID;
                    return true;
                }
            }
        }

        return false;
    }


    private void PaintMarkers()
    {
        if (_TileMarkers == null)
        {
            return;
        }


        ClearMarkers();


        foreach (Vector2Int Coordinate in _MovementArea)
        {
            PaintTile(
                Coordinate,
                _MovementAreaTile
            );
        }


        if (_GameMaster.CurrentPhase == GameplayPhase.PreBattle)
        {
            if ((_MineTargeting || _UnitDeploymentTargeting) &&
                _MovementArea.Contains(_CandidatePivot))
            {
                TileBase CandidateTile = _CandidateLegal
                    ? _LegalDestinationTile
                    : _IllegalDestinationTile;

                if (_UnitDeploymentTargeting &&
                    _UnitPresentationCatalog.TryGetPresentation(
                        _DeploymentPreviewDefinitionID,
                        out Definition_Unit DeploymentDefinition,
                        out _) &&
                    UnitFootprintGeometry.TryBuildPlacement(
                        DeploymentDefinition,
                        _CandidatePivot,
                        _CandidateFacing,
                        out RuntimeUnitTilePlacement[] DeploymentPlacements,
                        out _))
                {
                    for (int i = 0; i < DeploymentPlacements.Length; i++)
                        PaintTile(DeploymentPlacements[i].MapCoordinate, CandidateTile);
                }
                else
                {
                    PaintTile(_CandidatePivot, CandidateTile);
                }
            }

            return;
        }


        if (!UnitFootprintGeometry.TryBuildPlacement(
                _SelectedDefinition,
                _CandidatePivot,
                _CandidateFacing,
                out RuntimeUnitTilePlacement[] Placements,
                out _))
        {
            return;
        }


        TileBase DestinationTile =
            _CandidateLegal
                ? _LegalDestinationTile
                : _IllegalDestinationTile;


        for (int i = 0; i < Placements.Length; i++)
        {
            if (_GameMaster.GameMap.IsInsideBounds(
                    Placements[i].MapCoordinate))
            {
                PaintTile(
                    Placements[i].MapCoordinate,
                    DestinationTile
                );
            }
        }
    }


    private void PaintTile(
        Vector2Int Coordinate,
        TileBase Tile
    )
    {
        if (Tile == null)
        {
            return;
        }


        Vector3Int Cell =
            new Vector3Int(
                Coordinate.x,
                Coordinate.y,
                0
            );

        _TileMarkers.SetTile(
            Cell,
            Tile
        );

        _PaintedCells.Add(
            Cell
        );
    }


    private void ClearMarkers()
    {
        if (_TileMarkers != null)
        {
            _CellsToClear.Clear();
            _CellsToClear.AddRange(_PaintedCells);


            for (int i = 0; i < _CellsToClear.Count; i++)
            {
                _TileMarkers.SetTile(
                    _CellsToClear[i],
                    null
                );
            }
        }


        _PaintedCells.Clear();
        _CellsToClear.Clear();
    }


    private bool TryFindOwnUnitAtCoordinate(
        Vector2Int Coordinate,
        out ParticipantUnitSnapshot Found
    )
    {
        Found =
            null;


        if (_Snapshot?.OwnUnits == null)
        {
            return false;
        }


        for (int i = 0; i < _Snapshot.OwnUnits.Length; i++)
        {
            ParticipantUnitSnapshot Unit =
                _Snapshot.OwnUnits[i];


            if (Unit?.OccupiedCoordinates == null)
            {
                continue;
            }


            for (int TileIndex = 0;
                TileIndex < Unit.OccupiedCoordinates.Length;
                TileIndex++)
            {
                if (Unit.OccupiedCoordinates[TileIndex] == Coordinate)
                {
                    Found = Unit;
                    return true;
                }
            }
        }


        return false;
    }


    private bool TryFindOwnUnit(
        string RuntimeUnitID,
        out ParticipantUnitSnapshot Found
    )
    {
        Found =
            null;


        if (_Snapshot?.OwnUnits == null)
        {
            return false;
        }


        for (int i = 0; i < _Snapshot.OwnUnits.Length; i++)
        {
            ParticipantUnitSnapshot Unit =
                _Snapshot.OwnUnits[i];


            if (Unit != null &&
                string.Equals(
                    Unit.RuntimeUnitID,
                    RuntimeUnitID,
                    StringComparison.Ordinal))
            {
                Found = Unit;
                return true;
            }
        }


        return false;
    }


    private static bool HasMoved(
        GameMovement.ParticipantView Movement,
        string RuntimeUnitID
    )
    {
        if (Movement?.Units == null)
        {
            return false;
        }


        for (int i = 0; i < Movement.Units.Length; i++)
        {
            GameMovement.UnitView Unit =
                Movement.Units[i];


            if (Unit != null &&
                Unit.HasMoved &&
                string.Equals(
                    Unit.RuntimeUnitID,
                    RuntimeUnitID,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }


        return false;
    }


    private bool TryGetPointerCoordinate(
        Vector2 ScreenPosition,
        out Vector2Int Coordinate
    )
    {
        Coordinate =
            default;


        if (_GameplayCamera == null ||
            _GameMaster.MapAuthoring == null ||
            _GameMaster.MapAuthoring.GridReference == null)
        {
            return false;
        }


        float MapPlaneZ =
            _GameMaster.MapAuthoring.GridReference.transform.position.z;

        Plane MapPlane =
            new Plane(
                Vector3.forward,
                new Vector3(0f, 0f, MapPlaneZ)
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


        Coordinate =
            _GameMaster.GameMap.WorldToCoordinate(
                PointerRay.GetPoint(Distance)
            );

        return true;
    }


    private static bool IsPointerOverUI()
    {
        return
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject();
    }


    private bool IsMovementPlaybackLocked()
    {
        return
            _UnitPresenter != null &&
            _UnitPresenter.IsPlayingMovementPresentation;
    }


    private void BindButtons()
    {
        if (_ButtonsBound ||
            _GameplayHUD == null)
        {
            return;
        }


        AddButtonListener(_GameplayHUD.MoveUpButton, MoveUp);
        AddButtonListener(_GameplayHUD.MoveDownButton, MoveDown);
        AddButtonListener(_GameplayHUD.MoveLeftButton, MoveLeft);
        AddButtonListener(_GameplayHUD.MoveRightButton, MoveRight);
        AddButtonListener(_GameplayHUD.MovementRotateLeftButton, RotateLeft);
        AddButtonListener(_GameplayHUD.MovementRotateRightButton, RotateRight);
        AddButtonListener(_GameplayHUD.ConfirmMovementButton, ConfirmMovement);
        AddButtonListener(_GameplayHUD.CancelMovementButton, ClearSelection);
        AddButtonListener(_GameplayHUD.EndMovementButton, EndMovement);

        _ButtonsBound =
            true;
    }


    private void UnbindButtons()
    {
        if (!_ButtonsBound ||
            _GameplayHUD == null)
        {
            return;
        }


        RemoveButtonListener(_GameplayHUD.MoveUpButton, MoveUp);
        RemoveButtonListener(_GameplayHUD.MoveDownButton, MoveDown);
        RemoveButtonListener(_GameplayHUD.MoveLeftButton, MoveLeft);
        RemoveButtonListener(_GameplayHUD.MoveRightButton, MoveRight);
        RemoveButtonListener(_GameplayHUD.MovementRotateLeftButton, RotateLeft);
        RemoveButtonListener(_GameplayHUD.MovementRotateRightButton, RotateRight);
        RemoveButtonListener(_GameplayHUD.ConfirmMovementButton, ConfirmMovement);
        RemoveButtonListener(_GameplayHUD.CancelMovementButton, ClearSelection);
        RemoveButtonListener(_GameplayHUD.EndMovementButton, EndMovement);

        _ButtonsBound =
            false;
    }


    private static void AddButtonListener(
        Button Button,
        UnityEngine.Events.UnityAction Action
    )
    {
        if (Button != null)
            Button.onClick.AddListener(Action);
    }


    private static void RemoveButtonListener(
        Button Button,
        UnityEngine.Events.UnityAction Action
    )
    {
        if (Button != null)
            Button.onClick.RemoveListener(Action);
    }


    private void MoveUp() => NudgeCandidate(Vector2Int.up);
    private void MoveDown() => NudgeCandidate(Vector2Int.down);
    private void MoveLeft() => NudgeCandidate(Vector2Int.left);
    private void MoveRight() => NudgeCandidate(Vector2Int.right);
    private void RotateLeft() => RotateCandidate(false);
    private void RotateRight() => RotateCandidate(true);


    private void ReportRejectedInput(
        string ValidationMessage
    )
    {
        if (!string.IsNullOrWhiteSpace(
                ValidationMessage))
        {
            Debug.Log(
                $"Movement input rejected: {ValidationMessage}",
                this
            );
        }
    }


    private bool ValidateReferences()
    {
        bool Valid =
            _GameMaster != null &&
            _HumanGameplayInput != null &&
            _UnitPresenter != null &&
            _UnitPresentationCatalog != null &&
            _GameplayCamera != null &&
            _GameplayHUD != null &&
            _MovementAreaTile != null &&
            _LegalDestinationTile != null &&
            _IllegalDestinationTile != null;


        if (!Valid)
        {
            Debug.LogError(
                "Movement Map Interaction has missing Game, HUD, camera, catalog, presenter, or marker references.",
                this
            );
        }


        return Valid;
    }


    private void OnDisable()
    {
        ClearSelection();
        UnbindButtons();
    }


    private void OnDestroy()
    {
        if (_HumanGameplayInput != null)
        {
            _HumanGameplayInput.ParticipantChanged -=
                HandleParticipantChanged;
        }


        if (_ParticipantAccess != null)
        {
            _ParticipantAccess.SnapshotChanged -=
                HandleSnapshotChanged;
        }


        ClearSelection();
        UnbindButtons();
    }
}
