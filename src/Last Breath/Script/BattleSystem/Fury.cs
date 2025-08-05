namespace LastBreath.Script.BattleSystem
{
    using Core.Enums;
    using LastBreath.Components;

    public class Fury : BaseResource
    {
        private const float BaseMaximumAmount = 6f;
        private const float BaseRecovery = 1f;

        public Fury() : base
            (recoveryAmount: BaseRecovery,
            maximumAmount: BaseMaximumAmount,
            resourceType: ResourceType.Fury)
        {
            LoadData();
        }
    }
}
