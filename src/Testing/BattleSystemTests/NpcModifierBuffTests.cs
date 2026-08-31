namespace LastBreathTest.BattleSystemTests
{
    using Battle.Internal.Npc;
    using Battle.Source;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Data.NpcBuffsData;
    using Core.Data.NpcModifiersData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Entity.NpcModifiers;
    using Core.Enums;
    using Core.Items.Grants;
    using Core.Modifiers;
    using LootGeneration.Source;
    using Moq;
    using Newtonsoft.Json;

    /// <summary>
    /// A modifier is a bargain: the NPC gets stronger, the corpse pays better. Only the paying half was
    /// ever wired — every buff id in the catalog resolved into nothing on the bearer, and nothing said so.
    /// These are the guards over the half that was missing: the promise in NpcBuffs.json reaching the NPC,
    /// a promise with nothing behind it being reported instead of swallowed, and the two stacking rules
    /// the modifiers are balanced around.
    /// </summary>
    [TestClass]
    public class NpcModifierBuffTests
    {
        private const string BuffsFile = "NpcBuffs.json";
        private const string ModifiersFile = "NpcModifiers.json";

        /// <summary>
        /// The convention, checked over the SHIPPED catalogs rather than a list written here: every modifier
        /// that names a buff gets exactly the lines that buff declares, on the parameter it declares, in the
        /// value type it declares — and stamped with its own instance, which is what lets the lines leave
        /// with it. A buff id with no entry behind it fails here too: that is the silent death itself.
        /// </summary>
        [TestMethod]
        public void EveryShippedModifierWithABuffIdPutsItsDeclaredLinesOnTheBearer()
        {
            NpcBuffsData catalog = ShippedBuffs();
            var byId = catalog.Buffs.ToDictionary(buff => buff.Id, StringComparer.Ordinal);
            var provider = new NpcBuffProvider(catalog);

            List<INpcModifier> modifiers = [.. ShippedModifiers().Where(modifier => !string.IsNullOrEmpty(modifier.NpcBuffId))];
            Assert.IsTrue(modifiers.Count > 0, "no shipped modifier names a buff — this guard would be checking nothing");

            List<string> broken = [];
            foreach (INpcModifier modifier in modifiers)
            {
                if (!byId.TryGetValue(modifier.NpcBuffId, out NpcBuffData? buff))
                {
                    broken.Add($"{modifier.Id} names '{modifier.NpcBuffId}', which the catalog does not hold");
                    continue;
                }

                var owner = new BuffTarget();
                new NpcBuffBinder(provider).Rebuild(owner.Fighter, [modifier]);

                foreach (NpcBuffModifierData line in buff.Modifiers)
                {
                    var parameter = EnumParser.ParseEnum<EntityParameter>(line.Parameter);
                    var type = EnumParser.ParseEnum<ModifierValueType>(line.Type);
                    bool landed = owner.Modifiers.EntityModifiers.TryGetValue(parameter, out var bucket)
                                  && bucket.Any(instance => instance.ModifierValueType == type
                                                            && Math.Abs(instance.Value - line.Value) < 0.0001f
                                                            && instance.Source == modifier.InstanceId);
                    if (!landed) broken.Add($"{modifier.Id}: {line.Type} {line.Value} {line.Parameter} never reached the bearer");
                }
            }

            Assert.AreEqual(0, broken.Count, string.Join("; ", broken));
        }

        /// <summary>The other half of the bargain's bearer side: the modifiers that hand over a passive
        /// rather than a number. The grant has to name the very skill the modifier's loot side rolls onto
        /// the drop — "Палач" that executes and drops an executing weapon, not two unrelated ids.</summary>
        [TestMethod]
        public void EveryEffectModifierGrantsThePassiveItsDropCarries()
        {
            var byId = ShippedBuffs().Buffs.ToDictionary(buff => buff.Id, StringComparer.Ordinal);

            List<IItemEffectsModifier> effects = [.. ShippedModifiers().OfType<IItemEffectsModifier>()];
            Assert.IsTrue(effects.Count > 0, "the catalog ships no item-effect modifiers");

            List<string> mismatched = [];
            foreach (IItemEffectsModifier modifier in effects)
            {
                var buff = byId.GetValueOrDefault(((INpcModifier)modifier).NpcBuffId);
                string[] granted = buff == null ? [] : [.. buff.Grants.Select(grant => grant.Id)];
                if (!granted.Contains(modifier.EffectId))
                    mismatched.Add($"{((INpcModifier)modifier).Id} rolls '{modifier.EffectId}' onto the drop but grants [{string.Join(", ", granted)}]");
            }

            Assert.AreEqual(0, mismatched.Count, string.Join("; ", mismatched));
        }

        /// <summary>
        /// Every grant the catalog ships is really BUILDABLE — each one run through the live registry, not
        /// just the one a hand-picked test happens to use. This is the item-grant-property-keys scar: a
        /// property key the skill's factory does not read (a typo, a renamed field) makes the registry
        /// refuse the skill, and the NPC would go into the fight without the ability its modifier promised
        /// and without a word anywhere.
        /// </summary>
        [TestMethod]
        public void EveryShippedGrantIsBuiltByTheLiveRegistry()
        {
            NpcBuffsData catalog = ShippedBuffs();
            var provider = new NpcBuffProvider(catalog);

            List<NpcBuffData> granting = [.. catalog.Buffs.Where(buff => buff.Grants.Count > 0)];
            Assert.IsTrue(granting.Count > 0, "no shipped buff grants anything — this guard would be checking nothing");

            List<string> refused = [];
            foreach (NpcBuffData buff in granting)
            {
                var owner = new BuffTarget();
                var carrier = new ScaleModifier($"Test_Carrier_{buff.Id}", 1f, 1f, 0f, false, buff.Id);
                new NpcBuffBinder(provider, Grants()).Rebuild(owner.Fighter, [carrier]);

                foreach (NpcBuffGrantData grant in buff.Grants)
                    if (!owner.Passives.Skills.Any(skill => skill.Id == grant.Id))
                        refused.Add($"{buff.Id} → {grant.Id} (properties: {string.Join(", ", grant.Properties.Keys)})");
            }

            Assert.AreEqual(0, refused.Count,
                $"grants the shipped catalog promises that the registry refuses to build: {string.Join("; ", refused)}");
        }

        /// <summary>The granted passive is really on the fighter — built by the live registry, through the
        /// same grant factory an item uses — and it leaves when the modifier does.</summary>
        [TestMethod]
        public void AGrantedPassiveArrivesWithTheModifierAndLeavesWithIt()
        {
            var owner = new BuffTarget();
            var component = owner.NpcModifiers;
            component.UseBuffs(new NpcBuffBinder(new NpcBuffProvider(ShippedBuffs()), Grants()));

            INpcModifier executioner = ShippedModifiers().First(modifier => modifier.Id == "Npc_Modifier_Item_Effect_Execution");
            component.AddModifier(executioner);

            Assert.IsTrue(owner.Passives.Skills.Any(skill => skill.Id == "Passive_Skill_Execute"),
                "the Executioner modifier left its bearer without Execute");

            component.RemoveModifier(executioner.InstanceId);

            Assert.IsFalse(owner.Passives.Skills.Any(skill => skill.Id == "Passive_Skill_Execute"),
                "the granted passive outlived the modifier that handed it over");
        }

        /// <summary>Parameter lines leave with their modifier too — they are stamped with its instance for
        /// exactly this reason.</summary>
        [TestMethod]
        public void ParameterLinesLeaveWithTheModifier()
        {
            var owner = new BuffTarget();
            var component = owner.NpcModifiers;
            component.UseBuffs(new NpcBuffBinder(new NpcBuffProvider(ShippedBuffs())));

            INpcModifier royal = ShippedModifiers().First(modifier => modifier.Id == "Npc_Modifier_Tier_Upgrade_To_Maximum");
            component.AddModifier(royal);
            Assert.IsTrue(owner.Modifiers.EntityModifiers.ContainsKey(EntityParameter.CriticalChance));

            component.RemoveModifier(royal.InstanceId);
            Assert.IsFalse(owner.Modifiers.EntityModifiers.ContainsKey(EntityParameter.CriticalChance),
                "the crit line stayed behind after its modifier was taken off");
        }

        /// <summary>A scaling modifier is worth its scale on every OTHER modifier's buff, and the batch is
        /// bound after the whole list is in — so the order the two arrive in cannot change the result.</summary>
        [TestMethod]
        public void AScalingModifierIsWorthItsScaleOnAnotherModifiersBuff()
        {
            foreach (bool scalerFirst in (bool[])[true, false])
            {
                var owner = new BuffTarget();
                var component = owner.NpcModifiers;
                component.UseBuffs(new NpcBuffBinder(new NpcBuffProvider(ShippedBuffs())));

                INpcModifier scaler = ShippedModifiers().First(modifier => modifier.Id == "Npc_Modifier_Scale_Double_Health");
                INpcModifier royal = ShippedModifiers().First(modifier => modifier.Id == "Npc_Modifier_Tier_Upgrade_To_Maximum");
                component.AddModifiers(scalerFirst ? [scaler, royal] : [royal, scaler]);

                float crit = owner.Modifiers.EntityModifiers[EntityParameter.CriticalChance].Sum(line => line.Value);
                Assert.AreEqual(0.15f * 2.5f, crit, 0.0001f,
                    $"the 1.5 scale did not reach the crit buff (scaler added {(scalerFirst ? "first" : "last")})");
            }
        }

        /// <summary>A scaler arriving AFTER the buff was already bound rebinds it at the new scale, and
        /// taking that scaler off again gives the value back. The batch path never reaches this branch —
        /// it settles the whole list before binding — so without this pin the rebuild is dead code.</summary>
        [TestMethod]
        public void ABuffAlreadyBoundIsRecalculatedWhenAScalerArrivesLater()
        {
            var owner = new BuffTarget();
            var component = owner.NpcModifiers;
            component.UseBuffs(new NpcBuffBinder(new NpcBuffProvider(ShippedBuffs())));

            INpcModifier royal = ShippedModifiers().First(modifier => modifier.Id == "Npc_Modifier_Tier_Upgrade_To_Maximum");
            INpcModifier scaler = ShippedModifiers().First(modifier => modifier.Id == "Npc_Modifier_Scale_Double_Health");

            component.AddModifiers([royal]);
            Assert.AreEqual(0.15f, Crit(owner), 0.0001f, "the crit buff did not land at its own scale");

            component.AddModifiers([scaler]);
            Assert.AreEqual(0.15f * 2.5f, Crit(owner), 0.0001f, "a scaler arriving after the buff was bound left the old value standing");

            component.RemoveModifier(scaler.InstanceId);
            Assert.AreEqual(0.15f, Crit(owner), 0.0001f, "taking the scaler off did not give the scaled-up value back");
        }

        /// <summary>#224: a flat buff keeps its relative weight as the bearer levels — every bound line is
        /// worth base × levelFactor × totalScale, where levelFactor is the very 1 + (level − 1) × levelScaling
        /// the provider applied to the base parameters. Level 25 at scaling 0.05 is the owner's calibration
        /// point: ×2.2.</summary>
        [TestMethod]
        public void TheLevelFactorMultipliesEveryBoundLine()
        {
            var owner = new BuffTarget();
            var component = owner.NpcModifiers;
            component.UseBuffs(new NpcBuffBinder(new NpcBuffProvider(ShippedBuffs()), levelFactor: 2.2f));

            component.AddModifier(ShippedModifiers().First(modifier => modifier.Id == "Npc_Modifier_Tier_Upgrade_To_Maximum"));

            Assert.AreEqual(0.15f * 2.2f, Crit(owner), 0.0001f, "the bearer's level factor did not reach the bound line");
        }

        /// <summary>A level factor of exactly 1 — every level-1 spawn — leaves the values standing where
        /// they always stood: the feature changes nothing until the bearer actually outgrows level 1.</summary>
        [TestMethod]
        public void ALevelFactorOfOneChangesNothing()
        {
            var owner = new BuffTarget();
            var component = owner.NpcModifiers;
            component.UseBuffs(new NpcBuffBinder(new NpcBuffProvider(ShippedBuffs()), levelFactor: 1f));

            component.AddModifier(ShippedModifiers().First(modifier => modifier.Id == "Npc_Modifier_Tier_Upgrade_To_Maximum"));

            Assert.AreEqual(0.15f, Crit(owner), 0.0001f, "a level factor of 1 moved a value it must leave alone");
        }

        /// <summary>The two scalars stack multiplicatively and the order cannot matter — the whole line is
        /// base × levelFactor × totalScale whichever side arrives first.</summary>
        [TestMethod]
        public void TheLevelFactorAndTheScaleMultiplyTogether()
        {
            var owner = new BuffTarget();
            var component = owner.NpcModifiers;
            component.UseBuffs(new NpcBuffBinder(new NpcBuffProvider(ShippedBuffs()), levelFactor: 2.2f));

            List<INpcModifier> shipped = ShippedModifiers();
            component.AddModifiers(
            [
                shipped.First(modifier => modifier.Id == "Npc_Modifier_Tier_Upgrade_To_Maximum"),
                shipped.First(modifier => modifier.Id == "Npc_Modifier_Scale_Double_Health"),
            ]);

            Assert.AreEqual(0.15f * 2.2f * 2.5f, Crit(owner), 0.0001f,
                "level factor and TotalScale did not multiply into one line");
        }

        /// <summary>Granted passives stay WHOLE under the level factor for the same magnitude reason
        /// TotalScale never touches them: their properties are thresholds and durations, and 0.3 × 2.2
        /// would turn Execute into a kill-on-hit. The factory must see the authored properties untouched.</summary>
        [TestMethod]
        public void TheLevelFactorNeverTouchesAGrant()
        {
            IReadOnlyDictionary<string, float>? handed = null;
            var grants = new Mock<IGrantFactory>();
            grants.Setup(factory => factory.Create(
                    It.IsAny<GrantKind>(), It.IsAny<string>(), It.IsAny<List<IModifier>>(), It.IsAny<IReadOnlyDictionary<string, float>>()))
                .Callback((GrantKind _, string _, List<IModifier> _, IReadOnlyDictionary<string, float> properties) => handed = properties)
                .Returns(Mock.Of<Core.Items.IItemGrant>());

            var owner = new BuffTarget();
            new NpcBuffBinder(new NpcBuffProvider(ShippedBuffs()), grants.Object, levelFactor: 2.2f)
                .Rebuild(owner.Fighter, [ShippedModifiers().First(modifier => modifier.Id == "Npc_Modifier_Item_Effect_Execution")]);

            Assert.IsNotNull(handed, "the execution grant never reached the factory");
            Assert.AreEqual(0.3f, handed["threshold"], 0.0001f, "the level factor leaked into a grant's properties");
        }

        /// <summary>The vault gives the scaling section its own reading of "unique": several DIFFERENT
        /// scalers on one NPC, and that is authored as uniqueScope on the section, not decided by code.</summary>
        [TestMethod]
        public void DifferentScalersLiveTogetherOnOneNpc()
        {
            var owner = new BuffTarget();
            List<INpcModifier> shipped = ShippedModifiers();

            owner.NpcModifiers.AddModifiers(
            [
                shipped.First(modifier => modifier.Id == "Npc_Modifier_Scale_Double_Health"),
                shipped.First(modifier => modifier.Id == "Npc_Modifier_Scale_Double_Damage"),
            ]);

            Assert.AreEqual(2, owner.NpcModifiers.AllModifiers.Count,
                "the scaling section lost a modifier to a uniqueness rule the vault does not give it");
        }

        /// <summary>The same scaler twice is still refused — "unique" narrowed to the id, not dropped.</summary>
        [TestMethod]
        public void TheSameScalerTwiceIsStillRefused()
        {
            var owner = new BuffTarget();

            owner.NpcModifiers.AddModifiers(
            [
                ShippedModifiers().First(modifier => modifier.Id == "Npc_Modifier_Scale_Double_Health"),
                ShippedModifiers().First(modifier => modifier.Id == "Npc_Modifier_Scale_Double_Health"),
            ]);

            Assert.AreEqual(1, owner.NpcModifiers.AllModifiers.Count, "one NPC took the same scaler twice");
        }

        /// <summary>Narrowing the scaling section did not narrow the rest: the rarity floors stay one at a
        /// time, on the shipped data rather than on hand-built modifiers.</summary>
        [TestMethod]
        public void TheShippedRarityFloorsStayOneAtATime()
        {
            var owner = new BuffTarget();
            List<INpcModifier> shipped = ShippedModifiers();

            owner.NpcModifiers.AddModifiers(
            [
                shipped.First(modifier => modifier.Id == "Npc_Modifier_Min_Rarity_Rare"),
                shipped.First(modifier => modifier.Id == "Npc_Modifier_Min_Rarity_Epic"),
                shipped.First(modifier => modifier.Id == "Npc_Modifier_Min_Rarity_Legend"),
            ]);

            Assert.AreEqual(1, owner.NpcModifiers.AllModifiers.Count, "the NPC carries more than one rarity floor");
            Assert.AreEqual("Npc_Modifier_Min_Rarity_Legend", owner.NpcModifiers.AllModifiers[0].Id);
        }

        /// <summary>A second binder would own half the bearer's buffs and be unable to take them off again.</summary>
        [TestMethod]
        public void ASecondBinderIsRefused()
        {
            var owner = new BuffTarget();
            var provider = new NpcBuffProvider(ShippedBuffs());
            owner.NpcModifiers.UseBuffs(new NpcBuffBinder(provider));

            Assert.ThrowsException<InvalidOperationException>(() => owner.NpcModifiers.UseBuffs(new NpcBuffBinder(provider)));
        }

        /// <summary>The point of the whole pass: a buff id pointing at nothing is REPORTED. Before, it read
        /// exactly like a buff with no lines, and the NPC simply stayed weak.</summary>
        [TestMethod]
        public void ABuffIdWithNothingBehindItIsReported()
        {
            List<string> reported = [];
            var owner = new BuffTarget();
            var binder = new NpcBuffBinder(new NpcBuffProvider(ShippedBuffs()), report: reported.Add);

            binder.Rebuild(owner.Fighter, [new ScaleModifier("Npc_Modifier_Test", 1f, 1f, 0.5f, true, "Npc_Buff_That_Never_Shipped")]);

            Assert.AreEqual(1, reported.Count, "an unknown buff id passed unnoticed");
            StringAssert.Contains(reported[0], "Npc_Buff_That_Never_Shipped");
        }

        /// <summary>An empty buff id is not a broken link: the modifier simply promises the bearer nothing.</summary>
        [TestMethod]
        public void AModifierThatPromisesNothingIsNotReported()
        {
            List<string> reported = [];
            var owner = new BuffTarget();

            new NpcBuffBinder(new NpcBuffProvider(ShippedBuffs()), report: reported.Add)
                .Rebuild(owner.Fighter, [new ScaleModifier("Npc_Modifier_Test", 1f, 1f, 0.5f, true, string.Empty)]);

            Assert.AreEqual(0, reported.Count, $"a modifier with no buff id was reported as broken: {string.Join("; ", reported)}");
        }

        /// <summary>Design: "Повышающие тир — стакаются". Two of them raise the drop by the SUM of their
        /// steps, and the stack fires as often as "at least one of the two" — never less often than the
        /// better of them alone.</summary>
        [TestMethod]
        public void TierUpgradesStackInsteadOfTheStrongestWinning()
        {
            var context = new ModifierApplyingContext();
            var byTwo = new TierUpgradeModifier("Npc_Modifier_Tier_Upgrade_By_Two", 1f, 0.7f, false, string.Empty, 0.3f, 2);
            var byOne = new TierUpgradeModifier("Npc_Modifier_Tier_Upgrade_By_One", 1f, 0.5f, false, string.Empty, 0.5f, 1);

            byTwo.ApplyModifier(context);
            byOne.ApplyModifier(context);

            Assert.AreEqual(3, context.TierUpgradeBy, "the steps of the two upgrades did not add up");
            Assert.AreEqual(1f - (0.7f * 0.5f), context.TierUpgradeChance, 0.0001f,
                "the stack's chance is not 'at least one of them fires'");
            Assert.IsTrue(context.TierUpgradeChance > 0.5f, "stacking made the pair fire less often than the better one alone");
        }

        /// <summary>
        /// Scaled past certainty, a chance is CERTAINTY — not a negative remainder. Several scaling
        /// modifiers coexist on one NPC (the vault's reading of the scaling section), so TotalScale reaches
        /// 6.1 and a 0.3 chance scales to 1.83. Unclamped, two such modifiers multiply two negative
        /// remainders back into a positive product and the pair ends up LESS likely than either alone:
        /// (1 - 1.83) × (1 - 1.83) = 0.69, a chance of 0.31 for two guaranteed upgrades.
        /// </summary>
        [TestMethod]
        public void AChanceScaledPastCertaintyStaysCertain()
        {
            var context = new ModifierApplyingContext();
            var scaler = new ScaleModifier("Test_Scaler", 1f, 1f, 1.5f, false, string.Empty);

            var first = new TierUpgradeModifier("Test_Upgrade_A", 1f, 0.9f, false, string.Empty, 0.8f, 2);
            var second = new TierUpgradeModifier("Test_Upgrade_B", 1f, 0.7f, false, string.Empty, 0.9f, 1);
            foreach (var modifier in (TierUpgradeModifier[])[first, second])
            {
                modifier.ScaleUp(scaler);
                Assert.IsTrue(modifier.CurrentMultiplier > 1f, "the setup no longer scales the chance past 1 — the guard checks nothing");
                modifier.ApplyModifier(context);
            }

            Assert.AreEqual(1f, context.TierUpgradeChance, 0.0001f, "two certain upgrades did not add up to a certainty");
            Assert.AreEqual(3, context.TierUpgradeBy);
            Assert.AreEqual(0, context.TryUpgradeTier(currentTier: 2, chance: 1f), "the certain upgrade did not fire");
        }

        /// <summary>One modifier alone is untouched by the stacking rule — its own chance, its own step.</summary>
        [TestMethod]
        public void OneTierUpgradeAloneKeepsItsOwnChance()
        {
            var context = new ModifierApplyingContext();
            new TierUpgradeModifier("Npc_Modifier_Tier_Upgrade_To_Maximum", 1f, 0.9f, false, string.Empty, 0.2f, 99).ApplyModifier(context);

            Assert.AreEqual(99, context.TierUpgradeBy);
            Assert.AreEqual(0.2f, context.TierUpgradeChance, 0.0001f);
        }

        /// <summary>Design: "Уникальные модификаторы не складываются (более сильные заменяют более слабые)",
        /// read per catalog GROUP — three rarity floors at once is three difficulties for one floor.</summary>
        [TestMethod]
        public void TheStrongerUniqueOfAGroupReplacesTheWeakerOne()
        {
            var owner = new BuffTarget();
            var component = owner.NpcModifiers;

            component.AddModifier(Floor("Npc_Modifier_Min_Rarity_Rare", 0.1f));
            component.AddModifier(Floor("Npc_Modifier_Min_Rarity_Legend", 0.5f));

            Assert.AreEqual(1, component.AllModifiers.Count, "two floors of one group stand on the same NPC");
            Assert.AreEqual("Npc_Modifier_Min_Rarity_Legend", component.AllModifiers[0].Id);
        }

        [TestMethod]
        public void TheWeakerUniqueOfAGroupIsTurnedAway()
        {
            var owner = new BuffTarget();
            var component = owner.NpcModifiers;

            component.AddModifier(Floor("Npc_Modifier_Min_Rarity_Legend", 0.5f));
            component.AddModifier(Floor("Npc_Modifier_Min_Rarity_Rare", 0.1f));

            Assert.AreEqual(1, component.AllModifiers.Count);
            Assert.AreEqual("Npc_Modifier_Min_Rarity_Legend", component.AllModifiers[0].Id,
                "the weaker floor pushed the stronger one out");
        }

        /// <summary>Replacing takes the loser's buff off the bearer with it — the guard is worth nothing if
        /// the parameters of a modifier that is no longer there stay.</summary>
        [TestMethod]
        public void TheReplacedModifierTakesItsBuffWithIt()
        {
            var owner = new BuffTarget();
            var component = owner.NpcModifiers;
            component.UseBuffs(new NpcBuffBinder(new NpcBuffProvider(ShippedBuffs())));

            List<INpcModifier> shipped = ShippedModifiers();
            component.AddModifier(shipped.First(modifier => modifier.Id == "Npc_Modifier_Min_Rarity_Rare"));
            component.AddModifier(shipped.First(modifier => modifier.Id == "Npc_Modifier_Min_Rarity_Legend"));

            float attributes = owner.Modifiers.EntityModifiers[EntityParameter.AllAttribute].Sum(line => line.Value);
            Assert.AreEqual(50f, attributes, 0.0001f, "the replaced floor's attributes stayed on the bearer");
        }

        /// <summary>A modifier built by hand carries no group: uniqueness falls back to the id it always
        /// used, so nothing outside the catalog starts colliding with everything.</summary>
        [TestMethod]
        public void ModifiersWithoutAGroupStillCollideOnlyById()
        {
            var owner = new BuffTarget();
            var component = owner.NpcModifiers;

            component.AddModifier(new MinRarityModifier("Handmade_A", 1f, 0.5f, true, string.Empty, Rarity.Legendary));
            component.AddModifier(new MinRarityModifier("Handmade_B", 1f, 0.1f, true, string.Empty, Rarity.Rare));

            Assert.AreEqual(2, component.AllModifiers.Count);
        }

        private static float Crit(BuffTarget owner) =>
            owner.Modifiers.EntityModifiers.TryGetValue(EntityParameter.CriticalChance, out var lines) ? lines.Sum(line => line.Value) : 0f;

        private static INpcModifier Floor(string id, float difficulty) =>
            new MinRarityModifier(id, 1f, difficulty, true, string.Empty, Rarity.Rare) { Group = "minRarity" };

        private static IGrantFactory Grants() => new GrantFactory(() => new PassiveSkillProvider(), () => null, () => null);

        private static NpcBuffsData ShippedBuffs()
        {
            string path = Path.Combine(SharedData.Catalog(DataCatalog.NpcBuffs), BuffsFile);
            Assert.IsTrue(File.Exists(path), $"the NPC buff catalog ships at {path}");

            return JsonConvert.DeserializeObject<NpcBuffsData>(File.ReadAllText(path))!;
        }

        private static List<INpcModifier> ShippedModifiers()
        {
            string path = Path.Combine(SharedData.Catalog(DataCatalog.NpcModifiers), ModifiersFile);
            Assert.IsTrue(File.Exists(path), $"the NPC modifier catalog ships at {path}");

            return new NpcModifiersFactory().CreateNpcModifiers(NpcModifiersParser.Parse(File.ReadAllText(path)));
        }

        /// <summary>A body a buff can land on: a real parameters/modifiers pair, a real passive list and a
        /// real modifiers component — which is all a bound buff (and a scaling modifier) touches. An NPC
        /// rather than a bare fighter because a scaling modifier only recognises one.</summary>
        private sealed class BuffTarget
        {
            public BuffTarget()
            {
                var parameters = new EntityParametersComponent();
                parameters.Initialize(Modifiers.GetModifiers);
                Modifiers.ModifiersChanged += parameters.OnParameterModifiersChange;

                var mock = new Mock<IFightableNpc>();
                mock.SetupGet(fighter => fighter.ParameterModifiers).Returns(Modifiers);
                mock.SetupGet(fighter => fighter.Parameters).Returns(parameters);
                mock.SetupGet(fighter => fighter.CombatEvents).Returns(new CombatEventBus());
                Passives = new PassiveSkillsComponent(mock.Object);
                mock.SetupGet(fighter => fighter.PassiveSkills).Returns(Passives);
                NpcModifiers = new NpcModifiersComponent(mock.Object);
                mock.SetupGet(fighter => fighter.NpcModifiers).Returns(NpcModifiers);
                Fighter = mock.Object;
            }

            public IParameterModifiersComponent Modifiers { get; } = new ParameterModifiersComponent();

            public IPassiveSkillsComponent Passives { get; }

            public INpcModifiersComponent NpcModifiers { get; }

            public IFightable Fighter { get; }
        }
    }
}
