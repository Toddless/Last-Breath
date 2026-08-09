namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Abilities;
    using Battle.Source.Abilities.Activation;
    using Battle.Source.Abilities.HitDelivery;
    using Battle.Source.Abilities.IceBlock;
    using Battle.Source.Abilities.IncreasingPressure;
    using Battle.Source.Abilities.Riders;
    using Battle.Source.Abilities.SeriesOfAttacks;
    using Battle.Source.Effects;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Moq;
    using BerserkFuryCast = Battle.Source.Abilities.BerserkFury.BerserkFury;
    using ChainLightningCast = Battle.Source.Abilities.ChainLightning.ChainLightning;
    using DeepFreezeCast = Battle.Source.Abilities.DeepFreeze.DeepFreeze;
    using DischargeCast = Battle.Source.Abilities.Discharge.Discharge;
    using DoubleStrikeCast = Battle.Source.Abilities.DoubleStrike.DoubleStrike;
    using HeadButtCast = Battle.Source.Abilities.HeadButt.HeadButt;
    using IceAegisCast = Battle.Source.Abilities.IceAegis.IceAegis;
    using IncreasingPressureCast = Battle.Source.Abilities.IncreasingPressure.IncreasingPressure;
    using SeriesOfAttacksCast = Battle.Source.Abilities.SeriesOfAttacks.SeriesOfAttacks;

    /// <summary>
    /// Where the randomness ability DELIVERY rolls on comes from, and who decides it — the same question
    /// the cast seat answers one step earlier (see <see cref="CastGeneratorCompositionTests"/>). Every
    /// point below used to build the engine generator itself: some in a field initializer, where merely
    /// CREATING the class was fatal, some in the delivery method, where executing it was. A native
    /// generator built where Godot is not running kills the host whole (0xC0000005), taking every
    /// remaining test with it instead of failing one — so none of it was ever covered, and the mines held
    /// only because nothing walked over them. These tests walk over them.
    /// </summary>
    [TestClass]
    public class CombatGeneratorCompositionTests
    {
        private const float Health = 100f;
        private const float Damage = 40f;

        [TestMethod]
        public void TheDeliverySeatDefaultsToSomethingTheEngineNeverBuilt()
        {
            // The guard has to come before anything is delivered: a seat left on the engine source kills
            // the host past the reach of the assertion that would have reported it.
            Assert.AreEqual(StaticMethod(nameof(CombatRandom.DefaultCombatRandom)), CombatRandom.Source.Method,
                "the delivery seat defaults to something other than the engine-free source — the first unwrapped delivery kills the run instead of failing a test");
            Assert.IsNull(CombatRandom.Attacks,
                "the sandbox seat handed out an engine generator; only a composition inside Godot may put one there");
            Assert.IsInstanceOfType<DefaultRandomNumberGenerator>(CombatRandom.Rolls,
                "delivery outside the engine must roll on the pure-C# stream");
        }

        [TestMethod]
        public void ComposingTheGamePutsTheEngineGeneratorInTheDeliverySeat()
        {
            // What the game owes its deliveries: the engine object itself. The attack pipeline's contract
            // names that class, so the seat's attack face is typed to it and only the engine source can
            // fill it — the sandbox default leaves it empty.
            Assert.AreEqual(typeof(Godot.RandomNumberGenerator), typeof(CombatRandomStream).GetProperty(nameof(CombatRandomStream.Attacks))!.PropertyType,
                "the stream an attack context carries stopped being the engine one — attacks would roll on randomness the rest of combat knows nothing about");
            MethodInfo engine = StaticMethod(nameof(CombatRandom.EngineCombatRandom));

            try
            {
                BattleSystemModuleDependencies.UseEngineCombatRandom();

                Assert.AreEqual(engine, CombatRandom.Source.Method,
                    "composing the game no longer seats the engine source: outside the sandboxes a delivery must roll on Godot's generator");
            }
            finally
            {
                // The seat goes back whatever the assertion said. Only the assignment happened here —
                // a generator nobody built is harmless, one left armed for the next test is the crash.
                CombatRandom.Source = CombatRandom.DefaultCombatRandom;
            }
        }

        [TestMethod]
        public void EveryProjectThatBootsInsideGodotInstallsTheEngineDeliveryStream() =>
            // The other half of the chain: the hook above is worth exactly as much as the bootstraps
            // that call it.
            GodotBootstraps.AssertEachInstalls(nameof(BattleSystemModuleDependencies.UseEngineCombatRandom));

        [TestMethod]
        public void CreatingEveryDelivererIsSurvivable()
        {
            // The worst kind of mine: a generator in a field initializer, where there is nothing to
            // execute and nothing to catch — the constructor alone took the process down. Reaching the
            // assertion at all is the proof; the assertion itself only names what was built.
            var bearer = Fighter();
            object[] built =
            [
                new MulticastActivation(),
                new ChainLightningCast(Data()),
                new DeepFreezeCast(Data()),
                new DischargeCast(Data()),
                new IceAegisCast(Data()),
                new IceBlocks(Data()),
                new SeriesOfAttacksCast(Data()),
                new IncreasingPressureCast(Data()),
                new BerserkFuryCast(Data()),
                new DoubleStrikeCast(Data()),
                new HeadButtCast(Data()),
                new SplashRandomTargetRider(0.5f),
                new ReduceRandomCooldownActivationRider("Rider_Test", 1),
                new TransferPoisonOnDeathRider(),
                new PorcupineBuffEffect(new ChainLightningCast(Data()), duration: 2, damageReturn: 0.5f, armorReturn: 0f, healOnHitPercent: 0f, cooldownReduceChance: 1f),
                new StaticArmorEffect(duration: 2, Detonation(), FieldOf(bearer)),
                new SoAsDefaultExecutionStrategy(),
                new IpDefaultExecutionStrategy(),
                new IpSingleAttackExecutionStrategy(),
                new IpDamageRandomTargetStrategy(0.5f),
                new BouncingHits(bounces: 2),
            ];

            Assert.IsFalse(Array.Exists(built, deliverer => deliverer == null), "a deliverer refused to be built at all");
        }

        [TestMethod]
        public async Task EveryDelivererRollsOnTheStreamTheSeatHandsOut()
        {
            // Creation being survivable is half of it: the roll each of them was built for has to land on
            // the seated stream, or the generator simply moved somewhere the composition cannot reach.
            var rolls = new CountingRandom();
            using var scope = new CombatRandomScope(rolls);

            foreach ((string point, Func<Task> drive) in Deliveries())
            {
                int before = rolls.Count;
                await drive();
                Assert.IsTrue(rolls.Count > before,
                    $"{point} delivered without asking the seated stream for anything — its roll is somewhere the seat does not reach");
            }
        }

        [TestMethod]
        public async Task AnAttackDeliveryRunsToTheEndOnWhateverTheSeatHandsOut()
        {
            // These three roll nothing of their own: what they used to build the engine generator FOR is
            // the attack context every hit of theirs carries. The sandbox seat hands out no engine object,
            // and the delivery still has to run its hits to the end.
            var rolls = new CountingRandom();
            using var scope = new CombatRandomScope(rolls);

            foreach ((string point, Func<IFightable, Task> drive) in AttackDeliveries())
            {
                var owner = Fighter();
                int resolved = 0;
                owner.CombatEvents.Subscribe<AfterAttackEvent>(_ => resolved++);

                await drive(owner);

                Assert.IsTrue(resolved > 0, $"{point} resolved no attack at all — its hits never reached anybody");
            }
        }

        /// <summary>Every deliverer that owns a roll, driven to it.</summary>
        private static IEnumerable<(string Point, Func<Task> Drive)> Deliveries() =>
        [
            (nameof(MulticastActivation), TheStanceStageRoll),
            (nameof(ChainLightningCast), TheChainJumpPick),
            (nameof(DeepFreezeCast), TheFreezeSpread),
            (nameof(DischargeCast), TheDischargeCrit),
            (nameof(IceAegisCast), TheAegisStage),
            (nameof(IceBlocks), TheBlockVolley),
            (nameof(BouncingHits), TheBounceSequence),
            (nameof(SplashRandomTargetRider), TheSplashVictim),
            (nameof(ReduceRandomCooldownActivationRider), TheDiscountedCooldown),
            (nameof(PorcupineBuffEffect), TheRetaliationCooldownRoll),
            (nameof(StaticArmorEffect), TheDetonationSplash),
            (nameof(SoAsDefaultExecutionStrategy), TheSeriesLength),
            (nameof(BerserkFuryCast), TheFurysNextSwing),
            (nameof(IpDamageRandomTargetStrategy), ThePressureSplashVictim),
            (nameof(TransferPoisonOnDeathRider), ThePoisonHeir),
        ];

        /// <summary>Deliveries whose generator only ever went into the attack contexts they build.</summary>
        private static IEnumerable<(string Point, Func<IFightable, Task> Drive)> AttackDeliveries() =>
        [
            (nameof(IpDefaultExecutionStrategy), owner => new IpDefaultExecutionStrategy().Execute(Pressure(owner), owner, [Fighter()], FieldOf(owner))),
            (nameof(IpSingleAttackExecutionStrategy), owner => new IpSingleAttackExecutionStrategy().Execute(Pressure(owner), owner, [Fighter()], FieldOf(owner))),
            (nameof(DoubleStrikeCast), owner => Cast(new DoubleStrikeCast(Data()), owner)),
            (nameof(HeadButtCast), owner => Cast(new HeadButtCast(Data()), owner)),
        ];

        private static Task TheStanceStageRoll()
        {
            new MulticastActivation().Roll(Fighter());
            return Task.CompletedTask;
        }

        private static Task TheChainJumpPick() => Cast(new ChainLightningCast(Data()), Fighter());

        private static Task TheFreezeSpread()
        {
            var caster = Fighter();
            var bystander = Fighter();
            var cast = new DeepFreezeCast(Data()) { SpreadFreezeChance = 1f };
            cast.SetOwner(caster);
            return cast.Execute([], FieldOf(caster, bystander));
        }

        private static Task TheDischargeCrit() => Cast(new DischargeCast(Data()), Fighter());

        private static Task TheAegisStage()
        {
            var caster = Fighter();
            var cast = new IceAegisCast(Data());
            cast.SetOwner(caster);
            return cast.Execute([], FieldOf(caster));
        }

        private static Task TheBlockVolley() => Cast(new IceBlocks(Data()), Fighter());

        private static Task TheBounceSequence()
        {
            var owner = Fighter();
            var enemy = Fighter();
            new BouncingHits(bounces: 2).GetHitSequence(owner, [enemy], FieldOf(owner, enemy));
            return Task.CompletedTask;
        }

        private static Task TheSplashVictim()
        {
            var caster = Fighter();
            var hit = Fighter();
            var bystander = Fighter();
            return new SplashRandomTargetRider(0.5f)
                .Apply(new AbilityImpact(caster, hit, FieldOf(caster, hit, bystander), Succeeded: true, IsCritical: false, Damage)
                {
                    Source = new IncreasingPressureCast(Data()),
                    Kind = ImpactKind.Attack
                });
        }

        private static Task TheDiscountedCooldown()
        {
            var caster = Fighter();
            var cooling = new ChainLightningCast(Data("Ability_Cooling_Down"));
            caster.AbilityBook.Learn(Stance.Dexterity, cooling);
            cooling.CooldownLeft = 3;

            return new ReduceRandomCooldownActivationRider("Rider_Test", 1).Apply(new AbilityActivationContext
            {
                Ability = new ChainLightningCast(Data()),
                Caster = caster,
                Field = FieldOf(caster),
                Rnd = new DefaultRandomNumberGenerator()
            });
        }

        private static async Task TheRetaliationCooldownRoll()
        {
            var bearer = Fighter();
            var attacker = Fighter();
            var source = new ChainLightningCast(Data());
            source.SetOwner(bearer);
            source.CooldownLeft = 3;
            var buff = new PorcupineBuffEffect(source, duration: 2, damageReturn: 0.5f, armorReturn: 0f, healOnHitPercent: 0f, cooldownReduceChance: 1f);
            await buff.Apply(new EffectApplyingContext { Caster = bearer, Target = bearer, Source = nameof(PorcupineBuffEffect) });

            var taken = new DamageContext { Source = attacker, Cause = DamageCause.Attack };
            taken.Add(DamageType.Physical, Damage);
            bearer.CombatEvents.Publish(new DamageTakenEvent(taken, bearer, VitalsSnapshot.From(bearer)));
        }

        private static async Task TheDetonationSplash()
        {
            var bearer = Fighter();
            var victim = Fighter();
            var bystander = Fighter();
            var armor = new StaticArmorEffect(duration: 2, Detonation(), FieldOf(bearer, victim, bystander));
            await armor.Apply(new EffectApplyingContext { Caster = bearer, Target = bearer, Source = nameof(StaticArmorEffect) });

            var landed = new AttackContext(bearer, victim, Damage, null!, Mock.Of<IAttackContextScheduler>()) { Result = AttackResults.Succeed };
            bearer.CombatEvents.Publish(new AfterAttackEvent(landed));
        }

        private static Task TheSeriesLength()
        {
            var owner = Fighter();
            var target = Fighter();
            var series = new SeriesOfAttacksCast(Data());
            series.SetOwner(owner);
            return new SoAsDefaultExecutionStrategy().Execute(series, owner, [target], FieldOf(owner, target));
        }

        private static Task TheFurysNextSwing()
        {
            // Barely standing: the chance to keep swinging is health-scaled, so the series ends itself.
            var owner = Fighter();
            owner.CurrentHealth = 1.5f;
            return Cast(new BerserkFuryCast(Data()), owner);
        }

        private static Task ThePressureSplashVictim()
        {
            var owner = Fighter();
            var target = Fighter();
            var bystander = Fighter();
            owner.Group = Mock.Of<IEntityGroup>();
            // Only a LANDED hit splashes, and nothing in a sandbox decides hits — so the verdict is
            // written onto the attack in the window between joining it and resolving it.
            owner.CombatEvents.Subscribe<BeforeAttackEvent>(joined =>
            {
                joined.Context.Result = AttackResults.Succeed;
                joined.Context.FinalDamage = Damage;
            });

            return new IpDamageRandomTargetStrategy(0.5f)
                .Execute(Pressure(owner), owner, [target], FieldOf(owner, target, bystander));
        }

        private static async Task ThePoisonHeir()
        {
            var owner = Fighter();
            var dying = Fighter();
            var heir = Fighter();
            var poison = new DamageOverTurnEffect(duration: 3, StatusEffects.Poison);
            await poison.Apply(new EffectApplyingContext { Caster = owner, Target = dying, Source = nameof(ThePoisonHeir), Damage = Damage });
            await new TransferPoisonOnDeathRider().Apply(new AbilityImpact(owner, dying, FieldOf(owner, dying, heir))
            {
                Source = new ChainLightningCast(Data()),
                Kind = ImpactKind.Hit
            });

            dying.CombatEvents.Publish(new EntityDiedEvent(dying));
        }

        /// <summary>An ability cast at one enemy, with a bystander for whatever the delivery jumps to.</summary>
        private static Task Cast(Ability ability, IFightable caster)
        {
            var target = Fighter();
            var bystander = Fighter();
            ability.SetOwner(caster);
            return ability.Execute([target], FieldOf(caster, target, bystander));
        }

        private static IncreasingPressureCast Pressure(IFightable owner)
        {
            var pressure = new IncreasingPressureCast(Data());
            pressure.SetOwner(owner);
            return pressure;
        }

        /// <summary>A detonation that splashes: the splash is what reaches the roll.</summary>
        private static ChargeDetonation Detonation() => new(
            SourceAbilityId: "Ability_Static_Armor",
            Damage,
            WeaponScale: 0f,
            SpellScale: 0f,
            RequiredStacks: 1,
            ChargeDuration: 2,
            BarrierRestorePercent: 0f,
            SplashPercent: 0.5f,
            ApplyOnHitTaken: false,
            IgnoreResistances: false,
            OverkillToRandom: false);

        private static AbilityBaseData Data(string id = "Ability_Test_Delivery") => new() { Id = id };

        private static ConditionOwner Fighter()
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, Health);
            fighter.SetMaximum(EntityParameter.Mana, Health);
            fighter.CurrentHealth = Health;
            fighter.CurrentMana = Health;

            return fighter;
        }

        private static IBattleField FieldOf(IFightable owner, params IFightable[] enemies)
        {
            var field = new Mock<IBattleField>();
            field.Setup(battlefield => battlefield.GetEnemies(It.IsAny<IFightable>())).Returns(enemies);
            field.Setup(battlefield => battlefield.GetAllies(It.IsAny<IFightable>())).Returns([owner]);
            field.Setup(battlefield => battlefield.GetAll()).Returns([owner, .. enemies]);

            return field.Object;
        }

        /// <summary>A seat-related method named on the delivery seat, or the failure that says it is gone.</summary>
        private static MethodInfo StaticMethod(string name)
        {
            MethodInfo? method = typeof(CombatRandom).GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, $"{nameof(CombatRandom)} no longer names {name}, so nothing can say what a delivery rolls on");
            return method;
        }
    }
}
