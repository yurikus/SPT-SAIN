namespace SAIN.SAINComponent.Classes.Mover
{
    public enum EBotSprintStatus
    {
        None,
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