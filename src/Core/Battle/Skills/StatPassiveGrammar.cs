namespace Core.Battle.Skills
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Enums;

    /// <summary>One stat line of a record: which parameter, which value bucket, how much, and the carrier
    /// it is counted per unit of — null when it is worth its number outright.</summary>
    public readonly record struct StatPassiveLine(
        EntityParameter Parameter,
        ModifierValueType ValueType,
        EntityParameter? PerParameter,
        float Value);

    /// <summary>
    /// The field language a stat passive is written in, and the ONE place that reads it. The skill that
    /// hangs those lines on a fighter and the popup that prints them to the player both come through here,
    /// so a node can never promise a line the grant would refuse — or word it differently.
    ///
    /// <para><b>Field grammar.</b> A key is <c>Parameter:ValueType</c>, or
    /// <c>Parameter:ValueType:PerParameter</c> for a line measured per unit of a carrier; the value is the
    /// modifier's number on the game's own scale (a fraction for Increase/Multiplicative, a raw amount for
    /// Flat). The three words are the fields of a tree modifier line spelled into one key:</para>
    /// <code>
    /// "PhysicalDamage:Increase:Strength": 0.01   // +1% physical damage per point of Strength
    /// "HealthRecovery:Multiplicative": -0.25     // −25% health recovery
    /// </code>
    ///
    /// <para>The reading comes in two shapes and they answer the same question. <see cref="Read"/> is what
    /// a GRANT goes through: every field is read, and one that cannot be refuses the whole passive — a typo
    /// must cost the grant loudly rather than quietly hand out less than the author wrote.
    /// <see cref="ReadWhole"/> is that same all-or-nothing verdict without the throw, for a POPUP: a
    /// tooltip that threw would take the window down over a mistyped letter, and a tooltip that printed the
    /// readable half would promise the player lines the grant is about to refuse him.</para>
    ///
    /// <para>Gates are not part of the grammar: a conditional line belongs to the parametric channel of a
    /// node, which carries the predicate catalog with it.</para>
    /// </summary>
    public static class StatPassiveGrammar
    {
        /// <summary>Ids beginning with this are built from their fields instead of from a factory of their
        /// own, so the author names a fresh one — <c>Passive_Skill_Stats_Unlimited_Power</c> — without any
        /// code being written. The rest of the id is the passive's identity: two keystones sharing it would
        /// be one passive, and only the stronger of them would ever be worn.
        /// <para>The trailing separator is load-bearing: the family is the ids UNDER this name, so a
        /// <c>Passive_Skill_Statsomething</c> written later belongs to whoever writes it.</para></summary>
        public const string IdPrefix = "Passive_Skill_Stats_";

        private const char Separator = ':';

        /// <summary>Whether an id is one the family answers for. A node naming no passive at all is not
        /// one of them — the null is answered here so no caller has to ask twice.</summary>
        public static bool Owns(string? id) => id is not null && id.StartsWith(IdPrefix, StringComparison.Ordinal);

        /// <summary>Reads the record whole for a grant. Refuses rather than trims: an unknown word, a
        /// <see cref="ModifierValueType.Flag"/> (parameter math has no meaning for one), a line measured off
        /// the very parameter it feeds, or a record with no fields at all.</summary>
        public static List<StatPassiveLine> Read(string id, RecordProperties properties)
        {
            List<StatPassiveLine> lines = [];
            foreach (string name in properties.Names)
            {
                if (!TryReadLine(name, properties.Get(name), out StatPassiveLine line, out string? refusal))
                    throw new FormatException($"'{id}' cannot read field '{name}': {refusal}");

                lines.Add(line);
            }

            if (lines.Count == 0)
                throw new FormatException($"'{id}' carries no stat fields — a stat passive is nothing but its lines");

            return lines;
        }

        /// <summary>The record's lines in the order it wrote them, or NOTHING at all — the reading a popup
        /// makes, and the same verdict <see cref="Read"/> reaches without the exception. One unreadable
        /// field empties the answer because it empties the grant: a card showing two of three lines would
        /// have the player buy the node for them and receive none, and the refusal that explains it goes to
        /// the log, where nobody playing the game is looking.</summary>
        public static List<StatPassiveLine> ReadWhole(IReadOnlyDictionary<string, float> fields)
        {
            List<StatPassiveLine> lines = [];
            foreach (KeyValuePair<string, float> field in fields)
            {
                if (!TryReadLine(field.Key, field.Value, out StatPassiveLine line, out _)) return [];

                lines.Add(line);
            }

            return lines;
        }

        /// <summary>One field as a line, or the reason it is not one. The whole grammar lives here and
        /// nowhere else — both strictnesses above are this method plus a decision about the refusal.</summary>
        public static bool TryReadLine(string name, float value, out StatPassiveLine line, out string? refusal)
        {
            line = default;

            string[] words = name.Split(Separator);
            if (words.Length is < 2 or > 3)
            {
                refusal = "expected 'Parameter:ValueType' or 'Parameter:ValueType:PerParameter'";
                return false;
            }

            if (!Word(words[0], out EntityParameter parameter))
            {
                refusal = $"'{words[0]}' is not a {nameof(EntityParameter)}";
                return false;
            }

            if (!Word(words[1], out ModifierValueType valueType))
            {
                refusal = $"'{words[1]}' is not a {nameof(ModifierValueType)}";
                return false;
            }

            if (valueType == ModifierValueType.Flag)
            {
                refusal = "a flag is a pipeline switch, not a parametric line";
                return false;
            }

            refusal = null;
            if (words.Length == 2)
            {
                line = new StatPassiveLine(parameter, valueType, null, value);
                return true;
            }

            if (!Word(words[2], out EntityParameter carrier))
            {
                refusal = $"'{words[2]}' is not a {nameof(EntityParameter)}";
                return false;
            }

            if (carrier == parameter)
            {
                refusal = "a line cannot be measured per unit of the parameter it feeds";
                return false;
            }

            line = new StatPassiveLine(parameter, valueType, carrier, value);
            return true;
        }

        /// <summary>One word of a key as the enum member it names. Strict on purpose, and a number is not a
        /// name: enum parsing accepts the underlying value, so "1" would quietly become a member nobody
        /// wrote down.</summary>
        private static bool Word<TEnum>(string word, out TEnum parsed)
            where TEnum : struct, Enum
        {
            parsed = default;
            return word.Length > 0
                   && char.IsLetter(word[0])
                   && EnumParser.TryParseEnum(word, out parsed)
                   && Enum.IsDefined(parsed);
        }
    }
}
