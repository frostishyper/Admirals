using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;


// Human Search-phase translator. Preview is presentation-only; the selected
// Unit, sensor slot and facing are submitted through participant-bound access.
[DisallowMultipleComponent]
[RequireComponent(typeof(HumanGameplayInput))]
public sealed class SearchMapInteraction : MonoBehaviour
{
    [Header("Game")]
    [SerializeField] private GameMaster _GameMaster;
    [SerializeField] private HumanGameplayInput _HumanGameplayInput;
    [SerializeField] private UnitPresentationCatalog _UnitPresentationCatalog;
    [SerializeField] private Camera _GameplayCamera;
    [SerializeField] private GameplayHUD _GameplayHUD;

    [Header("Map Presentation")]
    [SerializeField] private TileBase _SearchPreviewTile;
    [SerializeField] private TileBase _SearchAnimatedTile;
    [SerializeField] private TileBase _AirContactTile;
    [SerializeField] private TileBase _SurfaceContactTile;
    [SerializeField] private TileBase _SubsurfaceContactTile;
    [SerializeField] private TMP_Text _StrengthLabelPrefab;
    [SerializeField, Min(0.1f)] private float _SearchPlaybackSeconds = 2f;

    private readonly HashSet<Vector3Int> _PreviewCells = new HashSet<Vector3Int>();
    private readonly HashSet<Vector3Int> _ContactCells = new HashSet<Vector3Int>();
    private readonly List<TMP_Text> _StrengthLabels = new List<TMP_Text>();

    private GameParticipantAPI.ParticipantAccess _Access;
    private ParticipantGameSnapshot _Snapshot;
    private ParticipantUnitSnapshot _SelectedUnit;
    private Definition_Unit _SelectedDefinition;
    private GameSearch.SearchTile[] _Preview = Array.Empty<GameSearch.SearchTile>();
    private int _SelectedSensorSlot = -1;
    private UnitFacing _SearchFacing;
    private Tilemap _Markers;
    private Tilemap _VFX;
    private bool _Initialized;
    private bool _ButtonsBound;
    private bool _PlaybackLocked;
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
        if (!_Initialized || _GameMaster.CurrentPhase != GameplayPhase.Search)
            return;

        if (Time.unscaledTime >= _NextTimerRefresh)
        {
            _NextTimerRefresh = Time.unscaledTime + 0.2f;
            RefreshStatusOnly();
        }

        if (_PlaybackLocked)
            return;

