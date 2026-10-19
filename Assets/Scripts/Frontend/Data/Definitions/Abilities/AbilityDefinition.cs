using UnityEngine;

// ScriptableObject Base Definition - Not Attached To A GameObject
// Provides Shared Authored Data For All Unit Abilities
public abstract class AbilityDefinition : ScriptableObject
{
    [SerializeField] private string _AbilityName;

    [SerializeField] private string _AbilityID;

    // Gameplay States Required Before This Ability Can Be Used Or Operate
    [SerializeField] private GameplayTag[] _RequiredTags;

    // Gameplay States That Prevent This Ability From Being Used Or Operating
    [SerializeField] private GameplayTag[] _BlockedTags;


    public string AbilityName
    {
        get { return _AbilityName; }
    }

    public string AbilityID
    {
        get { return _AbilityID; }
    }

    public GameplayTag[] RequiredTags
    {
        get { return _RequiredTags; }
    }

    public GameplayTag[] BlockedTags
    {
        get { return _BlockedTags; }
    }
}