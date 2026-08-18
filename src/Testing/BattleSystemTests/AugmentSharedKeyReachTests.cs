namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;

    /// <summary>
    /// Where every record standing on a shared key actually lands, written out. A record reaches an
    /// ability through the fitting rule (tags, or a named ability) and WORKS on it through the key —
    /// two questions answered in two places, and nothing anywhere holds them against each other. The
    /// gap between them is legal and deliberate: a tag promises a fit, not a result. It is also where
    /// the ugliest thing in the catalog lives — a record that fits an ability, charges it (fewer
    /// stacks, more mana) and delivers nothing, which is not an improvement the player declined but a
    /// straight loss he cannot see.
    ///
    /// So the split is written down rather than computed: for each record, the abilities it fits and
    /// works on, and the abilities it fits and is inert on. The table is literal for the same reason
    /// <see cref="AugmentParameterTableTests"/> is — read off production it would agree with whatever
    /// production became. A new tag on an ability, a new tag on a record or a key registered one place
    /// fewer all change these lists, and every one of those changes has to be looked at and written
    /// down here, which is the whole point: inertness stays allowed and stops being invisible.
    ///
    /// Hidden abilities are out of the walk. They are never socketed, so no augment is ever offered to
    /// one and where a record would land on them is not a fact about the catalog.
    /// </summary>
    [TestClass]
    public class AugmentSharedKeyReachTests
    {
        /// <summary>The move a probe makes on the key to find out whether anything reads it. Any figure
        /// would do — what is measured is that the number moved at all.</summary>
        private const float Probe = 7f;

        /// <summary>Health, mana and barrier the probe fighters carry: enough that no cast is refused
        /// for want of a resource and no figure is clamped by a pool that ran out.</summary>
        private const float ProbeVitals = 10000f;

        /// <summary>
        /// Abilities whose payload a bare cast cannot reach, so the walk below cannot speak for them.
        /// Double Strike lays its debuffs off REAL attacks, and an attack needs the engine's own roll
        /// face — which nothing outside Godot may build. Written out rather than skipped silently: the
        /// walk asserts these lay nothing at all, so an ability that becomes reachable turns up here
        /// instead of quietly staying unmeasured.
        /// </summary>
        /// <para>Discharge joined at CL-3b: its only scalable content is the stage-3 barrier refund, and
        /// the probe casts at the base stage where there is nothing to refund yet. The figure IS an
        /// <c>EffectValue</c> — <c>BarrierFromDamageEffect</c> — it simply is not laid this early.</para>
        private static readonly string[] s_beyondTheProbe = ["Ability_Double_Strike", "Ability_Discharge"];

        /// <summary>
        /// Abilities that own effectiveness for what AUGMENTS hang on them rather than for anything they
        /// lay themselves — the head butt, whose only payload is a stun (control carries no figure and is
        /// out of the canon by design), and the pressure, which lays nothing at all. Both are reached by
        /// on-hit debuff appliers through <c>attack</c>, and every debuff those lay reads the effectiveness
        /// of the cast the impact came out of, so declaring the key is what finishes that chain instead of
        /// leaving "+debuff effectiveness" seated on a key nobody registered.
        /// <para>Written out rather than folded into the walk: the claim asserted for them is the mirror of
        /// the usual one — they must lay nothing scalable of their own, so an ability that grows content
        /// with a figure in it turns up here instead of quietly staying unmeasured.</para>
        /// </summary>
        private static readonly string[] s_declaresForWhatAugmentsLay = ["Ability_Head_Butt", "Ability_Increasing_Pressure"];

        /// <summary>
        /// Where a record is knowingly worth more than it charges: the ability reads one of its moves
        /// and has no concept for the other, and the owner looked at that and let it stand. Written out
        /// per record so a NEW half-arrival still fails — none of these three counts what it lays, so
        /// "+effectiveness for two stacks" costs them nothing.
        /// </summary>
        private static readonly Dictionary<string, string[]> s_knowinglyFree = new(StringComparer.Ordinal)
        {
            // Ares left the list at the owner's re-markup: the blessing lost its "stacks" tag, so the
            // record does not reach it at all any more.
            ["Augment_Add_Effectiveness_Reduce_Stacks"] =
                ["Ability_Jar_Of_Poison", "Ability_Poison_Coating", "Ability_Porcupine"]
        };

        /// <summary>
        /// The mirror of <see cref="s_knowinglyFree"/>: there the record is worth more than it charges,
        /// here it charges and hands nothing back. SANCTIONED by the owner, 2026-08-15: an ability with no
        /// stun and no augment laying one simply takes the surcharge, and a bad bargain the player can walk
        /// into is a possibility the catalog is allowed to offer, not a fault to be repaired.
        /// Found at CL-7b when the cost half of the stun record first got a row: it moves CostValue on
        /// fourteen abilities and StunDuration on three, so on the eleven below it is paid for in mana and
        /// gives no stun.
        /// <para>What spreads it is <c>duration</c>, the wider of its two tags
        /// (<c>["control", "duration"]</c>) — there is no <c>cost</c> tag in the vocabulary at all,
        /// whatever the card's prose says. Kept as a literal count rather than narrowed, because the
        /// spread is the design.</para>
        /// <para>An entry here is not permanent: a stun applier seated through a granted tag gives the
        /// ability a StunDuration to move, and the seating turns from a surcharge into the whole bargain.
        /// The count survives that — the ability leaves this list and joins the record's works row — which
        /// is why it is written per ability rather than as one exemption for the record.</para>
        /// </summary>
        private static readonly Dictionary<string, string[]> s_costWithoutGoodsByDesign = new(StringComparer.Ordinal)
        {
            ["Augment_Extend_Stun_Add_Cost"] =
                ["Ability_Ares_Blessing", "Ability_Berserk_Fury", "Ability_Critical_Calculation", "Ability_Dark_Shroud",
                 "Ability_Deep_Freeze", "Ability_Double_Strike", "Ability_Ice_Aegis", "Ability_Jar_Of_Poison",
                 "Ability_Poison_Coating", "Ability_Porcupine", "Ability_Static_Armor"],

            // E-2a, and the same shape one wave on: the scales-for-turns record carries a tag that reaches
            // the damaging family and a tag that reaches everything, so on the ten below it adds turns of
            // waiting and no damage. Not a defect of the record — a consequence of the two tags its design
            // line gives it, and the narrower alternative (binding it to abilities that declare scales) is
            // the owner's to pick.
            ["Augment_Increasing_Scales_Add_Cooldown"] =
                ["Ability_Ares_Blessing", "Ability_Critical_Calculation", "Ability_Dark_Shroud", "Ability_Ice_Aegis",
                 "Ability_Overload", "Ability_Poison_Coating", "Ability_Poison_Explosion", "Ability_Porcupine",
                 "Ability_Sacrifice", "Ability_Static_Armor"]
        };

        private static IEnumerable<string> Accepted(string augmentId) =>
        [
            .. s_knowinglyFree.TryGetValue(augmentId, out string[]? free) ? free : [],
            .. s_costWithoutGoodsByDesign.TryGetValue(augmentId, out string[]? surcharged) ? surcharged : []
        ];

        /// <summary>Every gate open and every stage reached: the walk needs the cast to lay everything
        /// it has, because a payload the dice withheld would read as a payload nothing scales.</summary>
        private sealed class SteadyRoll : IRandomNumberGenerator
        {
            public float RandFloat() => 0f;

            public float RandFloatRange(float min, float max) => max;

            public int RandIntRange(int min, int max) => min;

            public float RandFloatN(float mean, float deviation) => mean;

            public uint RandInt() => 0;

            public long RandWeighted(float[] weights) => 0;

            public long RandWeighted(ReadOnlySpan<float> weights) => 0;

            public void Randomize()
            {
            }
        }

        /// <summary>
        /// Every move a shipped record makes on a shared key, and the two lists that move comes to:
        /// the abilities that declared the concept and the abilities that merely let the record in.
        /// </summary>
        private static readonly (string Augment, string Parameter, string[] Works, string[] Inert)[] s_reach =
        [
            // Written down for the first time at CL-7b, found by the completeness walk rather than by
            // anybody noticing: both records are OLDER than the crit pair and had been standing on shared
            // keys unledgered all along. The scales are declared by every damaging ability through
            // RegisterDamageParameters, so the pair works almost everywhere it lands; the Static Armor is
            // the one exception, its detonation carrying scales of its own instead.
            ("Augment_Increasing_Scales", AbilityParameter.WeaponDamageScale,
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Deep_Freeze", "Ability_Discharge",
                 "Ability_Double_Strike", "Ability_Head_Butt", "Ability_Ice_Block", "Ability_Ice_Shards",
                 "Ability_Increasing_Pressure", "Ability_Series_Of_Attacks"],
                ["Ability_Static_Armor"]),
            ("Augment_Increasing_Scales", AbilityParameter.SpellDamageScale,
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Deep_Freeze", "Ability_Discharge",
                 "Ability_Double_Strike", "Ability_Head_Butt", "Ability_Ice_Block", "Ability_Ice_Shards",
                 "Ability_Increasing_Pressure", "Ability_Series_Of_Attacks"],
                ["Ability_Static_Armor"]),

            // The cost half of the stun bargain. Cost is a base-contract key every ability registers, so
            // this half never misses — which is exactly why it needed writing down: the row above it
            // (StunDuration) is the half that can, and a bargain is only honest when both are measured.
            ("Augment_Extend_Stun_Add_Cost", AbilityParameter.CostValue,
                ["Ability_Ares_Blessing", "Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Critical_Calculation",
                 "Ability_Dark_Shroud", "Ability_Deep_Freeze", "Ability_Double_Strike", "Ability_Head_Butt",
                 "Ability_Ice_Aegis", "Ability_Ice_Block", "Ability_Jar_Of_Poison", "Ability_Poison_Coating",
                 "Ability_Porcupine", "Ability_Static_Armor"],
                []),
            // Unbound at CL-7 and written down here for the first time: the crit bonuses were declared by
            // MulticastAbility alone, so the 'critical' tag seated these two on twice the abilities that
            // read them. CL-7c closed eleven of the twelve; CL-7d closed the twelfth, the Armageddon,
            // which had been the one ability in the book that rolled no crit at all. **Both rows are full
            // now — the tag reaches twelve and all twelve read it, and an empty inert column is the claim.**
            ("Augment_Additional_Crit_Damage", AbilityParameter.CriticalDamageBonus,
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Deep_Freeze", "Ability_Discharge",
                 "Ability_Double_Strike", "Ability_Head_Butt", "Ability_Ice_Block", "Ability_Ice_Shards",
                 "Ability_Increasing_Pressure", "Ability_Overload", "Ability_Series_Of_Attacks", "Ability_Static_Armor"],
                []),
            ("Augment_Additional_Crit_Chance", AbilityParameter.CriticalChanceBonus,
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Deep_Freeze", "Ability_Discharge",
                 "Ability_Double_Strike", "Ability_Head_Butt", "Ability_Ice_Block", "Ability_Ice_Shards",
                 "Ability_Increasing_Pressure", "Ability_Overload", "Ability_Series_Of_Attacks", "Ability_Static_Armor"],
                []),

            // Generalised at CL-4 from Ares Blessing's private key. It reaches by "health" and "buff",
            // and Ares is the only ability in the book that raises a health CEILING — the rest of the
            // reach is legal silence until a second health-buffer declares the key.
            ("Augment_Health_Bonus", AbilityParameter.HealthBonus,
                ["Ability_Ares_Blessing"],
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Critical_Calculation", "Ability_Dark_Shroud",
                 "Ability_Ice_Aegis", "Ability_Overload", "Ability_Poison_Coating", "Ability_Porcupine", "Ability_Sacrifice"]),

            // Generalised at CL-4; the abilityId that used to keep it off strangers came off with it.
            // Two owners now — the Shroud's per-turn heal and the Aegis's heal-under-shield, which had
            // been the same concept under two private names.
            ("Augment_Additional_Health_Regen", AbilityParameter.HealthRegeneration,
                ["Ability_Dark_Shroud"],
                ["Ability_Ares_Blessing", "Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Discharge",
                 "Ability_Sacrifice", "Ability_Static_Armor"]),

            // Generalised at CL-4. One owner and no second one in sight: the Deep Freeze execute is a
            // share of health against a stack COUNT, so it stays on a key of its own.
            ("Augment_Reduce_Execution_Threshold", AbilityParameter.ExecutionThreshold,
                ["Ability_Poison_Explosion"],
                []),

            // Deep Freeze joined the inert list at the owner's re-markup: its doc hands it "damage"
            // while the ability itself carries no damage numbers yet (code catches up in CL-3).
            ("Augment_More_Attack_Damage", AbilityParameter.DamageMultiplier,
                ["Ability_Double_Strike", "Ability_Series_Of_Attacks"],
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Deep_Freeze", "Ability_Discharge",
                 "Ability_Head_Butt", "Ability_Ice_Block", "Ability_Ice_Shards", "Ability_Increasing_Pressure",
                 "Ability_Static_Armor"]),

            // Most of the inert abilities are reached through the bare tag "duration" alone. Sacrifice
            // left the list at the re-markup: it lost "duration" and the record stopped reaching it.
            ("Augment_Poison_Duration", AbilityParameter.PoisonDuration,
                ["Ability_Jar_Of_Poison", "Ability_Poison_Coating"],
                ["Ability_Ares_Blessing", "Ability_Berserk_Fury", "Ability_Critical_Calculation", "Ability_Dark_Shroud",
                 "Ability_Deep_Freeze", "Ability_Double_Strike", "Ability_Ice_Aegis", "Ability_Poison_Explosion",
                 "Ability_Porcupine", "Ability_Static_Armor"]),

            // The worst record in the catalog and the reason this walk exists: where it is inert it
            // still charges its fifty mana, so every name in the second list is a straight loss the
            // player cannot see. Armageddon reached the works list at the owner's re-markup ("control"
            // and "stun" arrived with the doc), closing the wave-E hole the old comment named.
            // The aegis joined the inert list at the catalog cleanup: its stun existed only through the
            // removed stun-attackers augment, so the duration key came off the ability with the branch.
            // Sacrifice left the row at the re-markup: it lost "duration".
            ("Augment_Extend_Stun_Add_Cost", AbilityParameter.StunDuration,
                ["Ability_Armageddon", "Ability_Head_Butt", "Ability_Ice_Block"],
                ["Ability_Ares_Blessing", "Ability_Berserk_Fury", "Ability_Critical_Calculation", "Ability_Dark_Shroud",
                 "Ability_Deep_Freeze", "Ability_Double_Strike", "Ability_Ice_Aegis", "Ability_Jar_Of_Poison",
                 "Ability_Poison_Coating", "Ability_Porcupine", "Ability_Static_Armor"]),

            // The jar carries "projectile" now (the doc's word for how it travels) and declares no
            // projectile count — a socket that fits and moves nothing until the count generalises.
            // The jar stopped being inert at CL-3b: it declares a count of one, and its throw loop reads it.
            ("Augment_Additional_Projectiles", AbilityParameter.ProjectileCount,
                ["Ability_Ice_Shards", "Ability_Jar_Of_Poison"], []),

            // Both effectiveness records stand on one key in one direction, so where they meet they are
            // one offer and the better works. What separates them is which abilities are offered the
            // deal — tags, not keys, which is the whole segmentation of the family.
            // The re-markup swapped the inert pair: Static Armor lost "buff" and left the row while
            // Overload gained it and arrived (declaring no effectiveness — inert until CL-3).
            ("Augment_Buff_Effectiveness", AbilityParameter.Effectiveness,
                // Overload stopped being inert at CL-3b: its charge multiplier became an EffectValue.
                ["Ability_Ares_Blessing", "Ability_Critical_Calculation", "Ability_Dark_Shroud", "Ability_Ice_Aegis",
                 "Ability_Overload", "Ability_Poison_Coating", "Ability_Porcupine"],
                ["Ability_Sacrifice"]),

            // Discharge and Static Armor arrived with the doc's "recovery" (their barrier refunds);
            // neither declares effectiveness yet, so both sit inert.
            ("Augment_Recovery_Effectiveness", AbilityParameter.Effectiveness,
                // Both stopped being inert at CL-3b: their barrier refunds travel as EffectValues now.
                ["Ability_Ares_Blessing", "Ability_Dark_Shroud", "Ability_Discharge", "Ability_Static_Armor"],
                []),

            // The record the debuff tag was handed out for: it reaches the four abilities that lay
            // something on their target and read how strongly it lands, and nothing else in the book.
            // The berserker arrived with the doc's "debuff" (his fury burns its own bearer); he does
            // not declare effectiveness, so the record sits inert on him.
            ("Augment_Debuff_Effectiveness", AbilityParameter.Effectiveness,
                ["Ability_Deep_Freeze", "Ability_Double_Strike", "Ability_Ice_Aegis", "Ability_Ice_Block"],
                ["Ability_Berserk_Fury"]),

            // Both halves of one bargain, so both lists have to be the SAME list: an ability where the
            // stacks come off and the effectiveness does not is charged for nothing.
            // Ares and Static Armor left every row of this record at the re-markup: both lost "stacks",
            // the only tag that carried it onto them.
            ("Augment_Add_Effectiveness_Reduce_Stacks", AbilityParameter.Stacks,
                ["Ability_Critical_Calculation", "Ability_Dark_Shroud", "Ability_Deep_Freeze", "Ability_Double_Strike",
                 "Ability_Ice_Aegis"],
                ["Ability_Berserk_Fury", "Ability_Jar_Of_Poison", "Ability_Poison_Coating", "Ability_Porcupine"]),

            ("Augment_Add_Effectiveness_Reduce_Stacks", AbilityParameter.Effectiveness,
                ["Ability_Critical_Calculation", "Ability_Dark_Shroud", "Ability_Deep_Freeze",
                 "Ability_Double_Strike", "Ability_Ice_Aegis", "Ability_Jar_Of_Poison", "Ability_Poison_Coating",
                 "Ability_Porcupine"],
                ["Ability_Berserk_Fury"]),

            // The seven records generalised at CL-7c, each off the private key of the one ability it was
            // written for. Each is judged by the tags of its card now, and every one of them has exactly
            // ONE owner — so the second column below IS the price of the unpinning, paid in the same coin
            // CL-4 named: a record that used to be pinned and always worked now fits a family and works in
            // one place. Legal by "a tag promises fitting, not work", and written out so it stays counted.
            ("Augment_Cooldown_Chance", AbilityParameter.CooldownReductionChance,
                ["Ability_Porcupine"],
                ["Ability_Ares_Blessing", "Ability_Critical_Calculation", "Ability_Dark_Shroud", "Ability_Ice_Aegis",
                 "Ability_Overload", "Ability_Poison_Coating", "Ability_Sacrifice"]),

            // The widest of the seven, and the one whose card tag is the widest: 'activation' is worn by
            // most of the book, while resetting one's own wait is the ice block's alone.
            ("Augment_Reset_Chance", AbilityParameter.CooldownResetChance,
                ["Ability_Ice_Block"],
                ["Ability_Ares_Blessing", "Ability_Armageddon", "Ability_Critical_Calculation", "Ability_Dark_Shroud",
                 "Ability_Deep_Freeze", "Ability_Discharge", "Ability_Ice_Aegis", "Ability_Ice_Shards",
                 "Ability_Overload", "Ability_Poison_Coating", "Ability_Poison_Explosion", "Ability_Porcupine",
                 "Ability_Sacrifice", "Ability_Static_Armor"]),

            // Unbound onto its design-line tags and moved onto the book's key in the same pass. One owner,
            // so the second column is the price of the unpinning, as with the seven of CL-7c.
            ("Augment_Heal_On_Hit", AbilityParameter.HealOnHit,
                ["Ability_Porcupine"],
                ["Ability_Ares_Blessing", "Ability_Critical_Calculation", "Ability_Dark_Shroud", "Ability_Ice_Aegis",
                 "Ability_Overload", "Ability_Poison_Coating", "Ability_Sacrifice"]),

            ("Augment_Heal_From_Empowered_Ability_Damage", AbilityParameter.HealFromEmpoweredDamage,
                ["Ability_Sacrifice"],
                ["Ability_Ares_Blessing", "Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Dark_Shroud",
                 "Ability_Deep_Freeze", "Ability_Discharge", "Ability_Double_Strike", "Ability_Head_Butt",
                 "Ability_Ice_Block", "Ability_Ice_Shards", "Ability_Increasing_Pressure", "Ability_Overload",
                 "Ability_Series_Of_Attacks", "Ability_Static_Armor"]),

            // Both halves of one record, so both lists are the same list — the double strike declares the
            // health give-back and the mana one, and an ability with only one of them would be sold half
            // a bargain.
            ("Augment_Restore_Mana_Health_On_Hit", AbilityParameter.HealthRestore,
                ["Ability_Double_Strike"],
                ["Ability_Berserk_Fury", "Ability_Head_Butt", "Ability_Increasing_Pressure", "Ability_Series_Of_Attacks"]),
            ("Augment_Restore_Mana_Health_On_Hit", AbilityParameter.ManaRestore,
                ["Ability_Double_Strike"],
                ["Ability_Berserk_Fury", "Ability_Head_Butt", "Ability_Increasing_Pressure", "Ability_Series_Of_Attacks"]),

            ("Augment_Stage_Four_Damage", AbilityParameter.StageFourDamage,
                ["Ability_Ice_Block"],
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Deep_Freeze", "Ability_Discharge",
                 "Ability_Double_Strike", "Ability_Head_Butt", "Ability_Ice_Aegis", "Ability_Ice_Shards",
                 "Ability_Increasing_Pressure", "Ability_Overload", "Ability_Series_Of_Attacks", "Ability_Static_Armor"]),

            // Accuracy went to the whole attacking family at CL-7d, so this row has no inert column left:
            // every ability the 'attack' tag carries it onto builds its own attack contexts and stamps the
            // bonus on them. The Armageddon is not among them and not missing from them — it never fits
            // this record (no 'attack' tag) and its direct hits are not dodged, so accuracy has no meaning
            // on it and it declares none.
            ("Augment_Accuracy", AbilityParameter.AccuracyBonus,
                ["Ability_Berserk_Fury", "Ability_Double_Strike", "Ability_Head_Butt", "Ability_Increasing_Pressure",
                 "Ability_Series_Of_Attacks"],
                []),
            ("Augment_Attack_Random_Target", AbilityParameter.SplashShare,
                ["Ability_Increasing_Pressure"],
                ["Ability_Berserk_Fury", "Ability_Double_Strike", "Ability_Head_Butt", "Ability_Series_Of_Attacks"]),

            // E-2a: the base contract stops being universal-only. Each of these is judged by the tag its
            // design line gives it, so for the first time the cost/cooldown/scale family has a reach worth
            // measuring at all — the four older records of that family claim the whole book and never could.
            // Three of the scale-tagged abilities never declare the coefficients: the aegis and the static
            // armor carry scales inside their own plans, and the poison explosion's damage IS the stacks
            // it consumes. They wear the tag and the record sits silent on them.
            // E-2b: the umbrella of the effectiveness family. It rides 'effect' — worn by every applier —
            // so it is the widest of the four, and where it meets any of the three segmented records they
            // are one offer on one key in one direction and only the stronger works.
            // Three appliers lay something and never declared the key: Armageddon's stun and the
            // berserker's fury are content the ability builds with plain numbers, and the sacrifice's
            // charge is a bill on the NEXT cast rather than a figure of this one.
            ("Augment_Applied_Effectiveness", AbilityParameter.Effectiveness,
                ["Ability_Ares_Blessing", "Ability_Critical_Calculation", "Ability_Dark_Shroud", "Ability_Deep_Freeze",
                 "Ability_Discharge", "Ability_Double_Strike", "Ability_Head_Butt", "Ability_Ice_Aegis",
                 "Ability_Ice_Block", "Ability_Jar_Of_Poison", "Ability_Overload", "Ability_Poison_Coating",
                 "Ability_Porcupine", "Ability_Static_Armor"],
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Sacrifice"]),

            ("Augment_Weapon_Scale", AbilityParameter.WeaponDamageScale,
                ["Ability_Armageddon", "Ability_Deep_Freeze", "Ability_Discharge", "Ability_Double_Strike",
                 "Ability_Head_Butt", "Ability_Ice_Block", "Ability_Ice_Shards", "Ability_Increasing_Pressure",
                 "Ability_Jar_Of_Poison", "Ability_Series_Of_Attacks"],
                ["Ability_Ice_Aegis", "Ability_Poison_Explosion", "Ability_Static_Armor"]),
            ("Augment_Spell_Scale", AbilityParameter.SpellDamageScale,
                ["Ability_Armageddon", "Ability_Deep_Freeze", "Ability_Discharge", "Ability_Double_Strike",
                 "Ability_Head_Butt", "Ability_Ice_Block", "Ability_Ice_Shards", "Ability_Increasing_Pressure",
                 "Ability_Jar_Of_Poison", "Ability_Series_Of_Attacks"],
                ["Ability_Ice_Aegis", "Ability_Poison_Explosion", "Ability_Static_Armor"]),

            // The widest half-arrival in the catalog, and it comes straight out of the design line's own
            // tags: 'scale' reaches the damaging family, 'cooldown' reaches EVERY ability, and the record
            // carries both. Where only the surcharge lands the player pays turns for nothing — the same
            // bargain shape the owner sanctioned for the stun record, written out per ability below.
            // The berserker takes the scales and not the tag: he reaches this record through 'cooldown'.
            ("Augment_Increasing_Scales_Add_Cooldown", AbilityParameter.WeaponDamageScale,
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Deep_Freeze", "Ability_Discharge",
                 "Ability_Double_Strike", "Ability_Head_Butt", "Ability_Ice_Block", "Ability_Ice_Shards",
                 "Ability_Increasing_Pressure", "Ability_Jar_Of_Poison", "Ability_Series_Of_Attacks"],
                ["Ability_Ares_Blessing", "Ability_Critical_Calculation", "Ability_Dark_Shroud", "Ability_Ice_Aegis",
                 "Ability_Overload", "Ability_Poison_Coating", "Ability_Poison_Explosion", "Ability_Porcupine",
                 "Ability_Sacrifice", "Ability_Static_Armor"]),
            ("Augment_Increasing_Scales_Add_Cooldown", AbilityParameter.SpellDamageScale,
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Deep_Freeze", "Ability_Discharge",
                 "Ability_Double_Strike", "Ability_Head_Butt", "Ability_Ice_Block", "Ability_Ice_Shards",
                 "Ability_Increasing_Pressure", "Ability_Jar_Of_Poison", "Ability_Series_Of_Attacks"],
                ["Ability_Ares_Blessing", "Ability_Critical_Calculation", "Ability_Dark_Shroud", "Ability_Ice_Aegis",
                 "Ability_Overload", "Ability_Poison_Coating", "Ability_Poison_Explosion", "Ability_Porcupine",
                 "Ability_Sacrifice", "Ability_Static_Armor"]),
            ("Augment_Increasing_Scales_Add_Cooldown", AbilityParameter.Cooldown,
                ["Ability_Ares_Blessing", "Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Critical_Calculation",
                 "Ability_Dark_Shroud", "Ability_Deep_Freeze", "Ability_Discharge", "Ability_Double_Strike",
                 "Ability_Head_Butt", "Ability_Ice_Aegis", "Ability_Ice_Block", "Ability_Ice_Shards",
                 "Ability_Increasing_Pressure", "Ability_Jar_Of_Poison", "Ability_Overload", "Ability_Poison_Coating",
                 "Ability_Poison_Explosion", "Ability_Porcupine", "Ability_Sacrifice", "Ability_Series_Of_Attacks",
                 "Ability_Static_Armor"],
                []),

            // Six abilities read their stacks key into what they lay; the rest wear the tag and lay their
            // payload from the canon, where the ability's number is never consulted. The berserker is the
            // named case of the second kind: his fury is built with a stack of one, literally.
            ("Augment_Additional_Stacks", AbilityParameter.Stacks,
                ["Ability_Critical_Calculation", "Ability_Dark_Shroud", "Ability_Deep_Freeze", "Ability_Double_Strike",
                 "Ability_Ice_Aegis", "Ability_Ice_Block"],
                ["Ability_Ares_Blessing", "Ability_Berserk_Fury", "Ability_Jar_Of_Poison", "Ability_Overload",
                 "Ability_Poison_Coating", "Ability_Porcupine", "Ability_Sacrifice"]),

            // The charge is the sacrifice's concept alone; the overload wears 'empowered' for the charge it
            // ARMS on the next cast rather than for charges of its own.
            ("Augment_Additional_Charges", AbilityParameter.Charges,
                ["Ability_Sacrifice"],
                ["Ability_Overload"]),

            // Both wearers of 'consume' own the concept and read it, so this one has no silent seating at
            // all: the sacrifice exchanges health for damage, the overload exchanges mana for it.
            ("Augment_Consume_Effectiveness", AbilityParameter.ConsumeEffectiveness,
                ["Ability_Overload", "Ability_Sacrifice"],
                []),
        ];

        [TestMethod]
        public void EveryRecordThatMovesASharedKeyHasARowAtAll()
        {
            // What the ledger could not say about itself. The walk below reads the table and the table
            // alone, so a record that never got a row was never asked anything — which is how two
            // legendary crit records came off their abilityId at CL-7, landed on twelve abilities,
            // worked on six, and reddened nothing. The source here is the registry's OWN record-to-key
            // map, not a guess from what a record fits: guessing from fitting reports every parameter
            // every fitting ability happens to declare, which is a page of noise and no claim.
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            HashSet<string> shared = SharedKeys();
            var written = s_reach.Select(row => (row.Augment, row.Parameter)).ToHashSet();
            List<string> unwritten = [];

            foreach (AbilityAugmentData record in catalog.All)
                foreach (string parameter in registry.ParametersMovedBy(record.Id))
                    if (shared.Contains(parameter) && !written.Contains((record.Id, parameter)))
                        unwritten.Add($"{record.Id} moves '{parameter}' and the ledger says nothing about it");

            Assert.AreEqual(0, unwritten.Count,
                $"records on a shared key with no row in the ledger:\n  {string.Join("\n  ", unwritten)}");
        }

        /// <summary>Every shared key of the book, read by reflection so the list cannot drift.</summary>
        private static HashSet<string> SharedKeys() =>
            [.. typeof(AbilityParameter)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(string))
                .Select(field => field.GetValue(null))
                .OfType<string>()];

        [TestMethod]
        public void EveryRecordOnASharedKeyReachesExactlyTheAbilitiesWrittenDown()
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var report = new StringBuilder();

            foreach ((string augmentId, string parameter, string[] works, string[] inert) in s_reach)
            {
                AbilityAugmentData? record = catalog.Find(augmentId);
                Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");

                (List<string> reached, List<string> ignored) = Landing(registry, record, parameter);

                Disagreement(report, $"{augmentId} on {parameter} — works on", works, reached);
                Disagreement(report, $"{augmentId} on {parameter} — inert on", inert, ignored);
            }

            Assert.AreEqual(0, report.Length,
                $"the reach of a shared-key record no longer matches what is written down:\n{report}");
        }

        [TestMethod]
        public async Task EveryAbilityThatDeclaresEffectivenessLaysSomethingThatReadsIt()
        {
            // The gap this walk exists for. Declaring the key and reading it are two different acts, and
            // between them a record can be offered, fitted, seated and PAID for while moving nothing at
            // all: the parameter shows a decorator, the multiplier reaches the effect, and every figure
            // the effect carries was written as a plain number that never asks for it. The Ice Aegis
            // shipped in exactly that state — two records worked on it by every measure except the only
            // one that matters.
            //
            // So the cast is actually run, and what is looked for among the effects it left standing is
            // an EffectValue: a figure that CANNOT be read except through the multiplier. Holding one is
            // the whole of the claim, because the type is the only way to build such a figure and the
            // only way to spend it is to ask the effect for it.
            AbilityProvider registry = ShippedAbilityData.Abilities();
            List<string> declaring = [];

            foreach (string abilityId in registry.KnownAbilityIds.Where(id => !registry.IsHidden(id)).OrderBy(id => id, StringComparer.Ordinal))
                if (Moves(registry.CreateAbility(abilityId), AbilityParameter.Effectiveness))
                    declaring.Add(abilityId);

            Assert.IsTrue(declaring.Count > 0, "no ability declares effectiveness, so this walk proves nothing");

            foreach (string abilityId in declaring)
            {
                List<IEffect> laid = await LaidBy(registry, abilityId);

                if (s_declaresForWhatAugmentsLay.Contains(abilityId, StringComparer.Ordinal))
                {
                    Assert.IsFalse(laid.Exists(HoldsAScalableFigure),
                        $"'{abilityId}' is written off as owning effectiveness for what augments hang on it, and it "
                        + "lays a scalable figure of its own — take it off the list and let the walk measure it");
                    continue;
                }

                if (s_beyondTheProbe.Contains(abilityId, StringComparer.Ordinal))
                {
                    Assert.AreEqual(0, laid.Count,
                        $"'{abilityId}' is written off as unreachable by this harness and it laid something anyway — "
                        + "take it off the list and let the walk measure it");
                    continue;
                }

                Assert.IsTrue(laid.Count > 0,
                    $"'{abilityId}' laid no effect at all: either its payload moved out of reach of a bare cast "
                    + "(then name it below and say why) or it stopped laying anything");
                Assert.IsTrue(laid.Exists(HoldsAScalableFigure),
                    $"'{abilityId}' declares effectiveness and every figure of what it lays is a plain number — "
                    + $"a record on that key is worn and paid for there and moves nothing. Laid: "
                    + string.Join(", ", laid.Select(effect => effect.Id).Distinct(StringComparer.Ordinal)));
            }
        }

        [TestMethod]
        public void ARecordThatMovesSeveralSharedKeysWorksOnTheSameAbilitiesThroughAllOfThem()
        {
            // A record made of several moves is one bargain, and an ability where only some of them
            // land gets the parts of that bargain the concepts happen to give it. When the missing part
            // is what the record CHARGES, the leftover is a bill with nothing behind it — which is how
            // "+35% effectiveness for two stacks" spent a wave taking two stacks off an ability that
            // never read effectiveness. The claim below is not about any one record: any record whose
            // moves come apart between abilities is the same bug wearing a different id — except where
            // the owner looked at a particular ability and accepted the arrival as it is, which is
            // written out in s_knowinglyFree and nowhere else.
            var byRecord = s_reach.GroupBy(row => row.Augment, StringComparer.Ordinal).Where(group => group.Count() > 1);

            foreach (IGrouping<string, (string Augment, string Parameter, string[] Works, string[] Inert)> record in byRecord)
            {
                (_, string firstKey, string[] first, _) = record.First();

                foreach ((_, string parameter, string[] works, _) in record.Skip(1))
                {
                    List<string> apart =
                    [
                        .. first.Except(works, StringComparer.Ordinal),
                        .. works.Except(first, StringComparer.Ordinal)
                    ];
                    List<string> unaccounted = [.. apart.Except(Accepted(record.Key), StringComparer.Ordinal)];

                    Assert.AreEqual(0, unaccounted.Count,
                        $"'{record.Key}' arrives in halves on [{string.Join(", ", unaccounted)}]: it moves '{firstKey}' "
                        + $"on [{string.Join(", ", first)}] but '{parameter}' on [{string.Join(", ", works)}]");
                }
            }
        }

        /// <summary>Which abilities the record both fits and moves, and which it fits and does not. The
        /// slot is given the record's own tier, so the tier never decides the answer — what is asked is
        /// which abilities the record BELONGS to, not which of the player's sockets can hold it.</summary>
        private static (List<string> Works, List<string> Inert) Landing(
            AbilityProvider registry, AbilityAugmentData record, string parameter)
        {
            List<string> works = [];
            List<string> inert = [];

            foreach (string abilityId in registry.KnownAbilityIds.Where(id => !registry.IsHidden(id)).OrderBy(id => id, StringComparer.Ordinal))
            {
                IAbility ability = registry.CreateAbility(abilityId);
                var slot = new AbilitySocketPlacement("socket_reach_probe", abilityId, record.Tier);

                if (AugmentFit.Check(slot, ability.Tags, record, []) != AugmentFitResult.Fits) continue;

                (Moves(ability, parameter) ? works : inert).Add(abilityId);
            }

            return (works, inert);
        }

        /// <summary>Whether anything on the ability reads the key: a move is laid on it and the number
        /// is read back. An unregistered key answers the same nothing before and after, which is what
        /// being inert IS — the augment is worn and changes not one number of the cast.</summary>
        private static bool Moves(IAbility ability, string parameter)
        {
            float before = ability[parameter];
            new AbilityAugmentParameterSet("Augment_Reach_Probe", [], 3, [(parameter, OperationType.Add, Probe)]).Apply(ability);
            return Math.Abs(ability[parameter] - before) > 0.0001f;
        }

        /// <summary>Everything one cast of the ability left standing, on its caster and on the field.</summary>
        private static async Task<List<IEffect>> LaidBy(AbilityProvider registry, string abilityId)
        {
            using var rolls = new CombatRandomScope(new SteadyRoll());
            var caster = Fighter();
            var enemy = Fighter();
            IAbility ability = registry.CreateAbility(abilityId);
            ability.SetOwner(caster);

            await ability.Execute([enemy], FieldOf(caster, enemy));

            return [.. caster.Effects.Effects, .. enemy.Effects.Effects];
        }

        /// <summary>Whether the effect carries at least one figure typed so that it cannot be read
        /// without the multiplier. Fields as well as properties, and private ones too: a primary
        /// constructor's parameter is captured as a private field and is the usual place such a figure
        /// lives. One level of nesting is followed: an effect may keep its numbers in a settings record
        /// of its own (the Static Armor's detonation), and a figure there is as unreadable-by-hand as one on the
        /// effect itself. Only types from the effect's own assembly are opened, so the walk stops at the
        /// project's edge instead of crawling the framework.</summary>
        private static bool HoldsAScalableFigure(IEffect effect) =>
            HoldsAScalableFigure(effect.GetType(), effect.GetType().Assembly, depth: 1);

        private static bool HoldsAScalableFigure(Type held, Assembly own, int depth)
        {
            const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            List<Type> carried = [];
            for (Type? type = held; type != null; type = type.BaseType)
            {
                carried.AddRange(type.GetFields(Declared).Select(field => field.FieldType));
                carried.AddRange(type.GetProperties(Declared).Select(property => property.PropertyType));
            }

            if (carried.Contains(typeof(EffectValue))) return true;

            return depth > 0 && carried.Distinct()
                .Where(type => type != held && type.Assembly == own)
                .Any(type => HoldsAScalableFigure(type, own, depth - 1));
        }

        private static ConditionOwner Fighter()
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, ProbeVitals);
            fighter.SetMaximum(EntityParameter.Mana, ProbeVitals);
            fighter.SetMaximum(EntityParameter.Barrier, ProbeVitals);
            fighter.CurrentHealth = ProbeVitals;
            fighter.CurrentMana = ProbeVitals;

            return fighter;
        }

        private static IBattleField FieldOf(IFightable owner, params IFightable[] enemies)
        {
            var field = new Mock<IBattleField>();
            field.Setup(battlefield => battlefield.GetEnemies(It.IsAny<IFightable>())).Returns(enemies);
            field.Setup(battlefield => battlefield.GetAllies(It.IsAny<IFightable>())).Returns([owner]);
            field.Setup(battlefield => battlefield.GetAll()).Returns([owner, .. enemies]);
            field.Setup(battlefield => battlefield.GetRandomEntity(It.IsAny<IFightable>())).Returns(owner);

            return field.Object;
        }

        private static void Disagreement(StringBuilder report, string what, IReadOnlyCollection<string> written, IReadOnlyCollection<string> found)
        {
            if (written.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(found.OrderBy(id => id, StringComparer.Ordinal), StringComparer.Ordinal))
                return;

            report.AppendLine($"  {what}");
            report.AppendLine($"    written: {string.Join(", ", written.OrderBy(id => id, StringComparer.Ordinal))}");
            report.AppendLine($"    found:   {string.Join(", ", found.OrderBy(id => id, StringComparer.Ordinal))}");
        }
    }
}
