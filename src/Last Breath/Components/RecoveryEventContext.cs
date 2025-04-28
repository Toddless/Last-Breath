namespace Playground.Components
{
    using Playground.Script;
    using Playground.Script.Enemy;

    public class RecoveryEventContext(ICharacter character, RecoveryEventType type)
    {
        public ICharacter Character { get; set; } = character;
        public RecoveryEventType Type { get; set; } = type;
    }
}
