using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;


// Human Battle-overworld translator. It presents participant-filtered state and
// submits only Unit IDs, domains and known target coordinates to GameMaster.
[DisallowMultipleComponent]
[RequireComponent(typeof(HumanGameplayInput))]
public sealed class BattleMapInteraction : MonoBehaviour
{
    [Header("Game")]
    [SerializeField] private GameMaster _GameMaster;
    [SerializeField] private HumanGameplayInput _HumanGameplayInput;
    [SerializeField] private UnitPresentationCatalog _UnitPresentationCatalog;
    [SerializeField] private Camera _GameplayCamera;
    [SerializeField] private GameplayHUD _GameplayHUD;

    [Header("Map Presentation")]
    [SerializeField] private TileBase _FireControlTile;
    [SerializeField] private TileBase _CrosshairTile;
    [SerializeField] private TileBase _AirContactTile;
    [SerializeField] private TileBase _SurfaceContactTile;
    [SerializeField] private TileBase _SubsurfaceContactTile;

    private readonly HashSet<Vector3Int> _FireControlCells = new HashSet<Vector3Int>();
    private static readonly Domain[] Domains =
        { Domain.Air, Domain.Surface, Domain.Subsurface };

    private GameParticipantAPI.ParticipantAccess _Access;
    private ParticipantGameSnapshot _Snapshot;
    private ParticipantUnitSnapshot _SelectedUnit;
    private Definition_Unit _SelectedDefinition;
    private GameBattle.TargetView _SelectedTarget;
    private Vector2Int _SelectedTargetCoordinate;
    private Domain _SelectedDomain = Domain.None;
    private Vector3Int? _HoverCell;
    private Tilemap _Markers;
    private Tilemap _VFX;
    private bool _Initialized;
    private bool _ButtonsBound;
    private float _NextTimerRefresh;


    private IEnumerator Start()
    {
        if (!ValidateReferences())
            yield break;
        while (!_GameMaster.InitializationComplete)
            yield return null;

        _Markers = _GameMaster.MapAuthoring.TileMarkers;
        _VFX = _GameMaster.MapAuthoring.VFX;
        _Initialized = true;
        _HumanGameplayInput.ParticipantChanged += HandleParticipantChanged;
        BindParticipant();
        BindButtons();
        RefreshFromSnapshot();
    }


    private void Update()
    {
        if (!_Initialized || _GameMaster.CurrentPhase != GameplayPhase.Battle)
            return;

        if (Time.unscaledTime >= _NextTimerRefresh)
        {
            _NextTimerRefresh = Time.unscaledTime + 0.2f;
            RefreshStatusOnly();
        }

        if (_Snapshot?.Battle?.EngagementActive == true)
        {
            ClearHover();
            return;
        }

        Mouse Mouse = UnityEngine.InputSystem.Mouse.current;
        if (Mouse == null)
            return;

        if (!IsPointerOverUI() &&
            TryGetPointerCoordinate(Mouse.position.ReadValue(), out Vector2Int Hover))
            RefreshHover(Hover);
        else
            ClearHover();

        if (Mouse.rightButton.wasPressedThisFrame)
        {
            ClearSelection();
            return;
        }

        if (!Mouse.leftButton.wasPressedThisFrame || IsPointerOverUI() ||
            !TryGetPointerCoordinate(Mouse.position.ReadValue(), out Vector2Int Coordinate))
            return;

        if (TryFindOwnUnitAtCoordinate(Coordinate, out ParticipantUnitSnapshot Unit))
            SelectUnit(Unit);
        else
            SelectTarget(Coordinate);
    }


    private void BindParticipant()
    {
        if (_Access != null)
            _Access.SnapshotChanged -= HandleSnapshotChanged;
        _Access = _HumanGameplayInput.ParticipantAccess;
        if (_Access != null)
            _Access.SnapshotChanged += HandleSnapshotChanged;
    }


    private void HandleParticipantChanged()
    {
        ClearSelection();
        BindParticipant();
        RefreshFromSnapshot();
    }


    private void HandleSnapshotChanged() => RefreshFromSnapshot();


