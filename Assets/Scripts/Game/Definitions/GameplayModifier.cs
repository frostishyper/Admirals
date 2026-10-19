using System;
using UnityEngine;


// Embedded Serializable Data - Not A ScriptableObject
// Describes One Numerical Gameplay Modifier.
// Its Lifetime Is Controlled By Whatever Applies It.
[Serializable]
public class GameplayModifier
{
    [SerializeField]
    private ModifierType _Type;

    [SerializeField]
    private int _Value;

    [SerializeField]
    private GameplayPhase _ApplicablePhases;


    public ModifierType Type
    {
        get
        {
            return _Type;
        }
    }


    public int Value
    {
        get
        {
            return _Value;
        }
    }


    public GameplayPhase ApplicablePhases
    {
        get
        {
            return _ApplicablePhases;
        }
    }


    // Checks that the Modifier has a real type and at least one active phase.
    public bool Validate(
        out string ValidationMessage
    )
    {
        if (_Type == ModifierType.None)
        {
            ValidationMessage =
                "Modifier Type is not configured.";

            return false;
        }


        if (_ApplicablePhases == GameplayPhase.None)
        {
            ValidationMessage =
                $"Modifier '{_Type}' has no Applicable Phases.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}