namespace Battle.TestData.Abilities.Effects
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;

    public class DamageOverTurnEffect : IEffect
    {
        public string Id { get; set; } = "Effect_Damage_Over_Turn";
        public StatusEffects StatusEffect { get; set; } = StatusEffects.None;
        public int Duration { get; set; } = 3;
        public int Stacks { get; set; } = 3;
        public bool Permanent => Duration <= 0;
        public bool Expired => Duration == 0;

        public float DamagePerTick { get; set; }


        public void OnApply(IEntity target, IEntity source, AbilityContext context)
        {
        }

        public void OnTick(IEntity target)
        {
            target.TakeDamage(DamagePerTick * Mathf.Max(1, Stacks));
        }

        public void OnRemove(IEntity target)
        {
        }

        public void OnStacks(IEffect newEffect)
        {
            Stacks += newEffect.Stacks;
            Duration = Mathf.Max(Duration, newEffect.Duration);
        }

        public IEffect Clone() => new DamageOverTurnEffect
        {
            Id = Id,
            StatusEffect = StatusEffect,
            Duration = Duration,
            Stacks = Stacks,
            DamagePerTick = DamagePerTick,
        };
    }
}
