namespace Tooling.Tests.Schema
{
    using System.Collections.Generic;
    using Tooling.Schema.Model;

    /// <summary>
    /// How a word written into a field answered from an open list is read against the words the run
    /// offers. The whole of what the mark on the box means, held here because a panel cannot be asked
    /// anything: an author is told he is writing a word nobody has met, and a mark that says so about a
    /// word the game reads perfectly well teaches him to stop reading marks.
    /// </summary>
    [TestClass]
    public class SuggestedWordsTests
    {
        private const string Wolf = "Kill_Count:Npc_Wolf";

        /// <summary>The same key spelled as nobody spells it. The game looks a fact up by the letter, so
        /// this is a word of its own however alike it reads.</summary>
        private const string Shouted = "kILL_cOUNT:npc_wOLF";

        private const string Family = "Kill_Count:<npcId>";

        private const string Invented = "Fact_Nobody_Has_Met";

        [TestMethod]
        public void AWordASourceOffers_IsMet() =>
            Assert.AreEqual(SuggestedWordKind.Met, SuggestedWords.Judge(Wolf, Met(Wolf)));

        /// <summary>Nothing written is nothing to judge: an empty field is the author having not started,
        /// and a mark on it would stand under every field of the record he has not reached yet.</summary>
        [TestMethod]
        public void NothingWrittenAtAll_IsNotMarked() =>
            Assert.AreEqual(SuggestedWordKind.Met, SuggestedWords.Judge(string.Empty, Met(Wolf)));

        [TestMethod]
        public void AWordNobodyHasMet_IsNew() =>
            Assert.AreEqual(SuggestedWordKind.New, SuggestedWords.Judge(Invented, Met(Wolf)));

        /// <summary>The one thing capitals must never do here. A search is forgiving of them — the author
        /// remembers the word and not its capitals — but the game reads a fact key by the letter, so a
        /// word spelled otherwise is another word and has to be marked as one nobody has met.</summary>
        [TestMethod]
        public void AWordSpelledInOtherCapitals_IsNotTheWordTheSourceOffers() =>
            Assert.AreEqual(SuggestedWordKind.New, SuggestedWords.Judge(Shouted, Met(Wolf)));

        /// <summary>A family's template is offered to be filled in and never to be written as it stands:
        /// picked out of the list and left alone, it is a word nothing will ever raise.</summary>
        [TestMethod]
        public void AFamilysTemplateWrittenAsItStands_IsUnfilled()
        {
            Assert.AreEqual(SuggestedWordKind.Unfilled, SuggestedWords.Judge(Family, Met(Family)));
            Assert.IsTrue(SuggestedWords.Unfilled(Family));
            Assert.IsFalse(SuggestedWords.Unfilled(Wolf));
        }

        /// <summary>Half a template is as unfinished as a whole one: a parameter half deleted leaves one
        /// mark standing, and the key it makes is one nothing answers either.</summary>
        [TestMethod]
        public void HalfATemplate_IsUnfilledToo()
        {
            Assert.AreEqual(SuggestedWordKind.Unfilled, SuggestedWords.Judge("Kill_Count:<npcId", Met(Wolf)));
            Assert.AreEqual(SuggestedWordKind.Unfilled, SuggestedWords.Judge("Kill_Count:npcId>", Met(Wolf)));
        }

        /// <summary>A family of one word — a template with no parameter to fill in — is a key like any
        /// other: the mark answers about the word written, not about the row it was picked from.</summary>
        [TestMethod]
        public void AFamilyWrittenWithNoParameter_IsMet() =>
            Assert.AreEqual(
                SuggestedWordKind.Met,
                SuggestedWords.Judge("Item_Equipped_Any", Met(new SuggestedWord("Item_Equipped_Any", Family: true))));

        private static IReadOnlySet<string> Met(params string[] words) =>
            SuggestedWords.Met([.. words.Select(word => new SuggestedWord(word, Family: false))]);

        private static IReadOnlySet<string> Met(SuggestedWord word) => SuggestedWords.Met([word]);
    }
}
