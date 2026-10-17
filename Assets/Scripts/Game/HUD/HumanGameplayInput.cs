using System;
using System.Collections;
using UnityEngine;


// Resolves participant-bound gameplay access for this local human HUD.
[DisallowMultipleComponent]
public sealed class HumanGameplayInput : MonoBehaviour
{
    [Header("Game")]
    [SerializeField]
    private GameMaster _GameMaster;

    private GameParticipantAPI.ParticipantAccess _ParticipantAccess;
    private bool _Initialized;

    public event Action ParticipantChanged;

    public GameMaster GameMaster => _GameMaster;
    public GameParticipantAPI.ParticipantAccess ParticipantAccess =>
        _ParticipantAccess;
    public RuntimeParticipant Participant =>
        _ParticipantAccess?.Participant;
    public bool Initialized => _Initialized;

    private IEnumerator Start()
    {
        if (!ValidateReferences())
            yield break;

        while (!_GameMaster.InitializationComplete)
        {
            if (!_GameMaster.enabled)
            {
                Debug.LogError(
                    "Human Gameplay Input could not initialize because GameMaster initialization failed.",
                    this);
                yield break;
            }

            yield return null;
        }

        Initialize();
    }

    public bool Initialize()
    {
        if (_Initialized)
            return true;

        if (!ValidateReferences() ||
            !_GameMaster.InitializationComplete ||
            _GameMaster.ParticipantAPI == null)
            return false;

        if (!_GameMaster.ParticipantAPI.TryGetLocalPlayerAccess(
                out GameParticipantAPI.ParticipantAccess Access,
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Human Gameplay Input could not resolve participant access: {ValidationMessage}",
                this);
            return false;
        }

        SetParticipantAccess(Access);
        _Initialized = true;
        return true;
    }

