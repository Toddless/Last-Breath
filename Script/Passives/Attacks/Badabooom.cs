namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Components;
    using Playground.Script.Passives.Interfaces;

    public partial class Badabooom : Ability, ICanDealDamage
    {
        private float _damageMultiplier = 2;

        private float _minBeforBuff;
        private float _maxBeforBuff;

        public Badabooom()
        {
            Cooldown = 4;
        }

        public override Type TargetTypeComponent =>typeof(AttackComponent);

        public override void ActivateAbility(IGameComponent? component)
        {
            if(component == null || component is not AttackComponent attack)
            {
                return;
            }
            _minBeforBuff = attack.BaseMinDamage;
            _maxBeforBuff = attack.BaseMaxDamage;

            attack.BaseMinDamage *= _damageMultiplier;
            attack.BaseMaxDamage *= _damageMultiplier;
        }

        public override void AfterBuffEnds(IGameComponent? component)
        {
            if (component == null || component is not AttackComponent attack)
            {
                return;
            }
            attack.BaseMinDamage = _minBeforBuff;
            attack.BaseMaxDamage = _maxBeforBuff;
        }

        public override void EffectAfterAttack(IGameComponent? component)
        {
           
        }
    }
}
