using System;
using UnityEngine;

// Embedded Serializable Effect Modifier - Not A ScriptableObject
// Overrides The Unit's Effective Domain While The Effect Is Active
[Serializable]
public class DomainOverrideModifier : EffectModifier
{
    [SerializeField] private Domain _Domain;

    public Domain Domain
    {
        get { return _Domain; }
    }
}