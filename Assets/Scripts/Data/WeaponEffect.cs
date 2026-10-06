using System;
using UnityEngine;

// Defines A Possible Secondary Effect Caused By A Weapon
[Serializable]
public class WeaponEffect
{
    // Effect That May Be Applied
    [SerializeField] private EffectDefinition _Effect;

    // Independent Percentage Chance For The Effect To Occur
    [SerializeField, Range(0f, 100f)]
    private float _Chance = 100f;

    // Allows This Effect To Attempt Its Roll After A Miss
    [SerializeField] private bool _OnMiss;

    // Allows This Effect To Attempt Its Roll After A Normal Hit
    [SerializeField] private bool _OnHit;

    // Allows This Effect To Attempt Its Roll After A Critical Hit
    [SerializeField] private bool _OnCritical;


    // Read-Only Access For Other Systems
    public EffectDefinition Effect
    {
        get { return _Effect; }
    }

    public float Chance
    {
        get { return _Chance; }
    }

    public bool OnMiss
    {
        get { return _OnMiss; }
    }

    public bool OnHit
    {
        get { return _OnHit; }
    }

    public bool OnCritical
    {
        get { return _OnCritical; }
    }
}