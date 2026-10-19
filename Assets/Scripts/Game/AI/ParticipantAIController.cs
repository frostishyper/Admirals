using UnityEngine;


// Runtime strategy adapter instantiated from an AI ParticipantInfo.
// Implementations receive only participant-bound access and must submit all
// decisions through it; they do not receive authoritative gameplay systems.
public abstract class ParticipantAIController : MonoBehaviour
{
    public bool IsInitialized { get; private set; }


    public bool Initialize(
        GameParticipantAPI.ParticipantAccess ParticipantAccess,
        out string ValidationMessage)
    {
        if (IsInitialized)
        {
            ValidationMessage = string.Empty;
            return true;
        }

        if (ParticipantAccess == null)
        {
            ValidationMessage =
                "AI controller requires participant-bound gameplay access.";
            return false;
        }

        if (ParticipantAccess.ParticipantType != ParticipantType.AI)
        {
            ValidationMessage =
                "AI controller can only bind to an AI participant.";
            return false;
        }

        if (!OnInitialize(ParticipantAccess, out ValidationMessage))
            return false;

        IsInitialized = true;
        ValidationMessage = string.Empty;
        return true;
    }


    protected abstract bool OnInitialize(
        GameParticipantAPI.ParticipantAccess ParticipantAccess,
        out string ValidationMessage);
}
