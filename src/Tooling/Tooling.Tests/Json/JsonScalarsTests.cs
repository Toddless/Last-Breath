namespace Tooling.Tests.Json
{
    using System.Globalization;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;

    /// <summary>
    /// Values as the file writes them, and numbers as the file already spelled them. What a number is
    /// written as is the file's own business and not the author's: an editor that changed an integer
    /// into a fraction because the digits were retyped would have that field pushed back the other way
    /// by the game's own serializer, and the two of them would keep rewriting one line forever.
    /// </summary>
    [TestClass]
    public class JsonScalarsTests
    {
        /// <summary>A culture that writes a fraction with a comma and is not the one the tests run in:
        /// the values a file holds are the file's, and a machine set to it must write them the same.</summary>
        private const string OtherCulture = "ru-RU";

        [TestMethod]
        public void Written_ReadsTextUnquotedAndEverythingElseAsJson()
        {
            Assert.AreEqual("Npc_Ronald", JsonScalars.Written(new JValue("Npc_Ronald")));
            Assert.AreEqual("true", JsonScalars.Written(new JValue(true)));
            Assert.AreEqual("12", JsonScalars.Written(new JValue(12L)));
        }

        [TestMethod]
        public void Number_KeepsAFractionAFractionWhenItsValueIsWhole()
        {
            Assert.AreEqual(JTokenType.Float, JsonScalars.Number(new JValue(2.5d), 2d, whole: false).Type);

            // What the file already holds outranks what the field would have chosen: a fraction typed
            // into a box that writes whole numbers still lands in a field the game reads as a fraction.
            Assert.AreEqual(JTokenType.Float, JsonScalars.Number(new JValue(2.5d), 2d, whole: true).Type);
        }

        [TestMethod]
        public void Number_KeepsAnIntegerAnIntegerAndTakesAFractionWhenItIsGivenOne()
        {
            Assert.AreEqual(JTokenType.Integer, JsonScalars.Number(new JValue(3L), 4d, whole: false).Type);

            // A number that is not whole cannot be written as an integer whatever the file had there —
            // the digits the author typed are the point, and half of them would be dropped.
            Assert.AreEqual(JTokenType.Float, JsonScalars.Number(new JValue(3L), 4.5d, whole: false).Type);
        }

        [TestMethod]
        public void Number_TakesTheKindFromTheFieldWhenTheFileHoldsNothingThere()
        {
            Assert.AreEqual(JTokenType.Integer, JsonScalars.Number(previous: null, 7d, whole: true).Type);
            Assert.AreEqual(JTokenType.Float, JsonScalars.Number(previous: null, 7d, whole: false).Type);
        }

        [TestMethod]
        public void Number_WritesAWholeNumberTooBigForADoubleAsAFraction()
        {
            // Past what a double holds exactly, an integer is no longer the number it was asked to be;
            // written as a fraction it at least says out loud how much of it survived.
            Assert.AreEqual(
                JTokenType.Float,
                JsonScalars.Number(new JValue(1L), JsonScalars.ExactWholeLimit * 4, whole: true).Type);
        }

        [TestMethod]
        public void Number_WritesTheSameKindOnAMachineOfAnotherCulture()
        {
            CultureInfo spoken = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo(OtherCulture);

            try
            {
                // The premise: in this culture the CLR's own way of writing a fraction is not the file's.
                Assert.AreNotEqual("0.5", 0.5.ToString(CultureInfo.CurrentCulture));

                Assert.AreEqual(JTokenType.Float, JsonScalars.Number(new JValue(0.5d), 1d, whole: true).Type);
            }
            finally
            {
                CultureInfo.CurrentCulture = spoken;
            }
        }
    }
}