        Mouse Mouse = UnityEngine.InputSystem.Mouse.current;
        if (Mouse == null)
            return;
        if (Mouse.rightButton.wasPressedThisFrame)
        {
            ClearSelection();
            return;
        }
        if (Mouse.leftButton.wasPressedThisFrame &&
            !IsPointerOverUI() &&
            TryGetPointerCoordinate(Mouse.position.ReadValue(), out Vector2Int Coordinate) &&
            TryFindOwnUnitAtCoordinate(Coordinate, out ParticipantUnitSnapshot Unit))
            SelectUnit(Unit);
    }


    private void BindParticipant()
    {
        if (_Access != null)
        {
            _Access.SnapshotChanged -= HandleSnapshotChanged;
            _Access.SearchPresented -= HandleSearchPresented;
        }
        _Access = _HumanGameplayInput.ParticipantAccess;
        if (_Access != null)
        {
            _Access.SnapshotChanged += HandleSnapshotChanged;
            _Access.SearchPresented += HandleSearchPresented;
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


    private void RefreshFromSnapshot()
    {
        string Message = string.Empty;
        if (!_Initialized || _Access == null ||
            !_Access.TryGetSnapshot(out _Snapshot, out Message))
        {
            if (!string.IsNullOrWhiteSpace(Message))
                Debug.LogError($"Search interaction could not read its snapshot: {Message}", this);
            return;
        }

        PaintContacts();
        if (_Snapshot.CurrentPhase != GameplayPhase.Search || _Snapshot.Search == null)
        {
            ClearSelection();
            return;
        }

        _GameplayHUD.ShowSearchPhase();
        _GameplayHUD.SetPlayerName(_Access.DisplayName);
        RefreshStatusOnly();

        if (!_Snapshot.Search.CanAct)
        {
            ClearSelection();
            return;
        }

        if (_SelectedUnit != null &&
            TryFindOwnUnit(_SelectedUnit.RuntimeUnitID, out ParticipantUnitSnapshot Current))
        {
            _SelectedUnit = Current;
            if (HasSearched(Current.RuntimeUnitID))
                ClearSelection();
            else
                RefreshSelectedPresentation();
        }
    }


    private void RefreshStatusOnly()
    {
        if (_Access == null || !_Access.TryGetSearchView(out GameSearch.ParticipantView View) ||
            View == null)
            return;
        _GameplayHUD.SetSearchStatus(View.SearchesCommitted, View.SearchLimit,
            View.SecondsRemaining, View.CanEndSearch && !_PlaybackLocked);
    }


    private void SelectUnit(ParticipantUnitSnapshot Unit)
    {
        if (Unit == null || !Unit.IsOnMap ||
            Unit.LifeState != RuntimeUnitLifeState.Active ||
            HasSearched(Unit.RuntimeUnitID) ||
            !_UnitPresentationCatalog.TryGetPresentation(
                Unit.DefinitionID, out Definition_Unit Definition, out _))
            return;

        ClearSelection();
        _SelectedUnit = Unit;
        _SelectedDefinition = Definition;
        _SearchFacing = Unit.Facing;
        RefreshSelectedPresentation();
    }


    private void SelectSensor(int SlotIndex)
    {
        if (_SelectedUnit == null)
            return;
        _SelectedSensorSlot = SlotIndex;
        RefreshPattern();
        RefreshSelectedPresentation();
    }


    private void RotateSearch(bool Clockwise)
    {
        if (_SelectedSensorSlot < 0 || _PlaybackLocked)
            return;
        int Value = (int)_SearchFacing;
        Value = Clockwise ? (Value + 1) % 4 : (Value + 3) % 4;
        _SearchFacing = (UnitFacing)Value;
        RefreshPattern();
    }


    private void RefreshPattern()
    {
        ClearPreview();
        string Message = string.Empty;
        if (_SelectedUnit == null || _SelectedSensorSlot < 0 ||
            !_Access.TryGetSearchPattern(_SelectedUnit.RuntimeUnitID,
                _SelectedSensorSlot, _SearchFacing, out _Preview, out Message))
        {
            if (!string.IsNullOrWhiteSpace(Message))
                Debug.LogWarning(Message, this);
            _Preview = Array.Empty<GameSearch.SearchTile>();
            _GameplayHUD.SetSearchControls(false, false);
            return;
        }

        for (int i = 0; i < _Preview.Length; i++)
        {
            GameSearch.SearchTile Tile = _Preview[i];
            Vector3Int Cell = ToCell(Tile.Coordinate);
            _Markers.SetTile(Cell, _SearchPreviewTile);
            _PreviewCells.Add(Cell);
            TMP_Text Label = Instantiate(_StrengthLabelPrefab,
                _GameMaster.MapAuthoring.transform);
            Label.name = $"SearchStrength_{Tile.Coordinate.x}_{Tile.Coordinate.y}";
            Label.text = Tile.Strength.ToString();
            Label.transform.position = _GameMaster.MapAuthoring.GridReference
                .GetCellCenterWorld(Cell);
            _StrengthLabels.Add(Label);
        }
        _GameplayHUD.SetSearchControls(true, _Preview.Length > 0);
    }


    private void CommitSearch()
    {
        if (_SelectedUnit == null || _SelectedSensorSlot < 0 || _PlaybackLocked)
            return;
        if (!_Access.TryCommitSearch(_SelectedUnit.RuntimeUnitID,
                _SelectedSensorSlot, _SearchFacing,
                out _, out string Message))
            Debug.LogWarning(Message, this);
    }


    private void EndSearch()
    {
        if (_PlaybackLocked)
            return;
        if (!_Access.TryEndSearch(out string Message))
            Debug.LogWarning(Message, this);
    }


    private void TryUseAbility(int SlotIndex)
    {
        if (_SelectedUnit != null &&
            !_Access.TryUseAbility(_SelectedUnit.RuntimeUnitID, SlotIndex,
                out string Message))
            Debug.LogWarning(Message, this);
    }


    private void HandleSearchPresented(GameSearch.CommitResult Result)
    {
        if (Result != null)
            StartCoroutine(PlaySearch(Result));
    }


    private IEnumerator PlaySearch(GameSearch.CommitResult Result)
    {
        _PlaybackLocked = true;
        ClearPreview();
        ClearSelection(false);
        for (int i = 0; i < Result.Tiles.Length; i++)
            _VFX.SetTile(ToCell(Result.Tiles[i].Coordinate), _SearchAnimatedTile);

        yield return new WaitForSecondsRealtime(_SearchPlaybackSeconds);

        for (int i = 0; i < Result.Tiles.Length; i++)
            _VFX.SetTile(ToCell(Result.Tiles[i].Coordinate), null);
        _PlaybackLocked = false;
        PaintContacts();
        RefreshFromSnapshot();
    }


    private void RefreshSelectedPresentation()
    {
        bool Selected = _SelectedUnit != null;
        bool CanAct = Selected && _Snapshot?.Search != null &&
            _Snapshot.Search.CanAct && !HasSearched(_SelectedUnit.RuntimeUnitID);
        _GameplayHUD.SetSearchSelectionVisible(Selected, _SelectedSensorSlot >= 0);
        if (!Selected)
            return;
        _GameplayHUD.BindMovementUnitDetails(_SelectedUnit, _SelectedDefinition,
            _UnitPresentationCatalog, GameplayPhase.Search, CanAct,
            null, TryUseAbility, SelectSensor, _SelectedSensorSlot);
        _GameplayHUD.SetSearchControls(CanAct && _SelectedSensorSlot >= 0,
            CanAct && _SelectedSensorSlot >= 0 && _Preview.Length > 0);
    }


    private void PaintContacts()
    {
        if (_PlaybackLocked || _VFX == null || _Snapshot == null)
            return;
        foreach (Vector3Int Cell in _ContactCells)
            _VFX.SetTile(Cell, null);
        _ContactCells.Clear();
        if (_Snapshot.Contacts == null)
            return;
        for (int i = 0; i < _Snapshot.Contacts.Length; i++)
        {
            ParticipantContactSnapshot Contact = _Snapshot.Contacts[i];
            TileBase Tile = Contact.Domain == Domain.Air ? _AirContactTile :
                Contact.Domain == Domain.Subsurface ? _SubsurfaceContactTile :
                _SurfaceContactTile;
            Vector3Int Cell = ToCell(Contact.Coordinate);
            _VFX.SetTile(Cell, Tile);
            _ContactCells.Add(Cell);
        }
    }


    private bool HasSearched(string RuntimeUnitID)
    {
        GameSearch.UnitView[] Units = _Snapshot?.Search?.Units;
        if (Units == null)
            return false;
        for (int i = 0; i < Units.Length; i++)
            if (Units[i] != null && Units[i].RuntimeUnitID == RuntimeUnitID)
                return Units[i].HasSearched;
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


    private void ClearSelection(bool ClearDetails = true)
    {
        _SelectedUnit = null;
        _SelectedDefinition = null;
        _SelectedSensorSlot = -1;
        _Preview = Array.Empty<GameSearch.SearchTile>();
        ClearPreview();
        if (_GameplayHUD != null)
        {
            if (ClearDetails)
                _GameplayHUD.ClearMovementUnitDetails();
            _GameplayHUD.SetSearchSelectionVisible(false, false);
            _GameplayHUD.SetSearchControls(false, false);
        }
    }


    private void ClearPreview()
    {
        if (_Markers != null)
            foreach (Vector3Int Cell in _PreviewCells)
                _Markers.SetTile(Cell, null);
        _PreviewCells.Clear();
        for (int i = 0; i < _StrengthLabels.Count; i++)
            if (_StrengthLabels[i] != null)
                Destroy(_StrengthLabels[i].gameObject);
        _StrengthLabels.Clear();
    }


    private void BindButtons()
    {
        if (_ButtonsBound || _GameplayHUD == null)
            return;
        AddListener(_GameplayHUD.SearchRotateLeftButton, () => RotateSearch(false));
        AddListener(_GameplayHUD.SearchRotateRightButton, () => RotateSearch(true));
        AddListener(_GameplayHUD.CommitSearchButton, CommitSearch);
        AddListener(_GameplayHUD.EndSearchButton, EndSearch);
        _ButtonsBound = true;
    }


    private void UnbindButtons()
    {
        if (!_ButtonsBound || _GameplayHUD == null)
            return;
        _GameplayHUD.SearchRotateLeftButton?.onClick.RemoveAllListeners();
        _GameplayHUD.SearchRotateRightButton?.onClick.RemoveAllListeners();
        _GameplayHUD.CommitSearchButton?.onClick.RemoveAllListeners();
        _GameplayHUD.EndSearchButton?.onClick.RemoveAllListeners();
        _ButtonsBound = false;
    }


    private static void AddListener(Button Button, UnityEngine.Events.UnityAction Action)
    {
        if (Button != null)
            Button.onClick.AddListener(Action);
    }


    private static bool IsPointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    private static Vector3Int ToCell(Vector2Int Coordinate) =>
        new Vector3Int(Coordinate.x, Coordinate.y, 0);


    private bool ValidateReferences()
    {
        bool Valid = _GameMaster != null && _HumanGameplayInput != null &&
            _UnitPresentationCatalog != null && _GameplayCamera != null &&
            _GameplayHUD != null && _SearchPreviewTile != null &&
            _SearchAnimatedTile != null && _AirContactTile != null &&
            _SurfaceContactTile != null && _SubsurfaceContactTile != null &&
            _StrengthLabelPrefab != null;
        if (!Valid)
            Debug.LogError("Search Map Interaction has missing Game, HUD, tile, label, camera, or catalog references.", this);
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
            _HumanGameplayInput.ParticipantChanged -= HandleParticipantChanged;
        if (_Access != null)
        {
            _Access.SnapshotChanged -= HandleSnapshotChanged;
            _Access.SearchPresented -= HandleSearchPresented;
        }
        ClearSelection();
        UnbindButtons();
    }
}
