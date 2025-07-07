namespace SAIN.SAINComponent.Classes.Mover
{
    public enum EBotSprintStatus
    {
        None,
        FirstTurn,
        Running,
        Turning,
        ShortCorner,
        NoStamina,
        InteractingWithDoor,
        ArrivingAtDestination,
        CantSprint,
        LookAtEnemyNoSprint,
        Canceling,
    }
}