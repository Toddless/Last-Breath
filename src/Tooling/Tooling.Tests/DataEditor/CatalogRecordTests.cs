namespace Tooling.Tests.DataEditor
{
    using System.IO;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>
    /// How a record answers to being named. The id is a field the author edits like any other, so a
    /// record has to be able to say what it is called now — everything that draws a record's name would
    /// otherwise go on showing the word the file held when the tool was opened.
    /// </summary>
    [TestClass]
    public class CatalogRecordTests
    {
        private const string NpcCatalog = "Npc";
        private const string NpcFile = "Npc.json";

        private const string NpcsKey = "npcs";
        private const string IdField = "id";
        private const string NameField = "name";

        private const string IdPointer = "/npcs/0/id";
        private const string RecordPointer = "/npcs/0";

        private const string ReadId = "Npc_Ronald";
        private const string TypedId = "Npc_Ronald_The_Elder";

        private const string OneNpc = """
            { "npcs": [ { "id": "Npc_Ronald", "name": "Ronald" } ] }
            """;

        /// <summary>A record the schema gives no id field, so it was named by its place.</summary>
        private const string OneNameless = """
            { "npcs": [ { "name": "Ronald" } ] }
            """;

        private string _root = string.Empty;

        [TestInitialize]
        public void CreateTheRoot()
        {
            _root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_root);
        }

        [TestCleanup]
        public void DeleteTheRoot()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        [TestMethod]
        public void CurrentId_FollowsTheDocumentWhileIdKeepsWhatWasRead()
        {
            CatalogRecord record = Read(OneNpc, IdField);

            Assert.AreEqual(ReadId, record.Id);
            Assert.AreEqual(ReadId, record.CurrentId);

            record.File.Document.SetValue(JsonPointer.Parse(IdPointer), new JValue(TypedId));

            // The two answers part company on purpose: one is the record's name now, the other is the
            // name it was listed under, which is what a note about the read still has to say.
            Assert.AreEqual(TypedId, record.CurrentId);
            Assert.AreEqual(ReadId, record.Id);

            record.File.Document.History.Undo();
            Assert.AreEqual(ReadId, record.CurrentId);
        }

        [TestMethod]
        public void CurrentId_KeepsThePlaceItWasNamedByWhenTheRecordCarriesNoId()
        {
            CatalogRecord record = Read(OneNameless, IdField);

            Assert.AreEqual(record.Id, record.CurrentId);
        }

        [TestMethod]
        public void CurrentId_KeepsTheNameWhenTheRecordIsGoneFromTheDocument()
        {
            CatalogRecord record = Read(OneNpc, IdField);

            record.File.Document.Remove(JsonPointer.Parse(RecordPointer));

            Assert.IsNull(record.Token);
            Assert.AreEqual(ReadId, record.CurrentId);
        }

        [TestMethod]
        public void CurrentId_NamesTheRecordByItsPlaceWhenTheSchemaHasNoIdField()
        {
            CatalogRecord record = Read(OneNpc, idField: null);

            Assert.AreEqual("#0", record.CurrentId);
        }

        private CatalogRecord Read(string content, string? idField)
        {
            CatalogFixture.Write(_root, NpcCatalog, NpcFile, content);

            ICatalogDescriptor descriptor = CatalogFixture.Descriptor(
                NpcCatalog,
                RootShape.ArrayUnderKey,
                [
                    CatalogFixture.Section(NpcsKey, CatalogFixture.Record(
                        idField,
                        CatalogFixture.Field(IdField, FieldKind.String),
                        CatalogFixture.Field(NameField, FieldKind.String)))
                ]);

            CatalogWorkspace workspace = CatalogWorkspace.Load(_root, [descriptor]);

            Assert.AreEqual(1, workspace.RecordCount);

            return workspace.Catalogs[0].Records[0];
        }
    }
}
