namespace LastBreathTest.BattleSystemTests
{
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using Battle.Source;
    using Battle.Source.Abilities;
    using Battle.Source.Effects;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Extensions;
    using Core.Interfaces;
    using Core.Modifiers.Conditions;
    using Moq;

    /// <summary>
    /// Combat announces the stretches inside which a predicate has a subject to read: an attack lasts from
    /// the attacker announcing a hit of his to announcing it spent, a cast from its activation to its end.
    /// Whoever owns such a window closes it, and he has to close it on every way out of what he is doing, a
    /// throw included — a window left open is read by everything its owner does until the next one opens:
    /// someone else's damage, a heal, a parameter resolved out of combat, a line written for the first
    /// action of a turn that never gets spent.
    /// <para>Where the window can be driven headless, that is what is proven here. Where it cannot — the
    /// four fighters that resolve a hit are scene nodes — the rule is pinned in the source of every copy
    /// that holds it. A copy that quietly drops it is the way the sandbox projects drift away from Main.</para>
    /// </summary>
    [TestClass]
    public class CombatWindowClosureTests
    {
        private const string Resolution = "public async Task ReceiveAttack(";
        private const string Closure = "new AfterAttackEvent(";
        private const string BrokenReaction = "a reaction to the announcement failed";
        private const string WhileTargetWounded = "Target_Below_Half";
        private const float WoundedShare = 0.5f;
        private const float MaxHealth = 100f;

        /// <summary>The lazy slot holding the generator a real cast rolls on.</summary>
        private const string CastGeneratorField = "s_castRnd";
        private const string ChargeSource = "Ability_Overload";

        /// <summary>Anything but <see cref="ChargeSource"/>: recasting the ability a charge came from
        /// refreshes the charge instead of spending it, and nothing would arm.</summary>
        private const string BoostedAbility = "Ability_Test_Silent_Cast";
        private const float ChargeMultiplier = 0.5f;
        private const float CastDamage = 100f;
        private const float Tolerance = 0.001f;

        /// <summary>Every copy of the hit resolution: two projects × player and NPC.</summary>
        private static readonly string[] s_fighterSources =
        [
            Path.Combine("Main", "Player", "Player.cs"),
            Path.Combine("Main", "Npc", "BaseNpc.cs"),
            Path.Combine("Battle", "Internal", "Player", "Player.cs"),
            Path.Combine("Battle", "Internal", "Npc", "BaseNpc.cs"),
        ];

        private static string SrcRoot
        {
            get
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SharedData")))
                    directory = directory.Parent;
                Assert.IsNotNull(directory, "SharedData not found above the test bin directory");
                return directory.FullName;
            }
        }

        [TestMethod]
        public void EveryFighterSpendsHisAttackOnTheWayOutOfAThrownHit()
        {
            string[] leaking = s_fighterSources.Where(source => !AnnouncedOnEveryPath(source, Resolution, Closure)).ToArray();

            Assert.AreEqual(0, leaking.Length,
                $"the attack is announced spent on the successful path only in {string.Join(", ", leaking)} — a throw "
                + "in the middle of the hit leaves the window open and the target readable by everything that follows");
        }

        [TestMethod]
        public async Task ACastJoinedByAThrowingReaction_StillEndsTheChargeItArmed()
        {
            // The announcement that opens the cast window is delivered subscriber by subscriber, and a
            // charge arms itself on it: by the time a later subscriber throws, the caster already carries
            // the boost that only the end of the cast takes off him. The end is also what spends the cast
            // for a predicate counting the owner's actions this turn.
            NeutralizeCastGenerator();
            var caster = new ConditionOwner();
            await Charge().Apply(new EffectApplyingContext { Caster = caster, Target = caster, Source = nameof(CombatWindowClosureTests) });
            float armedInside = 0f;
            caster.CombatEvents.Subscribe<AbilityActivatedEvent>(_ => armedInside = AbilityDamageOf(caster));
            caster.CombatEvents.Subscribe<AbilityActivatedEvent>(_ => throw new InvalidOperationException(BrokenReaction));
            var ability = SilentCast.Named(BoostedAbility);
            ability.SetOwner(caster);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => ability.Execute([], Mock.Of<IBattleField>()));

