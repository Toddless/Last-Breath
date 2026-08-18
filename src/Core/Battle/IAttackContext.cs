namespace Core.Battle
{
    using System.Collections.Generic;
    using Context;
    using Enums;
    using Entity;
    using Godot;

    public interface IAttackContext
    {
        RandomNumberGenerator Rnd { get; }
        IFightable Attacker { get; }
        IFightable Target { get; }
        IAttackContextScheduler AttackContextScheduler { get; }
        AttackResults Result { get; set; }

        /// <summary>Weapon-side scalar the attack was created with (the Physical seed), before bonuses and crits.</summary>
        float BaseDamage { get; }

        /// <summary>Damage of THIS attack split by type. Seeded at creation with Physical = <see cref="BaseDamage"/>
        /// plus the attacker's flat elemental damage parameters; mutators reshape it via
        /// <see cref="AddDamage"/>/<see cref="SetDamage"/>/<see cref="ScaleDamage"/>. The crit roll scales every component.</summary>
        IReadOnlyDictionary<DamageType, float> DamageComponents { get; }

        /// <summary>Sum of all damage components.</summary>
        float TotalDamage { get; }

        float RawCriticalChance { get; set; }
        float RawCriticalDamage { get; set; }

        /// <summary>Accuracy of THIS attack, initialized from the attacker's parameter; pre-attack modifiers may boost it.</summary>
        float RawAccuracy { get; set; }

        /// <summary>
        ///  <c>TakeDamage</c>: overwritten with the damage actually dealt to the target, split by type
        /// (post incoming-mitigation, barrier-absorbed portion included). Post-attack reactions
        /// (leech, damage-scaled DoTs, splash) should read it after the hit is applied — the whole blow
        /// through <see cref="DamageSnapshot.Total"/>, one kind of it through the indexer.
        /// </summary>
        DamageSnapshot FinalDamage { get; set; }

        bool IsCritical { get; set; }
        bool ForceCriticalAttack { get; set; }
        // TODO:
        // нужно как то быть уверенным что данные флаги были установлены единожды
        bool IsUnevadable { get; set; }
        bool IsUnblockable { get; set; }
        bool IsValid { get; }

        /// <summary>Id of the ability this attack belongs to (null = a plain basic attack).
        /// Carried onto the damage context so the presentation can play the ability's impact VFX per attack.</summary>
        string? SourceAbilityId { get; set; }

        /// <summary>Weapon the attack is delivered with (null = unarmed). Hook for weapon masteries.</summary>
        WeaponType? WeaponType => Attacker.Equipment?.Weapon?.WeaponType;

        /// <summary>Position of this attack within a multi-hit sequence (folded-in AttackMetadata). Single attacks: Index 0, TotalCount 1.</summary>
        int Index { get; set; }
        int TotalCount { get; set; }
        bool IsFirst { get; }
        bool IsLast { get; }

        /// <summary>Length of the reaction chain behind this attack: 0 = planned attack,
        /// 1 = reaction to it (counter, chain hit), 2 = reaction to a reaction... The scheduler
        /// refuses over-deep chains, so mutual reactions can never hang the battle.</summary>
        int ReactionDepth { get; }
        bool IsReaction => ReactionDepth > 0;

        void AddDamage(DamageType type, float amount);
        void SetDamage(DamageType type, float amount);

        /// <summary>Scales every damage component by <paramref name="factor"/> — the way "+X% to this attack"
        /// bonuses and the crit multiplier apply, so elemental components are boosted alongside Physical.</summary>
        void ScaleDamage(float factor);

        /// <summary>The one way to spawn a reaction attack: inherits the scheduler and rng of the
        /// triggering attack, deepens the chain by one, rolls crit from the reactor's parameter.</summary>
        IAttackContext CreateReaction(IFightable attacker, IFightable target, float baseDamage);

        bool Schedule();
    }
}
