namespace Playground.Components
{
    using Playground.Script.Enums;

    public class ComboPoints : BaseResource
    {
        private const float BaseMaximumAmount = 6f;
        private const float BaseRecovery = 1f;

        public ComboPoints() : base
            (parameter: Parameter.Resource,
            type: ResourceType.Combopoints,
            recoveryAmount: BaseRecovery,
            maximumAmount: BaseMaximumAmount,
            new ComboRecoveryRule())
        {
        }
    }
}
