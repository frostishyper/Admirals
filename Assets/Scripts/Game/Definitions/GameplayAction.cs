using System;
using UnityEngine;


// Embedded Serializable Data - Not A ScriptableObject
// Represents one general gameplay action produced by an effect or Ability.
[Serializable]
public class GameplayAction
{
    [SerializeField]
    private GameplayActionType _Type;


    [Header("Value")]
    [SerializeField]
    private int _Amount;


    [Header("Tag")]
    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_UnitTag _Tag;


    [Header("Status")]
    [SerializeField]
    private Definition_Status _Status;


    [Header("Modifier")]
    [SerializeField]
    private GameplayModifier _Modifier;

    [SerializeField]
    private DurationMode _ModifierDurationMode;

    [Min(1)]
    [SerializeField]
    private int _ModifierDurationTurns = 1;


    public GameplayActionType Type
    {
        get
        {
            return _Type;
        }
    }


    public int Amount
    {
        get
        {
            return _Amount;
        }
    }


    public Definition_UnitTag Tag
    {
        get
        {
            return _Tag;
        }
    }


    public Definition_Status Status
    {
        get
        {
            return _Status;
        }
    }


    public GameplayModifier Modifier
    {
        get
        {
            return _Modifier;
        }
    }


    public DurationMode ModifierDurationMode
    {
        get
        {
            return _ModifierDurationMode;
        }
    }


    public int ModifierDurationTurns
    {
        get
        {
            return _ModifierDurationTurns;
        }
    }


    // Checks only the data needed by the selected Action Type.
    public bool Validate(
        out string ValidationMessage
    )
    {
        switch (_Type)
        {
            case GameplayActionType.None:

                ValidationMessage =
                    "Gameplay Action Type is not configured.";

                return false;


            case GameplayActionType.AddTag:
            case GameplayActionType.RemoveTag:

                if (_Tag == null)
                {
                    ValidationMessage =
                        $"Gameplay Action '{_Type}' has no Unit Tag.";

                    return false;
                }


                if (!_Tag.Validate(
                        out string TagMessage))
                {
                    ValidationMessage =
                        $"Gameplay Action '{_Type}' has an invalid Unit Tag: {TagMessage}";

                    return false;
                }

                break;


            case GameplayActionType.ApplyStatus:
            case GameplayActionType.RemoveStatus:

                if (_Status == null)
                {
                    ValidationMessage =
                        $"Gameplay Action '{_Type}' has no Status.";

                    return false;
                }

                break;


            case GameplayActionType.ApplyModifier:

                if (_Modifier == null)
                {
                    ValidationMessage =
                        "Apply Modifier Action has no Modifier.";

                    return false;
                }


                if (!_Modifier.Validate(
                        out string ModifierMessage))
                {
                    ValidationMessage =
                        $"Apply Modifier Action is invalid: {ModifierMessage}";

                    return false;
                }


                if (_ModifierDurationMode ==
                    DurationMode.None)
                {
                    ValidationMessage =
                        "Apply Modifier Action has no Duration Mode.";

                    return false;
                }


                if (_ModifierDurationMode ==
                        DurationMode.Turns &&
                    _ModifierDurationTurns < 1)
                {
                    ValidationMessage =
                        "Apply Modifier Action must last at least one turn.";

                    return false;
                }

                break;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}
