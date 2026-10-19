using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


// Owns shared, non-gameplay HUD controls available throughout a Match.
[DisallowMultipleComponent]
public sealed class MatchHUDControls : MonoBehaviour
{
    [Header("Participant")]
    [SerializeField] private HumanGameplayInput _HumanGameplayInput;

    [Header("Turn")]
    [SerializeField] private TMP_Text _TurnNumberText;

    [Header("Exit Match")]
    [SerializeField] private Button _ExitMatchButton;
    [SerializeField] private GameObject _ExitConfirmationRoot;
    [SerializeField] private Button _ConfirmExitButton;
    [SerializeField] private Button _CancelExitButton;
    [SerializeField] private string _FrontendSceneName = "Frontend";

    private GameParticipantAPI.ParticipantAccess _Access;
    private float _PreviousTimeScale = 1f;
    private bool _PromptOpen;
    private bool _ReturningToLobby;


    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        _ExitConfirmationRoot.SetActive(false);
        SetTurnLabel(null);
    }


    private void OnEnable()
    {
        if (_ExitMatchButton == null)
            return;

        _ExitMatchButton.onClick.AddListener(OpenExitPrompt);
        _ConfirmExitButton.onClick.AddListener(ConfirmExit);
        _CancelExitButton.onClick.AddListener(CancelExit);
        _HumanGameplayInput.ParticipantChanged += HandleParticipantChanged;
        TryBindParticipant();
    }


    private void Update()
    {
        if (_Access == null &&
            _HumanGameplayInput != null &&
            _HumanGameplayInput.Initialized)
            TryBindParticipant();

        Keyboard Keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (Keyboard == null ||
            !Keyboard.escapeKey.wasPressedThisFrame ||
            _ReturningToLobby)
            return;

        if (_PromptOpen)
            CancelExit();
        else
            OpenExitPrompt();
    }


    private void HandleParticipantChanged()
    {
        TryBindParticipant();
    }


    private void TryBindParticipant()
    {
        if (_Access != null)
            _Access.SnapshotChanged -= HandleSnapshotChanged;

        _Access = _HumanGameplayInput != null
            ? _HumanGameplayInput.ParticipantAccess
            : null;

        if (_Access != null)
            _Access.SnapshotChanged += HandleSnapshotChanged;

        RefreshTurnNumber();
    }


    private void HandleSnapshotChanged()
    {
        RefreshTurnNumber();
    }


    private void RefreshTurnNumber()
    {
        if (_Access == null ||
            !_Access.TryGetSnapshot(
                out ParticipantGameSnapshot Snapshot,
                out _) ||
            Snapshot == null)
        {
            SetTurnLabel(null);
            return;
        }

        SetTurnLabel(Mathf.Max(0, Snapshot.TurnNumber));
    }


    private void SetTurnLabel(int? TurnNumber)
    {
        if (_TurnNumberText == null)
            return;

        _TurnNumberText.text = TurnNumber.HasValue
            ? $"TURN {TurnNumber.Value}"
            : "TURN --";
        _TurnNumberText.textWrappingMode = TextWrappingModes.NoWrap;
    }


    private void OpenExitPrompt()
    {
        if (_PromptOpen || _ReturningToLobby)
            return;

        _PromptOpen = true;
        _PreviousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        _ExitMatchButton.interactable = false;
        _ExitConfirmationRoot.SetActive(true);
        _ExitConfirmationRoot.transform.SetAsLastSibling();
        _CancelExitButton.Select();
    }


    private void CancelExit()
    {
        if (!_PromptOpen || _ReturningToLobby)
            return;

        _PromptOpen = false;
        Time.timeScale = _PreviousTimeScale;
        _ExitConfirmationRoot.SetActive(false);
        _ExitMatchButton.interactable = true;
        _ExitMatchButton.Select();
    }


    private void ConfirmExit()
    {
        if (!_PromptOpen || _ReturningToLobby)
            return;

        _ReturningToLobby = true;
        _PromptOpen = false;

        // The Frontend must never inherit a paused time scale.
        Time.timeScale = 1f;
        _ConfirmExitButton.interactable = false;
        _CancelExitButton.interactable = false;
        _ExitMatchButton.interactable = false;

        MatchSession.Instance?.ClearMatch();
        SceneManager.LoadScene(_FrontendSceneName, LoadSceneMode.Single);
    }


    private bool ValidateReferences()
    {
        bool Valid =
            _HumanGameplayInput != null &&
            _TurnNumberText != null &&
            _ExitMatchButton != null &&
            _ExitConfirmationRoot != null &&
            _ConfirmExitButton != null &&
            _CancelExitButton != null &&
            !string.IsNullOrWhiteSpace(_FrontendSceneName);

        if (!Valid)
        {
            Debug.LogError(
                "Match HUD Controls has missing participant, turn, " +
                "exit-confirmation, or Frontend scene configuration.",
                this);
        }

        return Valid;
    }


    private void OnDisable()
    {
        if (_ExitMatchButton != null)
            _ExitMatchButton.onClick.RemoveListener(OpenExitPrompt);
        if (_ConfirmExitButton != null)
            _ConfirmExitButton.onClick.RemoveListener(ConfirmExit);
        if (_CancelExitButton != null)
            _CancelExitButton.onClick.RemoveListener(CancelExit);
        if (_HumanGameplayInput != null)
            _HumanGameplayInput.ParticipantChanged -= HandleParticipantChanged;
        if (_Access != null)
        {
            _Access.SnapshotChanged -= HandleSnapshotChanged;
            _Access = null;
        }

        if (_PromptOpen && !_ReturningToLobby)
        {
            Time.timeScale = _PreviousTimeScale;
            _PromptOpen = false;
        }
    }
}
