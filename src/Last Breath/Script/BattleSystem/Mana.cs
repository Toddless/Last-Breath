namespace LastBreath.Script.BattleSystem
{
    using Contracts.Enums;
    using LastBreath.Components;

    public class Mana : BaseResource
    {
        private const float BaseMaximumAmount = 6f;
        private const float BaseRecovery = 1f;

        public Mana() : base
            (recoveryAmount: BaseRecovery,
            maximumAmount: BaseMaximumAmount,
            resourceType: ResourceType.Mana)
        {
            LoadData();
        }
    }
}
