using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Synchronizes world Unit views from one participant-filtered snapshot.
// Owns no Unit state and never reads opponent runtime objects directly.
[DisallowMultipleComponent]
[RequireComponent(typeof(HumanGameplayInput))]
public sealed class ParticipantUnitPresenter : MonoBehaviour
{
    private sealed class PresentedUnit
    {
        public UnitView View;
        public Definition_Unit Definition;
        public Vector3 TargetPosition;
        public UnitFacing AuthoritativeFacing;
        public bool IsPreviewing;
        public bool IsAnimating;
    }


    [SerializeField]
    private UnitPresentationCatalog _UnitPresentationCatalog;

    [Min(0f)]
    [SerializeField]
    private float _PositionInterpolationSeconds = 0.15f;

    [Min(0.01f)]
    [SerializeField]
    private float _TranslationStepSeconds = 0.18f;

    [Min(0.01f)]
    [SerializeField]
    private float _TurnStepSeconds = 0.12f;


    private readonly Dictionary<string, PresentedUnit> _PresentedUnits =
        new Dictionary<string, PresentedUnit>();

    private readonly HashSet<string> _ExpectedKeys =
        new HashSet<string>();

    private readonly List<string> _RemovalKeys =
        new List<string>();

    private readonly HashSet<string> _ReportedMissingDefinitions =
        new HashSet<string>();

    private readonly Queue<GameParticipantAPI.MovementPresentation>
        _MovementPresentationQueue =
            new Queue<GameParticipantAPI.MovementPresentation>();


    private HumanGameplayInput _HumanGameplayInput;
    private GameParticipantAPI.ParticipantAccess _ParticipantAccess;
    private GameMaster _GameMaster;
    private Transform _UnitsRoot;
    private bool _Initialized;
    private Coroutine _MovementPlayback;
    private UnitView _DeploymentPreview;
    private Definition_Unit _DeploymentPreviewDefinition;


    public bool IsPlayingMovementPresentation =>
        _MovementPlayback != null;

    public event Action<bool> MovementPlaybackChanged;


    private IEnumerator Start()
    {
        _HumanGameplayInput =
            GetComponent<HumanGameplayInput>();


        if (!ValidateReferences())
        {
            yield break;
        }


        _GameMaster =
            _HumanGameplayInput.GameMaster;

        _HumanGameplayInput.ParticipantChanged +=
            HandleParticipantChanged;


        while (!_GameMaster.InitializationComplete)
        {
            if (!_GameMaster.enabled)
            {
                Debug.LogError(
                    "Participant Unit Presenter could not initialize because GameMaster initialization failed.",
                    this
                );

                yield break;
            }


            yield return null;
        }


        if (!_HumanGameplayInput.Initialize() ||
            _GameMaster.MapAuthoring == null ||
            _GameMaster.MapAuthoring.Units == null)
        {
            Debug.LogError(
                "Participant Unit Presenter requires initialized participant access and the runtime Map Units root.",
                this
            );

            yield break;
        }


        _UnitsRoot =
            _GameMaster.MapAuthoring.Units;

        _Initialized = true;
        BindParticipantAccess();
        RefreshPresentation(true);
    }


    private void Update()
    {
        if (!_Initialized ||
            _PositionInterpolationSeconds <= 0f)
        {
            return;
        }


        float Interpolation =
            1f - Mathf.Exp(
                -Time.deltaTime *
                6f /
                _PositionInterpolationSeconds
            );


        foreach (PresentedUnit Presented
            in _PresentedUnits.Values)
        {
            if (Presented?.View == null)
            {
                continue;
            }


            if (Presented.IsPreviewing ||
                Presented.IsAnimating)
            {
                continue;
            }


            Transform ViewTransform =
                Presented.View.transform;

            ViewTransform.position =
                Vector3.Lerp(
                    ViewTransform.position,
                    Presented.TargetPosition,
                    Interpolation
                );


            if ((ViewTransform.position - Presented.TargetPosition).sqrMagnitude < 0.000001f)
            {
                ViewTransform.position =
                    Presented.TargetPosition;
            }
        }
    }


    private void HandleParticipantChanged()
    {
        if (!_Initialized)
        {
            return;
        }


        CancelMovementPlayback();
        UnbindParticipantAccess();
        ClearPresentedUnits();
        BindParticipantAccess();
        RefreshPresentation(true);
    }