            Assert.AreEqual(CastDamage * (1 + ChargeMultiplier), armedInside, Tolerance,
                "the fixture never armed the charge the throw is supposed to leave behind");
            Assert.AreEqual(CastDamage, AbilityDamageOf(caster), Tolerance,
                "the activation threw and the charge stayed on the caster — every cast he makes from now on "
                + "is boosted for free, and the turn is short the action this one spent");
        }

        [TestMethod]
        public async Task AnAttackJoinedByAThrowingReaction_IsStillSpent()
        {
            // The other half of the same window: it is opened by the attacker and closed by whoever resolves
            // the hit, so a join that throws never reaches the closing half on its own.
            var scheduler = new AttackContextScheduler(_ => { });
            var attacker = new ConditionOwner();
            ICondition line = TargetLine();
            line.Attach(attacker);
            bool armedInside = false;
            attacker.CombatEvents.Subscribe<BeforeAttackEvent>(_ => armedInside = line.IsMet);
            attacker.CombatEvents.Subscribe<BeforeAttackEvent>(_ => throw new InvalidOperationException(BrokenReaction));
            scheduler.Schedule(Swing(attacker, Fighter(WoundedShare - 0.1f)));

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => scheduler.DrainQueue());

            Assert.IsTrue(armedInside, "the fixture never opened the window the throw is supposed to leave through");
            Assert.IsFalse(line.IsMet,
                "the join threw and the target stayed named — every line written about a target now counts on "
                + "whatever its owner does until his next swing");
        }

        /// <summary>Whether the closing announcement inside the given method is reached however that method
        /// is left.</summary>
        private static bool AnnouncedOnEveryPath(string source, string signature, string closure)
        {
            string body = Body(source, signature);
            int[] announcements = Occurrences(body, closure);

            Assert.AreEqual(1, announcements.Length,
                $"{source}: '{closure}' has to close the window exactly once, found {announcements.Length}");

            return IsGuaranteed(body, announcements[0], source);
        }

        /// <summary>The body of the method opening at the given signature, braces matched.</summary>
        private static string Body(string source, string signature)
        {
            string path = Path.Combine(SrcRoot, source);
            Assert.IsTrue(File.Exists(path), $"source not found: {path}");

            string text = File.ReadAllText(path);
            int declaration = text.IndexOf(signature, StringComparison.Ordinal);
            Assert.IsTrue(declaration >= 0, $"{source}: no '{signature}' to read");

            (int start, int end) = BlockSpan(text, declaration, source);

            return text[start..(end + 1)];
        }

        /// <summary>Whether the position is reached however the code around it exits: only a finally does that.</summary>
        private static bool IsGuaranteed(string code, int position, string source) =>
            Regex.Matches(code, @"\bfinally\b")
                .Select(keyword => BlockSpan(code, keyword.Index, source))
                .Any(block => position > block.Start && position < block.End);

        /// <summary>Bounds of the block opening at or after <paramref name="from"/>, closing brace included.</summary>
        private static (int Start, int End) BlockSpan(string text, int from, string source)
        {
            int open = text.IndexOf('{', from);
            Assert.IsTrue(open >= 0, $"{source}: a block was expected after position {from}");

            int depth = 0;
            for (int index = open; index < text.Length; index++)
            {
                if (text[index] == '{') depth++;
                if (text[index] == '}' && --depth == 0) return (open, index);
            }

            Assert.Fail($"{source}: the block opened at position {open} is never closed");
            return default;
        }

        private static int[] Occurrences(string code, string fragment)
        {
            var found = new List<int>();
            for (int index = code.IndexOf(fragment, StringComparison.Ordinal); index >= 0;
                 index = code.IndexOf(fragment, index + 1, StringComparison.Ordinal))
                found.Add(index);

            return [.. found];
        }

        /// <summary>An attack of the attacker's on the target, valid enough for the queue to drain it.</summary>
        private static IAttackContext Swing(IFightable attacker, IFightable target)
        {
            var context = new Mock<IAttackContext>();
            context.SetupGet(swing => swing.Attacker).Returns(attacker);
            context.SetupGet(swing => swing.Target).Returns(target);
            context.SetupGet(swing => swing.IsValid).Returns(true);

            return context.Object;
        }

        /// <summary>A predicate about the fighter being hit: it answers only while an attack names one.</summary>
        private static ICondition TargetLine()
        {
            var catalog = ConditionCatalogs.Of(
                $$"""{ "id": "{{WhileTargetWounded}}", "type": "{{ConditionTypes.TargetResourceThreshold}}", "resource": "Health", "value": {{WoundedShare}} }""");
            Assert.IsTrue(catalog.TryResolve(WhileTargetWounded, out ICondition? condition), $"the fixture catalog refused '{WhileTargetWounded}'");

            return condition!;
        }

        private static ConditionOwner Fighter(float healthShare)
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, MaxHealth);
            fighter.CurrentHealth = MaxHealth * healthShare;

            return fighter;
        }

        /// <summary>A charge of the "the next ability of yours hits harder" family: it attaches its boost
        /// to the caster when a cast is announced and takes it off when that same cast is announced over.</summary>
        private static OverloadChargeEffect Charge() => new(ChargeSource, ChargeMultiplier);

        /// <summary>What an ability of his would deal right now. The charge's boost is a damage mutator on
        /// the caster, so the number answers whether it is still attached to him.</summary>
        private static float AbilityDamageOf(IFightable caster)
        {
            var damage = new DamageContext { Source = caster, Cause = DamageCause.Ability };
            damage.Add(DamageType.Physical, CastDamage);
            caster.ModifierHandler.Apply(damage);

            return damage.TotalDamage;
        }

        /// <summary>
        /// A real cast rolls on Godot's generator, and constructing one outside the engine is a fatal
        /// access violation — which is why the ability keeps it in a lazy static, untouched until the
        /// first activation. The sandbox fills that slot before the first cast with an instance that is
        /// never constructed and never rolled: nothing here has a chance to roll on it.
        /// </summary>
        private static void NeutralizeCastGenerator()
        {
            var slot = typeof(Ability).GetField(CastGeneratorField, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(slot, $"{nameof(Ability)} no longer keeps its cast generator in '{CastGeneratorField}' — "
                + "a cast can no longer be driven without the engine, and this fixture would take the test host down with it");

            slot.SetValue(null, RuntimeHelpers.GetUninitializedObject(slot.FieldType));
        }

        /// <summary>A cast with no delivery of its own: what it announces about itself is the whole of it.</summary>
        private sealed class SilentCast(AbilityBaseData data) : Ability(data)
        {
            public static SilentCast Named(string id) => new(new AbilityBaseData { Id = id });

            public override IAbility Copy() => new SilentCast(Data);

            protected override Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) => Task.CompletedTask;
        }
    }
}
