namespace Playground.Components
{
    public class Mana : BaseResource
    {
        private const float BaseMaximumAmount = 6f;
        private const float BaseRecovery = 1f;

        public Mana() : base
            (recoveryAmount: BaseRecovery,
            maximumAmount: BaseMaximumAmount,
            resourceType: Script.Enums.ResourceType.Mana)
        {
            LoadData();
        }
    }
}
