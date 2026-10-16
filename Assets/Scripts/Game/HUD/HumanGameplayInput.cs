using System.Collections;
using UnityEngine;


// Shared context for gameplay input originating from this local client.
//
// This component resolves which RuntimeParticipant is represented by the
// local human-facing HUD. It does not own gameplay rules and does not encode
// AI or networking behavior.
[DisallowMultipleComponent]
public sealed class HumanGameplayInput :
    MonoBehaviour
{
    [Header("Game")]
    [SerializeField]
    private GameMaster _GameMaster;


    private RuntimeParticipant _Participant;

    private bool _Initialized;


    public GameMaster GameMaster =>
        _GameMaster;


    public RuntimeParticipant Participant =>
        _Participant;


    public bool Initialized =>
        _Initialized;


    private IEnumerator Start()
    {
        if (!ValidateReferences())
        {
            yield break;
        }


        while (!_GameMaster.InitializationComplete)
        {
            if (!_GameMaster.enabled)
            {
                Debug.LogError(
                    "Human Gameplay Input could not initialize because GameMaster initialization failed.",
                    this
                );

                yield break;
            }


            yield return null;
        }


        Initialize();
    }


    public bool Initialize()
    {
        if (_Initialized)
        {
            return true;
        }


        if (!ValidateReferences() ||
            !_GameMaster.InitializationComplete)
        {
            return false;
        }


        RuntimeParticipant ParticipantA =
            _GameMaster.ParticipantA;

        RuntimeParticipant ParticipantB =
            _GameMaster.ParticipantB;


        bool ParticipantAIsLocal =
            IsLocalPlayer(
                ParticipantA
            );

        bool ParticipantBIsLocal =
            IsLocalPlayer(
                ParticipantB
            );


        if (ParticipantAIsLocal ==
            ParticipantBIsLocal)
        {
            Debug.LogError(
                ParticipantAIsLocal
                    ? "Human Gameplay Input found more than one LocalPlayer participant."
                    : "Human Gameplay Input could not find a LocalPlayer participant.",
                this
            );

            return false;
        }


        _Participant =
            ParticipantAIsLocal
                ? ParticipantA
                : ParticipantB;

        _Initialized =
            true;

        return true;
    }


    private static bool IsLocalPlayer(
        RuntimeParticipant Participant
    )
    {
        return
            Participant != null &&
            Participant.Participant != null &&
            Participant.Participant.ParticipantType ==
                ParticipantType.LocalPlayer;
    }


    private bool ValidateReferences()
    {
        if (_GameMaster == null)
        {
            Debug.LogError(
                "Human Gameplay Input has no GameMaster.",
                this
            );

            return false;
        }


        return true;
    }
}
