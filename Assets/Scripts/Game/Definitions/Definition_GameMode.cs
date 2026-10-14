using System;
using UnityEngine;


public enum FleetSourceMode
{
    None = 0,

    GameModeProvided = 1,

    // Reserved for the future Fleet Builder.
    LobbyProvided = 2
}


public enum ConditionSubject
{
    None = 0,

    Self = 1,
    Opponent = 2
}


public enum NumericComparison
{
    None = 0,

    AtOrBelow = 1,
    AtOrAbove = 2
}


public enum DesignatedUnitState
{
    None = 0,

    Alive = 1,
    Destroyed = 2
}


public enum ScoreRelationship
{
    None = 0,

    HigherThanOpponent = 1,
    LowerThanOpponent = 2
}


// Embedded Serializable Data - Not A ScriptableObject
// Defines the Fleet supplied to one side by the GameMode.
[Serializable]
public class GameModeSideFleet
{
    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_Fleet _ProvidedFleet;

    // Used by GameModes that designate one Fleet Entry,
    // such as Protect The Admiral.
    [SerializeField]
    private string _AdmiralEntryID;


    public Definition_Fleet ProvidedFleet
    {
        get
        {
            return _ProvidedFleet;
        }
    }


    public string AdmiralEntryID
    {
        get
        {
            return _AdmiralEntryID;
        }
    }


    public bool HasAdmiral
    {
        get
        {
            return
                !string.IsNullOrWhiteSpace(
                    _AdmiralEntryID
                );
        }
    }


    public bool TryGetAdmiralEntry(
        out FleetEntry AdmiralEntry
    )
    {
        AdmiralEntry =
            null;


        if (_ProvidedFleet == null ||
            string.IsNullOrWhiteSpace(
                _AdmiralEntryID))
        {
            return false;
        }


        return
            _ProvidedFleet.TryGetEntry(
                _AdmiralEntryID,
                out AdmiralEntry
            );
    }


