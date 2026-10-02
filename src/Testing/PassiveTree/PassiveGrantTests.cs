namespace LastBreathTest.PassiveTree
{
    using Battle.Source;
    using Battle.Source.PassiveSkills;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.Save;
    using Core.Save.Participants;
    using Core.Services;
    using Moq;

    /// <summary>
    /// A keystone can be a passive rather than a handful of lines: the node names one and carries the
    /// numbers it is tuned by, and taking the node puts it on the character. The grant is the allocation's
    /// — giving the node back takes the passive off, and takes off exactly the instance this service handed
    /// over, so a passive an item also grants survives the refund.
    /// <para>The universal stat passive is the other half: a keystone made of nothing but modifier lines is
    /// a record with the right field names and no class of its own.</para>
    /// </summary>
    [TestClass]
    public class PassiveGrantTests
    {
        private const string Seed = "start_str";

        /// <summary>The shape keystone_6 will take: a line measured per unit of a carrier and a flat
        /// penalty, both written as fields.</summary>
        private const string StatNode = "keystone_stats";

        private const string StatId = "Passive_Skill_Stats_Unlimited_Power";
        private const string PerStrength = "PhysicalDamage:Increase:Strength";

        /// <summary>The same line written per a STEP of the carrier — the shape an author reaches for when
        /// the per-unit number is too small to read.</summary>
        private const string PerHundredStrength = "PhysicalDamage:Increase:Strength:100";

        private const string RecoveryCut = "HealthRecovery:Multiplicative";

        /// <summary>A second stat passive, so a refund has a neighbour to leave alone.</summary>
        private const string OtherStatNode = "keystone_stats_other";

        private const string OtherStatId = "Passive_Skill_Stats_Iron_Skin";
        private const string ArmorLine = "Armor:Flat";

        /// <summary>The shape keystone_7 will take: a passive written in code, tuned by fields.</summary>
        private const string ClawsNode = "keystone_claws";

        private const string ClawsId = "Passive_Skill_Poisoned_Claws";

        /// <summary>A node naming a passive no registry holds — what a rename or a dropped class leaves.</summary>
        private const string GhostNode = "keystone_ghost";

        private const string GhostId = "Passive_Skill_Never_Written";

        /// <summary>A stat node whose field name is a typo: the author meant PhysicalDamage.</summary>
        private const string TypoNode = "keystone_typo";

        private const string TypoId = "Passive_Skill_Stats_Typo";

        /// <summary>A node that speaks in lines: it must stay the parametric channel's business.</summary>
        private const string LineNode = "small_lines";

        private const float StrengthBase = 10f;
        private const float PhysicalDamageBase = 100f;
        private const float HealthRecoveryBase = 20f;

        [TestMethod]
        public void ACharacterWhoTookNothingCarriesNoPassive()
        {
            var carrier = new Carrier();
            CreateService(carrier, NewTree());

            Assert.AreEqual(0, carrier.Skills.Skills.Count, "a passive reached the character without a node paying for it");
        }

        [TestMethod]
        public void TakingANodeGrantsThePassiveTunedByItsOwnNumbers()
        {
            var carrier = new Carrier();
            IPassiveTreeService tree = NewTree();
            CreateService(carrier, tree);

            Assert.AreEqual(AllocationResult.Success, tree.Take(ClawsNode));

            var claws = carrier.Skills.GetSkill(ClawsId) as PoisonedClaws;
            Assert.IsNotNull(claws, "the node named a passive the character never received");
            Assert.AreEqual(0.75f, claws.PercentFromDamage, 0.0001f, "the node's numbers did not reach the passive");
            Assert.AreEqual(3, claws.PoisonDuration);
        }

        [TestMethod]
        public void AStatNodeHangsItsFieldsOnTheCharacterAsModifiers()
        {
            var carrier = new Carrier();
            IPassiveTreeService tree = NewTree();
            CreateService(carrier, tree);

            Assert.AreEqual(AllocationResult.Success, tree.Take(StatNode));

            // 0.01 per point of Strength, measured off a carrier holding 10: +10% of a 100 base.
            Assert.AreEqual(110f, carrier.Value(EntityParameter.PhysicalDamage), 0.001f);
            Assert.AreEqual(15f, carrier.Value(EntityParameter.HealthRecovery), 0.001f);
        }

        [TestMethod]
        public void AStatLineMeasuredPerParameterGrowsWithItsCarrier()
        {
            var carrier = new Carrier();
            IPassiveTreeService tree = NewTree();
            CreateService(carrier, tree);
            tree.Take(StatNode);

            carrier.Parameters.SetBaseValueForParameter(EntityParameter.Strength, 50f);

            Assert.AreEqual(150f, carrier.Value(EntityParameter.PhysicalDamage), 0.001f,
                "the line was minted flat instead of being measured off the carrier");
        }

        /// <summary>A passive is tuned at birth, so a node re-tuned under the ledger — same id, different
        /// numbers, which is what editing the document while the character holds the node does — has to be
        /// taken off and handed over again. A ledger comparing only the id would leave the character
        /// wearing the numbers the file no longer carries.</summary>
        [TestMethod]
        public void ANodeRetunedUnderTheLedgerHandsOverTheNumbersItNowCarries()
        {
            var carrier = new Carrier();
            PassiveTreeService tree = NewTree();
            PassiveGrantService service = CreateService(carrier, tree);
            tree.Take(StatNode);
            PassiveNode node = tree.Tree.Find(StatNode)!;

            node.Properties[PerStrength] = 0.05f;
            service.Reconcile();

            Assert.AreEqual(150f, carrier.Value(EntityParameter.PhysicalDamage), 0.001f,
                "the re-tuned node kept handing out the numbers it used to carry");
            Assert.AreEqual(1, carrier.Skills.Skills.Count, "the re-tune left the old copy standing beside the new one");
            Assert.AreEqual(1, carrier.Modifiers.GetModifiers(EntityParameter.PhysicalDamage).Count);
        }

        /// <summary>Taking the passive off has to release the line, not merely delete it from the list. A
        /// line measured off the carrier watches his parameters, and a watch left behind keeps answering for
        /// a fighter nobody owns any more — the shape a scene change turns into a native crash.</summary>
        [TestMethod]
        public void ALineTakenOffStopsAnsweringTheCarrier()
        {
            var carrier = new Carrier();
            IPassiveTreeService tree = NewTree();
            CreateService(carrier, tree);
            tree.Take(StatNode);
            IModifierInstance scaled = carrier.Modifiers.GetModifiers(EntityParameter.PhysicalDamage).Single();

            Assert.AreEqual(AllocationResult.Success, tree.Refund(StatNode));
            carrier.Parameters.SetBaseValueForParameter(EntityParameter.Strength, 50f);

            Assert.AreEqual(0f, scaled.Value, 0.0001f,
                "the removed line re-measured itself off a carrier it no longer belongs to");
        }

        [TestMethod]
        public void RespecTakesThePassiveOffAndItsModifiersWithIt()
        {
            var carrier = new Carrier();
            IPassiveTreeService tree = NewTree();
            CreateService(carrier, tree);
            tree.Take(StatNode);
            tree.Take(ClawsNode);

            tree.Respec();

            Assert.AreEqual(0, carrier.Skills.Skills.Count, "a refunded node's passive outlived the point that paid for it");
            Assert.AreEqual(PhysicalDamageBase, carrier.Value(EntityParameter.PhysicalDamage), 0.001f);
            Assert.AreEqual(HealthRecoveryBase, carrier.Value(EntityParameter.HealthRecovery), 0.001f);
            Assert.AreEqual(0, carrier.Modifiers.GetModifiers(EntityParameter.PhysicalDamage).Count,
                "the passive left a modifier behind on the character");
            Assert.AreEqual(0, carrier.Modifiers.GetModifiers(EntityParameter.HealthRecovery).Count);
        }

        [TestMethod]
        public void ARefundTakesBackOnlyWhatItsOwnNodeGaveOut()
        {
            var carrier = new Carrier();
            IPassiveTreeService tree = NewTree();
            CreateService(carrier, tree);
            tree.Take(StatNode);
            tree.Take(OtherStatNode);

            Assert.AreEqual(AllocationResult.Success, tree.Refund(StatNode));

            Assert.IsNull(carrier.Skills.GetSkill(StatId));
            Assert.IsNotNull(carrier.Skills.GetSkill(OtherStatId), "the refund took the neighbouring node's passive too");
        }

        /// <summary>An item and a node handing over one passive id: the node's refund must take back its own
        /// registration and leave the item's standing, which only an instance-exact revocation does.</summary>
        [TestMethod]
        public void ARefundLeavesAnotherSourcesCopyOfTheSamePassiveStanding()
        {
            var carrier = new Carrier();
            IPassiveTreeService tree = NewTree();
            CreateService(carrier, tree);
            ISkill fromAnItem = new PoisonedClaws(percentFormDamageFromDamage: 0.9f, poisonDuration: 5);
            carrier.Skills.AddSkill(fromAnItem);
            tree.Take(ClawsNode);

            Assert.AreEqual(AllocationResult.Success, tree.Refund(ClawsNode));

            var standing = carrier.Skills.GetSkill(ClawsId) as PoisonedClaws;
            Assert.IsNotNull(standing, "the node's refund took the item's passive with it");
            Assert.AreEqual(0.9f, standing.PercentFromDamage, 0.0001f);
        }

        /// <summary>A second pass must hand nothing over again. The roster hides a double grant — a fresh
        /// copy of one id evicts the older registration — so the claim is the copy on the character being
        /// the very one the ledger wrote down, and the refund afterwards leaving nothing at all: a second
        /// copy the ledger forgot would survive it.</summary>
        [TestMethod]
        public void RepeatedReconcileGrantsOneCopy()
        {
            var carrier = new Carrier();
            IPassiveTreeService tree = NewTree();
            PassiveGrantService service = CreateService(carrier, tree);
            tree.Take(StatNode);
            ISkill? granted = carrier.Skills.GetSkill(StatId);

            service.Reconcile();
            service.Reconcile();

            Assert.AreEqual(1, carrier.Skills.Skills.Count);
            Assert.AreSame(granted, carrier.Skills.GetSkill(StatId), "a second pass built the passive again");
            Assert.AreEqual(1, carrier.Modifiers.GetModifiers(EntityParameter.HealthRecovery).Count,
                "a second pass hung the same line on the character twice");
            Assert.AreEqual(110f, carrier.Value(EntityParameter.PhysicalDamage), 0.001f);

            Assert.AreEqual(AllocationResult.Success, tree.Refund(StatNode));

            Assert.AreEqual(0, carrier.Skills.Skills.Count, "a copy the ledger forgot outlived the node");
            Assert.AreEqual(0, carrier.Modifiers.GetModifiers(EntityParameter.HealthRecovery).Count);
        }

        [TestMethod]
        public void ANodeSpeakingInLinesHandsOverNoPassive()
        {
            var carrier = new Carrier();
            IPassiveTreeService tree = NewTree();
            CreateService(carrier, tree);

            Assert.AreEqual(AllocationResult.Success, tree.Take(LineNode));

            Assert.AreEqual(0, carrier.Skills.Skills.Count);
        }

        [TestMethod]
        public void ANodeNamingAPassiveNoRegistryHoldsGrantsNothing()
        {
            var carrier = new Carrier();
            IPassiveTreeService tree = NewTree();
            PassiveGrantService service = CreateService(carrier, tree);

            Assert.AreEqual(AllocationResult.Success, tree.Take(GhostNode));
            service.Reconcile();

            Assert.AreEqual(0, carrier.Skills.Skills.Count, "an id nobody can build became a passive anyway");
        }

        [TestMethod]
        public void ANewPlaythroughLeavesTheCharacterWithNoPassive()
        {
            var carrier = new Carrier();
            var tree = NewTree();
            CreateService(carrier, tree);
            tree.Take(StatNode);

            tree.ResetSession();

            Assert.AreEqual(0, carrier.Skills.Skills.Count, "the previous playthrough's passive survived the reset");
            Assert.AreEqual(PhysicalDamageBase, carrier.Value(EntityParameter.PhysicalDamage), 0.001f);
        }

        /// <summary>The service outlives the scene, the player does not: a second player in the same process
        /// starts with an empty roster and has to come back holding the allocation.</summary>
        [TestMethod]
        public void SecondPlayerInTheSameProcessGetsHisPassivesBack()
        {
            var accessor = new PlayerAccessor();
            var first = new Carrier();
            accessor.Set(first.Player);
            IPassiveTreeService tree = NewTree();
            CreateService(accessor, tree);
            tree.Take(StatNode);

            var second = new Carrier();
            accessor.Set(second.Player);

            Assert.IsNotNull(second.Skills.GetSkill(StatId), "the new player was left without the allocation's passives");
            Assert.AreEqual(110f, second.Value(EntityParameter.PhysicalDamage), 0.001f);
            Assert.AreEqual(0, first.Skills.Skills.Count, "the previous player kept wearing a passive nobody accounts for");
        }

        /// <summary>Nothing of the grant is saved: the allocation is, and restoring it announces itself
        /// through the same change every purchase raises. Run through a real save manager so the claim is
        /// the round trip and not a hand-made call.</summary>
        [TestMethod]
        public void LoadingAFileWithAPassiveNodeBringsThePassiveBack()
        {
            var sourceTree = NewTree();
            CreateService(new Carrier(), sourceTree);
            sourceTree.Take(StatNode);
            sourceTree.Take(ClawsNode);
            SaveFile file = ManagerFor(sourceTree).Capture(new SaveMetadata());

            var carrier = new Carrier();
            IPassiveTreeService targetTree = NewTree();
            CreateService(carrier, targetTree);
            ManagerFor(targetTree).Restore(file);

            Assert.IsNotNull(carrier.Skills.GetSkill(StatId), "the restored allocation did not bring its passive back");
            Assert.IsNotNull(carrier.Skills.GetSkill(ClawsId));
            Assert.AreEqual(110f, carrier.Value(EntityParameter.PhysicalDamage), 0.001f);
        }

        [TestMethod]
        public void AProjectWithoutTheTreeServiceGrantsNothing()
        {
            var carrier = new Carrier();
            var accessor = new PlayerAccessor();
            accessor.Set(carrier.Player);

            var service = new PassiveGrantService(accessor, new PassiveSkillProvider());
            service.Reconcile();

            Assert.AreEqual(0, carrier.Skills.Skills.Count);
        }

        // ---- the field language of the stat passive -------------------------------------------------

        [TestMethod]
        public void AStatPassiveTurnsEveryFieldItIsGivenIntoALine()
        {
            var skill = (StatPassiveSkill?)Build(StatId, KeystoneSixFields());

            Assert.IsNotNull(skill);
            Assert.AreEqual(2, skill.Lines.Count, "a field the record carried became no line");
            Assert.AreEqual(EntityParameter.PhysicalDamage, skill.Lines[0].Parameter);
            Assert.AreEqual(ModifierValueType.Increase, skill.Lines[0].ValueType);
            Assert.AreEqual(EntityParameter.Strength, skill.Lines[0].PerParameter);
            Assert.AreEqual(0.01f, skill.Lines[0].Value, 0.0001f);
            Assert.AreEqual(EntityParameter.HealthRecovery, skill.Lines[1].Parameter);
            Assert.AreEqual(ModifierValueType.Multiplicative, skill.Lines[1].ValueType);
            Assert.IsNull(skill.Lines[1].PerParameter);
            Assert.AreEqual(-0.25f, skill.Lines[1].Value, 0.0001f);
        }

        [TestMethod]
        public void AFieldTheGrammarCannotReadRefusesTheWholePassive()
        {
            Assert.IsNull(Build(TypoId, new Dictionary<string, float> { ["PhysicalDamge:Increase"] = 0.1f }),
                "a typo in a field name handed out a passive missing the line the author wrote");
            Assert.IsNull(Build(TypoId, new Dictionary<string, float> { ["PhysicalDamage"] = 0.1f }),
                "a key naming no value bucket was read anyway");
            // Every word of it names something real, so what is refused here is the COUNT and nothing else:
            // a fifth word with a bad name in it would be refused for the name and leave the ceiling untested.
            Assert.IsNull(Build(TypoId, new Dictionary<string, float> { ["PhysicalDamage:Increase:Strength:100:5"] = 0.1f }),
                "a key of five words was read as a line, so the grammar has no ceiling");
            Assert.IsNull(Build(TypoId, new Dictionary<string, float> { ["PhysicalDamage:Flag"] = 1f }),
                "a pipeline switch was read as a parametric line");
            Assert.IsNull(Build(TypoId, new Dictionary<string, float> { ["Strength:Increase:Strength"] = 0.1f }),
                "a line was measured per unit of the parameter it feeds");
            Assert.IsNull(Build(TypoId, new Dictionary<string, float> { ["1:Increase"] = 0.1f }),
                "a number was read as the name of an enum member");
            Assert.IsNull(Build(TypoId, new Dictionary<string, float>()),
                "a stat passive with no fields is a passive with nothing to give");
        }

        /// <summary>The step is the fourth word and nothing else may stand there. A key with no carrier has
        /// nowhere to put one; a step that is not a whole count above zero is not a step at all — and every
        /// one of these refuses the whole record, the way any other unreadable field does.</summary>
        [TestMethod]
        public void OnlyAWholeCountAboveZeroIsReadAsTheStep()
        {
            Assert.IsNull(Build(TypoId, new Dictionary<string, float> { ["PhysicalDamage:Increase:100"] = 0.1f }),
                "a step was read without the carrier it counts");
            Assert.IsNull(Build(TypoId, new Dictionary<string, float> { ["PhysicalDamage:Increase:Strength:Extra"] = 0.1f }),
                "a word that names no number was read as a step");
            Assert.IsNull(Build(TypoId, new Dictionary<string, float> { ["PhysicalDamage:Increase:Strength:0"] = 0.1f }),
                "a step of zero was read, and the mint would divide the line by it");
            Assert.IsNull(Build(TypoId, new Dictionary<string, float> { ["PhysicalDamage:Increase:Strength:-5"] = 0.1f }),
                "a negative step was read, and the line would come out the wrong way round");
            Assert.IsNull(Build(TypoId, new Dictionary<string, float> { ["PhysicalDamage:Increase:Strength:2.5"] = 0.1f }),
                "a fractional step was read, so a step has two spellings and the writer only knows one");

            Assert.IsNotNull(Build(StatId, new Dictionary<string, float> { [PerHundredStrength] = 0.01f }),
                "a step the writer spells is a step the grant refuses");
        }

        /// <summary>What the step IS: a way of writing the same per-unit number the grammar always took. The
        /// modifier a stepped line mints has to be the one its per-unit twin mints, or the wheel would sell
        /// two different lines under one sentence.</summary>
        [TestMethod]
        public void AStepIsOnlyAnotherSpellingOfThePerUnitNumber()
        {
            var stepped = new Carrier();
            var perUnit = new Carrier();
            Grant(stepped, PerHundredStrength, 1f);
            Grant(perUnit, PerStrength, 0.01f);

            stepped.Parameters.SetBaseValueForParameter(EntityParameter.Strength, 250f);
            perUnit.Parameters.SetBaseValueForParameter(EntityParameter.Strength, 250f);

            Assert.AreEqual(350f, stepped.Value(EntityParameter.PhysicalDamage), 0.001f,
                "'+1 per 100 Strength' on a carrier holding 250 is +2.5, and the mint did not divide by the step");

            Assert.AreEqual(perUnit.Value(EntityParameter.PhysicalDamage), stepped.Value(EntityParameter.PhysicalDamage), 0.001f,
                "the stepped line and its per-unit twin hand over different numbers");
        }

        /// <summary>A step of one is the step every key without one already means, so writing it changes
        /// nothing about what the character wears.</summary>
        [TestMethod]
        public void AStepOfOneIsTheLineTheGrammarAlwaysRead()
        {
            var written = new Carrier();
            Grant(written, "PhysicalDamage:Increase:Strength:1", 0.01f);

            Assert.AreEqual(110f, written.Value(EntityParameter.PhysicalDamage), 0.001f);
        }

        /// <summary>An id outside the family is built by the factory that names it, not read as fields.
        /// (That a NAMED entry would also beat the family on a shared id is stated in the provider and not
        /// pinned here: the table is a hardcoded static, so no test can put an entry under the prefix.)</summary>
        [TestMethod]
        public void AnIdOutsideTheFamilyIsBuiltByItsOwnFactory()
        {
            ISkill? claws = Build(ClawsId, new Dictionary<string, float> { ["percentFromDamage"] = 0.75f, ["duration"] = 3f });

            Assert.IsInstanceOfType<PoisonedClaws>(claws);
        }

        /// <summary>The family is the ids UNDER the prefix, separator included: without it an id that
        /// merely begins with the same letters would be read as fields and refused for carrying none.</summary>
        [TestMethod]
        public void TheFamilyEndsAtItsSeparator()
        {
            Assert.IsNotNull(Build(StatId, KeystoneSixFields()), "an id inside the family was not read as fields");
            Assert.IsNull(Build("Passive_Skill_Statsomething", KeystoneSixFields()),
                "the family swallowed an id that only starts like it");
        }

        // ---- the convention scan --------------------------------------------------------------------

        /// <summary>Every passive the authored tree names has to be one the registry can build out of the
        /// fields the node carries — the scar of item grants, where a property key that no factory reads
        /// costs the grant silently. A sweep that found nothing says so instead of passing quietly.</summary>
        [TestMethod]
        public void EveryPassiveTheShippedTreeNamesCanBeBuilt()
        {
            PassiveNode[] passives = [.. ShippedTree().Nodes.Where(node => node.IsPassive)];

            foreach (PassiveNode node in passives)
                Assert.IsNotNull(Build(node.PassiveId!, node.Properties),
                    $"node '{node.Id}' names passive '{node.PassiveId}' and nothing can build it from "
                    + $"[{string.Join(", ", node.Properties.Keys)}]");

            if (passives.Length == 0)
                Assert.Inconclusive("the shipped tree names no passives yet — this scan swept 0 nodes");
        }

        /// <summary>The same scan over a document written here, so the sweep above is known to be able to
        /// fail at all while the shipped tree carries nothing for it to read.</summary>
        [TestMethod]
        public void TheSameScanRefusesTheNodesItShould()
        {
            PassiveNode[] passives = [.. Document().Nodes.Where(node => node.IsPassive)];
            string[] refused = [.. passives.Where(node => Build(node.PassiveId!, node.Properties) == null).Select(node => node.Id)];

            Assert.AreEqual(5, passives.Length, "the fixture stopped carrying the shapes the scan is meant to read");
            CollectionAssert.AreEquivalent(new[] { GhostNode, TypoNode }, refused,
                "the scan no longer tells a buildable passive from an unbuildable one");
        }

        // ---- fixtures -------------------------------------------------------------------------------

        private static Dictionary<string, float> KeystoneSixFields() =>
            new() { [PerStrength] = 0.01f, [RecoveryCut] = -0.25f };

        private static ISkill? Build(string passiveId, IReadOnlyDictionary<string, float> properties) =>
            new PassiveSkillProvider().CreateSkill(passiveId, new RecordProperties(passiveId, properties));

        /// <summary>One stat field on a character, through the road a passive actually reaches him by, so
        /// what is measured is the modifier the mint produced rather than the line the record wrote.</summary>
        private static void Grant(Carrier carrier, string field, float value) =>
            carrier.Skills.AddSkill(StatPassiveSkill.Create(StatId,
                new RecordProperties(StatId, new Dictionary<string, float> { [field] = value })));

        private static PassiveTreeDocument ShippedTree()
        {
            List<string> issues = [];
            string path = Path.Combine(SharedData.Catalog(DataCatalog.PassiveTree), PassiveTreeFormat.DefaultFileName);
            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(File.ReadAllText(path), issues);

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            return document;
        }

        /// <summary>A seed with one node of every shape hanging off it, each reachable on its own so it can
        /// be taken and given back without disturbing the others.</summary>
        private static PassiveTreeDocument Document()
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });

            Passive(StatNode, StatId, KeystoneSixFields());
            Passive(OtherStatNode, OtherStatId, new Dictionary<string, float> { [ArmorLine] = 25f });
            Passive(ClawsNode, ClawsId, new Dictionary<string, float> { ["percentFromDamage"] = 0.75f, ["duration"] = 3f });
            Passive(GhostNode, GhostId, new Dictionary<string, float>());
            Passive(TypoNode, TypoId, new Dictionary<string, float> { ["PhysicalDamge:Increase"] = 0.1f });

            var lines = new PassiveNode { Id = LineNode, Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
            lines.Modifiers.Add(new ModifierLine { Parameter = EntityParameter.Armor, ValueType = ModifierValueType.Flat, Value = 5f });
            Add(lines);

            return document;

            void Passive(string id, string passiveId, Dictionary<string, float> properties)
            {
                var node = new PassiveNode { Id = id, Kind = PassiveNodeKind.Keystone, Stance = Stance.Strength, PassiveId = passiveId };
                foreach ((string key, float value) in properties) node.Properties[key] = value;
                Add(node);
            }

            void Add(PassiveNode node)
            {
                document.AddNode(node);
                document.Link(Seed, node.Id);
            }
        }

        private static PassiveTreeService NewTree()
        {
            PassiveTreeDocument document = Document();
            var service = new PassiveTreeService(new TreeStub(document), ConditionCatalogs.Empty());
            service.SetTotalPoints(document.Nodes.Count);
            return service;
        }

        private static ISaveManager ManagerFor(IPassiveTreeService tree)
        {
            var manager = new SaveManager(new LoadScope());
            manager.Register(new PassiveTreeSaveParticipant(tree));
            return manager;
        }

        private static PassiveGrantService CreateService(Carrier carrier, IPassiveTreeService tree)
        {
            var accessor = new PlayerAccessor();
            accessor.Set(carrier.Player);
            return CreateService(accessor, tree);
        }

        private static PassiveGrantService CreateService(IPlayerAccessor accessor, IPassiveTreeService tree) =>
            new(accessor, new PassiveSkillProvider(), tree);

        /// <summary>A player with the three components a granted passive actually reaches: the roster it
        /// registers on, the modifier list its lines land in and the parameters those lines resolve through.
        /// The rest of the fighter is a mock — no walk here asks it anything.</summary>
        private sealed class Carrier
        {
            public Carrier()
            {
                var mock = new Mock<IPlayer>();
                Player = mock.Object;
                Skills = new PassiveSkillsComponent(Player);
                Parameters.Initialize(Modifiers.GetModifiers);
                Modifiers.ModifiersChanged += Parameters.OnParameterModifiersChange;
                Parameters.SetBaseValueForParameter(EntityParameter.Strength, StrengthBase);
                Parameters.SetBaseValueForParameter(EntityParameter.PhysicalDamage, PhysicalDamageBase);
                Parameters.SetBaseValueForParameter(EntityParameter.HealthRecovery, HealthRecoveryBase);

                mock.SetupGet(player => player.PassiveSkills).Returns(Skills);
                mock.SetupGet(player => player.ParameterModifiers).Returns(Modifiers);
                mock.SetupGet(player => player.Parameters).Returns(Parameters);
                mock.SetupGet(player => player.CombatEvents).Returns(new CombatEventBus());
            }

            public IPlayer Player { get; }

            public IPassiveSkillsComponent Skills { get; }

            public IParameterModifiersComponent Modifiers { get; } = new ParameterModifiersComponent();

            public IEntityParametersComponent Parameters { get; } = new EntityParametersComponent();

            public float Value(EntityParameter parameter) => Parameters.GetValueForParameter(parameter);
        }

        /// <summary>Stands in for the data pipeline: the document is already parsed.</summary>
        private sealed class TreeStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}
