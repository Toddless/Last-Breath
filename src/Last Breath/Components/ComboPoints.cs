namespace LastBreath.Components
{
    public class ComboPoints : BaseResource
    {
        private const float BaseMaximumAmount = 6f;
        private const float BaseRecovery = 1f;

        public ComboPoints() : base
            (recoveryAmount: BaseRecovery,
            maximumAmount: BaseMaximumAmount,
            resourceType: Script.Enums.ResourceType.Combopoints)
        {
            LoadData();
        }
    }
}