    private void HandleSnapshotChanged()
    {
        RefreshPresentation(false);
    }


    private void HandleMovementPresented(
        GameParticipantAPI.MovementPresentation Presentation)
    {
        if (!_Initialized ||
            Presentation == null ||
            Presentation.Segments == null ||
            Presentation.Segments.Length == 0)
        {
            return;
        }


        PrepareMovementPresentation(
            Presentation
        );


        _MovementPresentationQueue.Enqueue(
            Presentation
        );


        if (_MovementPlayback == null)
        {
            _MovementPlayback =
                StartCoroutine(
                    PlayMovementPresentations()
                );

            MovementPlaybackChanged?.Invoke(true);
        }
    }


    private void PrepareMovementPresentation(
        GameParticipantAPI.MovementPresentation Presentation
    )
    {
        GameMovement.RouteStep[] Steps =
            Presentation.Segments[0]?.Steps;


        if (Steps == null ||
            Steps.Length == 0)
        {
            return;
        }


        string Key =
            Presentation.IsOwnUnit
                ? $"Own:{Presentation.RuntimeUnitID}"
                : $"Enemy:{Presentation.DefinitionID}:{Presentation.InstanceName}";

        GameMovement.RouteStep First =
            Steps[0];

        SynchronizeUnit(
            Key,
            Presentation.IsOwnUnit
                ? $"Own_{Presentation.InstanceName}"
                : $"VisibleEnemy_{Presentation.InstanceName}",
            Presentation.DefinitionID,
            First.MovementPivotCoordinate,
            First.Facing,
            -1,
            Presentation.Tags,
            true
        );


        if (_PresentedUnits.TryGetValue(
                Key,
                out PresentedUnit Presented) &&
            Presented != null)
        {
            Presented.IsAnimating =
                true;

            Presented.IsPreviewing =
                false;
        }
    }


    private void BindParticipantAccess()
    {
        _ParticipantAccess =
            _HumanGameplayInput.ParticipantAccess;


        if (_ParticipantAccess != null)
        {
            _ParticipantAccess.SnapshotChanged +=
                HandleSnapshotChanged;

            _ParticipantAccess.MovementPresented +=
                HandleMovementPresented;
        }
    }


    private void UnbindParticipantAccess()
    {
        if (_ParticipantAccess != null)
        {
            _ParticipantAccess.SnapshotChanged -=
                HandleSnapshotChanged;

            _ParticipantAccess.MovementPresented -=
                HandleMovementPresented;
        }


        _ParticipantAccess = null;
    }


    private void RefreshPresentation(
        bool RebuildImmediately
    )
    {
        if (!_Initialized ||
            _ParticipantAccess == null)
        {
            return;
        }


        if (_GameMaster.CurrentPhase == GameplayPhase.Deployment)
        {
            ClearPresentedUnits();
            return;
        }


        if (!_ParticipantAccess.TryGetSnapshot(
                out ParticipantGameSnapshot Snapshot,
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Participant Unit Presenter could not read its participant snapshot: {ValidationMessage}",
                this
            );

            return;
        }


        _ExpectedKeys.Clear();


        for (int i = 0; i < Snapshot.OwnUnits.Length; i++)
        {
            ParticipantUnitSnapshot Unit =
                Snapshot.OwnUnits[i];


            if (Unit == null ||
                !Unit.IsOnMap ||
                Unit.LifeState != RuntimeUnitLifeState.Active)
            {
                continue;
            }


            SynchronizeUnit(
                $"Own:{Unit.RuntimeUnitID}",
                $"Own_{Unit.InstanceName}",
                Unit.DefinitionID,
                Unit.MovementPivotCoordinate,
                Unit.Facing,
                Unit.EmbarkedPresentationLevel,
                Unit.Tags,
                RebuildImmediately
            );
        }


        for (int i = 0; i < Snapshot.VisibleEnemies.Length; i++)
        {
            VisibleEnemyUnitSnapshot Unit =
                Snapshot.VisibleEnemies[i];


            if (Unit == null)
            {
                continue;
            }


            SynchronizeUnit(
                $"Enemy:{Unit.DefinitionID}:{Unit.InstanceName}",
                $"VisibleEnemy_{Unit.InstanceName}",
                Unit.DefinitionID,
                Unit.MovementPivotCoordinate,
                Unit.Facing,
                Unit.EmbarkedPresentationLevel,
                Unit.Tags,
                RebuildImmediately
            );
        }


        RemoveUnexpectedUnits();
    }