    private void RefreshFromSnapshot()
    {
        string Message = string.Empty;
        if (!_Initialized || _Access == null ||
            !_Access.TryGetSnapshot(out _Snapshot, out Message))
        {
            if (!string.IsNullOrWhiteSpace(Message))
                Debug.LogError($"Battle interaction could not read its snapshot: {Message}", this);
            return;
        }

        if (_Snapshot.CurrentPhase != GameplayPhase.Battle || _Snapshot.Battle == null)
        {
            ClearSelection();
            return;
        }

        _GameplayHUD.ShowBattlePhase();
        _GameplayHUD.SetPlayerName(_Access.DisplayName);
        RefreshStatusOnly();

        if (_Snapshot.Battle.EngagementActive)
        {
            ClearMapPresentation();
            _GameplayHUD.SetBattleEngagementVisible(true);
            return;
        }

        if (!_Snapshot.Battle.CanAct)
        {
            ClearSelection();
            return;
        }

        if (_SelectedUnit != null &&
            TryFindOwnUnit(_SelectedUnit.RuntimeUnitID, out ParticipantUnitSnapshot Current) &&
            !HasAttacked(Current.RuntimeUnitID))
        {
            _SelectedUnit = Current;
            RefreshSelectedPresentation();
        }
        else if (_SelectedUnit != null)
            ClearSelection();
    }


    private void RefreshStatusOnly()
    {
        if (_Access == null || !_Access.TryGetBattleView(out GameBattle.ParticipantView View) ||
            View == null)
            return;
        _GameplayHUD.SetBattleStatus(View.AttacksCommitted, View.SecondsRemaining,
            View.CanAct && !View.EngagementActive);
    }


    private void SelectUnit(ParticipantUnitSnapshot Unit)
    {
        if (Unit == null || !Unit.IsOnMap ||
            Unit.LifeState != RuntimeUnitLifeState.Active || HasAttacked(Unit.RuntimeUnitID) ||
            !_UnitPresentationCatalog.TryGetPresentation(
                Unit.DefinitionID, out Definition_Unit Definition, out _))
            return;

        ClearSelection();
        _SelectedUnit = Unit;
        _SelectedDefinition = Definition;
        SelectFirstAvailableDomain();
        RefreshSelectedPresentation();
    }


    private void SelectFirstAvailableDomain()
    {
        _SelectedDomain = Domain.None;
        for (int i = 0; i < Domains.Length; i++)
            if (TryPaintDomain(Domains[i]))
            {
                _SelectedDomain = Domains[i];
                return;
            }
    }


    private void CycleDomain(int Direction)
    {
        if (_SelectedUnit == null)
            return;
        int Start = Array.IndexOf(Domains, _SelectedDomain);
        if (Start < 0)
            Start = 0;
        for (int Step = 1; Step <= Domains.Length; Step++)
        {
            int Index = (Start + Direction * Step + Domains.Length * 2) % Domains.Length;
            if (TryPaintDomain(Domains[Index]))
            {
                _SelectedDomain = Domains[Index];
                _SelectedTarget = null;
                RefreshSelectedPresentation();
                return;
            }
        }
    }


    private void SelectDomain(Domain Domain)
    {
        if (_SelectedUnit == null || !TryPaintDomain(Domain))
            return;
        _SelectedDomain = Domain;
        _SelectedTarget = null;
        RefreshSelectedPresentation();
    }


    private void DeselectTarget()
    {
        _SelectedTarget = null;
        RefreshSelectedPresentation();
    }


    private bool TryPaintDomain(Domain Domain)
    {
        ClearFireControl();
        if (_SelectedUnit == null ||
            !_Access.TryGetFireControlArea(_SelectedUnit.RuntimeUnitID, Domain,
                out Vector2Int[] Coordinates, out _))
            return false;
        for (int i = 0; i < Coordinates.Length; i++)
        {
            Vector3Int Cell = ToCell(Coordinates[i]);
            _Markers.SetTile(Cell, _FireControlTile);
            _FireControlCells.Add(Cell);
        }
        return Coordinates.Length > 0;
    }


    private void SelectTarget(Vector2Int Coordinate)
    {
        _SelectedTarget = null;
        if (_SelectedUnit != null &&
            _Access.TryGetBattleTarget(_SelectedUnit.RuntimeUnitID, Coordinate,
                out GameBattle.TargetView Target, out _))
        {
            _SelectedTarget = Target;
            _SelectedTargetCoordinate = Coordinate;
        }
        RefreshSelectedPresentation();
    }


