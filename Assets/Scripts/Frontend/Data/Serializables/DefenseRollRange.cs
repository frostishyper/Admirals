using System;
using UnityEngine;

// Serializable Defensive Roll Result
// Supplies Defensive Values To Combat Resolution
[Serializable]
public class DefenseRollRange
{
    [SerializeField] private string _ResultName;

    [SerializeField]
    [Range(1, 20)]
    private int _MinimumRoll = 1;

    [SerializeField]
    [Range(1, 20)]
    private int _MaximumRoll = 20;

    // Amount Subtracted From The Incoming Attack's Effective Roll
    [SerializeField]
    [Min(0)]
    private int _AttackRollPenalty;

    // Percentage Of Incoming Damage Mitigated By This Result
    [SerializeField]
    [Range(0f, 100f)]
    private float _DamageReduction;

    // Completely Negates The Incoming Attack
    [SerializeField] private bool _NegatesAttack;

    // Attack Properties Removed By This Result
    [SerializeField] private AttackProperty[] _RemovedAttackProperties;

    // Effects Caused By This Defensive Result
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

    public int AttackRollPenalty
    {
        get { return _AttackRollPenalty; }
    }

    public float DamageReduction
    {
        get { return _DamageReduction; }
    }

    public bool NegatesAttack
    {
        get { return _NegatesAttack; }
    }

    public AttackProperty[] RemovedAttackProperties
    {
        get { return _RemovedAttackProperties; }
    }

    public EffectApplication[] EffectApplications
    {
        get { return _EffectApplications; }
    }
}