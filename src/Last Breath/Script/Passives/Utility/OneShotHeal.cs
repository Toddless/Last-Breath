namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Components;

    public partial class OneShotHeal : Ability
    {
        private float _healAmoint = 50;

        public OneShotHeal()
        {
        
        }

        public override Type TargetTypeComponent => typeof(HealthComponent);

        public override void ActivateAbility(IGameComponent? component)
        {
            if (component == null || component is not HealthComponent health)
            {
                return;
            }
            health.CurrentHealth += _healAmoint;
        }
        public override void AfterBuffEnds(IGameComponent? component) => throw new NotImplementedException();

        public override void EffectAfterAttack(IGameComponent? component) => throw new NotImplementedException();
    }
}
