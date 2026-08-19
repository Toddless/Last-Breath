namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Battle.Source.Effects;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Entity.Components;
    using Core.Entity;
    using Moq;

    /// <summary>
    /// The other half of the shape sweep. <see cref="EffectEffectivenessTests"/> walks the
    /// parameter-changing families by reflection and asks each of them for its shape; but a figure only
    /// goes through a parameter change when the effect HAS one, and most of the wave's payloads do not —
    /// they hand their number to a cast mutator, a decorator or a damage modifier instead. Those calls
    /// were outside every sweep, and a shape swapped on one of them (the Primal Fury scaling attacks by
    /// 0.35 instead of by 1.35, which is a NERF of two thirds) left the whole run green.
    ///
    /// So the roster below is checked against the SOURCE: every <c>Effective</c> call in the effects
    /// folder is found and its shape read off the call itself. A payload written tomorrow fails here
    /// until somebody writes down what shape it is, and a shape quietly changed fails here too.
    /// </summary>
    [TestClass]
    public class EffectValueShapeRosterTests
    {
        private const float Authored = 0.15f;

        /// <summary>Files whose <c>Effective</c> calls belong to another sweep: the definition itself and
        /// the two parameter-change bases, whose descendants are walked by reflection next door.</summary>
        private static readonly string[] s_walkedElsewhere = ["Effect", "ParameterChangeEffect", "CompositeParameterChangeEffect"];

        /// <summary>Every effect that reads a figure of its own through <c>Effective</c>, and the shape of
        /// each reading — as many entries as the file has calls. "Plain" is what a call that names no
        /// shape means, and it is written out here so that leaving one off is a failure and not a default.
        /// </summary>
        private static readonly Dictionary<string, EffectValueShape[]> s_roster = new(StringComparer.Ordinal)
        {
            // --- Shares GAINED: the figure is what the number grows by, so the factor is one plus it.
            ["PrimalFuryEffect"] = [EffectValueShape.ShareGained],
            ["CloudedMindEffect"] = [EffectValueShape.ShareGained],
            ["OverloadChargeEffect"] = [EffectValueShape.ShareGained],

            // --- Shares LOST: one minus the figure, and never below nothing.
            ["SorceryGiftEffect"] = [EffectValueShape.ShareLost],
            ["Weakness"] = [EffectValueShape.ShareLost],

            // --- Plain amounts: a figure that is added, multiplied by something else, or compared
            // against something else, with no one to add to it.
            ["BarrierFromDamageEffect"] = [EffectValueShape.Plain],
            ["BurningFuryEffect"] = [EffectValueShape.Plain],
            ["CritDamageOnHitBuff"] = [EffectValueShape.Plain, EffectValueShape.Plain],
            ["CritLeechEffect"] = [EffectValueShape.Plain],
            ["CurseEffect"] = [EffectValueShape.Plain],
            ["DamageOverTurnEffect"] = [EffectValueShape.Plain],
            ["EvadeFirstDeath"] = [EffectValueShape.Plain],
            ["ExecutionEffect"] = [EffectValueShape.Plain],
            ["FragilityEffect"] = [EffectValueShape.Plain],
            ["FrostbiteEffect"] = [EffectValueShape.Plain],
            ["HealReductionEffect"] = [EffectValueShape.Plain],
            ["HealingFuryEffect"] = [EffectValueShape.Plain],
            ["HealthRegenerationEffect"] = [EffectValueShape.Plain],
            // Five readings of three figures: the aegis re-reads reflectPercent and healPerTurnPercent at
            // the guard as well as at the use, rather than naming them once.
            ["IceAegisEffect"] = [EffectValueShape.Plain, EffectValueShape.Plain, EffectValueShape.Plain, EffectValueShape.Plain, EffectValueShape.Plain],
            ["IncomingDamageReductionEffect"] = [EffectValueShape.Plain],
            ["InstantRestoreEffect"] = [EffectValueShape.Plain, EffectValueShape.Plain],
            ["LifeGivingShadeEffect"] = [EffectValueShape.Plain],
            ["ManaRegenerationEffect"] = [EffectValueShape.Plain],
            ["NextAbilityCooldownEffect"] = [EffectValueShape.Plain],
            ["NextCastSacredConversionEffect"] = [EffectValueShape.Plain],
            ["OnEdgeEffect"] = [EffectValueShape.Plain],
            ["PorcupineBuffEffect"] = [EffectValueShape.Plain, EffectValueShape.Plain, EffectValueShape.Plain, EffectValueShape.Plain],
            ["RegenerationEffect"] = [EffectValueShape.Plain],
            ["SacrificeChargeEffect"] = [EffectValueShape.Plain, EffectValueShape.Plain],
            ["ShieldEffect"] = [EffectValueShape.Plain, EffectValueShape.Plain],
            ["SlownessSeal"] = [EffectValueShape.Plain],
            ["StaticArmorEffect"] = [EffectValueShape.Plain]
        };

        [TestMethod]
        public void EveryShapeDeclaredInTheEffectsFolderIsOnTheRosterExactlyOnce()
        {
            Dictionary<string, List<EffectValueShape>> declared = ShapesInSource();
            Assert.IsTrue(declared.Count > 0, "no Effective calls were found in the source at all, so this walk proves nothing");

            string[] unrostered = [.. declared.Keys.Except(s_roster.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            string[] strangers = [.. s_roster.Keys.Except(declared.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)];

            Assert.AreEqual(0, unrostered.Length,
                $"effects reading a figure through Effective that no roster names — say which shape each reading is: [{string.Join(", ", unrostered)}]");
            Assert.AreEqual(0, strangers.Length,
                $"rostered effects that read no figure at all any more — the entry is stale: [{string.Join(", ", strangers)}]");
        }

        [TestMethod]
        public void EveryReadingIsOfTheShapeTheRosterSaysItIs()
        {
            List<string> divergent = [];

            foreach ((string effect, List<EffectValueShape> found) in ShapesInSource())
            {
                if (!s_roster.TryGetValue(effect, out EffectValueShape[]? expected)) continue; // the walk above owns this

                if (found.Count != expected.Length)
                {
                    divergent.Add($"{effect}: the roster names {expected.Length} reading(s), the source has {found.Count}");
                    continue;
                }

                if (!found.Order().SequenceEqual(expected.Order()))
                    divergent.Add($"{effect}: the roster says [{string.Join(", ", expected.Order())}], the source says [{string.Join(", ", found.Order())}]");
            }

            Assert.AreEqual(0, divergent.Count, $"shapes the source and the roster disagree about:\n  {string.Join("\n  ", divergent)}");
        }

        [TestMethod]
        public async Task ThePrimalFuryMultipliesTheAttackAndDoesNotReplaceIt()
        {
            // The swap that went unnoticed, pinned by its meaning rather than by its shape: the design
            // list says "+35%", so at plain effectiveness the attack is worth 1.35 of itself. Read as a
            // plain amount the very same figure scales the attack DOWN to about a third.
            var plain = new PrimalFuryEffect(duration: 3, maxMaxStacks: 1, healthPercent: 0.05f) { DamageMultiplier = 0.35f };
            await plain.Apply(Laying(new ConditionOwner()));

            Assert.AreEqual(1.35f, plain.DamageScale, 0.0001f, "the primal fury stopped multiplying the attack by one plus its share");

            var strong = new PrimalFuryEffect(duration: 3, maxMaxStacks: 1, healthPercent: 0.05f) { DamageMultiplier = 0.35f };
            await strong.Apply(Laying(new ConditionOwner(), effectiveness: 2f));

            Assert.AreEqual(1.7f, strong.DamageScale, 0.0001f, "effectiveness no longer moves the share the fury adds");
        }

        [TestMethod]
        public async Task TheCloudedMindMakesCastingDearerAndTheSorceryGiftMakesItCheaper()
        {
            // The pair that would be hardest to tell apart from a green run: both scale the same number
            // through the same mutator, and the only thing between "+25% to cast" and "a quarter off" is
            // which shape the figure is read in.
            Assert.AreEqual(125f, await CostAfter(new CloudedMindEffect(duration: 3, maxStacks: 3, value: 0.25f), effectiveness: 1f), 0.0001f,
                "the clouded mind is no longer charging more for a cast");
            Assert.AreEqual(75f, await CostAfter(new SorceryGiftEffect(duration: 3, maxStacks: 3, value: 0.25f), effectiveness: 1f), 0.0001f,
                "the sorcery gift is no longer taking anything off a cast");
        }

        [TestMethod]
        public async Task TheSorceryGiftStopsAtAFreeCastAndNeverPaysTheCaster()
        {
            // What the ShareLost shape is for. The hand-rolled subtraction this replaced went straight
            // past nothing at high effectiveness: a negative factor, and the "cost" of a cast became
            // income. Nothing else in the game knows how to read a fighter who is paid to cast.
            float cost = await CostAfter(new SorceryGiftEffect(duration: 3, maxStacks: 3, value: 0.25f), effectiveness: 6f);

            Assert.AreEqual(0f, cost, 0.0001f, "a strongly laid sorcery gift took the cast past free");
        }

        [TestMethod]
        public async Task TheFurysPriceIsTheSameHoweverStronglyItIsLaid()
        {
            // The price of a buff is not its payload. Strengthening the fury must not eat more of the
            // bearer's health for it — and because the Burning Fury's tick is a share OF the health this
            // burns, scaling both ends would have squared the effectiveness on the burn.
            var plain = new FuryEffect(duration: 3, maxStacks: 1, healthPercent: Authored);
            var strong = new FuryEffect(duration: 3, maxStacks: 1, healthPercent: Authored);

            await plain.Apply(Laying(new ConditionOwner()));
            await strong.Apply(Laying(new ConditionOwner(), effectiveness: 3f));

            Assert.AreEqual(Authored, plain.HealthPercent, 0.0001f, "the fury stopped burning what it was written to burn");
            Assert.AreEqual(plain.HealthPercent, strong.HealthPercent, 0.0001f,
                "a strongly laid fury eats more of its bearer — effectiveness reached the PRICE");
        }

        /// <summary>What a cast costs after the effect has been laid on the caster and the activation has
        /// gone through his mutators.</summary>
        private static async Task<float> CostAfter(IEffect effect, float effectiveness)
        {
            var caster = new ConditionOwner();
            await effect.Apply(Laying(caster, effectiveness));

            var activation = new AbilityActivationContext
            {
                Ability = Mock.Of<IAbility>(), Caster = caster, Field = Mock.Of<IBattleField>(), Rnd = Mock.Of<IRandomNumberGenerator>(), Cost = 100f
            };
            caster.ModifierHandler.Apply(activation);
            return activation.Cost;
        }

        /// <summary>Every <c>Effective</c> call in the effects folder, by the effect that makes it, with
        /// the shape each call names — a call naming none is a plain one.</summary>
        private static Dictionary<string, List<EffectValueShape>> ShapesInSource()
        {
            Dictionary<string, List<EffectValueShape>> found = new(StringComparer.Ordinal);

            foreach (string file in Directory.EnumerateFiles(EffectsSource(), "*.cs"))
            {
                string effect = Path.GetFileNameWithoutExtension(file);
                if (s_walkedElsewhere.Contains(effect, StringComparer.Ordinal)) continue;

                foreach (System.Text.RegularExpressions.Match call in Regex.Matches(File.ReadAllText(file), @"(?<!\w)Effective\((?<args>[^)]*)\)"))
                {
                    System.Text.RegularExpressions.Match shape = Regex.Match(call.Groups["args"].Value, @"EffectValueShape\.(?<name>\w+)");
                    if (!found.TryGetValue(effect, out List<EffectValueShape>? shapes)) found[effect] = shapes = [];
                    shapes.Add(shape.Success ? Enum.Parse<EffectValueShape>(shape.Groups["name"].Value) : EffectValueShape.Plain);
                }
            }

            return found;
        }

        /// <summary>The effects folder in the working tree. The sweep is over SOURCE, because a shape is
        /// written at the call and there is nothing left of it to reflect over once it is compiled.</summary>
        private static string EffectsSource()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "Battle", "Source", "Effects");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }

            throw new InvalidOperationException($"the effects source was not found above {AppContext.BaseDirectory}");
        }

        private static EffectApplyingContext Laying(ConditionOwner bearer, float effectiveness = 1f) =>
            new() { Caster = bearer, Target = bearer, Source = "test", Effectiveness = effectiveness };
    }
}
