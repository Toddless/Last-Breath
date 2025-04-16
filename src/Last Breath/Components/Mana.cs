namespace Playground.Components
{
    using Playground.Script.Enums;

    public class Mana : BaseResource
    {
        private const float BaseMaximumAmount = 6f;
        private const float BaseRecovery = 1f;

        public Mana() : base
            (parameter: Parameter.Resource,
            type: ResourceType.Mana,
            recoveryAmount: BaseRecovery,
            maximumAmount: BaseMaximumAmount,
            new ManaRecoveryRule())
        {
        }
    }
}
