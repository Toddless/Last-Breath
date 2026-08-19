namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle.Abilities;
    using Core.Data.GameData;
    using Core.Data.SaveData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.MessageBus;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.Save;
    using Core.Save.Participants;
    using Core.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The ability book is the passive tree's shadow: a node hands an ability over, giving the node
    /// back takes it away, and nothing else grants one. A character who has spent no point knows
    /// nothing at all.
    /// </summary>
    [TestClass]
    public class AbilityUnlockTests
    {
        private const string Seed = "start_1";

        /// <summary>The neutral seed of the wheel: no stance, no ability.</summary>
        private const string CoreSeed = "start_core";

        /// <summary>A seed of a stance — the node that stance's first ability comes from.</summary>
        private const string StanceSeed = "start_dexterity";

        private const string DexNode = "abilityunlock_dex";
        private const string DexNodeTwo = "abilityunlock_dex_two";
        private const string StrNode = "abilityunlock_str";
        private const string HiddenNode = "abilityunlock_hidden";
        private const string GhostNode = "abilityunlock_ghost";
        private const string SocketNode = "sockettier2_int";

        private const string DexAbility = "Ability_Dex";
        private const string DexAbilityTwo = "Ability_Dex_Two";
        private const string StrAbility = "Ability_Str";
        private const string IntAbility = "Ability_Int";
        private const string HiddenAbility = "Ability_Boss_Reaction";

        /// <summary>An id no ability catalog holds: what a node points at after the ability behind it
        /// was renamed or dropped.</summary>
        private const string GhostAbility = "Ability_Never_Written";

        private const string PlayerAnnouncement = "IPlayerAccessor>().Set(this)";

        /// <summary>Both scene copies of the player: a contract that holds in one and not the other
        /// is the way the battle sandbox drifts away from Main.</summary>
        private static readonly string[] s_playerSources =
        [
            Path.Combine("Main", "Player", "Player.cs"),
            Path.Combine("Battle", "Internal", "Player", "Player.cs"),
        ];

        /// <summary>Components a PlayerChanged subscriber dereferences the moment it is called:
        /// the ability book (this service) and the parameter component (the world HUD).</summary>
        private static readonly string[] s_componentsReadOnAnnouncement =
        [
            "AbilityBook = new AbilityBookComponent(this)",
            "Parameters = new EntityParametersComponent()",
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
        public void ACharacterWhoTookNothingKnowsNothing()
        {
            var book = NewBook();
            CreateService(book, NewTree());

            Assert.AreEqual(0, book.AllAbilities.Count, "an ability reached the book without a node paying for it");
        }

        [TestMethod]
        public void TakingTheNodeLearnsItsAbility()
        {
            var book = NewBook();
            IPassiveTreeService tree = NewTree();
            CreateService(book, tree);

            Assert.AreEqual(AllocationResult.Success, tree.Take(DexNode));

            Assert.AreEqual(DexAbility, book.GetAbilities(Stance.Dexterity).Single().Id);
        }

        [TestMethod]
        public void GivingTheNodeBackForgetsItsAbility()
        {
            var book = NewBook();
            IPassiveTreeService tree = NewTree();
            CreateService(book, tree);
            tree.Take(DexNode);
            tree.Take(StrNode);

            Assert.AreEqual(AllocationResult.Success, tree.Refund(DexNode));

            CollectionAssert.AreEquivalent(new[] { StrAbility }, LearnedIds(book),
                "the refunded node's ability outlived the point that paid for it");
            Assert.IsFalse(book.GetSlotLayout(Stance.Dexterity).Any(slot => slot != null),
                "the forgotten ability stayed on the bar");
        }

        /// <summary>
        /// A node someone is only PLANNING to buy hands nothing over. This is the expensive failure the
        /// plan is built to make impossible: a marked unlock node would put its ability in the book and
        /// open the slot that comes with it, the player would seat an augment in that slot, and dropping
        /// the plan would close the slot around property of his.
        /// </summary>
        [TestMethod]
        public void APlannedNodeGrantsNothing_UntilThePlanIsApplied()
        {
            var book = NewBook();
            var board = new AbilitySocketBoard();
            IPassiveTreeService tree = NewTree();
            CreateService(book, tree, board);
            using var draft = new PassiveTreeDraft(tree);

            Assert.AreEqual(AllocationResult.Success, draft.Mark(DexNode));

            Assert.AreEqual(0, book.AllAbilities.Count, "a planned node handed its ability over before it was paid for");
            Assert.IsNull(board.Find(board.At(DexNode)), "a planned node opened its augment slot before it was paid for");
            Assert.AreEqual(0, tree.SpentPoints);

            Assert.AreEqual(AllocationResult.Success, draft.ApplyTakes());

            CollectionAssert.AreEquivalent(new[] { DexAbility }, LearnedIds(book));
            Assert.IsNotNull(board.Find(board.At(DexNode)), "the applied node did not open its slot");
        }

        [TestMethod]
        public void RespecEmptiesTheBook()
        {
            var book = NewBook();
            IPassiveTreeService tree = NewTree();
            CreateService(book, tree);
            tree.Take(DexNode);
            tree.Take(StrNode);

            tree.Respec();

            Assert.AreEqual(0, book.AllAbilities.Count);
        }

        [TestMethod]
        public void ANewPlaythroughStartsWithAnEmptyBook()
        {
            // A second "New game" in one process: the tree service outlives the scene and resets itself.
            var book = NewBook();
            var tree = NewTree();
            CreateService(book, tree);
            tree.Take(DexNode);

            tree.ResetSession();

            Assert.AreEqual(0, book.AllAbilities.Count, "the previous playthrough's ability survived the reset");
        }

        [TestMethod]
        public void AHiddenAbilityNeverEntersTheBook()
        {
            var book = NewBook();
            IPassiveTreeService tree = NewTree();
            CreateService(book, tree);

            Assert.AreEqual(AllocationResult.Success, tree.Take(HiddenNode));

            Assert.AreEqual(0, book.AllAbilities.Count, "a boss reaction reached the player's book through the tree");
        }

        [TestMethod]
        public void ANodePointingAtAnAbilityTheCatalogLostGrantsNothing()
        {
            var book = NewBook();
            IPassiveTreeService tree = NewTree();
            CreateService(book, tree);

            Assert.AreEqual(AllocationResult.Success, tree.Take(GhostNode));

            Assert.AreEqual(0, book.AllAbilities.Count);
        }

        [TestMethod]
        public void OnlyAnUnlockNodeGrants_ASocketOfTheSameAbilityDoesNot()
        {
            // Socket nodes carry an abilityId too: reading the field without checking the class would
            // hand the ability over for opening its tier-2 socket.
            var book = NewBook();
            IPassiveTreeService tree = NewTree();
            CreateService(book, tree);

            Assert.AreEqual(AllocationResult.Success, tree.Take(SocketNode));

            Assert.AreEqual(0, book.AllAbilities.Count);
        }

        [TestMethod]
        public void TheStanceSeedHandsItsAbilityOverTheWayAnUnlockNodeDoes()
        {
            // A stance begins at its seed, and the seed is granted rather than bought: this is the one
            // ability a character has before he has spent anything, and the reason the battle bar is
            // not empty on a new game.
            var book = NewBook();
            IPassiveTreeService tree = SeededTree(DexAbility);

            CreateService(book, tree);

            CollectionAssert.AreEquivalent(new[] { DexAbility }, LearnedIds(book),
                "the stance seed named an ability and the book never received it");
            Assert.AreEqual(0, tree.SpentPoints, "the seed was charged for; a seed comes with the character");
        }

        [TestMethod]
        public void TheStanceSeedBringsTheTierOneSlotOfTheAbilityItOpens()
        {
            // The tier-1 slot belongs to the ability rather than to a node, so whatever opens the
            // ability opens the slot — otherwise the three abilities a character starts with would be
            // the only ones in the game with no augment slot of their own.
            var board = new AbilitySocketBoard();

            CreateService(NewBook(), SeededTree(DexAbility), board);

            AbilitySocket socket = board.SocketsOf(DexAbility).Single();
            Assert.AreEqual(NodeKindRules.UnlockSocketTier, socket.Tier);
            Assert.AreEqual(StanceSeed, socket.SocketId, "the slot was opened by something other than the seed that granted the ability");
        }

        [TestMethod]
        public void TheCoreSeedNamesNoAbilityAndThereforeOpensNothing()
        {
            // The seed at the middle of the wheel belongs to no stance and carries an empty reference.
            // An empty reference is not a name the catalog can be asked about: it must produce neither
            // an ability nor a slot hanging off one nobody owns.
            var book = NewBook();
            var board = new AbilitySocketBoard();

            CreateService(book, SeededTree(DexAbility), board);

            CollectionAssert.AreEquivalent(new[] { DexAbility }, LearnedIds(book), "the empty reference reached the book");
            Assert.IsNull(board.Find(board.At(CoreSeed)), "the empty reference became a slot on an ability nobody names");
            Assert.AreEqual(1, board.Sockets.Count, "the core seed put a second slot on the board");
        }

        [TestMethod]
        public void TheShippedSeedsOpenOneAbilityPerStanceAndTheSlotEachBringsWithIt()
        {
            // The authored tree, not a fixture: the abilities a character actually starts the game with.
            // Read structurally so that renaming one of them is content and not a broken test — what is
            // pinned is that every stance is opened by its seed, that the catalog holds what the seed
            // names, and that the neutral seed opens nothing.
            var book = NewBook();
            var board = new AbilitySocketBoard();
            var accessor = new PlayerAccessor();
            accessor.Set(NewPlayer(book));
            PassiveTreeService tree = ShippedTree();

            _ = new AbilityUnlockService(accessor, new ShippedCatalogStand(ShippedAbilityData.Abilities()), tree, board);

            PassiveNode[] seeds = [.. tree.Tree.Nodes.Where(node => node.Kind == PassiveNodeKind.Start)];
            PassiveNode[] opening = [.. seeds.Where(seed => seed.AbilityId.Length > 0)];

            foreach (Stance stance in Enum.GetValues<Stance>())
                Assert.AreEqual(1, opening.Count(seed => seed.Stance == stance),
                    $"{stance} is not opened by exactly one seed carrying an ability");

            CollectionAssert.AreEquivalent(opening.Select(seed => seed.AbilityId).ToArray(), LearnedIds(book),
                "the abilities the seeds name are not the abilities a new character holds");

            foreach (PassiveNode seed in opening)
            {
                AbilitySocket? socket = board.Find(board.At(seed.Id));
                Assert.IsNotNull(socket, $"{seed.Id} opened '{seed.AbilityId}' without the slot that comes with it");
                Assert.AreEqual(NodeKindRules.UnlockSocketTier, socket.Tier);
                Assert.AreEqual(seed.AbilityId, socket.AbilityId);
            }

            foreach (PassiveNode seed in seeds.Except(opening))
                Assert.IsNull(board.Find(board.At(seed.Id)), $"{seed.Id} names no ability and still opened a slot");
        }

        [TestMethod]
        public void MasteryLevelDoesNotChangeTheLearnedSet()
        {
            var book = NewBook();
            IPassiveTreeService tree = NewTree();
            var service = CreateService(book, tree);
            tree.Take(DexNode);
            var mastery = new MartialArtMastery(new Mock<IGameMessageBus>().Object);
            string[] beforeLevelUp = LearnedIds(book);

            mastery.AddExperience(100_000);
            service.Reconcile(notify: false);

            Assert.IsTrue(mastery.CurrentLevel > 0, "the experience must have produced level ups");
            CollectionAssert.AreEquivalent(beforeLevelUp, LearnedIds(book));
        }

        [TestMethod]
        public void RepeatedReconcileDoesNotRelearnAbilities()
        {
            var book = NewBook();
            IPassiveTreeService tree = NewTree();
            var service = CreateService(book, tree);
            tree.Take(DexNode);

            service.Reconcile(notify: false);
            service.Reconcile(notify: false);

            Assert.AreEqual(1, book.AllAbilities.Count);
        }

        /// <summary>
        /// The service outlives the scene, the player does not: leaving to the menu and starting a
        /// new game builds a second player in the same process. The constructor cannot catch that one
        /// up — only the accessor's signal can, so a fresh book has to come back holding the allocation.
        /// </summary>
        [TestMethod]
        public void SecondPlayerInTheSameProcessGetsHisBookFilled()
        {
            var accessor = new PlayerAccessor();
            accessor.Set(NewPlayer(NewBook()));
            IPassiveTreeService tree = NewTree();
            CreateService(accessor, tree); // the first scene: caught up by the constructor
            tree.Take(DexNode);

            var secondBook = NewBook();
            accessor.Set(NewPlayer(secondBook));

            CollectionAssert.AreEquivalent(new[] { DexAbility }, LearnedIds(secondBook));
        }

        /// <summary>
        /// The signal above is only worth having if the fighter is whole when it fires. The player
        /// is a scene node no headless test can build, so the ordering is pinned where it lives:
        /// the accessor hand-off must be the last thing _Ready does, after every component a
        /// subscriber dereferences on the spot.
        /// </summary>
        [TestMethod]
        public void PlayerAnnouncesItselfOnlyAfterItsComponentsExist()
        {
            foreach (string source in s_playerSources)
            {
                string path = Path.Combine(SrcRoot, source);
                Assert.IsTrue(File.Exists(path), $"player source not found: {path}");

                string[] lines = File.ReadAllLines(path);
                int announcement = SingleLineWith(lines, PlayerAnnouncement, source);
                foreach (string component in s_componentsReadOnAnnouncement)
                {
                    int assignment = SingleLineWith(lines, component, source);
                    Assert.IsTrue(
                        assignment < announcement,
                        $"{source}: the accessor is handed the player on line {announcement + 1}, before "
                        + $"'{component}' on line {assignment + 1} — PlayerChanged subscribers get a null field");
                }
            }
        }

        [TestMethod]
        public void AProjectWithoutTheTreeServiceGrantsNothing()
        {
            // The battle sandbox composes the module without the tree: the service must resolve and
            // stay quiet rather than refuse to be built or hand the catalog out for free.
            var services = new ServiceCollection();
            var accessor = new PlayerAccessor();
            var book = NewBook();
            accessor.Set(NewPlayer(book));
            services.AddSingleton<IPlayerAccessor>(accessor);
            services.AddSingleton<IAbilityProvider>(new FakeAbilityProvider());
            services.AddSingleton<IAbilityUnlockService, AbilityUnlockService>();

            var service = services.BuildServiceProvider().GetRequiredService<IAbilityUnlockService>();
            service.Reconcile(notify: false);

            Assert.AreEqual(0, book.AllAbilities.Count);
        }

        [TestMethod]
        public void TheRestoredBookIsTheRestoredAllocation()
        {
            var source = Save(taken: [DexNode, StrNode], equip: DexAbility, slot: 4);

            var targetBook = NewBook();
            IPassiveTreeService targetTree = NewTree();
            CreateService(targetBook, targetTree);
            ManagerFor(targetBook, targetTree).Restore(source);

            CollectionAssert.AreEquivalent(new[] { DexAbility, StrAbility }, LearnedIds(targetBook));
            Assert.AreEqual(DexAbility, targetBook.GetSlotLayout(Stance.Dexterity)[4]?.Id,
                "the slot the player chose did not survive the load");
        }

        [TestMethod]
        public void TheFileCannotKeepAnAbilityWhoseNodeStoppedGrantingIt()
        {
            // Catalog drift: the file was written when the node handed out one dexterity ability, and
            // the tree now points that same node at another. The node is what the character owns, so
            // the node decides — the file's memory of the old payload is stale, and a section able to
            // enforce it would leave the character holding an ability he has no node for and missing
            // the one he does.
            SaveFile file = Save(taken: [DexNode], equip: DexAbility, slot: 0);
            var targetBook = NewBook();
            IPassiveTreeService targetTree = NewTree(node => node.AbilityId = node.Id == DexNode ? DexAbilityTwo : node.AbilityId);
            CreateService(targetBook, targetTree);

            ManagerFor(targetBook, targetTree).Restore(file);

            CollectionAssert.AreEquivalent(new[] { DexAbilityTwo }, LearnedIds(targetBook));
        }

        [TestMethod]
        public void ALearnedListInTheSectionLearnsNothingOnItsOwn()
        {
            // The learned list is what saves written before this build carried, and it is unread: an
            // ability is the allocation's to give, and a file cannot put one into the book of a
            // character who owns no node granting it. Written at the version the build reads, so the
            // claim is the list being ignored rather than the file being refused for its age.
            var book = NewBook();
            var accessor = new PlayerAccessor();
            accessor.Set(NewPlayer(book));
            var section = JToken.FromObject(new AbilityBookSaveData
            {
                Stances = { [Stance.Dexterity.ToString()] = new StanceBookSaveData { Learned = [DexAbility] } }
            });
            var participant = new AbilityBookSaveParticipant(accessor);

            participant.Restore(section, participant.Version);

            Assert.AreEqual(0, book.AllAbilities.Count);
        }

        [TestMethod]
        public void TheTreeRestoresBeforeTheBook()
        {
            // The book's slots resolve against abilities the allocation has already handed over.
            Assert.IsTrue(RestoreOrder.PassiveTree < RestoreOrder.Abilities);
        }

        /// <summary>A file holding an allocation and the slot the player put one of its abilities in.</summary>
        private static SaveFile Save(string[] taken, string equip, int slot)
        {
            var book = NewBook();
            IPassiveTreeService tree = NewTree();
            CreateService(book, tree);
            foreach (string nodeId in taken) tree.Take(nodeId);

            var equipped = book.AllAbilities.Single(ability => ability.Id == equip);
            book.Equip(Stance.Dexterity, equipped.InstanceId, slot);

            return ManagerFor(book, tree).Capture(new SaveMetadata());
        }

        private static ISaveManager ManagerFor(IAbilityBookComponent book, IPassiveTreeService tree)
        {
            var accessor = new PlayerAccessor();
            accessor.Set(NewPlayer(book));

            var manager = new SaveManager(new LoadScope());
            manager.Register(new PassiveTreeSaveParticipant(tree));
            manager.Register(new AbilityBookSaveParticipant(accessor));
            return manager;
        }

        private static int SingleLineWith(string[] lines, string fragment, string source)
        {
            int[] matches = lines
                .Select((line, index) => (line, index))
                .Where(entry => entry.line.Contains(fragment, StringComparison.Ordinal))
                .Select(entry => entry.index)
                .ToArray();
            Assert.AreEqual(1, matches.Length, $"{source}: expected exactly one line with '{fragment}'");
            return matches[0];
        }

        private static string[] LearnedIds(IAbilityBookComponent book) =>
            book.AllAbilities.Select(ability => ability.Id).ToArray();

        private static AbilityBookComponent NewBook() => new(new Mock<IFightable>().Object);

        private static IPlayer NewPlayer(IAbilityBookComponent book)
        {
            var player = new Mock<IPlayer>();
            player.SetupGet(p => p.AbilityBook).Returns(book);
            return player.Object;
        }

        /// <summary>A seed with one node of every shape that carries an ability hanging off it, each
        /// reachable on its own so it can be taken and given back without disturbing the others.</summary>
        private static PassiveTreeService NewTree(Action<PassiveNode>? edit = null)
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });

            Add(DexNode, PassiveNodeKind.AbilityUnlock, DexAbility);
            Add(DexNodeTwo, PassiveNodeKind.AbilityUnlock, DexAbilityTwo);
            Add(StrNode, PassiveNodeKind.AbilityUnlock, StrAbility);
            Add(HiddenNode, PassiveNodeKind.AbilityUnlock, HiddenAbility);
            Add(GhostNode, PassiveNodeKind.AbilityUnlock, GhostAbility);
            Add(SocketNode, PassiveNodeKind.SocketTier2, IntAbility);

            var service = new PassiveTreeService(new TreeProviderStub(document), ConditionCatalogs.Empty());
            service.SetTotalPoints(document.Nodes.Count);
            return service;

            void Add(string id, PassiveNodeKind kind, string abilityId)
            {
                var node = new PassiveNode { Id = id, Kind = kind, AbilityId = abilityId };
                edit?.Invoke(node);
                document.AddNode(node);
                document.Link(Seed, id);
            }
        }

        /// <summary>
        /// The shape the shipped tree has around its seeds: the neutral one at the core of the wheel,
        /// which belongs to no stance and names no ability, and a stance seed naming the ability its
        /// stance begins with. Neither is bought — a seed is granted with the character.
        /// </summary>
        private static PassiveTreeService SeededTree(string seedAbility)
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = CoreSeed, Kind = PassiveNodeKind.Start });
            document.AddNode(new PassiveNode
            {
                Id = StanceSeed,
                Kind = PassiveNodeKind.Start,
                Stance = Stance.Dexterity,
                AbilityId = seedAbility
            });
            document.Link(CoreSeed, StanceSeed);

            var service = new PassiveTreeService(new TreeProviderStub(document), ConditionCatalogs.Empty());
            service.SetTotalPoints(document.Nodes.Count);
            return service;
        }

        /// <summary>The tree the owner authors, read through the same serializer the game runs.</summary>
        private static PassiveTreeService ShippedTree()
        {
            List<string> issues = [];
            string path = Path.Combine(SharedData.Catalog(DataCatalog.PassiveTree), PassiveTreeFormat.DefaultFileName);
            PassiveTreeDocument document = PassiveTreeSerializer.Deserialize(File.ReadAllText(path), issues);

            Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
            var service = new PassiveTreeService(new TreeProviderStub(document), ConditionCatalogs.Empty());
            service.SetTotalPoints(document.Budget);
            return service;
        }

        private static AbilityUnlockService CreateService(
            IAbilityBookComponent book,
            IPassiveTreeService tree,
            IAbilitySocketBoard? sockets = null)
        {
            var accessor = new PlayerAccessor();
            accessor.Set(NewPlayer(book));
            return CreateService(accessor, tree, sockets);
        }

        /// <summary>No mastery argument on purpose: the service must not be able to see a level.</summary>
        private static AbilityUnlockService CreateService(
            IPlayerAccessor accessor,
            IPassiveTreeService tree,
            IAbilitySocketBoard? sockets = null) =>
            new(accessor, new FakeAbilityProvider(), tree, sockets);

        /// <summary>An ability instance with nothing but an identity: the book only ever asks it what it
        /// is, and building the real thing is the ability provider's business and not this file's.</summary>
        private static IAbility StubAbility(string abilityId)
        {
            string instanceId = Guid.NewGuid().ToString();
            var ability = new Mock<IAbility>();
            ability.SetupGet(a => a.Id).Returns(abilityId);
            ability.SetupGet(a => a.InstanceId).Returns(instanceId);
            ability.SetupGet(a => a.InstalledUpgrades).Returns(new Dictionary<string, IAugment>());
            ability.Setup(a => a.IsSame(It.IsAny<string>())).Returns((string other) => other == instanceId);
            return ability.Object;
        }

        /// <summary>The shipped ability catalog with instantiation stubbed out. Which ids the tree hands
        /// over is the question — whether the catalog holds them, whether one of them is hidden — while
        /// building a real ability wants the battle module wired into a scene.</summary>
        private sealed class ShippedCatalogStand(IAbilityProvider shipped) : IAbilityProvider
        {
            public IReadOnlyCollection<string> KnownAbilityIds => shipped.KnownAbilityIds;

            public IAbility CreateAbility(string abilityId) => StubAbility(abilityId);

            public IAugment? CreateUpgrade(AugmentInstance augment) => null; // no walk here asks what an augment does

            public Stance GetAbilityStance(string abilityId) => shipped.GetAbilityStance(abilityId);

            public bool IsHidden(string abilityId) => shipped.IsHidden(abilityId);
        }

        /// <summary>Catalog stub: three player abilities across the stances plus one hidden boss reaction.</summary>
        private sealed class FakeAbilityProvider : IAbilityProvider
        {
            private static readonly Dictionary<string, Stance> s_stances = new()
            {
                [DexAbility] = Stance.Dexterity,
                [DexAbilityTwo] = Stance.Dexterity,
                [StrAbility] = Stance.Strength,
                [IntAbility] = Stance.Intelligence,
                [HiddenAbility] = Stance.Strength,
            };

            public IReadOnlyCollection<string> KnownAbilityIds => s_stances.Keys;

            public IAbility CreateAbility(string abilityId) => StubAbility(abilityId);

            public IAugment? CreateUpgrade(AugmentInstance augment) => null; // no walk here asks what an augment does

            public Stance GetAbilityStance(string abilityId) => s_stances[abilityId];

            public bool IsHidden(string abilityId) => abilityId == HiddenAbility;
        }

        /// <summary>Stands in for the data pipeline: the document is already parsed.</summary>
        private sealed class TreeProviderStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}
