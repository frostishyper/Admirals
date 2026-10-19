using System;
using System.Collections.Generic;
using UnityEngine;


// Authoritative End-phase participant-priority state.
// Automatic upkeep is resolved by GameUnitEffects before this phase begins;
// this class owns the remaining End action windows and completion state.
public sealed class GameEnd
{
    public sealed class ParticipantView
    {
        public bool IsEndPhaseActive { get; }
        public bool Ended { get; }
        public bool CanAct { get; }
        public int ActionsCommitted { get; }
        public float SecondsRemaining { get; }


        internal ParticipantView(bool IsEndPhaseActive, bool Ended,
            bool CanAct, int ActionsCommitted, float SecondsRemaining)
        {
            this.IsEndPhaseActive = IsEndPhaseActive;
            this.Ended = Ended;
            this.CanAct = CanAct;
            this.ActionsCommitted = ActionsCommitted;
            this.SecondsRemaining = SecondsRemaining;
        }
    }


    private sealed class ParticipantState
    {
        public readonly RuntimeParticipant Participant;
        public readonly HashSet<string> UsedAbilities =
            new HashSet<string>(StringComparer.Ordinal);

        public bool Ended;
        public int ActionsCommitted;


        public ParticipantState(RuntimeParticipant Participant)
        {
            this.Participant = Participant;
        }
    }


    private ParticipantState _ParticipantA;
    private ParticipantState _ParticipantB;
    private RuntimeParticipant _ActiveParticipant;
    private bool _PhaseActive;
    private float _SecondsPerParticipant;
    private float _SecondsRemaining;


    public bool IsInitialized { get; private set; }


    internal bool Initialize(RuntimeParticipant ParticipantA,
        RuntimeParticipant ParticipantB, out string ValidationMessage)
    {
        IsInitialized = false;
        if (ParticipantA == null || ParticipantB == null ||
            ReferenceEquals(ParticipantA, ParticipantB))
        {
            ValidationMessage =
                "End phase requires two distinct runtime participants.";
            return false;
        }

        _ParticipantA = new ParticipantState(ParticipantA);
        _ParticipantB = new ParticipantState(ParticipantB);
        _ActiveParticipant = null;
        _PhaseActive = false;
        IsInitialized = true;
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool BeginPhase(RuntimeParticipant FirstParticipant,
        float SecondsPerParticipant, out string ValidationMessage)
    {
        if (!IsInitialized ||
            (!ReferenceEquals(FirstParticipant, _ParticipantA.Participant) &&
             !ReferenceEquals(FirstParticipant, _ParticipantB.Participant)))
        {
            ValidationMessage =
                "End-phase first participant does not belong to this Match.";
            return false;
        }

        ResetState(_ParticipantA);
        ResetState(_ParticipantB);
        _ActiveParticipant = FirstParticipant;
        _SecondsPerParticipant = Mathf.Max(1f, SecondsPerParticipant);
        _SecondsRemaining = _SecondsPerParticipant;
        _PhaseActive = true;
        ValidationMessage = string.Empty;
        return true;
    }


    internal bool TryGetView(RuntimeParticipant Participant,
        out ParticipantView View)
    {
        View = null;
        if (!TryGetState(Participant, out ParticipantState State))
            return false;

        View = new ParticipantView(
            _PhaseActive,
            State.Ended,
            _PhaseActive && !State.Ended &&
                ReferenceEquals(Participant, _ActiveParticipant),
            State.ActionsCommitted,
            _SecondsRemaining);
        return true;
    }


    internal bool CanAcceptParticipantAction(RuntimeParticipant Participant,
        out string ValidationMessage)
    {
        if (!_PhaseActive || !TryGetState(Participant,
                out ParticipantState State))
        {
            ValidationMessage = "End phase is not active for this participant.";
            return false;
        }

        if (State.Ended)
        {
            ValidationMessage = "This participant has already finished End.";
            return false;
        }

        if (!ReferenceEquals(Participant, _ActiveParticipant))
        {
            ValidationMessage =
                "The other participant currently has End-phase priority.";
            return false;
        }

        ValidationMessage = string.Empty;
        return true;
    }


    internal bool CanUseAbility(RuntimeParticipant Participant,
        string RuntimeUnitID, int AbilitySlotIndex,
        out string ValidationMessage)
    {
        if (!CanAcceptParticipantAction(Participant, out ValidationMessage) ||
            !TryGetState(Participant, out ParticipantState State))
            return false;

        if (State.UsedAbilities.Contains(
                AbilityKey(RuntimeUnitID, AbilitySlotIndex)))
        {
            ValidationMessage =
                "That Ability has already been used during this End phase.";
            return false;
        }

        ValidationMessage = string.Empty;
        return true;
    }


    internal void CommitAbilityUse(RuntimeParticipant Participant,
        string RuntimeUnitID, int AbilitySlotIndex)
    {
        if (!TryGetState(Participant, out ParticipantState State))
            return;

        State.UsedAbilities.Add(AbilityKey(RuntimeUnitID, AbilitySlotIndex));
        State.ActionsCommitted++;
    }


    internal bool TryFinishParticipant(RuntimeParticipant Participant,
        out bool AllParticipantsFinished, out string ValidationMessage)
    {
        AllParticipantsFinished = false;
        if (!CanAcceptParticipantAction(Participant, out ValidationMessage) ||
            !TryGetState(Participant, out ParticipantState State))
            return false;

        State.Ended = true;
        if (_ParticipantA.Ended && _ParticipantB.Ended)
        {
            _PhaseActive = false;
            _ActiveParticipant = null;
            _SecondsRemaining = 0f;
            AllParticipantsFinished = true;
        }
        else
        {
            _ActiveParticipant = ReferenceEquals(
                    Participant, _ParticipantA.Participant)
                ? _ParticipantB.Participant
                : _ParticipantA.Participant;
            _SecondsRemaining = _SecondsPerParticipant;
        }

        ValidationMessage = string.Empty;
        return true;
    }


    internal bool Tick(float DeltaSeconds,
        out RuntimeParticipant TimedOutParticipant,
        out bool AllParticipantsFinished)
    {
        TimedOutParticipant = null;
        AllParticipantsFinished = false;
        if (!_PhaseActive || _ActiveParticipant == null)
            return false;

        _SecondsRemaining = Mathf.Max(0f,
            _SecondsRemaining - Mathf.Max(0f, DeltaSeconds));
        if (_SecondsRemaining > 0f)
            return false;

        TimedOutParticipant = _ActiveParticipant;
        return TryFinishParticipant(TimedOutParticipant,
            out AllParticipantsFinished, out _);
    }


    internal void TerminatePhase()
    {
        _PhaseActive = false;
        _ActiveParticipant = null;
        _SecondsRemaining = 0f;
    }


    private bool TryGetState(RuntimeParticipant Participant,
        out ParticipantState State)
    {
        State = null;
        if (!IsInitialized || Participant == null)
            return false;
        if (ReferenceEquals(Participant, _ParticipantA.Participant))
            State = _ParticipantA;
        else if (ReferenceEquals(Participant, _ParticipantB.Participant))
            State = _ParticipantB;
        return State != null;
    }


    private static void ResetState(ParticipantState State)
    {
        State.Ended = false;
        State.ActionsCommitted = 0;
        State.UsedAbilities.Clear();
    }


    private static string AbilityKey(string RuntimeUnitID,
        int AbilitySlotIndex)
    {
        return $"{RuntimeUnitID ?? string.Empty}:{AbilitySlotIndex}";
    }
}
