namespace LastBreath.Script.BattleSystem.Decorators
{
    using Core.Enums;
    using LastBreath.Script;

    public class ChainAttackOnSucceedDecorator(DecoratorPriority priority, ICharacter owner) : ActionModuleDecorator(type: ActionModule.SucceedAction, priority)
    {
        private readonly ICharacter _owner = owner;

        public override void PerformModuleAction(ICharacter target)
        {
            base.PerformModuleAction(target);
            var currentStance = _owner.CurrentStance;
            if (currentStance != null && currentStance.IsChainAttack())
                currentStance.OnAttack(target);
        }
    }
}
