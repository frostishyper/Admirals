using System;


[Flags]
public enum Domain
{
    None = 0,

    Air = 1 << 0,
    Surface = 1 << 1,
    Subsurface = 1 << 2,

    All =
        Air |
        Surface |
        Subsurface
}


[Flags]
public enum GameplayPhase
{
    None = 0,

    Deployment = 1 << 0,
    Movement = 1 << 1,
    PreBattle = 1 << 2,
    Search = 1 << 3,
    Battle = 1 << 4,
    End = 1 << 5,

    All =
        Deployment |
        Movement |
        PreBattle |
        Search |
        Battle |
        End
}


public enum SystemSlotType
{
    None = 0,

    Sensor = 1,
    Defensive = 2,
    Passive = 3,
    Deployment = 4
}


public enum PatternMode
{
    OriginPoint = 1,
    FootprintMovement = 2
}


[Flags]
public enum CombatRole
{
    None = 0,

    Attacker = 1 << 0,
    Defender = 1 << 1,

    Both =
        Attacker |
        Defender
}


[Flags]
public enum GameplayTarget
{
    None = 0,

    Self = 1 << 0,
    Ally = 1 << 1,
    Enemy = 1 << 2,
    Contact = 1 << 3,
    Tile = 1 << 4
}


public enum OutcomeResult
{
    None = 0,

    Hit = 1,
    Miss = 2
}


public enum AttackProperty
{
    None = 0,

    Critical = 1,
    GuaranteedHit = 2,
    BypassDefense = 3
}


[Flags]
public enum OutcomeTrigger
{
    None = 0,

    Hit = 1 << 0,
    Miss = 1 << 1,
    Critical = 1 << 2
}


public enum ModifierType
{
    None = 0,

    MovementRange = 1,
    AttackRange = 2,
    Replenishment = 3,
    RollOutcome = 4,
    StatusProtection = 5,
    StatusInfliction = 6,
    HealthPerEndPhase = 7,
    Damage = 8,
    Stealth = 9,
    Search = 10
}


public enum DurationMode
{
    None = 0,

    Permanent = 1,
    Turns = 2
}


public enum GameplayActionType
{
    None = 0,

    Damage = 1,
    Health = 2,

    AddTag = 3,
    RemoveTag = 4,

    ApplyStatus = 5,
    RemoveStatus = 6,

    ApplyModifier = 7
}


public enum AbilityOutcomeType
{
    None = 0,

    Actions = 1,
    Attack = 2,
    Defense = 3
}