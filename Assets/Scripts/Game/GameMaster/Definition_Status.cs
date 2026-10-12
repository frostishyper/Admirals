using UnityEngine;


// ScriptableObject Asset - Not Attached To A GameObject
// Defines One Status That Can Exist On A Unit.
[CreateAssetMenu(
    fileName = "Status_New",
    menuName = "Admirals/Status"
)]
public class Definition_Status :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string _Name;

    [SerializeField]
    private string _ID;


    [Header("Duration")]
    [SerializeField]
    private DurationMode _DurationMode;

    [Min(1)]
    [SerializeField]
    private int _DurationTurns = 1;


    [Header("Interval")]
    [Min(1)]
    [SerializeField]
    private int _IntervalTurns = 1;


    [Header("Interval Values")]
    [Min(0)]
    [SerializeField]
    private int _DamagePerInterval;

    // Health is added to the affected Unit when this Status reaches its interval.
    [Min(0)]
    [SerializeField]
    private int _HealthPerInterval;


    [Header("Modifiers")]
    [SerializeField]
    private GameplayModifier[] _Modifiers;


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


    public DurationMode DurationMode
    {
        get
        {
            return _DurationMode;
        }
    }


    public int DurationTurns
    {
        get
        {
            return _DurationTurns;
        }
    }


    public int IntervalTurns
    {
        get
        {
            return _IntervalTurns;
        }
    }


    public int DamagePerInterval
    {
        get
        {
            return _DamagePerInterval;
        }
    }


    public int HealthPerInterval
    {
        get
        {
            return _HealthPerInterval;
        }
    }


    public GameplayModifier[] Modifiers
    {
        get
        {
            return _Modifiers;
        }
    }


    // Validates this Status and every Modifier it owns.
    public bool Validate(
        out string ValidationMessage
    )
    {
        if (string.IsNullOrWhiteSpace(
                _ID))
        {
            ValidationMessage =
                "Status ID is empty.";

            return false;
        }


        if (_DurationMode == DurationMode.None)
        {
            ValidationMessage =
                "Status Duration Mode is not configured.";

            return false;
        }


        if (_DurationMode == DurationMode.Turns &&
            _DurationTurns < 1)
        {
            ValidationMessage =
                "Timed Status must last at least one turn.";

            return false;
        }


        if (_Modifiers != null)
        {
            for (
                int ModifierIndex = 0;
                ModifierIndex < _Modifiers.Length;
                ModifierIndex++
            )
            {
                GameplayModifier Modifier =
                    _Modifiers[ModifierIndex];


                if (Modifier == null)
                {
                    ValidationMessage =
                        $"Modifier {ModifierIndex} is empty.";

                    return false;
                }


                if (!Modifier.Validate(
                        out string ModifierMessage))
                {
                    ValidationMessage =
                        $"Modifier {ModifierIndex} is invalid: {ModifierMessage}";

                    return false;
                }
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    // Unity calls this whenever the Status asset is changed in the Editor.
    private void OnValidate()
    {
        if (!Validate(
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Invalid Status '{name}': {ValidationMessage}",
                this
            );
        }
    }
}