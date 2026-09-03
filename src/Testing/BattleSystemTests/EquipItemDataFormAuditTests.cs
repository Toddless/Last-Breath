namespace LastBreathTest.BattleSystemTests
{
    using System.Reflection;
    using Core.Data.EquipData;
    using Core.Data.GameData;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A range field of the equipment catalog is written as an object with two bounds, and only so.
    /// <para>The converters also read a bare number as a fixed range, which is why nothing but a guard keeps
    /// the two forms from spreading side by side: a scalar loads, plays and reviews the same, so the file
    /// grows a second spelling of one field that every reader — the game, the authoring tool, the next
    /// audit — has to know about. The tolerance stays for old files; the shipped ones are one shape.</para>
    /// </summary>
    [TestClass]
    public class EquipItemDataFormAuditTests
    {
        /// <summary>What a range is written with. The converters know the two names and nothing else, and a
        /// bound written any other way is read as missing.</summary>
        private static readonly string[] s_bounds = ["min", "max"];

        /// <summary>The records a field of either converter stands under. Held as the types themselves, so the
        /// json keys the walk looks for follow the DTOs instead of being spelled out a second time.</summary>
        private static readonly Type[] s_rangeRecords = [typeof(ValueRangeData), typeof(LevelRangeData)];

        /// <summary>Separates the steps of the path a finding is addressed by.</summary>
        private const char PathSeparator = '/';

        [TestMethod]
        public void NoRangeOfTheShippedEquipmentIsWrittenAsANumber()
        {
            List<string> scalars = [.. Ranges().Where(range => range.Token is not JObject).Select(range => range.Address)];

            Assert.AreEqual(0, scalars.Count,
                "A range of the equipment catalog is written as an object with both bounds, so that one field has one "
                + $"shape wherever it stands. Written as a plain number:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", scalars)}");
        }

        [TestMethod]
        public void EveryRangeOfTheShippedEquipmentWritesBothBoundsAsNumbers()
        {
            List<string> broken =
            [
                .. Ranges()
                    .Where(range => range.Token is JObject written && !Bounded(written))
                    .Select(range => range.Address)
            ];

            Assert.AreEqual(0, broken.Count,
                "A bound the converter cannot read is a missing bound: the record is dropped at load, or rolls around "
                + $"nothing. Ranges without two numbers:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", broken)}");
        }

        /// <summary>Whether a range names both of its ends with a number. The converters match the two names
        /// without regard to case and read anything else as a bound that is not there.</summary>
        private static bool Bounded(JObject range) =>
            s_bounds.All(bound => range.GetValue(bound, StringComparison.OrdinalIgnoreCase) is { Type: JTokenType.Integer or JTokenType.Float });

        /// <summary>Every range the shipped files write, addressed by the path it was reached by. WHICH fields
        /// are ranges is read off the DTOs rather than listed here: the walk descends the real types beside the
        /// real json, so a field added later is covered, and a grant property named like a range — a plain
        /// number in a map the schema does not rank — is never mistaken for one.</summary>
        private static List<(string Address, JToken Token)> Ranges()
        {
            List<(string, JToken)> found = [];
            foreach (string file in ShippedFiles())
                Walk(JObject.Parse(File.ReadAllText(file)), typeof(EquipItemDataList), Path.GetFileNameWithoutExtension(file), found);

            Assert.AreNotEqual(0, found.Count, "The shipped equipment writes no range at all — the audit is auditing nothing.");
            return found;
        }

        private static void Walk(JToken token, Type type, string address, List<(string, JToken)> found)
        {
            if (token is not JObject holder) return;

            foreach (JProperty property in holder.Properties())
            {
                if (FieldType(type, property.Name) is not { } held) continue;

                string at = $"{address}{PathSeparator}{property.Name}";
                if (IsRange(held))
                {
                    found.Add((at, property.Value));
                    continue;
                }

                if (Element(held) is { } element)
                {
                    WalkAll(property.Value, element, at, found);
                    continue;
                }

                if (IsRecord(held)) Walk(property.Value, held, at, found);
            }
        }

        private static void WalkAll(JToken token, Type element, string address, List<(string, JToken)> found)
        {
            var list = token as JArray ?? [];
            for (int index = 0; index < list.Count; index++)
                Walk(list[index], element, $"{address}[{index}]", found);
        }

        /// <summary>What the DTO holds under a json key, matched the way the game's reader matches it —
        /// without regard to case, and passing over a key no field is written under.</summary>
        private static Type? FieldType(Type type, string jsonName) =>
            type.GetProperties()
                .FirstOrDefault(property => JsonName(property).Equals(jsonName, StringComparison.OrdinalIgnoreCase))
                ?.PropertyType;

        private static string JsonName(PropertyInfo property) =>
            property.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? property.Name;

        /// <summary>The records written as a number OR a pair of bounds — the two the converters stand on.</summary>
        private static bool IsRange(Type type) => s_rangeRecords.Contains(Nullable.GetUnderlyingType(type) ?? type);

        /// <summary>The DTO a list holds, when it holds one: a list of anything else is a leaf to the walk.</summary>
        private static Type? Element(Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && IsRecord(type.GetGenericArguments()[0])
                ? type.GetGenericArguments()[0]
                : null;

        /// <summary>A DTO of the equipment schema, which is what the walk may descend into. A map of numbers
        /// or an array of names is not one, and neither is a range — that one is the walk's answer.</summary>
        private static bool IsRecord(Type type) =>
            type.Namespace == typeof(EquipItemData).Namespace && !IsRange(type);

        private static IEnumerable<string> ShippedFiles() =>
            Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.EquipItems), "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal);
    }
}
