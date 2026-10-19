using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


// Human End-phase translator. The HUD may select and inspect any owned Unit;
// only plain ability and finish commands cross the participant API boundary.
[DisallowMultipleComponent]
[RequireComponent(typeof(HumanGameplayInput))]
public sealed class EndMapInteraction : MonoBehaviour
{
    [SerializeField] private GameMaster _GameMaster;
    [SerializeField] private HumanGameplayInput _HumanGameplayInput;
    [SerializeField] private UnitPresentationCatalog _UnitPresentationCatalog;
    [SerializeField] private Camera _GameplayCamera;
    [SerializeField] private GameplayHUD _GameplayHUD;

    private GameParticipantAPI.ParticipantAccess _Access;
    private ParticipantGameSnapshot _Snapshot;
    private ParticipantUnitSnapshot _SelectedUnit;
    private Definition_Unit _SelectedDefinition;
    private bool _Initialized;
    private bool _ButtonBound;
    private bool _ReturningToLobby;
    private float _NextTimerRefresh;


    private IEnumerator Start()
    {
        if (!ValidateReferences())
            yield break;
        while (!_GameMaster.InitializationComplete)
            yield return null;

        _Initialized = true;
        _HumanGameplayInput.ParticipantChanged += HandleParticipantChanged;
        BindParticipant();
        BindButton();
        RefreshFromSnapshot();
    }


    private void Update()
    {
        if (!_Initialized || _GameMaster.CurrentPhase != GameplayPhase.End)
            return;

        if (Time.unscaledTime >= _NextTimerRefresh)
        {
            _NextTimerRefresh = Time.unscaledTime + 0.2f;
            RefreshStatusOnly();
        }

        Mouse Mouse = UnityEngine.InputSystem.Mouse.current;
        if (Mouse == null)
            return;
        if (Mouse.rightButton.wasPressedThisFrame)
        {
            ClearSelection();
            return;
        }
        if (Mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI() &&
            TryGetPointerCoordinate(Mouse.position.ReadValue(),
                out Vector2Int Coordinate) &&
            TryFindOwnUnitAtCoordinate(Coordinate,
                out ParticipantUnitSnapshot Unit))
            SelectUnit(Unit);
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
                Debug.LogError(
                    $"End interaction could not read its snapshot: {Message}",
                    this);
            return;
        }

        if (_Snapshot.CurrentPhase != GameplayPhase.End ||
            _Snapshot.End == null)
        {
            ClearSelection();
            return;
        }

        _GameplayHUD.ShowEndPhase();
        _GameplayHUD.SetPlayerName(_Access.DisplayName);

        if (_Snapshot.Match?.MatchComplete == true)
        {
            ClearSelection();
            _GameplayHUD.SetMatchConclusion(_Snapshot.Match);
            return;
        }

        RefreshStatusOnly();

        if (_SelectedUnit != null &&
            TryFindOwnUnit(_SelectedUnit.RuntimeUnitID,
                out ParticipantUnitSnapshot Current))
        {
            _SelectedUnit = Current;
            RefreshSelectedPresentation();
        }
        else if (_SelectedUnit != null)
        {
            ClearSelection();
        }
    }


    private void RefreshStatusOnly()
    {
        if (_Access == null ||
            !_Access.TryGetEndView(out GameEnd.ParticipantView View) ||
            View == null)
            return;
        _GameplayHUD.SetEndStatus(View.ActionsCommitted,
            View.SecondsRemaining, View.CanAct);
    }


    private void SelectUnit(ParticipantUnitSnapshot Unit)
    {
        if (Unit == null || !Unit.IsOnMap ||
            Unit.LifeState != RuntimeUnitLifeState.Active ||
            !_UnitPresentationCatalog.TryGetPresentation(
                Unit.DefinitionID, out Definition_Unit Definition, out _))
            return;

        _SelectedUnit = Unit;
        _SelectedDefinition = Definition;
        RefreshSelectedPresentation();
    }


    private void RefreshSelectedPresentation()
    {
        bool Selected = _SelectedUnit != null;
        bool CanAct = Selected && _Snapshot?.End?.CanAct == true;
        _GameplayHUD.SetEndSelectionVisible(Selected);
        if (!Selected)
            return;
        _GameplayHUD.BindMovementUnitDetails(
            _SelectedUnit, _SelectedDefinition,
            _UnitPresentationCatalog, GameplayPhase.End,
            CanAct, null, TryUseAbility);
    }


    private void TryUseAbility(int SlotIndex)
    {
        if (_SelectedUnit != null &&
            !_Access.TryUseAbility(_SelectedUnit.RuntimeUnitID,
                SlotIndex, out string Message))
            Debug.LogWarning(Message, this);
    }


    private void FinishEnd()
    {
        if (_Snapshot?.Match?.MatchComplete == true)
        {
            ReturnToLobby();
            return;
        }

        if (_Access != null &&
            !_Access.TryFinishEndPhase(out string Message))
            Debug.LogWarning(Message, this);
    }


    private void ReturnToLobby()
    {
        if (_ReturningToLobby)
            return;

        _ReturningToLobby = true;
        if (_GameplayHUD?.FinishEndButton != null)
            _GameplayHUD.FinishEndButton.interactable = false;
        MatchSession.Instance?.ClearMatch();
        SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
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


    private bool TryFindOwnUnit(string RuntimeUnitID,
        out ParticipantUnitSnapshot Found)
    {
        Found = null;
        ParticipantUnitSnapshot[] Units = _Snapshot?.OwnUnits;
        if (Units == null)
            return false;
        for (int i = 0; i < Units.Length; i++)
            if (Units[i] != null &&
                Units[i].RuntimeUnitID == RuntimeUnitID)
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
        Coordinate = _GameMaster.GameMap.WorldToCoordinate(
            Ray.GetPoint(Distance));
        return true;
    }


    private void ClearSelection()
    {
        _SelectedUnit = null;
        _SelectedDefinition = null;
        if (_GameplayHUD != null)
        {
            _GameplayHUD.ClearMovementUnitDetails();
            _GameplayHUD.SetEndSelectionVisible(false);
        }
    }


    private void BindButton()
    {
        if (_ButtonBound || _GameplayHUD?.FinishEndButton == null)
            return;
        _GameplayHUD.FinishEndButton.onClick.AddListener(FinishEnd);
        _ButtonBound = true;
    }


    private void UnbindButton()
    {
        if (!_ButtonBound || _GameplayHUD?.FinishEndButton == null)
            return;
        _GameplayHUD.FinishEndButton.onClick.RemoveListener(FinishEnd);
        _ButtonBound = false;
    }


    private bool ValidateReferences()
    {
        bool Valid = _GameMaster != null && _HumanGameplayInput != null &&
            _UnitPresentationCatalog != null && _GameplayCamera != null &&
            _GameplayHUD != null;
        if (!Valid)
            Debug.LogError(
                "End Map Interaction has missing Game, HUD, camera, or catalog references.",
                this);
        return Valid;
    }


    private static bool IsPointerOverUI() =>
        EventSystem.current != null &&
        EventSystem.current.IsPointerOverGameObject();


    private void OnDisable()
    {
        ClearSelection();
        UnbindButton();
    }


    private void OnDestroy()
    {
        if (_HumanGameplayInput != null)
            _HumanGameplayInput.ParticipantChanged -= HandleParticipantChanged;
        if (_Access != null)
            _Access.SnapshotChanged -= HandleSnapshotChanged;
        ClearSelection();
        UnbindButton();
    }
}
