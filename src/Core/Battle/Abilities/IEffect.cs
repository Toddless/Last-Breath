namespace Core.Battle.Abilities
{
    using System;
    using System.Threading.Tasks;
    using Enums;
    using Entity;
    using Interfaces;

    public interface IEffect : IIdentifiable, IDisplayable
    {
        IFightable? Target { get; }
        StatusEffects Status { get; set; }
        int Duration { get; set; }
        int MaxStacks { get; set; }
        string Source { get; }

        /// <summary>Effectiveness of the cast that laid this instance, stamped on application; one for
        /// anything not laid by a cast. Multiplies every number of the effect except duration and stacks.</summary>
        float Effectiveness { get; }

        /// <summary>The cast that laid this instance; nothing at all for a passive, an item grant or a
        /// boss stage. What a rule reading "only what is MINE" would be decided on.
        /// <para>Declared without a default on purpose, exactly like <see cref="Effectiveness"/>: an
        /// implementor that never answers it would quietly belong to nobody, and "nobody" is a real
        /// answer here rather than an absent one.</para></summary>
        AbilityTrace Trace { get; }

        /// <summary>Buff/debuff split for the UI counters. Default false (a buff); debuff classes
        /// override. Unclassified effects count as buffs until the design pass says otherwise.</summary>
        bool IsHarmful => false;

        /// <summary>What strength of dispel can take this effect off. Weak unless the canon names the
        /// effect stronger; seals are absolute and no dispel takes them.</summary>
        EffectPower Power => EffectPower.Weak;

        event Action<int>? DurationChanged;

        Task Apply(EffectApplyingContext context);
        void OnStackChanged(int currentStack);
        void Remove();
        void TurnStart();
        void TurnEnd();
        bool IsStronger(IEffect otherEffect);
        IEffect Copy();

        /// <summary>
        /// Makes an effect that has ALREADY landed last longer, and the only road that does: raising
        /// <see cref="Duration"/> from outside bypasses the budget every instance extends within.
        /// Extending is not applying — the mutators that shape a duration while the effect is being
        /// applied (scaling, flat bonuses, control resistance, a re-application refreshing the
        /// standing stack) run before the effect stands and are not charged here. An effect whose
        /// duration has run out is past extending too — that would be a resurrection.
        /// </summary>
        /// <returns>Turns actually added: less than asked for once the budget runs short, zero once
        /// it is spent, so a caller can tell a real extension from one the cap swallowed.</returns>
        int Extend(int turns);
    }
}