    private void SetParticipantAccess(
        GameParticipantAPI.ParticipantAccess Access)
    {
        if (Access == null ||
            ReferenceEquals(_ParticipantAccess, Access))
            return;

        _ParticipantAccess = Access;
        ParticipantChanged?.Invoke();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [ContextMenu("Development/Control Participant A")]
    private void DevelopmentControlParticipantA()
    {
        SetDevelopmentParticipant(_GameMaster?.ParticipantA);
    }

    [ContextMenu("Development/Control Participant B")]
    private void DevelopmentControlParticipantB()
    {
        SetDevelopmentParticipant(_GameMaster?.ParticipantB);
    }


    [ContextMenu("Development/Log Participant Snapshot")]
    private void DevelopmentLogParticipantSnapshot()
    {
        string ValidationMessage =
            "Human Gameplay Input is not initialized.";

        if (!Initialize() ||
            !_ParticipantAccess.TryGetSnapshot(
                out ParticipantGameSnapshot Snapshot,
                out ValidationMessage))
        {
            Debug.LogError(
                $"Participant snapshot could not be read: {ValidationMessage}",
                this);
            return;
        }

        int Absolute = 0;
        int Obscured = 0;
        int Fog = 0;

        for (int i = 0; i < Snapshot.Tiles.Length; i++)
        {
            switch (Snapshot.Tiles[i].Visibility)
            {
                case ParticipantTileVisibility.Absolute:
                    Absolute++;
                    break;

                case ParticipantTileVisibility.Obscured:
                    Obscured++;
                    break;

                default:
                    Fog++;
                    break;
            }
        }

        Debug.Log(
            $"Participant snapshot '{_ParticipantAccess.DisplayName}': " +
            $"Phase {Snapshot.CurrentPhase}, Turn {Snapshot.TurnNumber}, " +
            $"Own Units {Snapshot.OwnUnits.Length}, " +
            $"Visible Enemies {Snapshot.VisibleEnemies.Length}, " +
            $"Movement {Snapshot.Movement?.MovesCommitted ?? 0}/" +
            $"{Snapshot.Movement?.MovementLimit ?? 0}, " +
            $"Movement Ended {Snapshot.Movement?.MovementEnded ?? false}, " +
            $"Can Act {Snapshot.Movement?.CanAct ?? false}, " +
            $"Time {(Snapshot.Movement?.SecondsRemaining ?? 0f):0.0}s, " +
            $"Tiles Absolute {Absolute}, Obscured {Obscured}, Fog {Fog}.",
            this);
    }


    [ContextMenu("Development/Movement/Commit First Available Unit Furthest Forward")]
    private void DevelopmentCommitFirstAvailableUnitFurthestForward()
    {
        string ValidationMessage =
            "Human Gameplay Input is not initialized.";


        if (!Initialize() ||
            !_ParticipantAccess.TryGetSnapshot(
                out ParticipantGameSnapshot Snapshot,
                out ValidationMessage))
        {
            Debug.LogError(
                $"Development Movement could not read a snapshot: {ValidationMessage}",
                this
            );

            return;
        }


        if (Snapshot.Movement == null ||
            !Snapshot.Movement.CanAct)
        {
            Debug.LogError(
                "Development Movement was rejected because this participant is waiting for Movement priority.",
                this
            );

            return;
        }


        for (int i = 0;
            i < Snapshot.OwnUnits.Length;
            i++)
        {
            ParticipantUnitSnapshot Unit =
                Snapshot.OwnUnits[i];


            if (Unit == null ||
                Unit.LifeState != RuntimeUnitLifeState.Active ||
                !Unit.IsOnMap ||
                HasMoved(
                    Snapshot.Movement,
                    Unit.RuntimeUnitID))
            {
                continue;
            }


            Vector2Int Forward =
                FacingDirection(
                    Unit.Facing
                );

            Vector2Int Sideways =
                new Vector2Int(
                    -Forward.y,
                    Forward.x
                );

            Vector2Int Destination =
                default;

            int BestForwardDistance =
                0;

            int BestSidewaysDistance =
                int.MaxValue;

            bool FoundDestination =
                false;


            for (int TileIndex = 0;
                TileIndex < Snapshot.Tiles.Length;
                TileIndex++)
            {
                Vector2Int Candidate =
                    Snapshot.Tiles[TileIndex].Coordinate;

                Vector2Int Delta =
                    Candidate -
                    Unit.MovementPivotCoordinate;

                int ForwardDistance =
                    Delta.x * Forward.x +
                    Delta.y * Forward.y;


                if (ForwardDistance <= 0)
                {
                    continue;
                }


                int SidewaysDistance =
                    Mathf.Abs(
                        Delta.x * Sideways.x +
                        Delta.y * Sideways.y
                    );


                if ((ForwardDistance < BestForwardDistance) ||
                    (ForwardDistance == BestForwardDistance &&
                     SidewaysDistance >= BestSidewaysDistance) ||
                    !_ParticipantAccess.ValidateMovement(
                        Unit.RuntimeUnitID,
                        Candidate,
                        Unit.Facing,
                        out GameMovement.MovementPlan Plan,
                        out ValidationMessage) ||
                    !Plan.WouldConsumeMove)
                {
                    continue;
                }


                Destination = Candidate;
                BestForwardDistance = ForwardDistance;
                BestSidewaysDistance = SidewaysDistance;
                FoundDestination = true;
            }


            if (!FoundDestination)
            {
                continue;
            }


            if (!_ParticipantAccess.TryCommitMovement(
                    Unit.RuntimeUnitID,
                    Destination,
                    Unit.Facing,
                    out GameMovement.CommitResult Result,
                    out ValidationMessage))
            {
                Debug.LogError(
                    $"Development Movement was rejected: {ValidationMessage}",
                    this
                );

                return;
            }


            Debug.Log(
                $"Development Movement resolved '{Unit.InstanceName}' at " +
                $"{Result.FinalMovementPivotCoordinate}, facing {Result.FinalFacing}. " +
                $"Outcome {Result.Outcome}; consumed {Result.MoveConsumed}.",
                this
            );

            return;
        }


        Debug.LogError(
            $"No owned Unit has a known legal forward destination. Last result: {ValidationMessage}",
            this
        );
    }


    [ContextMenu("Development/Movement/End Movement")]
    private void DevelopmentEndMovement()
    {
        string ValidationMessage =
            "Human Gameplay Input is not initialized.";


        if (!Initialize() ||
            !_ParticipantAccess.TryEndMovement(
                out ValidationMessage))
        {
            Debug.LogError(
                $"Development End Movement was rejected: {ValidationMessage}",
                this
            );
        }
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


        for (int i = 0;
            i < Movement.Units.Length;
            i++)
        {
            GameMovement.UnitView Unit =
                Movement.Units[i];


            if (Unit != null &&
                string.Equals(
                    Unit.RuntimeUnitID,
                    RuntimeUnitID,
                    StringComparison.Ordinal))
            {
                return
                    Unit.HasMoved;
            }
        }


        return false;
    }


    private static Vector2Int FacingDirection(
        UnitFacing Facing
    )
    {
        switch (Facing)
        {
            case UnitFacing.North:
                return Vector2Int.up;

            case UnitFacing.East:
                return Vector2Int.right;

            case UnitFacing.South:
                return Vector2Int.down;

            case UnitFacing.West:
                return Vector2Int.left;

            default:
                return Vector2Int.zero;
        }
    }


    [ContextMenu("Development/Use First Available Submerge Ability")]
    private void DevelopmentUseSubmergeAbility()
    {
        DevelopmentUseFirstAbility(
            "ability_submerge"
        );
    }


    [ContextMenu("Development/Use First Available Surface Ability")]
    private void DevelopmentUseSurfaceAbility()
    {
        DevelopmentUseFirstAbility(
            "ability_surface"
        );
    }


    private void DevelopmentUseFirstAbility(
        string AbilityID
    )
    {
        string ValidationMessage =
            "Human Gameplay Input is not initialized.";


        if (!Initialize() ||
            !_ParticipantAccess.TryGetSnapshot(
                out ParticipantGameSnapshot Snapshot,
                out ValidationMessage))
        {
            Debug.LogError(
                $"Development Ability could not read a snapshot: {ValidationMessage}",
                this
            );

            return;
        }


        for (int UnitIndex = 0;
            UnitIndex < Snapshot.OwnUnits.Length;
            UnitIndex++)
        {
            ParticipantUnitSnapshot Unit =
                Snapshot.OwnUnits[UnitIndex];


            for (int AbilityIndex = 0;
                AbilityIndex < Unit.Abilities.Length;
                AbilityIndex++)
            {
                ParticipantAbilitySnapshot Ability =
                    Unit.Abilities[AbilityIndex];


                if (!string.Equals(
                        Ability.DefinitionID,
                        AbilityID,
                        StringComparison.Ordinal))
                {
                    continue;
                }


                if (!_ParticipantAccess.TryUseAbility(
                        Unit.RuntimeUnitID,
                        Ability.SlotIndex,
                        out ValidationMessage))
                {
                    Debug.LogError(
                        $"Development Ability '{AbilityID}' was rejected: " +
                        ValidationMessage,
                        this
                    );
                }


                return;
            }
        }


        Debug.LogError(
            $"No owned Unit exposes Ability '{AbilityID}'.",
            this
        );
    }


    private void SetDevelopmentParticipant(
        RuntimeParticipant Participant)
    {
        if (!Application.isPlaying ||
            !Initialize() ||
            Participant == null ||
            !_GameMaster.ParticipantAPI.TryGetParticipantAccess(
                Participant,
                out GameParticipantAPI.ParticipantAccess Access))
            return;

        SetParticipantAccess(Access);
        Debug.Log(
            $"Development control switched to '{Access.DisplayName}'.",
            this);
    }
#endif

    private bool ValidateReferences()
    {
        if (_GameMaster != null)
            return true;

        Debug.LogError(
            "Human Gameplay Input has no GameMaster.",
            this);
        return false;
    }
}





