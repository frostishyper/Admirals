using System;
using UnityEngine;


// Embedded Serializable Data - Not A ScriptableObject
// Supplies only the inclusive range used by the shared D20 mechanism.
[Serializable]
public class D20RollRange
{
    [Range(1, 20)]
    [SerializeField]
    private int _MinimumRoll = 1;

    [Range(1, 20)]
    [SerializeField]
    private int _MaximumRoll = 20;


    public int MinimumRoll
    {
        get
        {
            return _MinimumRoll;
        }
    }


    public int MaximumRoll
    {
        get
        {
            return _MaximumRoll;
        }
    }


    // Checks whether this individual range is legal.
    public bool ValidateRange(
        out string ValidationMessage
    )
    {
        if (_MinimumRoll < 1 ||
            _MaximumRoll > 20)
        {
            ValidationMessage =
                $"D20 range {_MinimumRoll}-{_MaximumRoll} must stay between 1 and 20.";

            return false;
        }


        if (_MinimumRoll >
            _MaximumRoll)
        {
            ValidationMessage =
                $"D20 range {_MinimumRoll}-{_MaximumRoll} has its minimum above its maximum.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    // Checks whether one final D20 value belongs to this range.
    public bool Contains(
        int Roll
    )
    {
        return
            Roll >= _MinimumRoll &&
            Roll <= _MaximumRoll;
    }
}


// Shared D20 Mechanism - Not A ScriptableObject
// Validates and queries any consumer-specific D20 outcome array.
public static class D20Roll
{
    // Every number from 1 through 20 must belong to exactly one range.
    // This is intended primarily for authoring validation.
    public static bool Validate<T>(
        T[] Entries,
        out string ValidationMessage
    )
        where T : D20RollRange
    {
        if (Entries == null ||
            Entries.Length == 0)
        {
            ValidationMessage =
                "D20 Roll has no ranges.";

            return false;
        }


        bool[] CoveredRolls =
            new bool[21];


        for (
            int EntryIndex = 0;
            EntryIndex < Entries.Length;
            EntryIndex++
        )
        {
            T Entry =
                Entries[EntryIndex];


            if (Entry == null)
            {
                ValidationMessage =
                    $"D20 range {EntryIndex} is empty.";

                return false;
            }


            if (!Entry.ValidateRange(
                    out string RangeMessage))
            {
                ValidationMessage =
                    $"D20 range {EntryIndex} is invalid: {RangeMessage}";

                return false;
            }


            for (
                int Roll = Entry.MinimumRoll;
                Roll <= Entry.MaximumRoll;
                Roll++
            )
            {
                if (CoveredRolls[Roll])
                {
                    ValidationMessage =
                        $"D20 value {Roll} is covered by more than one range.";

                    return false;
                }


                CoveredRolls[Roll] =
                    true;
            }
        }


        for (
            int Roll = 1;
            Roll <= 20;
            Roll++
        )
        {
            if (!CoveredRolls[Roll])
            {
                ValidationMessage =
                    $"D20 value {Roll} is not covered by any range.";

                return false;
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    // Finds the consumer-specific entry belonging to the final D20 value.
    // It does not know or care what data that entry contains.
    public static bool TryGetOutcome<T>(
        T[] Entries,
        int RawRoll,
        out T FoundOutcome,
        int RollModifier = 0
    )
        where T : D20RollRange
    {
        FoundOutcome =
            null;


        if (Entries == null ||
            Entries.Length == 0)
        {
            return false;
        }


        int FinalRoll =
            Mathf.Clamp(
                RawRoll + RollModifier,
                1,
                20
            );


        bool FoundMatch =
            false;


        for (
            int EntryIndex = 0;
            EntryIndex < Entries.Length;
            EntryIndex++
        )
        {
            T Entry =
                Entries[EntryIndex];


            if (Entry == null ||
                !Entry.Contains(
                    FinalRoll))
            {
                continue;
            }


            // An overlap means the D20 data is invalid.
            if (FoundMatch)
            {
                FoundOutcome =
                    null;

                return false;
            }


            FoundOutcome =
                Entry;

            FoundMatch =
                true;
        }


        return
            FoundMatch;
    }
}