using System;
using UnityEngine;

// Embedded Serializable Effect Modifier - Not A ScriptableObject
// Multiplies Search Strength While The Effect Is Active
[Serializable]
public class SearchStrengthModifier : EffectModifier
{
    [SerializeField]
    [Min(0f)]
    private float _Multiplier = 1f;

    public float Multiplier
    {
        get { return _Multiplier; }
    }
}