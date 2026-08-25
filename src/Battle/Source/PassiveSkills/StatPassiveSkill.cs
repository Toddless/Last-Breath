namespace Battle.Source.PassiveSkills
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers;

    /// <summary>
    /// A passive whose whole behaviour is the stat lines written in its own numbers: the record names the
    /// modifiers, this class hangs them on the owner and takes them off again. A keystone made of stats is
    /// therefore a record — in a tree node or an item catalog — and never a class of its own.
    /// <para><b>Field grammar.</b> A key is <c>Parameter:ValueType</c>, or <c>Parameter:ValueType:PerParameter</c>
    /// for a line measured per unit of a carrier; the value is the modifier's number on the game's own
    /// scale (a fraction for Increase/Multiplicative, a raw amount for Flat). The three words are the
    /// fields of a tree modifier line spelled into one key, so the record speaks the language the
    /// parametric channel already speaks:</para>
    /// <code>
    /// "PhysicalDamage:Increase:Strength": 0.01   // +1% physical damage per point of Strength
    /// "HealthRecovery:Multiplicative": -0.25     // −25% health recovery
    /// </code>
    /// <para>Every field the record carries is read, and a field that cannot be read refuses the whole
    /// passive: an unknown word, a <see cref="ModifierValueType.Flag"/> (parameter math has no meaning for
    /// one), a line measured off the very parameter it feeds, or a record with no fields at all. A typo
    /// must cost the grant loudly rather than quietly hand out less than the author wrote.</para>
    /// <para>Gates are not part of the grammar: a conditional line belongs to the parametric channel of a
    /// node, which carries the predicate catalog with it.</para>
    /// </summary>
    public sealed class StatPassiveSkill : Skill
    {
        /// <summary>Ids beginning with this are built from their fields instead of from a factory of their
        /// own, so the author names a fresh one — <c>Passive_Skill_Stats_Unlimited_Power</c> — without any
        /// code being written. The rest of the id is the passive's identity: two keystones sharing it would
        /// be one passive, and only the stronger of them would ever be worn.
        /// <para>The trailing separator is load-bearing: the family is the ids UNDER this name, so a
        /// <c>Passive_Skill_Statsomething</c> written later belongs to whoever writes it, not to this
        /// class.</para></summary>
        public const string IdPrefix = "Passive_Skill_Stats_";

        private const char Separator = ':';

        /// <summary>Minted once and reused across attach/detach: a scaled line watches its carrier through
        /// the same instance it is counted by, and a fresh mint on every attach would leave the ledger
        /// holding a modifier the owner no longer has.</summary>
        private readonly List<IModifierInstance> _modifiers;

        public StatPassiveSkill(string id, IReadOnlyList<StatLine> lines) : base(id)
        {
            Lines = lines;
            Magnitude = lines.Sum(line => Math.Abs(line.Value));
            _modifiers = [.. lines.Select(line => Mint(id, line))];
        }

        /// <summary>The lines the record was read into, in the order it wrote them.</summary>
        public IReadOnlyList<StatLine> Lines { get; }

        /// <summary>How much the passive moves in total, ignoring direction — what tells two registrations
        /// of one id apart, since they differ only when a scaling hand (item ascension) touched the numbers.</summary>
        public float Magnitude { get; }

        /// <summary>Reads the record whole. Refuses rather than trims: see the field grammar above.</summary>
        public static StatPassiveSkill Create(string id, RecordProperties properties)
        {
            List<StatLine> lines = [.. properties.Names.Select(name => ReadLine(id, name, properties.Get(name)))];
            if (lines.Count == 0)
                throw new FormatException($"'{id}' carries no stat fields — a stat passive is nothing but its lines");

            return new StatPassiveSkill(id, lines);
        }

        /// <summary>Whether an id is one this class answers for.</summary>
        public static bool Owns(string id) => id.StartsWith(IdPrefix, StringComparison.Ordinal);

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            foreach (IModifierInstance modifier in _modifiers) modifier.ApplyTo(owner);
        }

        /// <summary>Leaves nothing behind: the line is taken off the owner's list and, when it was measured
        /// off him, stops watching the parameter it was measured by.</summary>
        public override void Detach(IFightable owner)
        {
            foreach (IModifierInstance modifier in _modifiers) modifier.RemoveFrom(owner);
            Owner = null;
        }

        public override ISkill Copy() => new StatPassiveSkill(Id, Lines);

        public override bool IsStronger(ISkill skill) => skill is StatPassiveSkill other && Magnitude > other.Magnitude;

        /// <summary>The modifier a line becomes. A fixed amount resolves the same for everyone; a line
        /// counted per unit of a carrier has to read the fighter, and is inert until it is bound to one.</summary>
        private static IModifierInstance Mint(string source, StatLine line) =>
            line.PerParameter is { } carrier
                ? new ScaledByParameterModifier(line.ValueType, line.Parameter, carrier, line.Value, condition: null, source)
                : new SimpleModifier(line.Parameter, line.ValueType, line.Value, source);

        private static StatLine ReadLine(string id, string name, float value)
        {
            string[] words = name.Split(Separator);
            if (words.Length is < 2 or > 3)
                throw Refuse(id, name, "expected 'Parameter:ValueType' or 'Parameter:ValueType:PerParameter'");

            EntityParameter parameter = Word<EntityParameter>(id, name, words[0]);
            ModifierValueType valueType = Word<ModifierValueType>(id, name, words[1]);
            if (valueType == ModifierValueType.Flag)
                throw Refuse(id, name, "a flag is a pipeline switch, not a parametric line");

            if (words.Length == 2) return new StatLine(parameter, valueType, null, value);

            EntityParameter carrier = Word<EntityParameter>(id, name, words[2]);
            if (carrier == parameter)
                throw Refuse(id, name, "a line cannot be measured per unit of the parameter it feeds");

            return new StatLine(parameter, valueType, carrier, value);
        }

        /// <summary>One word of a key as the enum member it names. Strict on purpose, and a number is not a
        /// name: enum parsing accepts the underlying value, so "1" would quietly become a member nobody
        /// wrote down.</summary>
        private static TEnum Word<TEnum>(string id, string name, string word)
            where TEnum : struct, Enum =>
            word.Length > 0 && char.IsLetter(word[0]) && EnumParser.TryParseEnum(word, out TEnum parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw Refuse(id, name, $"'{word}' is not a {typeof(TEnum).Name}");

        private static FormatException Refuse(string id, string name, string reason) =>
            new($"'{id}' cannot read field '{name}': {reason}");

        /// <summary>One stat line of the record: which parameter, which value bucket, how much, and the
        /// carrier it is counted per unit of — null when it is worth its number outright.</summary>
        public readonly record struct StatLine(
            EntityParameter Parameter,
            ModifierValueType ValueType,
            EntityParameter? PerParameter,
            float Value);
    }
}
