namespace LastBreathTest.BattleSystemTests
{
    using System.Collections;
    using System.Reflection;
    using Core.Data.SaveData;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Every enum a save file carries is written by NAME. An ordinal is a promise the enum can never keep:
    /// inserting a member mid-scale silently renames the value in every file already on disk — a bagged
    /// augment would come back a different rarity than the one put away, and nothing would report it.
    /// <para>The rule is checked by reflection over the save DTOs rather than by capturing seeded
    /// participants: a capture only shows the fields the seed happened to fill, so a participant seeded
    /// without an augment would produce no rarity at all and the hole would pass unnoticed. Reflection sees
    /// every persisted member of every DTO, including the ones no test composition can reach.</para>
    /// </summary>
    [TestClass]
    public class SaveEnumConventionTests
    {
        private static readonly DefaultContractResolver Contracts = new();

        [TestMethod]
        public void PersistedEnumsSerializeByName()
        {
            List<string> inspected = [];
            List<string> ordinals = [];

            foreach (Type dto in SaveDataTypes())
            {
                if (Contracts.ResolveContract(dto) is not JsonObjectContract contract) continue;
                foreach (JsonProperty written in contract.Properties.Where(property => !property.Ignored))
                    Inspect(dto, written, inspected, ordinals);
            }

            // Anchors against a scan that has gone blind (a moved namespace, a DTO that lost its
            // parameterless constructor): a green run must have actually looked at these two.
            CollectionAssert.Contains(inspected, Name<AugmentSaveData>(nameof(AugmentSaveData.Rarity)));
            CollectionAssert.Contains(inspected, Name<EquipItemSaveData>(nameof(EquipItemSaveData.BaseStats)));

            Assert.AreEqual(0, ordinals.Count, string.Join(Environment.NewLine, ordinals));
        }

        /// <summary>Checks the one property, if it carries an enum at all: as its own value, or as the keys
        /// of a map. An enum nested inside another save DTO needs no walk — that DTO is scanned in its own
        /// right.</summary>
        private static void Inspect(Type dto, JsonProperty written, List<string> inspected, List<string> ordinals)
        {
            var member = dto.GetProperty(written.UnderlyingName ?? string.Empty);
            if (member == null) return;

            if (EnumOf(written.PropertyType!) is { } value)
            {
                inspected.Add(Name(dto, member.Name));
                CheckValue(dto, written, member, value, ordinals);
                return;
            }

            if (DictionaryKey(written.PropertyType!) is not { } key) return;

            inspected.Add(Name(dto, member.Name));
            CheckKeys(dto, written, member, key, ordinals);
        }

        /// <summary>Sets the member to a real member of its enum and reads back what the file would hold.</summary>
        private static void CheckValue(Type dto, JsonProperty written, PropertyInfo member, Type enumType, List<string> ordinals)
        {
            if (Sample(enumType) is not { } sample) return;

            object instance = Activator.CreateInstance(dto)!;
            member.SetValue(instance, sample);

            JToken? value = JObject.FromObject(instance)[written.PropertyName!];
            if (value == null)
            {
                ordinals.Add($"{Name(dto, member.Name)} holds {enumType.Name}.{sample} but is not written to the file at all.");
                return;
            }

            if (IsName(value)) return;
            ordinals.Add($"{Name(dto, member.Name)} persists {enumType.Name}.{sample} as {value} — " +
                         "add [JsonConverter(typeof(StringEnumConverter))].");
        }

        /// <summary>Puts one entry under an enum key and reads back the property name the file would hold.</summary>
        private static void CheckKeys(Type dto, JsonProperty written, PropertyInfo member, Type keyType, List<string> ordinals)
        {
            if (Sample(keyType) is not { } sample) return;

            object instance = Activator.CreateInstance(dto)!;
            var map = member.GetValue(instance) as IDictionary ?? (IDictionary)Activator.CreateInstance(member.PropertyType)!;
            map[sample] = Blank(member.PropertyType.GetGenericArguments()[1]);
            member.SetValue(instance, map);

            foreach (JProperty entry in (JObject.FromObject(instance)[written.PropertyName!] as JObject)?.Properties() ?? [])
            {
                if (!int.TryParse(entry.Name, out _)) continue;
                ordinals.Add($"{Name(dto, member.Name)} keys the map by the ordinal of {keyType.Name} ('{entry.Name}').");
            }
        }

        /// <summary>Every save DTO: the whole namespace, so a type added later is covered without being listed.</summary>
        private static IEnumerable<Type> SaveDataTypes() =>
            typeof(AugmentSaveData).Assembly.GetTypes()
                .Where(type => type.Namespace == typeof(AugmentSaveData).Namespace)
                .Where(type => type is { IsPublic: true, IsClass: true, IsAbstract: false })
                .Where(type => type.GetConstructor(Type.EmptyTypes) != null);

        private static Type? EnumOf(Type type) =>
            (Nullable.GetUnderlyingType(type) ?? type) is { IsEnum: true } enumType ? enumType : null;

        private static Type? DictionaryKey(Type type)
        {
            Type[] arguments = type.GetInterfaces().Append(type)
                .FirstOrDefault(face => face.IsGenericType && face.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                ?.GetGenericArguments() ?? [];

            return arguments.Length > 0 && arguments[0].IsEnum ? arguments[0] : null;
        }

        /// <summary>A member the file could plausibly hold: a non-zero one where the enum has any, so a
        /// default-valued property is not mistaken for a written one.</summary>
        private static object? Sample(Type enumType)
        {
            object[] values = [.. Enum.GetValues(enumType).Cast<object>()];
            return values.FirstOrDefault(value => Convert.ToInt64(value) != 0) ?? values.FirstOrDefault();
        }

        private static object? Blank(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

        private static bool IsName(JToken token) =>
            token.Type == JTokenType.String && !int.TryParse(token.Value<string>(), out _);

        private static string Name<T>(string member) => Name(typeof(T), member);

        private static string Name(Type dto, string member) => $"{dto.Name}.{member}";
    }
}
