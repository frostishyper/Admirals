// Runtime representation of one Match participant.
//
// Runtime Units themselves are owned by GameUnits.
// This object identifies the Participant and the Fleet assigned to them.
public class RuntimeParticipant
{
    public ParticipantSlot Participant
    {
        get;
        private set;
    }


    public Definition_Fleet Fleet
    {
        get;
        private set;
    }


    public bool DeploymentLocked
    {
        get;
        private set;
    }


    public RuntimeParticipant(
        ParticipantSlot Participant,
        Definition_Fleet Fleet
    )
    {
        this.Participant =
            Participant;

        this.Fleet =
            Fleet;

        DeploymentLocked =
            false;
    }


    internal void SetDeploymentLocked(
        bool Locked
    )
    {
        DeploymentLocked =
            Locked;
    }
}