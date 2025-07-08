namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public abstract class PersonSubClass : PersonBase
    {
        public PersonSubClass(PersonClass person, PlayerData playerData) : base(playerData)
        {
            Person = person;
        }

        protected PersonClass Person { get; }
    }
}