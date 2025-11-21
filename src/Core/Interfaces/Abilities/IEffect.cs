namespace Core.Interfaces.Abilities
{
    using Enums;
    using Entity;

    public interface IEffect
    {
        string Id { get; set; }
        StatusEffects StatusEffect { get; set; }
        int Duration { get; set; }
        int Stacks { get; set; }
        bool Permanent { get; }
        bool Expired { get; }

        void OnApply(IEntity target, IEntity source, AbilityContext context);
        void OnTick(IEntity target);
        void OnRemove(IEntity target);
        void OnStacks(IEffect newEffect);
        IEffect Clone();
    }
}