    private void BeginEngagement()
    {
        if (_SelectedUnit == null || _SelectedTarget == null)
            return;
        if (!_Access.TryBeginEngagement(_SelectedUnit.RuntimeUnitID,
                _SelectedTargetCoordinate, out string Message))
            Debug.LogWarning(Message, this);
    }


    private void EndBattle()
    {
        if (!_Access.TryEndBattle(out string Message))
            Debug.LogWarning(Message, this);
    }


    private void RefreshSelectedPresentation()
    {
        bool Selected = _SelectedUnit != null;
        _GameplayHUD.SetBattleSelectionVisible(Selected);
        if (Selected)
            _GameplayHUD.BindMovementUnitDetails(_SelectedUnit, _SelectedDefinition,
                _UnitPresentationCatalog, GameplayPhase.Battle,
                _Snapshot?.Battle?.CanAct == true, null, null);
        _GameplayHUD.SetBattleControls(_SelectedDomain, Selected,
            _SelectedTarget);
    }


    private void RefreshHover(Vector2Int Coordinate)
    {
        if (_SelectedUnit == null ||
            !_Access.TryGetBattleTarget(_SelectedUnit.RuntimeUnitID, Coordinate,
                out GameBattle.TargetView Target, out _))
        {
            ClearHover();
            return;
        }
        Vector3Int Cell = ToCell(Target.Coordinate);
        if (_HoverCell == Cell)
            return;
        ClearHover();
        _VFX.SetTile(Cell, _CrosshairTile);
        _HoverCell = Cell;
    }


    private void ClearHover()
    {
        if (!_HoverCell.HasValue || _VFX == null)
            return;
        Vector3Int Cell = _HoverCell.Value;
        _VFX.SetTile(Cell, ResolveContactTile(new Vector2Int(Cell.x, Cell.y)));
        _HoverCell = null;
    }


    private TileBase ResolveContactTile(Vector2Int Coordinate)
    {
        ParticipantContactSnapshot[] Contacts = _Snapshot?.Contacts;
        if (Contacts == null)
            return null;
        for (int i = 0; i < Contacts.Length; i++)
            if (Contacts[i] != null && Contacts[i].Coordinate == Coordinate)
                return Contacts[i].Domain == Domain.Air ? _AirContactTile :
                    Contacts[i].Domain == Domain.Subsurface ? _SubsurfaceContactTile :
                    _SurfaceContactTile;
        return null;
    }


    private bool HasAttacked(string RuntimeUnitID)
    {
        GameBattle.UnitView[] Units = _Snapshot?.Battle?.Units;
        if (Units == null)
            return false;
        for (int i = 0; i < Units.Length; i++)
            if (Units[i] != null && Units[i].RuntimeUnitID == RuntimeUnitID)
                return Units[i].HasAttacked;
        return false;
    }


    private bool TryFindOwnUnitAtCoordinate(Vector2Int Coordinate,
        out ParticipantUnitSnapshot Found)
    {
        Found = null;
        ParticipantUnitSnapshot[] Units = _Snapshot?.OwnUnits;
        if (Units == null)
            return false;
        for (int i = 0; i < Units.Length; i++)
        {
            Vector2Int[] Occupied = Units[i]?.OccupiedCoordinates;
            if (Occupied == null)
                continue;
            for (int c = 0; c < Occupied.Length; c++)
                if (Occupied[c] == Coordinate)
                {
                    Found = Units[i];
                    return true;
                }
        }
        return false;
    }


    private bool TryFindOwnUnit(string RuntimeUnitID, out ParticipantUnitSnapshot Found)
    {
        Found = null;
        ParticipantUnitSnapshot[] Units = _Snapshot?.OwnUnits;
        if (Units == null)
            return false;
        for (int i = 0; i < Units.Length; i++)
            if (Units[i] != null && Units[i].RuntimeUnitID == RuntimeUnitID)
            {
                Found = Units[i];
                return true;
            }
        return false;
    }