    public bool ValidateProvidedFleet(
        bool AdmiralRequired,
        out string ValidationMessage
    )
    {
        if (_ProvidedFleet == null)
        {
            ValidationMessage =
                "Provided Fleet is missing.";

            return false;
        }


        if (!_ProvidedFleet.Validate(
                out ValidationMessage))
        {
            return false;
        }


        if (!AdmiralRequired)
        {
            ValidationMessage =
                string.Empty;

            return true;
        }


        if (string.IsNullOrWhiteSpace(
                _AdmiralEntryID))
        {
            ValidationMessage =
                "An Admiral Fleet Entry is required.";

            return false;
        }


        if (!_ProvidedFleet.ContainsEntry(
                _AdmiralEntryID))
        {
            ValidationMessage =
                $"Admiral Entry ID '{_AdmiralEntryID}' does not exist in Fleet '{_ProvidedFleet.Name}'.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
// Tests the remaining size of either participant's Fleet.
[Serializable]
public class FleetSizeCondition
{
    [SerializeField]
    private ConditionSubject _Subject;

    [SerializeField]
    private NumericComparison _Comparison;

    [Min(0)]
    [SerializeField]
    private int _Value;


    public ConditionSubject Subject
    {
        get
        {
            return _Subject;
        }
    }


    public NumericComparison Comparison
    {
        get
        {
            return _Comparison;
        }
    }


    public int Value
    {
        get
        {
            return _Value;
        }
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (_Subject ==
            ConditionSubject.None)
        {
            ValidationMessage =
                "Fleet Size Condition has no Subject.";

            return false;
        }


        if (_Comparison ==
            NumericComparison.None)
        {
            ValidationMessage =
                "Fleet Size Condition has no Comparison.";

            return false;
        }


        if (_Value < 0)
        {
            ValidationMessage =
                "Fleet Size Condition Value cannot be negative.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
// Tests the state of the participant's GameMode-designated Unit.
[Serializable]
public class DesignatedUnitCondition
{
    [SerializeField]
    private ConditionSubject _Subject;

    [SerializeField]
    private DesignatedUnitState _RequiredState;


    public ConditionSubject Subject
    {
        get
        {
            return _Subject;
        }
    }


    public DesignatedUnitState RequiredState
    {
        get
        {
            return _RequiredState;
        }
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (_Subject ==
            ConditionSubject.None)
        {
            ValidationMessage =
                "Designated Unit Condition has no Subject.";

            return false;
        }


        if (_RequiredState ==
            DesignatedUnitState.None)
        {
            ValidationMessage =
                "Designated Unit Condition has no required State.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
// Tests one participant's current Score against a fixed value.
[Serializable]
public class ScoreCondition
{
    [SerializeField]
    private ConditionSubject _Subject;

    [SerializeField]
    private NumericComparison _Comparison;

    [Min(0)]
    [SerializeField]
    private int _Value;


    public ConditionSubject Subject
    {
        get
        {
            return _Subject;
        }
    }


    public NumericComparison Comparison
    {
        get
        {
            return _Comparison;
        }
    }


    public int Value
    {
        get
        {
            return _Value;
        }
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (_Subject ==
            ConditionSubject.None)
        {
            ValidationMessage =
                "Score Condition has no Subject.";

            return false;
        }


        if (_Comparison ==
            NumericComparison.None)
        {
            ValidationMessage =
                "Score Condition has no Comparison.";

            return false;
        }


        if (_Value < 0)
        {
            ValidationMessage =
                "Score Condition Value cannot be negative.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
// Tests the current completed Turn count.
[Serializable]
public class TurnCondition
{
    [SerializeField]
    private NumericComparison _Comparison;

    [Min(1)]
    [SerializeField]
    private int _Turn = 1;


    public NumericComparison Comparison
    {
        get
        {
            return _Comparison;
        }
    }


    public int Turn
    {
        get
        {
            return _Turn;
        }
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (_Comparison ==
            NumericComparison.None)
        {
            ValidationMessage =
                "Turn Condition has no Comparison.";

            return false;
        }


        if (_Turn < 1)
        {
            ValidationMessage =
                "Turn Condition must reference Turn 1 or later.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
// Checks score leadership at a turn-limit checkpoint.
// A tie moves the next checkpoint forward by TieExtensionTurns.
[Serializable]
public class ScoreAtTurnLimitCondition
{
    [SerializeField]
    private ScoreRelationship _Relationship;

    [Min(1)]
    [SerializeField]
    private int _InitialTurnLimit = 1;

    [Min(1)]
    [SerializeField]
    private int _TieExtensionTurns = 1;


    public ScoreRelationship Relationship
    {
        get
        {
            return _Relationship;
        }
    }


    public int InitialTurnLimit
    {
        get
        {
            return _InitialTurnLimit;
        }
    }


    public int TieExtensionTurns
    {
        get
        {
            return _TieExtensionTurns;
        }
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (_Relationship ==
            ScoreRelationship.None)
        {
            ValidationMessage =
                "Score At Turn Limit Condition has no Score Relationship.";

            return false;
        }


        if (_InitialTurnLimit < 1)
        {
            ValidationMessage =
                "Score At Turn Limit Condition must begin on Turn 1 or later.";

            return false;
        }


        if (_TieExtensionTurns < 1)
        {
            ValidationMessage =
                "Score At Turn Limit Condition must extend a tied score by at least one Turn.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
// Every condition inside one Group must be true.
// The GameMode treats separate Groups as OR alternatives.
[Serializable]
public class GameModeConditionGroup
{
    [SerializeField]
    private string _Name;


    [Header("Fleet Size")]
    [SerializeField]
    private FleetSizeCondition[] _FleetSizeConditions;


    [Header("Designated Unit")]
    [SerializeField]
    private DesignatedUnitCondition[] _DesignatedUnitConditions;


    [Header("Score")]
    [SerializeField]
    private ScoreCondition[] _ScoreConditions;


    [Header("Turn")]
    [SerializeField]
    private TurnCondition[] _TurnConditions;


    [Header("Score At Turn Limit")]
    [SerializeField]
    private ScoreAtTurnLimitCondition[] _ScoreAtTurnLimitConditions;


    public string Name
    {
        get
        {
            return _Name;
        }
    }


    public FleetSizeCondition[] FleetSizeConditions
    {
        get
        {
            return _FleetSizeConditions;
        }
    }


    public DesignatedUnitCondition[] DesignatedUnitConditions
    {
        get
        {
            return _DesignatedUnitConditions;
        }
    }


    public ScoreCondition[] ScoreConditions
    {
        get
        {
            return _ScoreConditions;
        }
    }


    public TurnCondition[] TurnConditions
    {
        get
        {
            return _TurnConditions;
        }
    }


    public ScoreAtTurnLimitCondition[] ScoreAtTurnLimitConditions
    {
        get
        {
            return _ScoreAtTurnLimitConditions;
        }
    }


    public bool UsesDesignatedUnit
    {
        get
        {
            return
                _DesignatedUnitConditions != null &&
                _DesignatedUnitConditions.Length > 0;
        }
    }


    public bool HasConditions
    {
        get
        {
            return
                HasEntries(
                    _FleetSizeConditions
                ) ||
                HasEntries(
                    _DesignatedUnitConditions
                ) ||
                HasEntries(
                    _ScoreConditions
                ) ||
                HasEntries(
                    _TurnConditions
                ) ||
                HasEntries(
                    _ScoreAtTurnLimitConditions
                );
        }
    }


    private bool HasEntries<T>(
        T[] Entries
    )
    {
        return
            Entries != null &&
            Entries.Length > 0;
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (!HasConditions)
        {
            ValidationMessage =
                $"Condition Group '{_Name}' contains no Conditions.";

            return false;
        }


        if (_FleetSizeConditions != null)
        {
            for (
                int i = 0;
                i < _FleetSizeConditions.Length;
                i++
            )
            {
                if (_FleetSizeConditions[i] == null)
                {
                    ValidationMessage =
                        $"Fleet Size Condition {i} is empty.";

                    return false;
                }


                if (!_FleetSizeConditions[i].Validate(
                        out ValidationMessage))
                {
                    return false;
                }
            }
        }


        if (_DesignatedUnitConditions != null)
        {
            for (
                int i = 0;
                i < _DesignatedUnitConditions.Length;
                i++
            )
            {
                if (_DesignatedUnitConditions[i] == null)
                {
                    ValidationMessage =
                        $"Designated Unit Condition {i} is empty.";

                    return false;
                }


                if (!_DesignatedUnitConditions[i].Validate(
                        out ValidationMessage))
                {
                    return false;
                }
            }
        }


        if (_ScoreConditions != null)
        {
            for (
                int i = 0;
                i < _ScoreConditions.Length;
                i++
            )
            {
                if (_ScoreConditions[i] == null)
                {
                    ValidationMessage =
                        $"Score Condition {i} is empty.";

                    return false;
                }


                if (!_ScoreConditions[i].Validate(
                        out ValidationMessage))
                {
                    return false;
                }
            }
        }


        if (_TurnConditions != null)
        {
            for (
                int i = 0;
                i < _TurnConditions.Length;
                i++
            )
            {
                if (_TurnConditions[i] == null)
                {
                    ValidationMessage =
                        $"Turn Condition {i} is empty.";

                    return false;
                }


                if (!_TurnConditions[i].Validate(
                        out ValidationMessage))
                {
                    return false;
                }
            }
        }


        if (_ScoreAtTurnLimitConditions != null)
        {
            for (
                int i = 0;
                i < _ScoreAtTurnLimitConditions.Length;
                i++
            )
            {
                if (_ScoreAtTurnLimitConditions[i] == null)
                {
                    ValidationMessage =
                        $"Score At Turn Limit Condition {i} is empty.";

                    return false;
                }


                if (!_ScoreAtTurnLimitConditions[i].Validate(
                        out ValidationMessage))
                {
                    return false;
                }
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
// Awards Score whenever an enemy Unit is destroyed.
[Serializable]
public class EnemyUnitDestroyedScoringRule
{
    [SerializeField]
    private bool _Enabled;

    [Min(1)]
    [SerializeField]
    private int _PointsPerUnit = 1;


    public bool Enabled
    {
        get
        {
            return _Enabled;
        }
    }


    public int PointsPerUnit
    {
        get
        {
            return _PointsPerUnit;
        }
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (!_Enabled)
        {
            ValidationMessage =
                string.Empty;

            return true;
        }


        if (_PointsPerUnit < 1)
        {
            ValidationMessage =
                "Enemy Unit Destroyed scoring must award at least one Point.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// ScriptableObject Asset - Not Attached To A GameObject
// Defines The Authored Rules And Fleet Setup For One GameMode.
[CreateAssetMenu(
    fileName = "GameMode_New",
    menuName = "Admirals/Game Mode"
)]
public class Definition_GameMode :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string _Name;

    [SerializeField]
    private string _ID;


    [Header("Fleet Source")]
    [SerializeField]
    private FleetSourceMode _FleetSource =
        FleetSourceMode.GameModeProvided;


    [Header("Side A")]
    [SerializeField]
    private GameModeSideFleet _SideA;


    [Header("Side B")]
    [SerializeField]
    private GameModeSideFleet _SideB;


    [Header("Victory Groups")]
    [Tooltip(
        "Every Condition inside one Group is AND. " +
        "Separate Groups are OR."
    )]
    [SerializeField]
    private GameModeConditionGroup[] _VictoryGroups;


    [Header("Defeat Groups")]
    [Tooltip(
        "Every Condition inside one Group is AND. " +
        "Separate Groups are OR."
    )]
    [SerializeField]
    private GameModeConditionGroup[] _DefeatGroups;


    [Header("Scoring")]
    [SerializeField]
    private EnemyUnitDestroyedScoringRule _EnemyUnitDestroyedScoring;


    public string Name
    {
        get
        {
            return _Name;
        }
    }


    public string ID
    {
        get
        {
            return _ID;
        }
    }


    public FleetSourceMode FleetSource
    {
        get
        {
            return _FleetSource;
        }
    }


    public GameModeSideFleet SideA
    {
        get
        {
            return _SideA;
        }
    }


    public GameModeSideFleet SideB
    {
        get
        {
            return _SideB;
        }
    }


    public GameModeConditionGroup[] VictoryGroups
    {
        get
        {
            return _VictoryGroups;
        }
    }


    public GameModeConditionGroup[] DefeatGroups
    {
        get
        {
            return _DefeatGroups;
        }
    }


    public EnemyUnitDestroyedScoringRule EnemyUnitDestroyedScoring
    {
        get
        {
            return _EnemyUnitDestroyedScoring;
        }
    }


    // Checks whether any authored rule relies on
    // a GameMode-designated Unit such as an Admiral.
    public bool UsesDesignatedUnitConditions()
    {
        if (GroupsUseDesignatedUnit(
                _VictoryGroups))
        {
            return true;
        }


        return
            GroupsUseDesignatedUnit(
                _DefeatGroups
            );
    }


    private bool GroupsUseDesignatedUnit(
        GameModeConditionGroup[] Groups
    )
    {
        if (Groups == null)
        {
            return false;
        }


        for (
            int i = 0;
            i < Groups.Length;
            i++
        )
        {
            if (Groups[i] != null &&
                Groups[i].UsesDesignatedUnit)
            {
                return true;
            }
        }


        return false;
    }


    private bool ValidateGroups(
        GameModeConditionGroup[] Groups,
        string GroupType,
        out string ValidationMessage
    )
    {
        if (Groups == null)
        {
            ValidationMessage =
                string.Empty;

            return true;
        }


        for (
            int i = 0;
            i < Groups.Length;
            i++
        )
        {
            GameModeConditionGroup Group =
                Groups[i];


            if (Group == null)
            {
                ValidationMessage =
                    $"{GroupType} Group {i} is empty.";

                return false;
            }


            if (!Group.Validate(
                    out ValidationMessage))
            {
                ValidationMessage =
                    $"{GroupType} Group {i} is invalid: {ValidationMessage}";

                return false;
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (string.IsNullOrWhiteSpace(
                _Name))
        {
            ValidationMessage =
                "GameMode Name is empty.";

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                _ID))
        {
            ValidationMessage =
                "GameMode ID is empty.";

            return false;
        }


        if (_FleetSource ==
            FleetSourceMode.None)
        {
            ValidationMessage =
                "GameMode has no Fleet Source.";

            return false;
        }


        bool UsesAdmirals =
            UsesDesignatedUnitConditions();


        if (_FleetSource ==
            FleetSourceMode.GameModeProvided)
        {
            if (_SideA == null)
            {
                ValidationMessage =
                    "GameMode Side A Fleet setup is missing.";

                return false;
            }


            if (_SideB == null)
            {
                ValidationMessage =
                    "GameMode Side B Fleet setup is missing.";

                return false;
            }


            if (!_SideA.ValidateProvidedFleet(
                    UsesAdmirals,
                    out ValidationMessage))
            {
                ValidationMessage =
                    $"Side A is invalid: {ValidationMessage}";

                return false;
            }


            if (!_SideB.ValidateProvidedFleet(
                    UsesAdmirals,
                    out ValidationMessage))
            {
                ValidationMessage =
                    $"Side B is invalid: {ValidationMessage}";

                return false;
            }
        }


        bool HasVictoryGroups =
            _VictoryGroups != null &&
            _VictoryGroups.Length > 0;


        bool HasDefeatGroups =
            _DefeatGroups != null &&
            _DefeatGroups.Length > 0;


        if (!HasVictoryGroups &&
            !HasDefeatGroups)
        {
            ValidationMessage =
                "GameMode has no Victory or Defeat Conditions.";

            return false;
        }


        if (!ValidateGroups(
                _VictoryGroups,
                "Victory",
                out ValidationMessage))
        {
            return false;
        }


        if (!ValidateGroups(
                _DefeatGroups,
                "Defeat",
                out ValidationMessage))
        {
            return false;
        }


        if (_EnemyUnitDestroyedScoring != null &&
            !_EnemyUnitDestroyedScoring.Validate(
                out ValidationMessage))
        {
            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    private void OnValidate()
    {
        if (!Validate(
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Invalid GameMode '{name}': {ValidationMessage}",
                this
            );
        }
    }
}