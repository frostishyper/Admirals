using System.Collections.Generic;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines A Gameplay Effect And Its Embedded Actions And Modifiers
[CreateAssetMenu(
    fileName = "Effect_New",
    menuName = "Admirals/Effect Definition"
)]
public class EffectDefinition : ScriptableObject
{
    [SerializeField] private string _EffectName;
    [SerializeField] private string _EffectID;

    [SerializeField] private EffectDurationType _DurationType;

    [SerializeField]
    [Min(1)]
    private int _Duration = 1;

    [SerializeField] private bool _Periodic;

    [SerializeField]
    [Min(1)]
    private int _Interval = 1;

    [SerializeReference]
    [SubclassSelector]
    private List<EffectAction> _Actions = new();

    [SerializeReference]
    [SubclassSelector]
    private List<EffectModifier> _Modifiers = new();


    public string EffectName
    {
        get { return _EffectName; }
    }

    public string EffectID
    {
        get { return _EffectID; }
    }

    public EffectDurationType DurationType
    {
        get { return _DurationType; }
    }

    public int Duration
    {
        get { return _Duration; }
    }

    public bool Periodic
    {
        get { return _Periodic; }
    }

    public int Interval
    {
        get { return _Interval; }
    }

    public IReadOnlyList<EffectAction> Actions
    {
        get { return _Actions; }
    }

    public IReadOnlyList<EffectModifier> Modifiers
    {
        get { return _Modifiers; }
    }
}