    private bool TryGetPointerCoordinate(Vector2 ScreenPosition,
        out Vector2Int Coordinate)
    {
        Coordinate = default;
        Grid Grid = _GameMaster.MapAuthoring?.GridReference;
        if (_GameplayCamera == null || Grid == null)
            return false;
        Plane Plane = new Plane(Vector3.forward,
            new Vector3(0f, 0f, Grid.transform.position.z));
        Ray Ray = _GameplayCamera.ScreenPointToRay(ScreenPosition);
        if (!Plane.Raycast(Ray, out float Distance))
            return false;
        Coordinate = _GameMaster.GameMap.WorldToCoordinate(Ray.GetPoint(Distance));
        return true;
    }


    private void ClearSelection()
    {
        _SelectedUnit = null;
        _SelectedDefinition = null;
        _SelectedTarget = null;
        _SelectedDomain = Domain.None;
        ClearMapPresentation();
        if (_GameplayHUD != null)
        {
            _GameplayHUD.ClearMovementUnitDetails();
            _GameplayHUD.SetBattleSelectionVisible(false);
            _GameplayHUD.SetBattleControls(Domain.None, false, null);
        }
    }


    private void ClearMapPresentation()
    {
        ClearHover();
        ClearFireControl();
    }


    private void ClearFireControl()
    {
        if (_Markers != null)
            foreach (Vector3Int Cell in _FireControlCells)
                _Markers.SetTile(Cell, null);
        _FireControlCells.Clear();
    }


    private void BindButtons()
    {
        if (_ButtonsBound || _GameplayHUD == null)
            return;
        AddListener(_GameplayHUD.BattlePreviousDomainButton, () => CycleDomain(-1));
        AddListener(_GameplayHUD.BattleNextDomainButton, () => CycleDomain(1));
        AddListener(_GameplayHUD.BattleAirDomainButton, () => SelectDomain(Domain.Air));
        AddListener(_GameplayHUD.BattleSurfaceDomainButton, () => SelectDomain(Domain.Surface));
        AddListener(_GameplayHUD.BattleSubsurfaceDomainButton,
            () => SelectDomain(Domain.Subsurface));
        AddListener(_GameplayHUD.BattleDeselectButton, DeselectTarget);
        AddListener(_GameplayHUD.EngageButton, BeginEngagement);
        AddListener(_GameplayHUD.EndBattleButton, EndBattle);
        _ButtonsBound = true;
    }


    private void UnbindButtons()
    {
        if (!_ButtonsBound || _GameplayHUD == null)
            return;
        _GameplayHUD.BattlePreviousDomainButton?.onClick.RemoveAllListeners();
        _GameplayHUD.BattleNextDomainButton?.onClick.RemoveAllListeners();
        _GameplayHUD.BattleAirDomainButton?.onClick.RemoveAllListeners();
        _GameplayHUD.BattleSurfaceDomainButton?.onClick.RemoveAllListeners();
        _GameplayHUD.BattleSubsurfaceDomainButton?.onClick.RemoveAllListeners();
        _GameplayHUD.BattleDeselectButton?.onClick.RemoveAllListeners();
        _GameplayHUD.EngageButton?.onClick.RemoveAllListeners();
        _GameplayHUD.EndBattleButton?.onClick.RemoveAllListeners();
        _ButtonsBound = false;
    }


    private static void AddListener(Button Button, UnityEngine.Events.UnityAction Action)
    {
        if (Button != null)
            Button.onClick.AddListener(Action);
    }


    private bool ValidateReferences()
    {
        bool Valid = _GameMaster != null && _HumanGameplayInput != null &&
            _UnitPresentationCatalog != null && _GameplayCamera != null &&
            _GameplayHUD != null && _FireControlTile != null &&
            _CrosshairTile != null && _AirContactTile != null &&
            _SurfaceContactTile != null && _SubsurfaceContactTile != null;
        if (!Valid)
            Debug.LogError("Battle Map Interaction has missing Game, HUD, tile, camera, or catalog references.", this);
        return Valid;
    }


    private static bool IsPointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    private static Vector3Int ToCell(Vector2Int Coordinate) =>
        new Vector3Int(Coordinate.x, Coordinate.y, 0);


    private void OnDisable()
    {
        ClearSelection();
        UnbindButtons();
    }


    private void OnDestroy()
    {
        if (_HumanGameplayInput != null)
            _HumanGameplayInput.ParticipantChanged -= HandleParticipantChanged;
        if (_Access != null)
            _Access.SnapshotChanged -= HandleSnapshotChanged;
        ClearSelection();
        UnbindButtons();
    }
}
