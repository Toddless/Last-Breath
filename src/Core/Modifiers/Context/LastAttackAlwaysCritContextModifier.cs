namespace Core.Modifiers.Context
{
    using Battle;
    using Enums;

    // NOTE: Конфлик с декоратором параметров по типу "атаки не могут быть критическими".
    // Модификатору не важен показатель шанса критической атаки, он выставит IsCrit/FroceCrit гарантировано.
    public class LastAttackAlwaysCritContextModifier()
        : ContextModifier(priority: ContextModifierPriority.Late, id: "Modifier_Last_Attack_Always_Crit"), IAttackModifier
    {
        public void Apply(IAttackContext context) => context.ForceCriticalAttack = context.IsLast;
    }
}
