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
        /// anything not laid by a cast. Multiplies every number of the effect except duration and stacks.
        /// <para>Settable for the same reason <see cref="Duration"/> is: the caster-side application
        /// pipeline tunes the instance before the stacking rules see it, and the owner's own
        /// effectiveness knobs multiply what the cast stamped there.</para></summary>
        float Effectiveness { get; set; }

        /// <summary>Which of the three kinds of effect this one is, read off the instance: what nothing
        /// flagged and nothing ticks is a buff, what is flagged harmful is a debuff, and anything ticking
        /// damage over turns is damaging whether or not it was flagged. Decided here rather than by the
        /// record that laid it, because a passive, an item grant and a boss stage lay effects with no
        /// record at all.</summary>
        EffectGenus Genus => this is IDamageOverTurnEffect ? EffectGenus.Damaging
            : IsHarmful ? EffectGenus.Debuff
            : EffectGenus.Buff;

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
