namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.GameData;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Narrative;

    /// <summary>
    /// A conversation read as a map: the columns a walk from the opening rules lays its nodes out in, and
    /// every route between them with what a reader has to see about it — the roll's second way out, the
    /// clause that may hide a choice, the way back to the greeting, and the route naming a node nobody
    /// wrote.
    /// <para>Run over a forged conversation for the boundaries and over the one the game ships for the
    /// shape: a map an author reads the veteran by is only worth anything if it draws the veteran the way
    /// he is written.</para>
    /// </summary>
    [TestClass]
    public class DialogueGraphTests
    {
        private const string DialoguesKey = "dialogues";

        private const string NodesKey = "nodes";

        private const string NpcIdKey = "npcId";

        private const string VeteranNpcId = "Npc_Bandit_Veteran";

        private const string ForgedAt = "/dialogues/0";

        /// <summary>The veteran's conversation as the game ships it, read once for the whole class: the
        /// readings below are of one file and there is no gesture between them that could move it.</summary>
        private static DialogueGraph s_veteran = null!;

        private static JsonPointer s_veteranAt = null!;

        /// <summary>
        /// One conversation written around the ways a route can go wrong: one leading to a node nobody
        /// wrote, one written empty, one rolling into the node it leaves, a ring back to the greeting, a
        /// node offering nothing at all, and a node nothing routes to.
        /// </summary>
        private const string ForgedJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "entryRules": [ { "priority": 0, "node": "Greeting" } ],
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Npc", "key": "Dlg_Forged_Greeting_1" } ],
                      "options": [
                        { "id": "Away", "key": "Dlg_Forged_Away", "next": "Nowhere" },
                        {
                          "id": "On",
                          "key": "Dlg_Forged_On",
                          "visibleConditions": [ { "type": "Fact", "key": "Fact_Forged" } ],
                          "next": "Second"
                        },
                        { "id": "Ends", "key": "Dlg_Forged_Ends", "next": "" },
                        {
                          "id": "Roll",
                          "key": "Dlg_Forged_Roll",
                          "speechCheck": { "difficulty": 2, "failNext": "Greeting" },
                          "next": "Second"
                        }
                      ]
                    },
                    {
                      "id": "Second",
                      "lines": [],
                      "options": [
                        { "id": "Ring", "key": "Dlg_Forged_Ring", "next": "Greeting" },
                        { "id": "Self", "key": "Dlg_Forged_Self", "next": "Second" }
                      ]
                    },
                    {
                      "id": "Leaf",
                      "lines": [ { "speaker": "Npc", "key": "Dlg_Forged_Leaf_1" } ]
                    },
                    {
                      "lines": [],
                      "options": [ { "next": "Second" } ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A conversation nothing opens: every node of it is written and none of it is ever
        /// played.</summary>
        private const string UnopenedJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Unopened",
                  "nodes": [
                    { "id": "Greeting", "options": [ { "id": "On", "next": "Second" } ] },
                    { "id": "Second", "options": [] }
                  ]
                }
              ]
            }
            """;

        /// <summary>A conversation writing two nodes under one name — what the loader refuses the whole
        /// record over, and what an author looking at the map is trying to find.</summary>
        private const string TwiceNamedJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Twice",
                  "entryRules": [ { "priority": 0, "node": "Greeting" } ],
                  "nodes": [
                    { "id": "Greeting", "options": [ { "id": "On", "next": "Second" } ] },
                    { "id": "Second", "lines": [ { "speaker": "Npc", "key": "Dlg_Twice_Second_1" } ] },
                    { "id": "Second", "options": [ { "id": "Away", "next": "Greeting" } ] }
                  ]
                }
              ]
            }
            """;

        /// <summary>A conversation whose only entry rule names a node nobody wrote: the game opens it on
        /// nothing, and every node of it is written and never played.</summary>
        private const string RuleToNowhereJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Nowhere",
                  "entryRules": [ { "priority": 0, "node": "Missing" } ],
                  "nodes": [
                    { "id": "Greeting", "options": [ { "id": "On", "next": "Second" } ] },
                    { "id": "Second", "options": [] }
                  ]
                }
              ]
            }
            """;

        /// <summary>A record whose nodes are written as something that is not a collection: the game reads
        /// no node out of it, and neither may a map — least of all by refusing to be drawn at all while its
        /// author is in the middle of typing.</summary>
        private const string MalformedJson =
            """
            { "dialogues": [ { "npcId": "Npc_Broken", "entryRules": 3, "nodes": {} } ] }
            """;

        [TestMethod]
        public void TheVeteran_StandsInColumnsFromTheNodesHisRulesOpenOn()
        {
            DialogueGraph graph = Veteran();

            Assert.AreEqual(3, graph.Layers.Count, Drawn(graph));
            CollectionAssert.AreEqual(new[] { "Greeting", "Working", "HaveIt" }, Named(graph.Layers[0]), Drawn(graph));
            CollectionAssert.AreEqual(
                new[] { "Offer", "Thanks", "FlatterGood", "FlatterBad" }, Named(graph.Layers[1]), Drawn(graph));
            CollectionAssert.AreEqual(new[] { "Accepted" }, Named(graph.Layers[2]), Drawn(graph));
        }

        /// <summary>Every node the veteran's rules open on is marked as one, and every node of his is
        /// arrived at: a conversation the game ships has nothing in it nobody can reach.</summary>
        [TestMethod]
        public void TheVeteran_MarksTheNodesHisRulesOpenOn_AndReachesEveryOtherOne()
        {
            DialogueGraph graph = Veteran();

            CollectionAssert.AreEquivalent(
                new[] { "Greeting", "Working", "HaveIt" },
                graph.Nodes.Where(node => node.Entry).Select(node => node.Id).ToArray(),
                Drawn(graph));

            Assert.IsTrue(graph.Nodes.All(node => node.Reached), Drawn(graph));
        }

        /// <summary>An option that rolls is two routes and not one: a map showing only the won one hides
        /// half of what the author wrote.</summary>
        [TestMethod]
        public void TheVeteran_OptionThatRolls_IsDrawnAsBothOfItsRoutes()
        {
            DialogueGraphEdge[] flattery = Out(Veteran(), "Flatter");

            Assert.AreEqual(2, flattery.Length);

            Assert.AreEqual("FlatterGood", flattery[0].To);
            Assert.AreEqual(DialogueEdgeKind.Next, flattery[0].Kind);

            Assert.AreEqual("FlatterBad", flattery[1].To);
            Assert.AreEqual(DialogueEdgeKind.FailNext, flattery[1].Kind);
        }

        /// <summary>The route out of both endings of the flattery leads back to the greeting the roll was
        /// made in. Marked, or a reader would follow a line into a column already behind him and read the
        /// conversation as going on where it goes round.</summary>
        [TestMethod]
        public void TheVeteran_RouteLeadingBackToTheGreeting_IsMarkedAsOne()
        {
            DialogueGraph graph = Veteran();

            DialogueGraphEdge[] back = [.. graph.Edges.Where(edge => edge.Back)];

            CollectionAssert.AreEquivalent(
                new[] { "FlatterGood", "FlatterBad" }, back.Select(edge => edge.From).ToArray(), Drawn(graph));

            Assert.IsTrue(back.All(edge => edge.To == "Greeting"), Drawn(graph));
            Assert.IsFalse(Out(graph, "AskWork")[0].Back, "a route one column onward was called a way back");
        }

        /// <summary>A choice written with conditions is one the player may never be offered, whichever of
        /// the two kinds of clause is written on it.</summary>
        [TestMethod]
        public void TheVeteran_OptionWrittenWithConditions_IsMarkedGuarded()
        {
            DialogueGraph graph = Veteran();

            Assert.IsTrue(Out(graph, "AskWork")[0].Guarded, "the offer is hidden by conditions and was drawn open");
            Assert.IsTrue(Out(graph, "TurnIn")[0].Guarded, "the hand-in is locked by conditions and was drawn open");
            Assert.IsFalse(Out(graph, "Accept")[0].Guarded, "an option written with no clause was drawn as gated");
        }

        /// <summary>What a node's own row says: how much it says and how much it offers, and the address
        /// the reader is taken to by pressing it.</summary>
        [TestMethod]
        public void TheVeteran_NodeCountsWhatItSaysAndOffers_AndCarriesItsOwnAddress()
        {
            DialogueGraph graph = Veteran();
            DialogueGraphNode offer = Node(graph, "Offer");

            Assert.AreEqual(2, offer.Lines);
            Assert.AreEqual(2, offer.Options);
            Assert.IsFalse(offer.Leaf);
            Assert.AreEqual(VeteranAt().Append(NodesKey).Append(1), offer.Pointer);
        }

        /// <summary>Nothing of the shipped conversation leads out of it — which is also what says these
        /// readings are of the veteran as he is written and not of a bench standing in for him.</summary>
        [TestMethod]
        public void TheVeteran_LeadsNowhereOutsideHimself()
        {
            DialogueGraph graph = Veteran();

            Assert.AreEqual(0, graph.Edges.Count(edge => edge.Dangling), Drawn(graph));
            Assert.AreNotEqual(0, graph.Edges.Count, "the run read no route of the veteran at all");
        }

        /// <summary>A route naming a node nobody wrote is what the loader drops the whole dialogue over.
        /// It is drawn, marked, and never quietly counted among the routes that arrive somewhere.</summary>
        [TestMethod]
        public void ARouteToANodeNobodyWrote_IsAnEdgeThatSaysSo()
        {
            DialogueGraph graph = Forged();
            DialogueGraphEdge away = Out(graph, "Away")[0];

            Assert.AreEqual("Nowhere", away.To);
            Assert.IsTrue(away.Dangling, "a route out of the conversation was drawn as one that arrives");
            Assert.IsFalse(away.Back, "a route arriving nowhere was called a way back");
            Assert.AreEqual(1, graph.Edges.Count(edge => edge.Dangling), Drawn(graph));
        }

        /// <summary>A route written empty is where the conversation ends, which is what the game plays and
        /// what the map draws: no line at all. The option itself is still one the node offers.</summary>
        [TestMethod]
        public void ARouteWrittenEmpty_IsNoEdgeAndStillAnOption()
        {
            DialogueGraph graph = Forged();

            Assert.AreEqual(0, Out(graph, "Ends").Length, Drawn(graph));
            Assert.AreEqual(4, Node(graph, "Greeting").Options);
        }

        /// <summary>The nodes no walk arrives at stand in one band after the last column, marked. Written
        /// and never played is a thing an author has to be able to see at a glance.</summary>
        [TestMethod]
        public void ANodeNoRouteReaches_StandsInTheBandDrawnLast()
        {
            DialogueGraph graph = Forged();

            Assert.AreEqual(3, graph.Layers.Count, Drawn(graph));
            CollectionAssert.AreEqual(new[] { "Leaf", "#3" }, Named(graph.Layers[2]), Drawn(graph));
            Assert.IsTrue(graph.Layers[2].All(node => !node.Reached), Drawn(graph));
            Assert.IsTrue(graph.Layers[0].Concat(graph.Layers[1]).All(node => node.Reached), Drawn(graph));
        }

        /// <summary>A node offering nothing stops the conversation dead, which is not the same as one whose
        /// options all end it: the player is left with nothing to press.</summary>
        [TestMethod]
        public void ANodeThatOffersNothing_IsALeaf()
        {
            DialogueGraph graph = Forged();

            Assert.IsTrue(Node(graph, "Leaf").Leaf);
            Assert.IsFalse(Node(graph, "Greeting").Leaf);
        }

        /// <summary>Both ways a route can fail to advance the conversation: back into an earlier column,
        /// and into the very node it leaves.</summary>
        [TestMethod]
        public void ARouteThatDoesNotAdvanceTheConversation_IsMarkedBack()
        {
            DialogueGraph graph = Forged();

            Assert.IsTrue(Out(graph, "Ring")[0].Back, "a ring back to the greeting was drawn as going onward");
            Assert.IsTrue(Out(graph, "Self")[0].Back, "a route onto the node it leaves was drawn as going onward");
            Assert.IsTrue(Out(graph, "Roll")[1].Back, "a lost roll falling back to its own node was drawn as going onward");
            Assert.IsFalse(Out(graph, "On")[0].Back);
        }

        /// <summary>An element nobody has named yet is named by its place: an author writes the routes
        /// before the names, and a row he cannot point at is one he cannot fix.</summary>
        [TestMethod]
        public void AnElementCarryingNoId_IsNamedByItsPlace()
        {
            DialogueGraph graph = Forged();

            Assert.AreEqual("#3", graph.Nodes[3].Id);
            Assert.AreEqual("#0", graph.Edges.Single(edge => edge.From == "#3").Option);
        }

        /// <summary>Every route carries the address of the option it is written on, so that a reader
        /// pressing it is taken to the very option and not to the node holding it.</summary>
        [TestMethod]
        public void EveryRouteCarriesTheAddressOfTheOptionItIsWrittenOn()
        {
            DialogueGraph graph = Forged();

            Assert.AreEqual(
                JsonPointer.Parse($"{ForgedAt}/nodes/0/options/1"), Out(graph, "On")[0].Pointer);
        }

        /// <summary>A conversation no rule opens is played by nobody: it is drawn whole, in the one band
        /// that says so, rather than as a map of nothing.</summary>
        [TestMethod]
        public void ADialogueNoRuleOpens_IsDrawnWholeInOneBand()
        {
            DialogueGraph graph = Read(UnopenedJson);

            Assert.AreEqual(1, graph.Layers.Count, Drawn(graph));
            CollectionAssert.AreEqual(new[] { "Greeting", "Second" }, Named(graph.Layers[0]), Drawn(graph));
            Assert.IsTrue(graph.Nodes.All(node => !node.Reached && !node.Entry), Drawn(graph));
        }

        /// <summary>Two nodes under one name: every route naming it arrives at the FIRST, and the second is
        /// drawn whole in the band nothing opens. One answer and not two — the loader refuses such a record
        /// outright, and a map routing to both would show the author a conversation the game never plays.</summary>
        [TestMethod]
        public void TwoNodesUnderOneName_AreRoutedToByTheFirstOfThem()
        {
            DialogueGraph graph = Read(TwiceNamedJson);

            Assert.AreEqual(3, graph.Nodes.Count, Drawn(graph));

            DialogueGraphEdge on = Out(graph, "On")[0];

            Assert.IsFalse(on.Dangling, "a route to a name written twice was drawn as leading out of the dialogue");

            Assert.AreEqual(1, graph.Nodes[1].Layer, Drawn(graph));
            Assert.IsTrue(graph.Nodes[1].Reached, "the first node of the name was not the one the route arrives at");
            Assert.IsFalse(graph.Nodes[2].Reached, "the second node of the name was drawn as one a route reaches");

            CollectionAssert.AreEqual(new[] { "Second" }, Named(graph.Layers[2]), Drawn(graph));
        }

        /// <summary>An entry rule naming a node nobody wrote opens nothing: there is no place to start a
        /// walk at, so the conversation is drawn as the unplayed thing it is rather than as one opening on
        /// whichever node happens to be written first.</summary>
        [TestMethod]
        public void AnEntryRuleNamingANodeNobodyWrote_OpensNothing()
        {
            DialogueGraph graph = Read(RuleToNowhereJson);

            Assert.AreEqual(1, graph.Layers.Count, Drawn(graph));
            CollectionAssert.AreEqual(new[] { "Greeting", "Second" }, Named(graph.Layers[0]), Drawn(graph));
            Assert.IsTrue(graph.Nodes.All(node => !node.Entry), "a rule opening on nothing marked a node as an opening");
            Assert.IsTrue(graph.Nodes.All(node => !node.Reached), Drawn(graph));
        }

        /// <summary>A collection written as something else holds no node, the way the game reads none out
        /// of it. The map says so by being empty and not by refusing to be drawn.</summary>
        [TestMethod]
        public void ARecordWhoseNodesAreWrittenAsSomethingElse_IsReadAsHoldingNone()
        {
            DialogueGraph graph = Read(MalformedJson);

            Assert.AreEqual(0, graph.Nodes.Count);
            Assert.AreEqual(0, graph.Edges.Count);
            Assert.AreEqual(0, graph.Layers.Count);
        }

        /// <summary>The routes one option writes, in the order they are read: the way it is taken first,
        /// and the way a lost roll falls back second.</summary>
        private static DialogueGraphEdge[] Out(DialogueGraph graph, string option) =>
            [.. graph.Edges.Where(edge => edge.Option == option)];

        private static DialogueGraphNode Node(DialogueGraph graph, string id) =>
            graph.Nodes.Single(node => node.Id == id);

        private static string[] Named(IReadOnlyList<DialogueGraphNode> column) => [.. column.Select(node => node.Id)];

        /// <summary>The map written out, for a run that failed to say what it drew instead.</summary>
        private static string Drawn(DialogueGraph graph) =>
            Environment.NewLine
            + string.Join(Environment.NewLine, graph.Layers.Select((column, index) =>
                $"  layer {index}: {string.Join(", ", column.Select(node => node.Id))}"))
            + Environment.NewLine
            + string.Join(Environment.NewLine, graph.Edges.Select(edge =>
                $"  {edge.From} --{edge.Option}--> {edge.To}  {edge.Kind}"
                + $"{(edge.Guarded ? " guarded" : string.Empty)}{(edge.Dangling ? " dangling" : string.Empty)}"
                + $"{(edge.Back ? " back" : string.Empty)}"));

        private static DialogueGraph Forged() => Read(ForgedJson);

        private static DialogueGraph Read(string json)
        {
            JsonPointer at = JsonPointer.Parse(ForgedAt);

            return DialogueGraph.Read(at.Resolve(JToken.Parse(json))!, at);
        }

        private static DialogueGraph Veteran() => s_veteran;

        private static JsonPointer VeteranAt() => s_veteranAt;

        /// <summary>The veteran's conversation out of the files the game ships. Found by the npc it belongs
        /// to rather than by a file name: a catalog holds as many files as its author split it into, and
        /// which of them holds him is not a fact about the conversation.</summary>
        [ClassInitialize]
        public static void OpenTheShippedVeteran(TestContext context)
        {
            foreach (string path in SharedData.Files(DataCatalog.Dialogues))
            {
                JToken document = JToken.Parse(File.ReadAllText(path));

                if (document[DialoguesKey] is not JArray dialogues) continue;

                for (int index = 0; index < dialogues.Count; index++)
                {
                    if (!string.Equals((string?)dialogues[index][NpcIdKey], VeteranNpcId, StringComparison.Ordinal))
                        continue;

                    s_veteranAt = JsonPointer.Parse($"/{DialoguesKey}/{index}");
                    s_veteran = DialogueGraph.Read(dialogues[index], s_veteranAt);

                    return;
                }
            }

            throw new AssertFailedException($"no shipped dialogue is written for '{VeteranNpcId}'");
        }
    }
}
