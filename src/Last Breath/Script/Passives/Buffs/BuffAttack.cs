namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Components.Interfaces;
    using Playground.Script.Passives.Interfaces;

    public partial class BuffAttack : Ability, ICanBuffAttack
    {
        private readonly float _additionalDamage = 1.3f;

        private float _baseMinDamageBeforBuff;
        private float _baseMaxDamageBeforBuff;

        public BuffAttack()
        {
        }

        public override Type TargetTypeComponent => typeof(AttackComponent);

        public override void ActivateAbility(IGameComponent? component)
        {
            if (component == null || component is not AttackComponent attack)
            {
                return;
            }
            _baseMinDamageBeforBuff = attack.BaseMinDamage;
            _baseMaxDamageBeforBuff = attack.BaseMaxDamage;

            attack.BaseMinDamage *= _additionalDamage;
            attack.BaseMaxDamage *= _additionalDamage;
        }

        public override void AfterBuffEnds(IGameComponent? component)
        {
            if (component == null || component is not AttackComponent attack)
            {
                return;
            }
            attack.BaseMinDamage = _baseMinDamageBeforBuff;
            attack.BaseMaxDamage = _baseMaxDamageBeforBuff;
        }

        public override void EffectAfterAttack(IGameComponent? component) => throw new System.NotImplementedException();
    }
}
