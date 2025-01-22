namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Script.Passives.Interfaces;

    public partial class BuffAttack : Ability<AttackComponent, BaseEnemy>, ICanBuffAttack
    {
        private readonly float _additionalDamage = 1.3f;

        private float _baseMinDamageBeforBuff;
        private float _baseMaxDamageBeforBuff;

        public BuffAttack() : base()
        {
        }

        public override void ActivateAbility(AttackComponent? component)
        {
            _baseMinDamageBeforBuff = component.BaseMinDamage;
            _baseMaxDamageBeforBuff = component.BaseMaxDamage;

            component.BaseMinDamage *= _additionalDamage;
            component.BaseMaxDamage *= _additionalDamage;
        }

        public override void SetTargetCharacter(BaseEnemy? target) => throw new NotImplementedException();


        //public override void AfterBuffEnds(AttackComponent? component)
        //{
        //    // bad idea. If this buff lasts for another turn and the target gets a debuff, that lasts for 3 turns and reduces e.g. damage, 
        //    // ffter this buff ends, it is reset to the value it had before the debuff.
        //    component.BaseMinDamage = _baseMinDamageBeforBuff;
        //    component.BaseMaxDamage = _baseMaxDamageBeforBuff;
        //}
    }
}
