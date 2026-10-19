using TMPro;
using UnityEngine;
using UnityEngine.UI;


// Local privacy gate for a finalized match containing two LocalPlayers.
// It owns no gameplay state: it only pauses phase clocks, covers participant
// information, and changes the participant-bound human presentation after the
// receiving player explicitly accepts the device.
[DisallowMultipleComponent]
public sealed class PassAndPlayHandoffUI : MonoBehaviour
{
    [SerializeField]
    private GameObject _PrivacyRoot;

    [SerializeField]
    private TMP_Text _ReasonText;

    [SerializeField]
    private TMP_Text _PlayerText;

    [SerializeField]
    private TMP_Text _InstructionText;

    [SerializeField]
    private Button _ContinueButton;


    private HumanGameplayInput _Input;
    private GameMaster _GameMaster;
    private RuntimeParticipant _PendingParticipant;
    private GameplayPhase _ReleasedPhase;
    private bool _HasReleasedGate;
    private bool _Initialized;
    private bool _Showing;


    private void Awake()
    {
        if (_ContinueButton != null)
            _ContinueButton.onClick.AddListener(Continue);

        _PrivacyRoot?.SetActive(false);
    }


    internal void Initialize(
        HumanGameplayInput Input,
        GameMaster GameMaster)
    {
        _Input = Input;
        _GameMaster = GameMaster;
        _Initialized = _Input != null && _GameMaster != null;

        if (!_Initialized || !_Input.IsPassAndPlay)
        {
            gameObject.SetActive(false);
            return;
        }

        transform.SetAsLastSibling();

        if (!_Input.ParticipantAccess.TryGetSnapshot(
                out ParticipantGameSnapshot Snapshot,
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Pass & Play handoff could not read its initial snapshot: {ValidationMessage}",
                this);
            return;
        }

        Show(
            _Input.Participant,
            Snapshot,
            "MATCH READY");
    }


    private void LateUpdate()
    {
        if (!_Initialized || _Showing ||
            _Input?.ParticipantAccess == null)
            return;

        if (!_Input.ParticipantAccess.TryGetSnapshot(
                out ParticipantGameSnapshot Snapshot,
                out _))
            return;

        // The terminal End HUD is shared information and must remain visible.
        if (Snapshot.Match?.MatchComplete == true)
        {
            _GameMaster.SetPassAndPlayHandoffPaused(false);
            return;
        }

        RuntimeParticipant Required = _Input.ResolveActionParticipant();
        if (Required == null)
            return;

        bool PhaseChanged =
            _HasReleasedGate &&
            Snapshot.CurrentPhase != _ReleasedPhase;
        bool ParticipantChanged =
            !ReferenceEquals(Required, _Input.Participant);

        if (!PhaseChanged && !ParticipantChanged)
            return;

        string Reason = PhaseChanged
            ? $"{PhaseName(_ReleasedPhase)} COMPLETE"
            : Snapshot.CurrentPhase == GameplayPhase.Battle &&
              Snapshot.Battle?.Engagement?.IsActive == true
                ? "ENGAGEMENT HANDOFF"
                : $"{PhaseName(Snapshot.CurrentPhase)} HANDOFF";

        Show(Required, Snapshot, Reason);
    }


    private void Show(
        RuntimeParticipant NextParticipant,
        ParticipantGameSnapshot Snapshot,
        string Reason)
    {
        if (NextParticipant == null || Snapshot == null ||
            _PrivacyRoot == null || _ContinueButton == null)
        {
            Debug.LogError(
                "Pass & Play handoff prefab is missing required references.",
                this);
            return;
        }

        _PendingParticipant = NextParticipant;
        _Showing = true;
        _GameMaster.SetPassAndPlayHandoffPaused(true);

        if (_ReasonText != null)
            _ReasonText.text = Reason;
        if (_PlayerText != null)
            _PlayerText.text =
                $"PASS TO {NextParticipant.Participant.DisplayName}";
        if (_InstructionText != null)
            _InstructionText.text =
                $"Turn {Snapshot.TurnNumber} | {PhaseName(Snapshot.CurrentPhase)}\n" +
                "Press Continue when the next player is ready.";

        _ContinueButton.interactable = true;
        _PrivacyRoot.SetActive(true);
        transform.SetAsLastSibling();
    }


    private void Continue()
    {
        if (!_Showing || _PendingParticipant == null)
            return;

        _ContinueButton.interactable = false;

        if (!_Input.TryTakePassAndPlayControl(
                _PendingParticipant,
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Pass & Play handoff failed: {ValidationMessage}",
                this);
            _ContinueButton.interactable = true;
            return;
        }

        if (_Input.ParticipantAccess.TryGetSnapshot(
                out ParticipantGameSnapshot Snapshot,
                out _))
        {
            _ReleasedPhase = Snapshot.CurrentPhase;
            _HasReleasedGate = true;
        }

        _PendingParticipant = null;
        _Showing = false;
        _PrivacyRoot.SetActive(false);
        _GameMaster.SetPassAndPlayHandoffPaused(false);
    }


    private static string PhaseName(GameplayPhase Phase)
    {
        return Phase == GameplayPhase.PreBattle
            ? "PRE-BATTLE"
            : Phase.ToString().ToUpperInvariant();
    }


    private void OnDestroy()
    {
        if (_ContinueButton != null)
            _ContinueButton.onClick.RemoveListener(Continue);

        _GameMaster?.SetPassAndPlayHandoffPaused(false);
    }
}
