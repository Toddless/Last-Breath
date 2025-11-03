namespace LastBreath.Components
{
    using Core.Enums;

    public class ComboPoints : BaseResource
    {
        private const float BaseMaximumAmount = 6f;
        private const float BaseRecovery = 1f;

        public ComboPoints() : base
            (recoveryAmount: BaseRecovery,
            maximumAmount: BaseMaximumAmount,
            resourceType: ResourceType.Combopoints)
        {
        }
    }
}