    private void SynchronizeUnit(
        string Key,
        string ViewName,
        string DefinitionID,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing,
        int EmbarkedPresentationLevel,
        IReadOnlyList<ParticipantUnitTagSnapshot> Tags,
        bool RebuildImmediately
    )
    {
        _ExpectedKeys.Add(Key);


        if (!_PresentedUnits.TryGetValue(
                Key,
                out PresentedUnit Presented) ||
            Presented?.View == null)
        {
            if (!_UnitPresentationCatalog.TryGetPresentation(
                    DefinitionID,
                    out Definition_Unit Definition,
                    out UnitView Prefab))
            {
                ReportMissingPresentation(DefinitionID);
                return;
            }


            UnitView View =
                Instantiate(
                    Prefab,
                    _UnitsRoot
                );

            View.name = ViewName;

            Presented =
                new PresentedUnit
                {
                    View = View,
                    Definition = Definition
                };

            _PresentedUnits[Key] =
                Presented;

            Presented.TargetPosition =
                CalculateWorldPosition(
                    Definition,
                    MovementPivotCoordinate,
                    Facing
                );

            View.transform.position =
                Presented.TargetPosition;
        }
        else if (_UnitPresentationCatalog.TryGetPresentation(
                    DefinitionID,
                    out Definition_Unit Definition,
                    out _))
        {
            Presented.Definition =
                Definition;

            Presented.TargetPosition =
                CalculateWorldPosition(
                    Definition,
                    MovementPivotCoordinate,
                    Facing
                );


            if (!Presented.IsAnimating &&
                (RebuildImmediately ||
                _PositionInterpolationSeconds <= 0f)
               )
            {
                Presented.View.transform.position =
                    Presented.TargetPosition;
            }
        }


        Presented.AuthoritativeFacing =
            Facing;

        if (EmbarkedPresentationLevel >= 0)
        {
            Presented.View.SetEmbarkedPresentationLevel(
                EmbarkedPresentationLevel
            );
        }


        if (!Presented.IsPreviewing &&
            !Presented.IsAnimating)
        {
            Presented.View.SetFacing(Facing);
            Presented.View.SetColor(Color.white);
        }

        if (Tags != null)
        {
            Presented.View.SetTags(Tags);
        }
        Presented.View.gameObject.SetActive(true);
    }


    public bool TryShowOwnUnitPreview(
        string RuntimeUnitID,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing,
        Color Color
    )
    {
        if (string.IsNullOrWhiteSpace(
                RuntimeUnitID) ||
            !_PresentedUnits.TryGetValue(
                $"Own:{RuntimeUnitID}",
                out PresentedUnit Presented) ||
            Presented?.View == null ||
            Presented.Definition == null)
        {
            return false;
        }


        Presented.IsPreviewing =
            true;

        Presented.View.transform.position =
            CalculateWorldPosition(
                Presented.Definition,
                MovementPivotCoordinate,
                Facing
            );

        Presented.View.SetFacing(Facing);
        Presented.View.SetColor(Color);

        return true;
    }


    public void ClearOwnUnitPreview(
        string RuntimeUnitID
    )
    {
        if (string.IsNullOrWhiteSpace(
                RuntimeUnitID) ||
            !_PresentedUnits.TryGetValue(
                $"Own:{RuntimeUnitID}",
                out PresentedUnit Presented) ||
            Presented?.View == null)
        {
            return;
        }


        Presented.IsPreviewing =
            false;


        if (Presented.IsAnimating)
        {
            Presented.View.SetColor(
                Color.white
            );

            return;
        }

        Presented.View.transform.position =
            Presented.TargetPosition;

        Presented.View.SetFacing(
            Presented.AuthoritativeFacing
        );

        Presented.View.SetColor(
            Color.white
        );
    }


