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
        private static readonly string[] s_beyondTheProbe = ["Ability_Double_Strike"];

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

        private static IEnumerable<string> Accepted(string augmentId) =>
            s_knowinglyFree.TryGetValue(augmentId, out string[]? abilities) ? abilities : [];

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
            ("Augment_Additional_Projectiles", AbilityParameter.ProjectileCount,
                ["Ability_Ice_Shards"], ["Ability_Jar_Of_Poison"]),

            // Both effectiveness records stand on one key in one direction, so where they meet they are
            // one offer and the better works. What separates them is which abilities are offered the
            // deal — tags, not keys, which is the whole segmentation of the family.
            // The re-markup swapped the inert pair: Static Armor lost "buff" and left the row while
            // Overload gained it and arrived (declaring no effectiveness — inert until CL-3).
            ("Augment_Buff_Effectiveness", AbilityParameter.Effectiveness,
                ["Ability_Ares_Blessing", "Ability_Critical_Calculation", "Ability_Dark_Shroud", "Ability_Ice_Aegis",
                 "Ability_Poison_Coating", "Ability_Porcupine"],
                ["Ability_Overload", "Ability_Sacrifice"]),

            // Discharge and Static Armor arrived with the doc's "recovery" (their barrier refunds);
            // neither declares effectiveness yet, so both sit inert.
            ("Augment_Recovery_Effectiveness", AbilityParameter.Effectiveness,
                ["Ability_Ares_Blessing", "Ability_Dark_Shroud"],
                ["Ability_Discharge", "Ability_Static_Armor"]),

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
        ];

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
        /// lives.</summary>
        private static bool HoldsAScalableFigure(IEffect effect)
        {
            for (Type? type = effect.GetType(); type != null; type = type.BaseType)
            {
                const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Instance | BindingFlags.DeclaredOnly;

                if (type.GetFields(Declared).Any(field => field.FieldType == typeof(EffectValue))) return true;
                if (type.GetProperties(Declared).Any(property => property.PropertyType == typeof(EffectValue))) return true;
            }

            return false;
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
