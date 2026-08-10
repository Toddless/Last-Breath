namespace LastBreathTest.BattleSystemTests
{
    using System.Reflection;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Abilities;
    using Battle.Source.Effects;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Moq;
    using static AugmentCopies;

    /// <summary>
    /// An installed applier teaches its ability a genus: the record declares GRANTED tags — separate
    /// from the tags it fits by — and the fitting rule reads the ability's own tags together with what
    /// its installed augments grant. This is the link transitive combos stand on: a poison amplifier
    /// seats beside the applier that taught a poisonless ability poison. The grant is a promise of
    /// fitting and never of work, so extraction of the donor throws nobody out — the amplifier stays
    /// seated and silent, exactly like an augment in the slot of a refunded node.
    /// </summary>
    [TestClass]
    public class AugmentGrantedTagsTests
    {
        private const string Ability = "Ability_Attacks_Probe";

        private const string SlotOne = "socket_probe_one";
        private const string SlotTwo = "socket_probe_two";
        private const string SlotThree = "socket_probe_three";

        private const string Donor = "Augment_Applies_Poison";
        private const string Amplifier = "Augment_Amplifies_Poison";
        private const string TaggedAmplifier = "Augment_Rides_The_Attack_Tag";

        /// <summary>The shipped donor of the flagship combo — "a landing impact poisons what it
        /// touched" — and the shipped amplifier that reaches the poison it lays.</summary>
        private const string ShippedDonor = "Augment_Poison_On_Hit";
        private const string ShippedAmplifier = "Augment_Poison_Duration";
        private const string ShippedAbility = "Ability_Series_Of_Attacks";
        private const string DurationProperty = "poisonDuration";

        /// <summary>
        /// Every shipped record that grants, and what it grants — the appliers, by the genus of what
        /// they lay. Held literally because the discipline is one-way: an amplifier that started
        /// granting would bootstrap its family without any applier installed, and that failure seats
        /// augments silently instead of failing a build.
        /// </summary>
        private static readonly (string Id, string[] Grants)[] s_shippedGrants =
        [
            ("Augment_Poison_On_Hit", [AbilityTags.Poison, AbilityTags.Debuff]),

            ("Ability_Arm_Augment_Stage3_Burning", [AbilityTags.Burn, AbilityTags.Debuff]),
            ("Ability_Bf_Augment_Burning_Fury", [AbilityTags.Burn, AbilityTags.Debuff]),

            ("Ability_SoA_Augment_Apply_Buff_Critical_Chance", [AbilityTags.Buff]),
            ("Ability_SoA_Augment_Apply_Buff_Critical_Damage", [AbilityTags.Buff]),
            ("Augment_Lucky_Crit", [AbilityTags.Buff]),
            ("Augment_Additional_Crit_Multiplier", [AbilityTags.Buff]),
            ("Augment_Apply_Enhanced_Defence", [AbilityTags.Buff]),
            ("Augment_Leach_On_Crit", [AbilityTags.Buff]),
            ("Augment_Immortality", [AbilityTags.Buff]),
            ("Augment_Incoming_Reduction", [AbilityTags.Buff]),
            ("Ability_Dst_Augment_Both_Hits_Buff", [AbilityTags.Buff]),
            ("Ability_Ar_Augment_Incoming_Reduction", [AbilityTags.Buff]),
            ("Ability_Ar_Augment_Damage_Buff", [AbilityTags.Buff]),
            ("Ability_Porc_Augment_Armor_Buff", [AbilityTags.Buff]),
            ("Ability_Porc_Augment_Echo", [AbilityTags.Buff]),
            ("Ability_Porc_Augment_Incoming_Reduction", [AbilityTags.Buff]),
            ("Ability_Porc_Augment_Crit_Mitigation", [AbilityTags.Buff]),
            ("Ability_Ia_Augment_Crit_Mitigation", [AbilityTags.Buff]),

            ("Augment_Free_Cast", [AbilityTags.Buff]),
            ("Augment_Next_Cast_Pure", [AbilityTags.Buff]),

            ("Augment_Mana_Flow", [AbilityTags.Buff, AbilityTags.Recovery]),
            ("Ability_Ar_Augment_Turn_End_Heal", [AbilityTags.Buff, AbilityTags.Recovery]),
            ("Ability_Ds_Augment_Mana_Regen", [AbilityTags.Buff, AbilityTags.Recovery]),

            ("Augment_Restore_On_Hit", [AbilityTags.Recovery]),
            ("Augment_Heal_On_Hit", [AbilityTags.Recovery]),

            ("Ability_Ia_Augment_Stun_Attackers", [AbilityTags.Stun, AbilityTags.Control]),

            ("Augment_Clumsiness", [AbilityTags.Debuff]),
            ("Augment_Apply_Blind", [AbilityTags.Debuff]),
            ("Augment_Apply_Weakness", [AbilityTags.Debuff]),
            ("Augment_Apply_Seal_Of_Oblivion", [AbilityTags.Debuff]),
            ("Ability_Pc_Augment_Attacks_Reduce_Incoming_Heal", [AbilityTags.Debuff]),
            ("Ability_Pc_Augment_Attacks_Reduce_Armor", [AbilityTags.Debuff]),
            ("Ability_Hb_Augment_Armor_Debuff", [AbilityTags.Debuff]),
            ("Ability_Is_Augment_Apply_Fragility", [AbilityTags.Debuff]),
            ("Ability_Df_Augment_Enemy_Cooldown", [AbilityTags.Debuff]),
            // Declared by a record and no code at all — the first applier the behaviour registry builds.
            ("Augment_Clumsy_Blows", [AbilityTags.Debuff]),
        ];

        /// <summary>
        /// The consequence ledger of granting: every record a granted tag carries onto abilities its
        /// own tags never reach, and on which of them it then moves a number (probed with the genus
        /// donors installed) versus sits inert until the ability declares the key. Kept literally so
        /// the second reachable state of the board the grants created stays measured — the other
        /// guard tables all describe the bare ability.
        /// </summary>
        private static readonly (string Record, string[] WorksOn, string[] InertOn)[] s_grantReach =
        [
            ("Augment_Poison_Duration",
                ["Ability_Series_Of_Attacks"],
                []),
            ("Augment_Buff_Duration",
                [],
                ["Ability_Armageddon", "Ability_Discharge", "Ability_Head_Butt", "Ability_Ice_Block", "Ability_Ice_Shards", "Ability_Increasing_Pressure", "Ability_Overload", "Ability_Poison_Explosion", "Ability_Series_Of_Attacks"]),
            ("Augment_Increased_Buff_Duration",
                [],
                ["Ability_Armageddon", "Ability_Discharge", "Ability_Head_Butt", "Ability_Ice_Block", "Ability_Ice_Shards", "Ability_Increasing_Pressure", "Ability_Overload", "Ability_Poison_Explosion", "Ability_Series_Of_Attacks"]),
            ("Augment_Buff_Effectiveness",
                ["Ability_Deep_Freeze", "Ability_Double_Strike", "Ability_Ice_Block", "Ability_Jar_Of_Poison"],
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Discharge", "Ability_Head_Butt", "Ability_Ice_Shards", "Ability_Increasing_Pressure", "Ability_Overload", "Ability_Poison_Explosion", "Ability_Series_Of_Attacks"]),
            // The porcupine reads effectiveness now, so the recovery record stopped being inert on it.
            ("Augment_Recovery_Effectiveness",
                ["Ability_Critical_Calculation", "Ability_Deep_Freeze", "Ability_Double_Strike", "Ability_Ice_Aegis", "Ability_Ice_Block", "Ability_Jar_Of_Poison", "Ability_Poison_Coating", "Ability_Porcupine"],
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Discharge", "Ability_Head_Butt", "Ability_Ice_Shards", "Ability_Increasing_Pressure", "Ability_Overload", "Ability_Poison_Explosion", "Ability_Sacrifice", "Ability_Series_Of_Attacks", "Ability_Static_Armor"]),
            ("Augment_Debuff_Effectiveness",
                ["Ability_Jar_Of_Poison", "Ability_Poison_Coating"],
                // Increasing Pressure joined through the codeless applier: its attacks now grant "debuff".
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Head_Butt", "Ability_Ice_Shards", "Ability_Increasing_Pressure", "Ability_Poison_Explosion", "Ability_Series_Of_Attacks"]),
        ];

        /// <summary>The shared key each grant-opened parameter record stands on, for the ledger's
        /// probe. Values are held to <see cref="AbilityParameter"/> by the private-key guard: a
        /// private key written here would be the dead-socket class hiding inside its own audit.</summary>
        private static readonly Dictionary<string, string> s_openedKeys = new(StringComparer.Ordinal)
        {
            ["Augment_Poison_Duration"] = AbilityParameter.PoisonDuration,
            ["Augment_Buff_Duration"] = AbilityParameter.Duration,
            ["Augment_Increased_Buff_Duration"] = AbilityParameter.Duration,
            ["Augment_Buff_Effectiveness"] = AbilityParameter.Effectiveness,
            ["Augment_Recovery_Effectiveness"] = AbilityParameter.Effectiveness,
            ["Augment_Debuff_Effectiveness"] = AbilityParameter.Effectiveness,
            ["Augment_Extend_Stun_Add_Cost"] = AbilityParameter.StunDuration,
        };

        /// <summary>Grant-openable records whose whole behaviour rides in an impact rider and stands
        /// on no ability key — working wherever they land is their construction, not a probe's verdict.</summary>
        private static readonly HashSet<string> s_selfContained = new(StringComparer.Ordinal)
        {
            "Augment_Extend_Poison",
        };

        [TestMethod]
        public void TheGrantedTagSeatsAnAmplifierTheAbilitysOwnTagsRefuse()
        {
            // The link itself, and the state before it: the amplifier shares no tag with the ability
            // and stays out. The donor goes in, grants the genus of what it lays, and the same
            // amplifier seats — without the ability's own tags having moved at all.
            AbilitySocketBoard board = Board();

            Assert.IsFalse(board.Install(board.At(SlotTwo), Copy(Amplifier)),
                "the amplifier went onto a bare ability — the combo below proves nothing");
            Assert.AreEqual(AugmentFitResult.NoSharedTag, board.Judge(board.At(SlotTwo), Copy(Amplifier)));

            Assert.IsTrue(board.Install(board.At(SlotOne), Copy(Donor)), "the donor never reached its slot");

            Assert.AreEqual(AugmentFitResult.Fits, board.Judge(board.At(SlotTwo), Copy(Amplifier)));
            Assert.IsTrue(board.Install(board.At(SlotTwo), Copy(Amplifier)),
                "the donor is seated and the amplifier still cannot follow it");
        }

        [TestMethod]
        public void ExtractingTheDonorLeavesTheAmplifierSeatedAndTakesTheGrantWithIt()
        {
            // The tag promised fitting, not work: pulling the donor out throws nobody's property away.
            // The amplifier stays where it was seated — and the grant itself is gone, so the same
            // record is refused a fresh slot until another donor arrives.
            AbilitySocketBoard board = Board();
            Assert.IsTrue(board.Install(board.At(SlotOne), Copy(Donor)), "the donor never reached its slot");
            Assert.IsTrue(board.Install(board.At(SlotTwo), Copy(Amplifier)), "the amplifier never reached its slot");

            Assert.AreEqual(Donor, board.Extract(board.At(SlotOne))?.AugmentId, "the donor never came back out");

            Assert.AreEqual(Amplifier, board.Find(board.At(SlotTwo))?.Augment?.AugmentId,
                "extraction of the donor threw the seated amplifier out");
            Assert.IsFalse(board.Install(board.At(SlotThree), Copy(Amplifier)),
                "the donor is gone and its grant went on seating augments");
        }

        [TestMethod]
        public void AnAugmentGrantsOnlyWhatItsRecordDeclares()
        {
            // The bootstrap boundary. This amplifier CARRIES the poison tag — that is how it fits
            // poison abilities — but declares no grant, so wearing it teaches the ability nothing:
            // two amplifiers without an applier stay two refusals, however many tags they ride in on.
            AbilitySocketBoard board = Board();
            Assert.IsTrue(board.Install(board.At(SlotOne), Copy(TaggedAmplifier)),
                "the attack-tagged amplifier never reached the ability's own tag");

            Assert.IsFalse(board.Install(board.At(SlotTwo), Copy(Amplifier)),
                "a worn tag seated the next amplifier — amplifiers are bootstrapping each other without an applier");
        }

        [TestMethod]
        public void AClosedSlotsDonorGrantsNothing()
        {
            // The precedent the grant follows: an augment in a closed slot does nothing — it is not
            // worn, so it neither works nor grants. Same reading as exclusion groups.
            AbilitySocketBoard board = Board();
            Assert.IsTrue(board.Install(board.At(SlotOne), Copy(Donor)), "the donor never reached its slot");

            board.Sync([Slot(SlotTwo), Slot(SlotThree)]);

            Assert.IsFalse(board.Find(board.At(SlotOne))?.IsOpen, "the donor's slot never closed — the refusal below proves nothing");
            Assert.IsFalse(board.Install(board.At(SlotTwo), Copy(Amplifier)),
                "a donor whose node is gone went on granting its tag");
        }

        [TestMethod]
        public void ARecordGrantsNothingUntilItSaysSo()
        {
            Assert.AreEqual(0, new AbilityUpgradeData().GrantsTags.Length, "an augment grants tags it never declared");
        }

        [TestMethod]
        public void TheShippedGrantsAreTheAppliersAndSpeakTheVocabulary()
        {
            // Both directions against the literal roster: every granting record is an applier written
            // down here, and every applier written down still grants what it lays. A tag outside the
            // vocabulary would promise a genus no ability and no amplifier can ever speak.
            AbilityAugmentCatalog catalog = ShippedAbilityData.Augments();
            var declared = s_shippedGrants.ToDictionary(entry => entry.Id, entry => entry.Grants, StringComparer.Ordinal);

            foreach (AbilityUpgradeData record in catalog.All)
            {
                if (record.GrantsTags.Length == 0)
                {
                    Assert.IsFalse(declared.ContainsKey(record.Id), $"'{record.Id}' is an applier of the roster and grants nothing any more");
                    continue;
                }

                Assert.IsTrue(declared.TryGetValue(record.Id, out string[]? grants),
                    $"'{record.Id}' grants tags and the roster does not name it — an amplifier quietly turned donor, or a new applier goes unaudited");
                CollectionAssert.AreEquivalent(grants, record.GrantsTags, $"'{record.Id}' grants something other than the genus it lays");

                foreach (string tag in record.GrantsTags)
                    Assert.IsTrue(AbilityTags.All.Contains(tag), $"'{record.Id}' grants unknown tag '{tag}'");
            }
        }

        [TestMethod]
        public void TheGrantsOpenExactlyTheSeatingsTheLedgerNames()
        {
            // The consequence ledger. The binding, markup and reach tables all describe the BARE
            // ability; granting created a second reachable state of the board those tables never see,
            // so what it opens is written out here — per record, split by whether the seated record
            // then moves a number there (probed with the genus donors installed) or sits inert until
            // the ability declares the key. Inert seatings are legal — the tag promises fitting, not
            // work — but they are counted: a grant quietly widening a family is a red diff here, not
            // a discovery in a player's build. First order only: donors seated on the ability's own
            // tags; a donor seated through another donor's grant is not counted.
            (AbilityProvider book, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();

            string actual = string.Join("\n", Render(ComputeGrantReach(book, catalog)));
            string written = string.Join("\n", Render(s_grantReach));

            Assert.AreEqual(written, actual,
                "granting no longer opens the seatings the ledger names — rewrite s_grantReach to the actual reach printed above");
        }

        [TestMethod]
        public void NoGrantOpensARecordStandingOnAPrivateKey()
        {
            // The dead-socket class: a record standing on one ability's private key, carried by a
            // granted tag onto any other ability, seats, costs, reports TrackNotFound and moves
            // nothing — property the player cannot use. Every grant-opened record must stand on a
            // shared key of the book (so the ledger can probe it) or carry its whole work in a rider;
            // anything else is refused here, which is what keeps the class from coming back.
            HashSet<string> sharedKeys = SharedKeys();
            foreach ((string record, string key) in s_openedKeys)
                Assert.IsTrue(sharedKeys.Contains(key),
                    $"the ledger probes '{record}' on '{key}', which is not a shared key of AbilityParameter — the probe itself hides the class");

            (AbilityProvider book, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            List<string> strays = [];

            foreach ((AbilityUpgradeData record, string ability, _) in ComputeGrantReach(book, catalog))
                if (!s_selfContained.Contains(record.Id) && !s_openedKeys.ContainsKey(record.Id))
                    strays.Add($"'{record.Id}' (carried onto '{ability}')");

            Assert.AreEqual(0, strays.Count,
                "a granted tag carries records the ledger cannot probe — a private-key row bound to no ability, or an unregistered rider:\n  "
                + string.Join("\n  ", strays.Distinct()));
        }

        [TestMethod]
        public async Task TheShippedDonorCompletesTheComboAndItsLossSilencesTheAmplifier()
        {
            // The whole chain on shipped data. "Poison lasts a turn longer" shares no tag with Series
            // of Attacks and used to stay out; the poison applier seats, grants its genus, and the
            // amplifier follows — then every stack the rider lays comes out one turn longer, neither
            // record naming the other. And the mirror: the donor leaves, the amplifier stays worn,
            // and the swing lays nothing — it decorates a number nobody registers any more.
            (AbilityProvider book, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            AbilityUpgradeData donor = Record(catalog, ShippedDonor);
            AbilityUpgradeData amplifier = Record(catalog, ShippedAmplifier);
            Assert.IsTrue(donor.GrantsTags.Contains(AbilityTags.Poison, StringComparer.OrdinalIgnoreCase),
                $"'{ShippedDonor}' no longer grants the genus it lays");

            var board = new AbilitySocketBoard(catalog);
            board.Sync([new AbilitySocketPlacement(SlotOne, ShippedAbility, donor.Tier), new AbilitySocketPlacement(SlotTwo, ShippedAbility, amplifier.Tier)]);
            Assert.IsFalse(board.Install(board.At(SlotTwo), Copy(ShippedAmplifier)),
                $"'{ShippedAmplifier}' seats on a bare '{ShippedAbility}' — the granted tag below proves nothing");
            Assert.IsTrue(board.Install(board.At(SlotOne), Copy(ShippedDonor)), "the donor never reached its slot");
            Assert.IsTrue(board.Install(board.At(SlotTwo), Copy(ShippedAmplifier)),
                "the donor is seated and the amplifier still cannot follow it");

            IAbilityUpgrade? donorUpgrade = book.CreateUpgrade(donor);
            IAbilityUpgrade? amplifierUpgrade = book.CreateUpgrade(amplifier);
            Assert.IsNotNull(donorUpgrade, $"the registry builds nothing for '{ShippedDonor}'");
            Assert.IsNotNull(amplifierUpgrade, $"the registry builds nothing for '{ShippedAmplifier}'");
            var ability = (Ability)book.CreateAbility(ShippedAbility);
            donorUpgrade.Apply(ability);
            amplifierUpgrade.Apply(ability);

            var caster = new Fighter();
            var victim = new Fighter();
            await ability.ApplyImpactRiders(Swing(ability, caster, victim));

            int declared = (int)donor.UpgradeProperties[DurationProperty] + (int)amplifier.UpgradeProperties[DurationProperty];
            IEffect? poison = victim.Object.Effects.Effects.FirstOrDefault();
            Assert.IsNotNull(poison, "the seated donor laid no poison at all");
            Assert.AreEqual(declared, poison.Duration,
                "the amplifier is seated beside the donor and the stack came out undecorated");

            donorUpgrade.Remove(ability);
            var untouched = new Fighter();
            await ability.ApplyImpactRiders(Swing(ability, caster, untouched));

            Assert.AreEqual(0, untouched.Object.Effects.Effects.Count,
                "the donor is gone and the ability still lays its poison — the amplifier is not silent, something re-applies");
        }

        /// <summary>One swing of the ability, as its own delivery would hand it to the riders.</summary>
        private static Core.Data.AbilityImpact Swing(IAbility source, Fighter caster, Fighter victim) =>
            new(caster.Object, victim.Object, Mock.Of<IBattleField>(), Damage: 100f)
            {
                Source = source,
                Kind = Core.Data.ImpactKind.Attack
            };

        private static AbilityUpgradeData Record(AbilityAugmentCatalog catalog, string id)
        {
            AbilityUpgradeData? record = catalog.Find(id);
            Assert.IsNotNull(record, $"the shipped data declares no '{id}'");
            return record;
        }

        /// <summary>Every seating that exists only through granting, first order: for each visible
        /// ability, the donors its OWN tags seat and the tags they grant, then every tag-judged record
        /// the bare ability refuses and the granted one takes. The works verdict is probed with the
        /// record's genus donors applied — the state a player's board is actually in.</summary>
        private static List<(AbilityUpgradeData Record, string Ability, bool Works)> ComputeGrantReach(
            AbilityProvider book, AbilityAugmentCatalog catalog)
        {
            List<(AbilityUpgradeData, string, bool)> reach = [];

            foreach (string abilityId in book.KnownAbilityIds.Where(id => !book.IsHidden(id)))
            {
                IReadOnlyCollection<string> tags = catalog.TagsOf(abilityId);
                List<AbilityUpgradeData> donors = [.. catalog.All
                    .Where(donor => donor.GrantsTags.Length > 0 && Fits(abilityId, tags, donor, granted: null))];
                string[] granted = [.. donors.SelectMany(donor => donor.GrantsTags).Distinct(StringComparer.OrdinalIgnoreCase)];
                if (granted.Length == 0) continue;

                foreach (AbilityUpgradeData record in catalog.All.Where(TagJudged))
                {
                    if (Fits(abilityId, tags, record, granted: null)) continue;
                    if (!Fits(abilityId, tags, record, granted)) continue;

                    var openers = donors.Where(donor => AbilityTags.SharesAny(donor.GrantsTags, record.Tags));
                    bool works = s_selfContained.Contains(record.Id)
                                 || (s_openedKeys.TryGetValue(record.Id, out string? key) && Moves(book, abilityId, openers, key));
                    reach.Add((record, abilityId, works));
                }
            }

            return reach;
        }

        private static bool TagJudged(AbilityUpgradeData record) =>
            !record.FitsAnyAbility && string.IsNullOrWhiteSpace(record.AbilityId) && record.Tags.Length > 0;

        private static bool Fits(
            string abilityId, IReadOnlyCollection<string> tags, AbilityUpgradeData record, IReadOnlyCollection<string>? granted) =>
            AugmentFit.Check(
                new AbilitySocketPlacement("socket_grant_probe", abilityId, record.Tier), tags, record, [], granted)
            == AugmentFitResult.Fits;

        /// <summary>Whether anything on the ability reads the key once the genus donors are worn — a
        /// move is laid on it and the number is read back, the reach guard's own probe.</summary>
        private static bool Moves(AbilityProvider book, string abilityId, IEnumerable<AbilityUpgradeData> openers, string parameter)
        {
            IAbility ability = book.CreateAbility(abilityId);
            foreach (AbilityUpgradeData opener in openers) book.CreateUpgrade(opener)?.Apply(ability);

            float before = ability[parameter];
            new AbilityUpgradeParameterSet("Augment_Grant_Probe", [], 3, [(parameter, Core.Enums.OperationType.Add, 5f)]).Apply(ability);
            return Math.Abs(ability[parameter] - before) > 0.0001f;
        }

        private static List<string> Render(List<(AbilityUpgradeData Record, string Ability, bool Works)> reach) =>
        [
            .. reach.GroupBy(entry => entry.Record.Id, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => Row(
                    group.Key,
                    group.Where(entry => entry.Works).Select(entry => entry.Ability),
                    group.Where(entry => !entry.Works).Select(entry => entry.Ability)))
        ];

        private static List<string> Render((string Record, string[] WorksOn, string[] InertOn)[] ledger) =>
        [
            .. ledger.OrderBy(row => row.Record, StringComparer.Ordinal)
                .Select(row => Row(row.Record, row.WorksOn, row.InertOn))
        ];

        private static string Row(string record, IEnumerable<string> works, IEnumerable<string> inert) =>
            $"{record}: works [{Names(works)}] inert [{Names(inert)}]";

        private static string Names(IEnumerable<string> abilities) =>
            string.Join(", ", abilities.OrderBy(ability => ability, StringComparer.Ordinal));

        /// <summary>The keys of the book's shared vocabulary, off <see cref="AbilityParameter"/> itself.</summary>
        private static HashSet<string> SharedKeys() =>
            [.. typeof(AbilityParameter)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(string))
                .Select(field => field.GetValue(null))
                .OfType<string>()];

        /// <summary>Three same-tier slots of one attack-tagged ability, judged by a catalog holding
        /// the donor, the amplifier it grants for, and the amplifier that only carries tags.</summary>
        private static AbilitySocketBoard Board()
        {
            var catalog = new AugmentCatalogStub()
                .WithAbility(Ability, AbilityTags.Attack)
                .With(new AbilityUpgradeData { Id = Donor, Tier = 2, Tags = [AbilityTags.Attack], GrantsTags = [AbilityTags.Poison] })
                .With(new AbilityUpgradeData { Id = Amplifier, Tier = 2, Tags = [AbilityTags.Poison] })
                .With(new AbilityUpgradeData { Id = TaggedAmplifier, Tier = 2, Tags = [AbilityTags.Attack, AbilityTags.Poison] });

            var board = new AbilitySocketBoard(catalog);
            board.Sync([Slot(SlotOne), Slot(SlotTwo), Slot(SlotThree)]);
            return board;
        }

        private static AbilitySocketPlacement Slot(string socketId) => new(socketId, Ability, 2);

        /// <summary>A fightable an effect can actually land on: real effects and modifier pipelines,
        /// only the entity itself is a mock.</summary>
        private sealed class Fighter
        {
            private readonly Mock<IFightable> _mock = new();

            public IFightable Object => _mock.Object;

            public Fighter()
            {
                var effects = new EffectsComponent(_mock.Object);
                _mock.Setup(fighter => fighter.InstanceId).Returns(Guid.NewGuid().ToString());
                _mock.Setup(fighter => fighter.IsAlive).Returns(true);
                _mock.Setup(fighter => fighter.Effects).Returns(effects);
                _mock.Setup(fighter => fighter.ModifierHandler).Returns(new ModifierHandlerComponent());
                _mock.Setup(fighter => fighter.CombatEvents).Returns(new CombatEventBus());
                _mock.Setup(fighter => fighter.Parameters).Returns(Mock.Of<IEntityParametersComponent>());
                _mock.Setup(fighter => fighter.TryApplyStatusEffect(It.IsAny<StatusEffects>())).Returns(false);
            }
        }

        /// <summary>Stands in for the data pipeline: the records are already parsed.</summary>
        private sealed class AugmentCatalogStub : IAbilityAugmentCatalog
        {
            private readonly Dictionary<string, AbilityUpgradeData> _augments = new(StringComparer.Ordinal);
            private readonly Dictionary<string, string[]> _abilityTags = new(StringComparer.Ordinal);

            public IReadOnlyCollection<AbilityUpgradeData> All => _augments.Values;

            public AugmentCatalogStub WithAbility(string abilityId, params string[] tags)
            {
                _abilityTags[abilityId] = tags;
                return this;
            }

            public AugmentCatalogStub With(AbilityUpgradeData augment)
            {
                _augments[augment.Id] = augment;
                return this;
            }

            public AbilityUpgradeData? Find(string augmentId) => _augments.GetValueOrDefault(augmentId);

            public IReadOnlyCollection<string> TagsOf(string abilityId) => _abilityTags.GetValueOrDefault(abilityId, []);
        }
    }
}
