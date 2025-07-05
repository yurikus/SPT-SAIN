namespace SAIN.SAINComponent.Classes.Mover
{
    public enum MoveStatus
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