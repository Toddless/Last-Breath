namespace LastBreathTest.BattleSystemTests
{
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
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
        private const int TranslatedRecords = 24;

        /// <summary>Two bases every move is measured on. One of them has to be something other than
        /// nothing: an override and an addition are the same number on a base of zero, and a walk that
        /// only ever measured there would pass on either.</summary>
        private static readonly float[] s_bases = [0f, 8f];

        /// <summary>Every move every translated record makes: the augment, the parameter key it stands
        /// on, what it does to the value there, and the amount — which is both what the shipped record
        /// carries and what the augment falls back to when a record carries nothing.</summary>
        private static readonly (string Augment, string Parameter, OperationType Operation, float Amount)[] s_moves =
        [
            ("Augment_More_Attack_Damage", "DamageMultiplier", OperationType.Add, 0.15f),
            ("Augment_Additional_Attacks", "MinAttacks", OperationType.Add, 1f),
            ("Augment_Additional_Attacks", "MaxAttacks", OperationType.Add, 1f),

            ("Augment_Increasing_Scales", "WeaponDamageScale", OperationType.Add, 0.6f),
            ("Augment_Increasing_Scales", "SpellDamageScale", OperationType.Add, 0.85f),
            ("Augment_Poison_Duration", "PoisonDuration", OperationType.Add, 1f),

            ("Augment_Overload_Mana_Step", "ManaPerStep", OperationType.Subtract, 1.5f),

            ("Augment_Cooldown_Chance", "CooldownReduceChance", OperationType.Add, 0.15f),
            ("Augment_Heal_On_Hit", "HealOnHit", OperationType.Add, 0.07f),
            ("Augment_More_Retaliation", "ArmorReturn", OperationType.Add, 0.15f),

            ("Augment_Heal_From_Empowered_Ability_Damage", "HealPercent", OperationType.Add, 0.15f),

            ("Augment_Fury_More_Burn", "FuryHealthPercent", OperationType.Add, 0.035f),
            ("Augment_Fury_Less_Burn", "FuryHealthPercent", OperationType.Subtract, 0.02f),

            ("Augment_Health_Bonus", "HealthBonus", OperationType.Add, 0.15f),

            ("Augment_Restore_Mana_Health_On_Hit", "HealthRestore", OperationType.Add, 0.07f),
            ("Augment_Restore_Mana_Health_On_Hit", "ManaRestore", OperationType.Add, 0.07f),

            ("Augment_Extend_Stun_Add_Cost", "StunDuration", OperationType.Add, 1f),
            ("Augment_Extend_Stun_Add_Cost", "CostValue", OperationType.Add, 50f),

            ("Augment_Additional_Health_Regen", "HealthRegen", OperationType.Add, 0.025f),
            ("Augment_Add_Effectiveness_Reduce_Stacks", "Effectiveness", OperationType.Add, 0.35f),
            ("Augment_Add_Effectiveness_Reduce_Stacks", "Stacks", OperationType.Subtract, 2f),

            ("Augment_Reduce_Execution_Threshold", "ExecutionThreshold", OperationType.Subtract, 5f),

            ("Augment_Additional_Projectiles", "ProjectileCount", OperationType.Add, 2f),
            ("Augment_Buff_Effectiveness", "Effectiveness", OperationType.Add, 0.25f),
            ("Augment_Recovery_Effectiveness", "Effectiveness", OperationType.Add, 0.35f),
            ("Augment_Debuff_Effectiveness", "Effectiveness", OperationType.Add, 0.25f),

            ("Augment_Additional_Crit_Damage", "CriticalDamageBonus", OperationType.Add, 0.75f),
            ("Augment_Additional_Crit_Chance", "CriticalChanceBonus", OperationType.Add, 0.35f),

            ("Augment_Stage_Four_Damage", "ExtraBlockDamagePercent", OperationType.Add, 0.25f)
        ];

        [TestMethod]
        public void EveryTranslatedRecordMovesTheParametersItsOwnClassUsedTo()
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();

            foreach (IGrouping<string, (string Augment, string Parameter, OperationType Operation, float Amount)> augmentEntry in Translated())
            {
                IAbilityAugment augment = Built(registry, catalog, augmentEntry.Key, properties => properties);

                foreach (float start in s_bases)
                {
                    ParameterProbe probe = ProbeAt(start);
                    augment.Apply(probe);

                    foreach ((_, string parameter, OperationType operation, float amount) in augmentEntry)
                        Assert.AreEqual(Moved(operation, start, amount), probe.Read(parameter),
                            $"'{augmentEntry.Key}' no longer moves '{parameter}' the way its own class did");
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
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();

            foreach (IGrouping<string, (string Augment, string Parameter, OperationType Operation, float Amount)> augmentEntry in Translated())
            {
                IAbilityAugment augment = Built(registry, catalog, augmentEntry.Key, _ => []);
                ParameterProbe probe = ProbeAt(0f);
                augment.Apply(probe);

                foreach ((_, string parameter, OperationType operation, float amount) in augmentEntry)
                    Assert.AreEqual(Moved(operation, 0f, amount), probe.Read(parameter),
                        $"'{augmentEntry.Key}' lost the amount it falls back to on '{parameter}' when the record carries no property");
            }
        }

        [TestMethod]
        public void EveryTranslatedRecordTakesItsAmountFromItsOwnRecordAndNotFromTheFallback()
        {
            // The other half: a fallback that happens to equal the shipped number hides a property name
            // nobody reads any more. Doubling every number the record carries has to double every number
            // the augment moves — a move reaching for a property the record does not have would sit on
            // its fallback and show up here as the figure that did not budge.
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();

            foreach (IGrouping<string, (string Augment, string Parameter, OperationType Operation, float Amount)> augmentEntry in Translated())
            {
                IAbilityAugment augment = Built(registry, catalog, augmentEntry.Key,
                    properties => properties.ToDictionary(entry => entry.Key, entry => entry.Value * 2f));
                ParameterProbe probe = ProbeAt(0f);
                augment.Apply(probe);

                foreach ((_, string parameter, OperationType operation, float amount) in augmentEntry)
                    Assert.AreEqual(Moved(operation, 0f, amount * 2f), probe.Read(parameter),
                        $"'{augmentEntry.Key}' reads '{parameter}' off its fallback instead of off its own record");
            }
        }

        [TestMethod]
        public void TakingATranslatedRecordOffLeavesTheAbilityWhereItWas()
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();

            foreach (IGrouping<string, (string Augment, string Parameter, OperationType Operation, float Amount)> augmentEntry in Translated())
            {
                IAbilityAugment augment = Built(registry, catalog, augmentEntry.Key, properties => properties);
                ParameterProbe probe = ProbeAt(8f);

                augment.Apply(probe);
                augment.Remove(probe);

                foreach ((_, string parameter, _, _) in augmentEntry)
                    Assert.AreEqual(8f, probe.Read(parameter),
                        $"'{augmentEntry.Key}' left a decorator on '{parameter}' after it was taken off");
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
            AbilityProvider registry = ShippedAbilityData.Abilities();
            var buildable = registry.BuildableAugmentIds.ToList();

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
        private static IAbilityAugment Built(
            AbilityProvider registry,
            AbilityAugmentCatalog catalog,
            string augmentId,
            Func<Dictionary<string, float>, Dictionary<string, float>> numbers)
        {
            AbilityAugmentData? record = catalog.Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");

            IAbilityAugment? upgrade = registry.CreateUpgrade(record with { UpgradeProperties = numbers(record.UpgradeProperties) });
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
