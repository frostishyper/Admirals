using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


// Frontend Match Launch Coordinator.
// Watches Lobby readiness, controls the countdown,
// validates the final MatchConfig, and loads the Game scene.
public class MatchLaunchBridge :
    MonoBehaviour
{
    [Header("Lobby")]
    [SerializeField]
    private LobbyController _LobbyController;


    [Header("Countdown")]
    [SerializeField]
    private TMP_Text _CountdownText;

    [Min(1f)]
    [SerializeField]
    private float _CountdownDuration =
        10f;


    [Header("Scene")]
    [SerializeField]
    private string _GameSceneName =
        "Game";


    private bool _IsCountingDown;

    private bool _IsLaunching;

    private float _RemainingTime;

    private int _DisplayedSecond =
        -1;


    private void OnEnable()
    {
        ResetCountdownDisplay();
    }


    private void Update()
    {
        if (_IsLaunching)
        {
            return;
        }


        if (_LobbyController == null ||
            _LobbyController.CurrentLobby == null)
        {
            CancelCountdown();

            return;
        }


        if (!_LobbyController.CurrentLobby.CanStartMatch)
        {
            CancelCountdown();

            return;
        }


        if (!_IsCountingDown)
        {
            if (!CanBeginCountdown())
            {
                return;
            }


            BeginCountdown();
        }


        _RemainingTime -=
            Time.unscaledDeltaTime;


        if (_RemainingTime <=
            0f)
        {
            FinishCountdown();

            return;
        }


        RefreshCountdownText();
    }


    private bool CanBeginCountdown()
    {
        if (MatchSession.Instance ==
            null)
        {
            Debug.LogError(
                "Cannot begin Match countdown because no MatchSession exists.",
                this
            );

            return false;
        }


        MatchConfig PreviewMatch =
            _LobbyController.CreateMatchConfig();


        if (PreviewMatch == null)
        {
            return false;
        }


        if (!PreviewMatch.ValidateGameplayReady(
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Cannot begin Match countdown: {ValidationMessage}",
                this
            );

            return false;
        }


        return true;
    }


    private void BeginCountdown()
    {
        _IsCountingDown =
            true;

        _RemainingTime =
            _CountdownDuration;

        _DisplayedSecond =
            -1;


        RefreshCountdownText();
    }


    private void CancelCountdown()
    {
        if (!_IsCountingDown)
        {
            return;
        }


        _IsCountingDown =
            false;

        _RemainingTime =
            0f;

        _DisplayedSecond =
            -1;


        ResetCountdownDisplay();
    }


    private void RefreshCountdownText()
    {
        if (_CountdownText ==
            null)
        {
            return;
        }


        int RemainingSecond =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    _RemainingTime
                )
            );


        if (_DisplayedSecond ==
            RemainingSecond)
        {
            return;
        }


        _DisplayedSecond =
            RemainingSecond;


        _CountdownText.text =
            RemainingSecond.ToString();
    }


    private void FinishCountdown()
    {
        _IsCountingDown =
            false;


        MatchConfig Match =
            _LobbyController.CreateMatchConfig();


        if (Match == null)
        {
            ResetCountdownDisplay();

            return;
        }


        if (!Match.ValidateGameplayReady(
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Match failed final validation: {ValidationMessage}",
                this
            );


            ResetCountdownDisplay();

            return;
        }


        if (MatchSession.Instance ==
            null)
        {
            Debug.LogError(
                "Match cannot launch because MatchSession is missing.",
                this
            );


            ResetCountdownDisplay();

            return;
        }


        _IsLaunching =
            true;


        if (_CountdownText !=
            null)
        {
            _CountdownText.text =
                string.Empty;
        }


        MatchSession.Instance.SetActiveMatch(
            Match
        );


        Debug.Log(
            "Frontend Match Complete - Loading Game Scene."
        );


        SceneManager.LoadScene(
            _GameSceneName,
            LoadSceneMode.Single
        );
    }


    private void ResetCountdownDisplay()
    {
        if (_CountdownText !=
            null)
        {
            _CountdownText.text =
                string.Empty;
        }
    }
}