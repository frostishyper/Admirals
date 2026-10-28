/*
public static class PreBattleIntegration
{
    public static void IntegratePreBattlePlanning(ParticipantAIController controller, ParticipantGameSnapshot snapshot)
    {
        if (snapshot?.PreBattle == null || !snapshot.PreBattle.CanAct)
            return;

        if (controller.AccessAPIs.TryGetValue(typeof(GameParticipantAPI), out ParticipantAccessAPI rawAccess) &&
            rawAccess is GameParticipantAPI.ParticipantAccess access)
        {
            FleetStance currentStance = DetermineStance(snapshot);
            GameParticipantAPI.ParticipantCommand command = PreBattleDecider.DecidePreBattle(snapshot, access, currentStance);

            if (command != null)
                controller.SubmitCommand(command);
        }
    }

    private static FleetStance DetermineStance(ParticipantGameSnapshot snapshot)
    {
        return FleetStance.Hold;
    }
}


*/