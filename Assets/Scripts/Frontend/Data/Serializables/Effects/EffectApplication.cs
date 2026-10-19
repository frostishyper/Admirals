using System;
using UnityEngine;

// Embedded Serializable Data - Not A ScriptableObject
// Defines An Effect, Its Application Chance, And When It Is Applied
[Serializable]
public class EffectApplication
{
    [ScriptableObjectPicker]
    [SerializeField] private EffectDefinition _Effect;

    [SerializeField]
    [Range(0f, 100f)]
    private float _Chance = 100f;

    [SerializeField]
    private EffectApplicationTiming _Timing =
        EffectApplicationTiming.Immediate;


    public EffectDefinition Effect
    {
        get { return _Effect; }
    }

    public float Chance
    {
        get { return _Chance; }
    }

    public EffectApplicationTiming Timing
    {
        get { return _Timing; }
    }
}