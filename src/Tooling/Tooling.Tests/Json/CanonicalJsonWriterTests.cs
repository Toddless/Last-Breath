namespace Tooling.Tests.Json
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;

    /// <summary>
    /// The one way this project writes JSON. What is pinned here is the promise a canonical writer makes:
    /// a file that came from it goes back out byte for byte, and a file that did not comes to the same shape
    /// once — because a tool that reformatted on every save would bury every real change under a whole-file
    /// diff, and the author would stop reading them.
    /// </summary>
    [TestClass]
    public class CanonicalJsonWriterTests
    {
        private const string Canonical = """
            {
                "id": "Npc_Ronald",
                "level": 3,
                "chance": 0.25,
                "spread": 2.0,
                "tags": [
                    "human",
                    "trader"
                ],
                "home": {
                    "x": 12,
                    "y": -4.5
                },
                "notes": null,
                "extra": {},
                "none": []
            }
            """;

        private const string TwoSpaceCrlf = "{\r\n  \"id\": \"Npc_Ronald\",\r\n  \"rate\": 1.0,\r\n  \"chance\": 2.50000,\r\n  \"tiny\": -0.00001\r\n}\r\n";

        private const string Expected = """
            {
                "id": "Npc_Ronald",
                "rate": 1.0,
                "chance": 2.5,
                "tiny": 0.0
            }
            """;

        private const string Id = "id";
        private const string Level = "level";
        private const string Name = "name";
        private const string Rate = "rate";

        /// <summary>Wider than a double: written back through anything that goes through a double it loses
        /// its last digit, and an author who typed an id or a seed gets another one back.</summary>
        private const long BigInteger = 9007199254740993L;

        private const string NonAscii = "Копьё «Утро»";

        private const char Lf = '\n';
        private const byte LfByte = 0x0A;
        private const byte CrByte = 0x0D;
        private const byte OpenBrace = 0x7B;

        private string _file = string.Empty;

        [TestInitialize]
        public void CreateTempPath() => _file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");

        [TestCleanup]
        public void DeleteTempFile()
        {
            if (File.Exists(_file)) File.Delete(_file);
        }

        [TestMethod]
        public void ACanonicalFile_SurvivesLoadAndWrite_Unchanged()
        {
            string source = Text(Canonical);

            Assert.AreEqual(source, JsonTreeDocument.Parse(source).Write(FileKeyOrder.Instance));
        }

        /// <summary>Indentation, line endings and the number of digits in a number are the writer's business
        /// and not the file's, so whatever the file had is replaced once and then never again.</summary>
        [TestMethod]
        public void AFileWrittenAnotherWay_ComesToCanonInOnePass()
        {
            string canonical = JsonTreeDocument.Parse(TwoSpaceCrlf).Write(FileKeyOrder.Instance);

            Assert.AreEqual(Text(Expected), canonical);
            Assert.AreEqual(canonical, JsonTreeDocument.Parse(canonical).Write(FileKeyOrder.Instance), "and stays there");
        }

        /// <summary>A byte order mark is dropped on the way in and never written on the way out, and neither
        /// is a carriage return: the file has one spelling on every machine that opens it.</summary>
        [TestMethod]
        public void AFileWithAByteOrderMark_IsWrittenWithoutOne()
        {
            File.WriteAllText(_file, TwoSpaceCrlf, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            JsonTreeDocument document = JsonTreeDocument.Load(_file);
            document.Save(_file, FileKeyOrder.Instance);

            byte[] written = File.ReadAllBytes(_file);

            Assert.AreEqual(OpenBrace, written[0], "no byte order mark");
            Assert.AreEqual(LfByte, written[^1], "a newline ends the file");
            CollectionAssert.DoesNotContain(written, CrByte);
            Assert.AreEqual(Text(Expected), File.ReadAllText(_file));
        }

        /// <summary>Known keys stand where the schema declares them; a key it has never heard of goes to the
        /// tail and keeps the place it had among the others, which is what carries a newer file's fields
        /// through an older build without losing their order.</summary>
        [TestMethod]
        public void KnownKeysComeFirst_AndUnknownOnesKeepTheirFileOrder()
        {
            const string source = """{"zeta":1,"level":2,"alpha":3,"id":4}""";
            var order = new StubKeyOrder(new Dictionary<string, int> { [Id] = 0, [Level] = 1 });

            string written = CanonicalJsonWriter.Write(JToken.Parse(source), order, CanonicalJsonOptions.Default);

            CollectionAssert.AreEqual(new[] { Id, Level, "zeta", "alpha" }, Keys(written));
        }

        /// <summary>A number keeps the kind it had. The game's own serializer writes a float as "2.0", and a
        /// second canonical writer that dropped the point would push every such line back and forth between
        /// the two of them for as long as both exist.</summary>
        [TestMethod]
        public void AFractionWithNothingAfterThePoint_KeepsThePoint()
        {
            var root = new JObject { [Rate] = 1.0, [Level] = 12 };

            Assert.AreEqual("{\n    \"rate\": 1.0,\n    \"level\": 12\n}\n", Write(root));
        }

        /// <summary>Zero has one spelling. A value that merely got small enough to round away would otherwise
        /// sit in the diff as "-0.0".</summary>
        [TestMethod]
        public void MinusZero_IsWrittenAsZero()
        {
            var root = new JObject { [Rate] = -0.0, [Level] = -0.00004 };

            Assert.AreEqual("{\n    \"rate\": 0.0,\n    \"level\": 0.0\n}\n", Write(root));
        }

        /// <summary>A number the tool itself put in as a decimal — nothing that comes out of a file is one —
        /// is written by the same rule as every other fraction.</summary>
        [TestMethod]
        public void ADecimalPutInByHand_IsWrittenLikeAnyOtherFraction()
        {
            var root = new JObject { [Rate] = 2.50000m, [Level] = 2m };

            Assert.AreEqual("{\n    \"rate\": 2.5,\n    \"level\": 2.0\n}\n", Write(root));
        }

        [TestMethod]
        public void ALongInteger_KeepsEveryDigit()
        {
            string written = JsonTreeDocument.Parse($$"""{"id":{{BigInteger}}}""").Write(FileKeyOrder.Instance);

            StringAssert.Contains(written, BigInteger.ToString());
        }

        [TestMethod]
        public void AFractionIsRounded_ToTheDecimalsTheOptionsName()
        {
            var root = new JObject { [Rate] = 0.123456 };

            Assert.AreEqual("{\n    \"rate\": 0.1235\n}\n", Write(root));
            Assert.AreEqual("{\n    \"rate\": 0.12\n}\n", CanonicalJsonWriter.Write(root, FileKeyOrder.Instance, new CanonicalJsonOptions { FloatDecimals = 2 }));
        }

        /// <summary>A number of decimals nobody can honour is a mistake where it is written down, not a file
        /// quietly written to some other precision.</summary>
        [TestMethod]
        public void OptionsOutsideWhatCanBeWritten_AreRefused()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new CanonicalJsonOptions { FloatDecimals = -1 });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new CanonicalJsonOptions { FloatDecimals = CanonicalJsonOptions.MaxFloatDecimals + 1 });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new CanonicalJsonOptions { Indent = -1 });
        }

        [TestMethod]
        public void TheIndentIsTheOneTheOptionsName()
        {
            var root = new JObject { [Rate] = 1 };

            StringAssert.StartsWith(CanonicalJsonWriter.Write(root, FileKeyOrder.Instance, new CanonicalJsonOptions { Indent = 2 }), "{\n  \"rate\"");
        }

        /// <summary>The text an author reads is the text in the file: escaping it into code points would make
        /// every localised line unreadable in a diff and in a review.</summary>
        [TestMethod]
        public void TextOutsideAscii_IsNotEscaped()
        {
            var root = new JObject { [Name] = NonAscii };

            StringAssert.Contains(Write(root), NonAscii);
        }

        [TestMethod]
        public void NullIsWritten_AndEmptyContainersStayOnOneLine()
        {
            var root = new JObject { [Name] = JValue.CreateNull(), ["extra"] = new JObject(), ["none"] = new JArray() };
            string written = Write(root);

            StringAssert.Contains(written, "\"name\": null");
            StringAssert.Contains(written, "\"extra\": {}");
            StringAssert.Contains(written, "\"none\": []");
        }

        /// <summary>JSON has no way to write these, and a writer that quietly put something else there would
        /// hand back a file that no longer says what the tool holds.</summary>
        [TestMethod]
        public void ANumberJsonCannotWrite_IsRefused()
        {
            var root = new JObject { [Rate] = double.NaN };

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => Write(root));
        }

        [TestMethod]
        public void Write_RefusesNothingWhereSomethingIsRequired()
        {
            Assert.ThrowsException<ArgumentNullException>(() => CanonicalJsonWriter.Write(null!, FileKeyOrder.Instance, CanonicalJsonOptions.Default));
            Assert.ThrowsException<ArgumentNullException>(() => CanonicalJsonWriter.Write(new JObject(), null!, CanonicalJsonOptions.Default));
            Assert.ThrowsException<ArgumentNullException>(() => CanonicalJsonWriter.Write(new JObject(), FileKeyOrder.Instance, null!));
        }

        private static string Write(JToken root) => CanonicalJsonWriter.Write(root, FileKeyOrder.Instance, CanonicalJsonOptions.Default);

        /// <summary>A fixture as the file holds it: the source of this test file may be checked out with
        /// either line ending, and only one of them is canonical.</summary>
        private static string Text(string fixture) => fixture.Replace("\r\n", "\n") + Lf;

        private static string[] Keys(string written) => [.. ((JObject)JToken.Parse(written)).Properties().Select(property => property.Name)];

        /// <summary>The order a schema would give: the keys it declares, in the order it declares them.</summary>
        private sealed class StubKeyOrder(Dictionary<string, int> ranks) : IKeyOrder
        {
            public int Rank(JsonPointer objectPointer, string key) => ranks.TryGetValue(key, out int rank) ? rank : IKeyOrder.Unknown;
        }
    }
}