    public bool TryShowDeploymentPreview(
        string DefinitionID,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing,
        Color Color
    )
    {
        if (_UnitsRoot == null ||
            !_UnitPresentationCatalog.TryGetPresentation(
                DefinitionID,
                out Definition_Unit Definition,
                out UnitView Prefab))
        {
            return false;
        }


        if (_DeploymentPreview == null ||
            !ReferenceEquals(_DeploymentPreviewDefinition, Definition))
        {
            ClearDeploymentPreview();
            _DeploymentPreview = Instantiate(Prefab, _UnitsRoot);
            _DeploymentPreview.name = $"DeploymentPreview_{Definition.Name}";
            _DeploymentPreviewDefinition = Definition;
        }


        _DeploymentPreview.transform.position = CalculateWorldPosition(
            Definition,
            MovementPivotCoordinate,
            Facing
        );
        _DeploymentPreview.SetFacing(Facing);
        _DeploymentPreview.SetColor(Color);
        _DeploymentPreview.gameObject.SetActive(true);
        return true;
    }


    public void ClearDeploymentPreview()
    {
        if (_DeploymentPreview != null)
            Destroy(_DeploymentPreview.gameObject);

        _DeploymentPreview = null;
        _DeploymentPreviewDefinition = null;
    }


    private Vector3 CalculateWorldPosition(
        Definition_Unit Definition,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing
    )
    {
        Vector3 WorldPosition =
            _GameMaster.GameMap.CoordinateToWorldCenter(
                MovementPivotCoordinate
            );


        if (UnitFootprintGeometry.TryGetVisualCenterOffset(
                Definition,
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
                $"Participant Unit Presenter could not position '{Definition?.name}': {ValidationMessage}",
                this
            );
        }


        return WorldPosition;
    }


    private IEnumerator PlayMovementPresentations()
    {
        while (_MovementPresentationQueue.Count > 0)
        {
            GameParticipantAPI.MovementPresentation Presentation =
                _MovementPresentationQueue.Dequeue();

            yield return PlayMovementPresentation(
                Presentation
            );
        }


        _MovementPlayback =
            null;

        MovementPlaybackChanged?.Invoke(false);
        RefreshPresentation(false);
    }


    private IEnumerator PlayMovementPresentation(
        GameParticipantAPI.MovementPresentation Presentation
    )
    {
        string Key =
            Presentation.IsOwnUnit
                ? $"Own:{Presentation.RuntimeUnitID}"
                : $"Enemy:{Presentation.DefinitionID}:{Presentation.InstanceName}";

        PresentedUnit Presented =
            null;


        for (int SegmentIndex = 0;
            SegmentIndex < Presentation.Segments.Length;
            SegmentIndex++)
        {
            GameMovement.RouteStep[] Steps =
                Presentation.Segments[SegmentIndex]?.Steps;


            if (Steps == null ||
                Steps.Length == 0)
            {
                continue;
            }


            GameMovement.RouteStep First =
                Steps[0];

            SynchronizeUnit(
                Key,
                Presentation.IsOwnUnit
                    ? $"Own_{Presentation.InstanceName}"
                    : $"VisibleEnemy_{Presentation.InstanceName}",
                Presentation.DefinitionID,
                First.MovementPivotCoordinate,
                First.Facing,
                -1,
                Presentation.Tags,
                true
            );


            if (!_PresentedUnits.TryGetValue(
                    Key,
                    out Presented) ||
                Presented?.View == null ||
                Presented.Definition == null)
            {
                continue;
            }


            Presented.IsAnimating =
                true;

            Presented.IsPreviewing =
                false;

            Presented.View.gameObject.SetActive(true);
            Presented.View.SetColor(Color.white);
            Presented.View.SetFacing(First.Facing);
            Presented.View.transform.position =
                CalculateWorldPosition(
                    Presented.Definition,
                    First.MovementPivotCoordinate,
                    First.Facing
                );


            for (int StepIndex = 1;
                StepIndex < Steps.Length;
                StepIndex++)
            {
                yield return AnimateRouteStep(
                    Presented,
                    Steps[StepIndex - 1],
                    Steps[StepIndex]
                );
            }


            bool HasLaterSegment =
                SegmentIndex <
                    Presentation.Segments.Length - 1;


            if (HasLaterSegment ||
                !Presentation.RemainsVisible)
            {
                Presented.View.gameObject.SetActive(false);
            }
        }


        if (Presented != null)
        {
            Presented.IsAnimating =
                false;
        }
    }


