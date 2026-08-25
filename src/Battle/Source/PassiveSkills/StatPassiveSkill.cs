namespace Battle.Source.PassiveSkills
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Localization;
    using Core.Modifiers;

    /// <summary>
    /// A passive whose whole behaviour is the stat lines written in its own numbers: the record names the
    /// modifiers, this class hangs them on the owner and takes them off again. A keystone made of stats is
    /// therefore a record — in a tree node or an item catalog — and never a class of its own.
    /// <para>The field language it is written in belongs to <see cref="StatPassiveGrammar"/>, which is also
    /// what the wheel's popup reads a node with: the passive granted and the lines promised beside the node
    /// are one reading, not two that have to be kept in step.</para>
    /// <para>Because the record is the whole passive, its DESCRIPTION is its lines. There is no
    /// <c>&lt;Id&gt;_Description</c> to write: an author names a fresh
    /// <c>Passive_Skill_Stats_Unlimited_Power</c> without writing any code, and a catalog entry nobody
    /// wrote would leave the card echoing a raw key. The lines are worded through the game's own modifier
    /// templates (<see cref="StatPassiveLineText"/>), so what the card says is what the node said.</para>
    /// </summary>
    public sealed class StatPassiveSkill : Skill
    {
        /// <summary>The family this class answers for — <see cref="StatPassiveGrammar.IdPrefix"/> under the
        /// name callers already know it by.</summary>
        public const string IdPrefix = StatPassiveGrammar.IdPrefix;

        /// <summary>Minted once and reused across attach/detach: a scaled line watches its carrier through
        /// the same instance it is counted by, and a fresh mint on every attach would leave the ledger
        /// holding a modifier the owner no longer has.</summary>
        private readonly List<IModifierInstance> _modifiers;

        public StatPassiveSkill(string id, IReadOnlyList<StatPassiveLine> lines) : base(id)
        {
            Lines = lines;
            Magnitude = lines.Sum(line => Math.Abs(line.Value));
            _modifiers = [.. lines.Select(line => Mint(id, line))];
        }

        /// <summary>The lines the record was read into, in the order it wrote them.</summary>
        public IReadOnlyList<StatPassiveLine> Lines { get; }

        /// <summary>How much the passive moves in total, ignoring direction — what tells two registrations
        /// of one id apart, since they differ only when a scaling hand (item ascension) touched the numbers.</summary>
        public float Magnitude { get; }

        /// <summary>Its own lines, worded the way the node that grants them words them. A stat passive has
        /// no rule text of its own to render values into — the values ARE the text.</summary>
        public override string Description =>
            string.Join(StatPassiveLineText.LineSeparator, Lines.Select(line => Localization.Format(line, TextFormat.Rich)));

        /// <summary>Reads the record whole. Refuses rather than trims: see
        /// <see cref="StatPassiveGrammar"/> for the field grammar and what it will not read.</summary>
        public static StatPassiveSkill Create(string id, RecordProperties properties) =>
            new(id, StatPassiveGrammar.Read(id, properties));

        /// <summary>Whether an id is one this class answers for.</summary>
        public static bool Owns(string id) => StatPassiveGrammar.Owns(id);

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
        private static IModifierInstance Mint(string source, StatPassiveLine line) =>
            line.PerParameter is { } carrier
                ? new ScaledByParameterModifier(line.ValueType, line.Parameter, carrier, line.Value, condition: null, source)
                : new SimpleModifier(line.Parameter, line.ValueType, line.Value, source);
    }
}
