using System;
using UnityEngine;


// Embedded Serializable Data - Not A ScriptableObject
// General Ability outcome for things such as Dive or other non-attack actions.
[Serializable]
public class AbilityOutcome
{
    [SerializeField]
    private string _Name;

    [SerializeField]
    private GameplayAction[] _Actions;


    public string Name
    {
        get
        {
            return _Name;
        }
    }


    public GameplayAction[] Actions
    {
        get
        {
            return _Actions;
        }
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (string.IsNullOrWhiteSpace(
                _Name))
        {
            ValidationMessage =
                "Ability Outcome Name is empty.";

            return false;
        }


        if (_Actions == null ||
            _Actions.Length == 0)
        {
            ValidationMessage =
                $"Ability Outcome '{_Name}' has no Actions.";

            return false;
        }


        for (
            int i = 0;
            i < _Actions.Length;
            i++
        )
        {
            if (_Actions[i] == null)
            {
                ValidationMessage =
                    $"Ability Action {i} is empty.";

                return false;
            }


            if (!_Actions[i].Validate(
                    out string ActionMessage))
            {
                ValidationMessage =
                    $"Ability Action {i} is invalid: {ActionMessage}";

                return false;
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class AbilityD20Outcome :
    D20RollRange
{
    [SerializeField]
    private AbilityOutcome _Outcome;


    public AbilityOutcome Outcome
    {
        get
        {
            return _Outcome;
        }
    }


    public bool ValidateOutcome(
        out string ValidationMessage
    )
    {
        if (_Outcome == null)
        {
            ValidationMessage =
                $"D20 range {MinimumRoll}-{MaximumRoll} has no Ability Outcome.";

            return false;
        }


        return
            _Outcome.Validate(
                out ValidationMessage
            );
    }
}


// ScriptableObject Asset - Not Attached To A GameObject
// Defines a Unit Ability not bound to onboard equipment.
[CreateAssetMenu(
    fileName = "Ability_New",
    menuName = "Admirals/Ability"
)]
public class Definition_Ability :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string _Name;

    [SerializeField]
    private string _ID;


    [Header("Availability")]
    [SerializeField]
    private GameplayPhase _UsablePhases;

    [SerializeField]
    private CombatRole _CombatRoles;


    [Header("Targeting")]
    [SerializeField]
    private GameplayTarget _ValidTargets;

    [SerializeField]
    private Domain _TargetDomains;

    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_Pattern _Pattern;


    [Header("Outcome Type")]
    [SerializeField]
    private AbilityOutcomeType _OutcomeType;

    [SerializeField]
    private bool _UsesD20Roll;


    [Header("General Actions")]
    [SerializeField]
    private AbilityOutcome _AbilityOutcome;

    [SerializeField]
    private AbilityD20Outcome[] _AbilityD20Outcomes;


    [Header("Attack")]
    [SerializeField]
    private AttackOutcome _AttackOutcome;

    [SerializeField]
    private AttackD20Outcome[] _AttackD20Outcomes;


    [Header("Defense")]
    [SerializeField]
    private DefenseOutcome _DefenseOutcome;

    [SerializeField]
    private DefenseD20Outcome[] _DefenseD20Outcomes;


    public string Name => _Name;

    public string ID => _ID;

    public GameplayPhase UsablePhases => _UsablePhases;

    public CombatRole CombatRoles => _CombatRoles;

    public GameplayTarget ValidTargets => _ValidTargets;

    public Domain TargetDomains => _TargetDomains;

    public Definition_Pattern Pattern => _Pattern;

    public AbilityOutcomeType OutcomeType => _OutcomeType;

    public bool UsesD20Roll => _UsesD20Roll;


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (string.IsNullOrWhiteSpace(
                _Name))
        {
            ValidationMessage =
                "Ability Name is empty.";

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                _ID))
        {
            ValidationMessage =
                "Ability ID is empty.";

            return false;
        }


        if (_UsablePhases ==
            GameplayPhase.None)
        {
            ValidationMessage =
                "Ability has no Usable Phases.";

            return false;
        }


        if (_ValidTargets ==
            GameplayTarget.None)
        {
            ValidationMessage =
                "Ability has no Valid Targets.";

            return false;
        }


        if (_Pattern != null &&
            _Pattern.Mode != PatternMode.OriginPoint)
        {
            ValidationMessage =
                "Ability Pattern must use OriginPoint mode.";

            return false;
        }


        switch (_OutcomeType)
        {
            case AbilityOutcomeType.None:

                ValidationMessage =
                    "Ability Outcome Type is not configured.";

                return false;


            case AbilityOutcomeType.Actions:

                return
                    ValidateGeneralOutcome(
                        out ValidationMessage
                    );


            case AbilityOutcomeType.Attack:

                return
                    ValidateAttackOutcome(
                        out ValidationMessage
                    );


            case AbilityOutcomeType.Defense:

                return
                    ValidateDefenseOutcome(
                        out ValidationMessage
                    );
        }


        ValidationMessage =
            "Ability Outcome Type is invalid.";

        return false;
    }


    private bool ValidateGeneralOutcome(
        out string ValidationMessage
    )
    {
        if (_UsesD20Roll)
        {
            if (!D20Roll.Validate(
                    _AbilityD20Outcomes,
                    out string D20Message))
            {
                ValidationMessage =
                    $"Ability D20 Roll is invalid: {D20Message}";

                return false;
            }


            for (
                int i = 0;
                i < _AbilityD20Outcomes.Length;
                i++
            )
            {
                if (!_AbilityD20Outcomes[i].ValidateOutcome(
                        out string OutcomeMessage))
                {
                    ValidationMessage =
                        OutcomeMessage;

                    return false;
                }
            }
        }
        else
        {
            if (_AbilityOutcome == null)
            {
                ValidationMessage =
                    "Ability has no general Outcome.";

                return false;
            }


            if (!_AbilityOutcome.Validate(
                    out ValidationMessage))
            {
                return false;
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    private bool ValidateAttackOutcome(
        out string ValidationMessage
    )
    {
        if (_UsesD20Roll)
        {
            if (!D20Roll.Validate(
                    _AttackD20Outcomes,
                    out string D20Message))
            {
                ValidationMessage =
                    $"Ability Attack D20 Roll is invalid: {D20Message}";

                return false;
            }


            for (
                int i = 0;
                i < _AttackD20Outcomes.Length;
                i++
            )
            {
                if (!_AttackD20Outcomes[i].ValidateOutcome(
                        out string OutcomeMessage))
                {
                    ValidationMessage =
                        OutcomeMessage;

                    return false;
                }
            }
        }
        else
        {
            if (_AttackOutcome == null)
            {
                ValidationMessage =
                    "Offensive Ability has no Attack Outcome.";

                return false;
            }


            if (!_AttackOutcome.Validate(
                    out ValidationMessage))
            {
                return false;
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    private bool ValidateDefenseOutcome(
        out string ValidationMessage
    )
    {
        if (_UsesD20Roll)
        {
            if (!D20Roll.Validate(
                    _DefenseD20Outcomes,
                    out string D20Message))
            {
                ValidationMessage =
                    $"Ability Defense D20 Roll is invalid: {D20Message}";

                return false;
            }


            for (
                int i = 0;
                i < _DefenseD20Outcomes.Length;
                i++
            )
            {
                if (!_DefenseD20Outcomes[i].ValidateOutcome(
                        out string OutcomeMessage))
                {
                    ValidationMessage =
                        OutcomeMessage;

                    return false;
                }
            }
        }
        else
        {
            if (_DefenseOutcome == null)
            {
                ValidationMessage =
                    "Defensive Ability has no Defense Outcome.";

                return false;
            }


            if (!_DefenseOutcome.Validate(
                    out ValidationMessage))
            {
                return false;
            }
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
                $"Invalid Ability '{name}': {ValidationMessage}",
                this
            );
        }
    }
}