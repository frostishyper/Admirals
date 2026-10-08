using System;
using UnityEngine;

// Embedded Serializable Ability Roll Result - Not A ScriptableObject
// Defines Which Effects An Active Ability Produces For A D20 Range
[Serializable]
public class AbilityRollRange
{
    [SerializeField] private string _ResultName;

    [SerializeField]
    [Range(1, 20)]
    private int _MinimumRoll = 1;

    [SerializeField]
    [Range(1, 20)]
    private int _MaximumRoll = 20;

    [SerializeField] private EffectApplication[] _EffectApplications;


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

    public EffectApplication[] EffectApplications
    {
        get { return _EffectApplications; }
    }
}