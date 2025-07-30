namespace Playground.Script.BattleSystem
{
    using Playground.Components;

    public class Fury : BaseResource
    {
        private const float BaseMaximumAmount = 6f;
        private const float BaseRecovery = 1f;

        public Fury() : base
            (recoveryAmount: BaseRecovery,
            maximumAmount: BaseMaximumAmount,
            resourceType: Enums.ResourceType.Fury)
        {
            LoadData();
        }
    }
}
