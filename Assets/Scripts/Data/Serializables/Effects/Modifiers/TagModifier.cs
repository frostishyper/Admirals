using System;
using UnityEngine;

// Embedded Serializable Effect Modifier - Not A ScriptableObject
// Adds A Gameplay Tag While The Effect Is Active
[Serializable]
public class TagModifier : EffectModifier
{
    [SerializeField] private GameplayTag _Tag;

    public GameplayTag Tag
    {
        get { return _Tag; }
    }
}