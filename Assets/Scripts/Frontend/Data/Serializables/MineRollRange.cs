using System;
using UnityEngine;

// Embedded Serializable Data - Not A ScriptableObject
// Defines One D20 Outcome Range For A Triggered Mine
[Serializable]
public class MineRollRange
{
    [SerializeField] private string _ResultName;

    [Range(1, 20)]
    [SerializeField] private int _MinimumRoll = 1;

    [Range(1, 20)]
    [SerializeField] private int _MaximumRoll = 20;

    [Min(0)]
    [SerializeField] private int _Damage;

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

    public int Damage
    {
        get { return _Damage; }
    }

    public EffectApplication[] EffectApplications
    {
        get { return _EffectApplications; }
    }
}