    private IEnumerator AnimateRouteStep(
        PresentedUnit Presented,
        GameMovement.RouteStep Previous,
        GameMovement.RouteStep Next
    )
    {
        bool IsTurn =
            Previous.MovementPivotCoordinate ==
                Next.MovementPivotCoordinate &&
            Previous.Facing !=
                Next.Facing;

        float Duration =
            IsTurn
                ? _TurnStepSeconds
                : _TranslationStepSeconds;

        Vector3 StartingPosition =
            Presented.View.transform.position;

        Vector3 TargetPosition =
            CalculateWorldPosition(
                Presented.Definition,
                Next.MovementPivotCoordinate,
                Next.Facing
            );

        Presented.View.SetFacing(
            Next.Facing
        );


        float Elapsed =
            0f;


        while (Elapsed < Duration)
        {
            Elapsed +=
                Time.deltaTime;

            float Progress =
                Mathf.Clamp01(
                    Elapsed / Duration
                );

            float Eased =
                Progress * Progress *
                (3f - 2f * Progress);

            Presented.View.transform.position =
                Vector3.LerpUnclamped(
                    StartingPosition,
                    TargetPosition,
                    Eased
                );

            yield return null;
        }


        Presented.View.transform.position =
            TargetPosition;
    }


    private void RemoveUnexpectedUnits()
    {
        _RemovalKeys.Clear();


        foreach (KeyValuePair<string, PresentedUnit> Pair
            in _PresentedUnits)
        {
            if (!_ExpectedKeys.Contains(Pair.Key))
            {
                if (Pair.Value != null &&
                    Pair.Value.IsAnimating)
                {
                    continue;
                }

                DestroyPresentedUnit(Pair.Value);
                _RemovalKeys.Add(Pair.Key);
            }
        }


        for (int i = 0; i < _RemovalKeys.Count; i++)
        {
            _PresentedUnits.Remove(
                _RemovalKeys[i]
            );
        }
    }


    private void ClearPresentedUnits()
    {
        ClearDeploymentPreview();

        foreach (PresentedUnit Presented
            in _PresentedUnits.Values)
        {
            DestroyPresentedUnit(Presented);
        }


        _PresentedUnits.Clear();
        _ExpectedKeys.Clear();
        _RemovalKeys.Clear();
    }


    private void CancelMovementPlayback()
    {
        bool WasPlaying =
            _MovementPlayback != null;


        if (_MovementPlayback != null)
        {
            StopCoroutine(
                _MovementPlayback
            );

            _MovementPlayback =
                null;
        }


        _MovementPresentationQueue.Clear();


        foreach (PresentedUnit Presented
            in _PresentedUnits.Values)
        {
            if (Presented != null)
            {
                Presented.IsAnimating =
                    false;
            }
        }


        if (WasPlaying)
        {
            MovementPlaybackChanged?.Invoke(false);
        }
    }


    private static void DestroyPresentedUnit(
        PresentedUnit Presented
    )
    {
        if (Presented?.View == null)
        {
            return;
        }


        Presented.View.gameObject.SetActive(false);
        Destroy(Presented.View.gameObject);
    }


    private void ReportMissingPresentation(
        string DefinitionID
    )
    {
        if (!_ReportedMissingDefinitions.Add(
                DefinitionID ?? string.Empty))
        {
            return;
        }


        Debug.LogError(
            $"Participant Unit Presenter found no catalog presentation for Unit definition '{DefinitionID}'.",
            this
        );
    }


    private bool ValidateReferences()
    {
        if (_HumanGameplayInput != null &&
            _UnitPresentationCatalog != null)
        {
            return true;
        }


        Debug.LogError(
            "Participant Unit Presenter requires Human Gameplay Input and a Unit Presentation Catalog.",
            this
        );

        return false;
    }


    private void OnDestroy()
    {
        if (_HumanGameplayInput != null)
        {
            _HumanGameplayInput.ParticipantChanged -=
                HandleParticipantChanged;
        }


        UnbindParticipantAccess();
        CancelMovementPlayback();
        ClearPresentedUnits();
    }
}
