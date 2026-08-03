namespace LastBreathTest.BattleSystemTests
{
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Enums;

    /// <summary>
    /// The augments that are numbers and nothing else, held to the numbers they were. Each of them used
    /// to be a factory calling a class written for that one record; they are rows of a table now, and a
    /// row says three things a class used to say in code — the parameter key, what it does to the value
    /// there, and where the amount comes from. None of the three fails loudly when it is lost: a wrong
    /// key decorates a parameter nobody reads, a wrong operation moves the right number the wrong way,
    /// and a lost fallback turns a missing property into zero. So all three are written out below and
    /// walked against what the registry actually builds out of the shipped records.
    ///
    /// The table is literal on purpose. Reading the keys off the production table would make this walk
    /// agree with whatever that table happens to say; written out, it is the second copy a claim of
    /// unchanged behaviour needs — every figure here is the figure the augment's own class passed to its
    /// decorator before the collapse.
    /// </summary>
    [TestClass]
    public class AugmentParameterTableTests
    {
        /// <summary>How many records the table took over from a class of their own.</summary>
        private const int TranslatedRecords = 47;

        /// <summary>Two bases every move is measured on. One of them has to be something other than
        /// nothing: an override and an addition are the same number on a base of zero, and a walk that
        /// only ever measured there would pass on either.</summary>
        private static readonly float[] s_bases = [0f, 8f];

        /// <summary>Every move every translated record makes: the augment, the parameter key it stands
        /// on, what it does to the value there, and the amount — which is both what the shipped record
        /// carries and what the augment falls back to when a record carries nothing.</summary>
        private static readonly (string Augment, string Parameter, OperationType Operation, float Amount)[] s_moves =
        [
            ("Augment_Additional_Max_Attacks", "MaxAttacks", OperationType.Add, 3f),
            ("Augment_More_Attack_Damage", "DamageMultiplier", OperationType.Add, 0.15f),
            ("Augment_Additional_Attacks", "MinAttacks", OperationType.Add, 1f),
            ("Augment_Additional_Attacks", "MaxAttacks", OperationType.Add, 1f),

            ("Augment_Additional_Amount_Attacks", "Attacks", OperationType.Add, 2f),
            ("Augment_Additional_Damage_Multiplier", "AttackDamageStepMultiplier", OperationType.Add, 0.05f),

            ("Augment_Increasing_Scales", "WeaponDamageScale", OperationType.Add, 0.6f),
            ("Augment_Increasing_Scales", "SpellDamageScale", OperationType.Add, 0.85f),
            ("Augment_Poison_Duration", "PoisonDuration", OperationType.Add, 1f),

            ("Ability_Ov_Augment_Mana_Step", "ManaPerStep", OperationType.Subtract, 1.5f),
            ("Ability_Ov_Augment_Additional_Multiplier", "DamagePerStep", OperationType.Add, 0.02f),

            ("Augment_Cooldown_Chance", "CooldownReduceChance", OperationType.Add, 0.15f),
            ("Augment_Heal_On_Hit", "HealOnHit", OperationType.Add, 0.07f),
            ("Augment_More_Armor_Return", "ArmorReturn", OperationType.Add, 0.15f),
            ("Augment_More_Damage_Return", "DamageReturn", OperationType.Add, 0.20f),

            ("Augment_Heal_From_Damage", "HealPercent", OperationType.Add, 0.15f),

            ("Augment_Fury_Duration", "FuryDuration", OperationType.Subtract, 1f),
            ("Augment_More_Burn", "FuryHealthPercent", OperationType.Add, 0.035f),
            ("Augment_Less_Burn", "FuryHealthPercent", OperationType.Subtract, 0.02f),

            ("Augment_Buff_Duration", "Duration", OperationType.Add, 1f),
            ("Augment_Recovery_Bonus", "RecoveryBonus", OperationType.Add, 0.15f),
            ("Augment_Health_Bonus", "HealthBonus", OperationType.Add, 0.15f),

            ("Augment_Damage_Multiplier", "DamageMultiplier", OperationType.Add, 0.25f),
            ("Augment_Restore_On_Hit", "HealthRestore", OperationType.Add, 0.07f),
            ("Augment_Restore_On_Hit", "ManaRestore", OperationType.Add, 0.07f),

            ("Ability_Hb_Augment_Additional_Scales", "WeaponDamageScale", OperationType.Add, 0.15f),
            ("Ability_Hb_Augment_Additional_Scales", "SpellDamageScale", OperationType.Add, 0.15f),
            ("Augment_Extend_Stun_Add_Cost", "StunDuration", OperationType.Add, 1f),
            ("Augment_Extend_Stun_Add_Cost", "CostValue", OperationType.Add, 50f),
            ("Augment_Additional_Lunges", "Attacks", OperationType.Add, 1f),

            ("Augment_More_Stacks_More_Cost", "Stacks", OperationType.Add, 1f),
            ("Augment_More_Stacks_More_Cost", "CostValue", OperationType.Add, 50f),

            ("Augment_Additional_Health_Regen", "HealthRegen", OperationType.Add, 0.025f),
            ("Augment_Add_Effectiveness_Reduce_Stacks", "Effectiveness", OperationType.Add, 0.35f),
            ("Augment_Add_Effectiveness_Reduce_Stacks", "Stacks", OperationType.Subtract, 2f),
            ("Augment_Increased_Buff_Duration", "Duration", OperationType.Add, 1f),

            ("Augment_Reduce_Execution_Trahsold", "ExecutionThreshold", OperationType.Subtract, 5f),

            ("Ability_Is_Augment_Additional_Scales", "WeaponDamageScale", OperationType.Add, 0.05f),
            ("Ability_Is_Augment_Additional_Scales", "SpellDamageScale", OperationType.Add, 0.15f),
            ("Ability_Is_Augment_Additional_Crit_Damage", "CriticalDamageBonus", OperationType.Add, 0.75f),
            ("Ability_Is_Augment_Additional_Crit_Chance", "CriticalChanceBonus", OperationType.Add, 0.35f),

            ("Ability_Ib_Augment_Withering_Value", "WitheringValue", OperationType.Add, 0.05f),
            ("Ability_Ib_Augment_Withering_Stacks", "WitheringMaxStacks", OperationType.Add, 1f),
            ("Ability_Ib_Augment_Extra_Block_Damage", "ExtraBlockDamagePercent", OperationType.Add, 0.25f),
            ("Ability_Ib_Augment_Heavy_Blocks", "Damage", OperationType.Add, 150f),
            ("Ability_Ib_Augment_Heavy_Blocks", "WeaponDamageScale", OperationType.Add, 0.15f),
            ("Ability_Ib_Augment_Heavy_Blocks", "SpellDamageScale", OperationType.Add, 0.45f),

            ("Ability_Df_Augment_Frostbite_Duration", "FrostbiteDuration", OperationType.Add, 1f),
            ("Ability_Df_Augment_More_Shred", "ColdResistanceShred", OperationType.Add, 0.15f),

            ("Ability_Dis_Augment_Multiplier", "BarrierMultiplier", OperationType.Add, 0.5f),
            ("Ability_Dis_Augment_More_Multiplier", "BarrierMultiplier", OperationType.Add, 1f),
            ("Ability_Dis_Augment_More_Restore", "StageThreeBarrierRestore", OperationType.Add, 0.25f),
            ("Ability_Dis_Augment_Spell_Scale", "SpellDamageScale", OperationType.Add, 0.35f),

            ("Ability_Sa_Augment_Detonation_Scales", "DetonationWeaponScale", OperationType.Add, 0.25f),
            ("Ability_Sa_Augment_Detonation_Scales", "DetonationSpellScale", OperationType.Add, 0.35f),
            ("Ability_Sa_Augment_Buff_Duration", "Duration", OperationType.Add, 1f),
            ("Ability_Sa_Augment_More_Splash", "StageThreeSplashDamage", OperationType.Add, 0.5f),
            ("Ability_Sa_Augment_Less_Stacks", "RequiredStacks", OperationType.Subtract, 1f)
        ];

        [TestMethod]
        public void EveryTranslatedRecordMovesTheParametersItsOwnClassUsedTo()
        {
            AbilityProvider catalog = ShippedCatalog();

            foreach (IGrouping<string, (string Augment, string Parameter, OperationType Operation, float Amount)> augment in Translated())
            {
                IAbilityUpgrade upgrade = Built(catalog, augment.Key, properties => properties);

                foreach (float start in s_bases)
                {
                    ParameterProbe probe = ProbeAt(start);
                    upgrade.Apply(probe);

                    foreach ((_, string parameter, OperationType operation, float amount) in augment)
                        Assert.AreEqual(Moved(operation, start, amount), probe.Read(parameter),
                            $"'{augment.Key}' no longer moves '{parameter}' the way its own class did");
                }
            }
        }

        [TestMethod]
        public void EveryTranslatedRecordFallsBackToItsOwnDefaultWhenTheRecordCarriesNoNumber()
        {
            // The half of the collapse that fails in silence. Every factory used to name the amount to
            // use when the record does not carry the property, and a record whose property went missing
            // in a data pass reads as zero: the augment is still offered, still chosen, still paid for,
            // and moves nothing. The fallbacks moved into the table with the parameters, and this is
            // what holds them there — the walk builds every record stripped of its numbers.
            AbilityProvider catalog = ShippedCatalog();

            foreach (IGrouping<string, (string Augment, string Parameter, OperationType Operation, float Amount)> augment in Translated())
            {
                IAbilityUpgrade upgrade = Built(catalog, augment.Key, _ => []);
                ParameterProbe probe = ProbeAt(0f);
                upgrade.Apply(probe);

                foreach ((_, string parameter, OperationType operation, float amount) in augment)
                    Assert.AreEqual(Moved(operation, 0f, amount), probe.Read(parameter),
                        $"'{augment.Key}' lost the amount it falls back to on '{parameter}' when the record carries no property");
            }
        }

        [TestMethod]
        public void EveryTranslatedRecordTakesItsAmountFromItsOwnRecordAndNotFromTheFallback()
        {
            // The other half: a fallback that happens to equal the shipped number hides a property name
            // nobody reads any more. Doubling every number the record carries has to double every number
            // the augment moves — a move reaching for a property the record does not have would sit on
            // its fallback and show up here as the figure that did not budge.
            AbilityProvider catalog = ShippedCatalog();

            foreach (IGrouping<string, (string Augment, string Parameter, OperationType Operation, float Amount)> augment in Translated())
            {
                IAbilityUpgrade upgrade = Built(catalog, augment.Key,
                    properties => properties.ToDictionary(entry => entry.Key, entry => entry.Value * 2f));
                ParameterProbe probe = ProbeAt(0f);
                upgrade.Apply(probe);

                foreach ((_, string parameter, OperationType operation, float amount) in augment)
                    Assert.AreEqual(Moved(operation, 0f, amount * 2f), probe.Read(parameter),
                        $"'{augment.Key}' reads '{parameter}' off its fallback instead of off its own record");
            }
        }

        [TestMethod]
        public void TakingATranslatedRecordOffLeavesTheAbilityWhereItWas()
        {
            AbilityProvider catalog = ShippedCatalog();

            foreach (IGrouping<string, (string Augment, string Parameter, OperationType Operation, float Amount)> augment in Translated())
            {
                IAbilityUpgrade upgrade = Built(catalog, augment.Key, properties => properties);
                ParameterProbe probe = ProbeAt(8f);

                upgrade.Apply(probe);
                upgrade.Remove(probe);

                foreach ((_, string parameter, _, _) in augment)
                    Assert.AreEqual(8f, probe.Read(parameter),
                        $"'{augment.Key}' left a decorator on '{parameter}' after it was taken off");
            }
        }

        [TestMethod]
        public void NoTranslatedRecordNamesTheSameParameterTwice()
        {
            // Two moves of one augment on one key would be written with the same decorator id, and the
            // second of them loses to the first without a word (AbilityParameterSet.AddDecorator). The
            // ids are the augment and the parameter, so this is the invariant that keeps them unique.
            var repeated = s_moves
                .GroupBy(move => (move.Augment, move.Parameter))
                .Where(group => group.Count() > 1)
                .Select(group => $"{group.Key.Augment} on {group.Key.Parameter}")
                .ToList();

            Assert.AreEqual(0, repeated.Count, $"one augment moves one parameter twice: {string.Join(", ", repeated)}");
        }

        [TestMethod]
        public void TheTableAndTheHandWrittenFactoriesNameDisjointAugments()
        {
            // The registry is two halves now, and an id written into both would be built by whichever
            // half is asked first while the other quietly never runs.
            AbilityProvider catalog = ShippedCatalog();
            var buildable = catalog.BuildableAugmentIds.ToList();

            Assert.AreEqual(buildable.Count, buildable.Distinct(StringComparer.Ordinal).Count(),
                "an augment id is answered by a factory and by the parameter table at once");
            Assert.AreEqual(TranslatedRecords, Translated().Count(), "the walks no longer cover the records the collapse translated");
        }

        private static IEnumerable<IGrouping<string, (string Augment, string Parameter, OperationType Operation, float Amount)>> Translated() =>
            s_moves.GroupBy(move => move.Augment, StringComparer.Ordinal);

        private static float Moved(OperationType operation, float baseValue, float amount) => operation switch
        {
            OperationType.Add => baseValue + amount,
            OperationType.Subtract => baseValue - amount,
            OperationType.Override => amount,
            _ => throw new InvalidOperationException($"the table names an operation the walk cannot predict: {operation}")
        };

        /// <summary>The upgrade the registry builds for an augment, out of its shipped record with the
        /// numbers put through the given change.</summary>
        private static IAbilityUpgrade Built(
            AbilityProvider catalog,
            string augmentId,
            Func<Dictionary<string, float>, Dictionary<string, float>> numbers)
        {
            AbilityUpgradeData? record = catalog.Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");

            IAbilityUpgrade? upgrade = catalog.CreateUpgrade(record with { UpgradeProperties = numbers(record.UpgradeProperties) });
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{augmentId}'");

            return upgrade;
        }

        /// <summary>An ability carrying every parameter the table names, all of them at the same base.</summary>
        private static ParameterProbe ProbeAt(float start) => new(new AbilityBaseData
        {
            Id = "Ability_Parameter_Probe",
            CostValue = (int)start,
            AbilityProperties = s_moves
                .Select(move => move.Parameter)
                .Distinct(StringComparer.Ordinal)
                .Where(parameter => !string.Equals(parameter, AbilityParameter.CostValue, StringComparison.Ordinal))
                .ToDictionary(parameter => parameter, _ => start, StringComparer.Ordinal)
        });

        private static AbilityProvider ShippedCatalog()
        {
            var provider = new AbilityProvider();
            var service = new GameDataService(new FileSystemDataSource(SharedData.Root()), [provider]);
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add($"{context}: {exception.Message}");

            service.LoadAll();

            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
            return provider;
        }

        /// <summary>An ability that is nothing but its parameters — it never casts, and the walks read
        /// the numbers straight off it.</summary>
        private sealed class ParameterProbe(AbilityBaseData data) : Ability(data)
        {
            public float Read(string parameter) => this[parameter];

            public override IAbility Copy() => new ParameterProbe(Data);

            protected override Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) =>
                Task.CompletedTask;
        }
    }
}
