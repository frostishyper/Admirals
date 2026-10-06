using System;
using UnityEngine;

// Defines A Range Of D20 Rolls And The Damage Result It Produces
[Serializable]
public class WeaponRollRange
{
    // Descriptive Name For This Result
    [SerializeField] private string _ResultName;

    // Lowest D20 Roll Included In This Range
    [SerializeField, Range(1, 20)]
    private int _MinimumRoll = 1;

    // Highest D20 Roll Included In This Range
    [SerializeField, Range(1, 20)]
    private int _MaximumRoll = 1;

    // Damage Dealt When This Range Is Rolled
    [SerializeField] private int _Damage;

    // Whether This Range Counts As A Miss
    [SerializeField] private bool _IsMiss;

    // Whether This Range Counts As A Critical Hit
    [SerializeField] private bool _IsCritical;


    // Read-Only Access For Other Systems
    public string ResultName
    {
        get { return _ResultName; }
    }

    public int MinimumRoll
    {
        get { return _MinimumRoll; }
    }

    public int MaximumRoll
    {
        get { return _MaximumRoll; }
    }

    public int Damage
    {
        get { return _Damage; }
    }

    public bool IsMiss
    {
        get { return _IsMiss; }
    }

    public bool IsCritical
    {
        get { return _IsCritical; }
    }
}