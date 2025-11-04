namespace Battle.Components
{
    using Core.Interfaces.Components;
    using Godot;

    internal class DefenseComponent : IDefenceComponent
    {
        private const float BaseArmor = 100f;
        private const float BaseEvade = 0f;
        private const float BaseEnergyBarrier = 0f;
        private const float BaseMaxReduceDamage = 0.7f;
        private const float BaseMaxEvade = 0.75f;

        public float Armor { get; private set; } = BaseArmor;
        public float Evade { get; private set; } = BaseEvade;
        public float EnergyBarrier { get; private set; } = BaseEnergyBarrier;
        public float MaxReduceDamage { get; private set; } = BaseMaxReduceDamage;
        public float MaxEvadeChance { get; private set; } = BaseMaxEvade;

        public void OnParameterChanges(object? sender, IModifiersChangedEventArgs args)
        {
           
        }

        public float BarrierAbsorbDamage(float amount)
        {
            if (EnergyBarrier == 0) return amount;

            float absorbed = Mathf.Min(EnergyBarrier, amount);

            // TODO: Raise event
            EnergyBarrier -= absorbed;

            return amount - absorbed;
        }
    }
}
