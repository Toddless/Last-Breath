namespace Playground.Components
{
    using Playground.Script.Enums;

    public class Fury : BaseResource
    {
        private const float BaseMaximumAmount = 6f;
        private const float BaseRecovery = 1f;

        public Fury() : base
            (parameter: Parameter.Resource,
            type: ResourceType.Fury,
            recoveryAmount: BaseRecovery,
            maximumAmount: BaseMaximumAmount,
            new FuryRecoveryRule())
        {
        }
    }
}
