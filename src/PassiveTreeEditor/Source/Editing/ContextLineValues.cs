namespace PassiveTreeEditor.Source.Editing
{
    using System.Runtime.CompilerServices;
    using Core.Enums;
    using Core.Modifiers.Context;
    using Core.PassiveTree;

    /// <summary>
    /// What a context line said as a number, held for as long as its knob makes it a switch.
    /// <para>The knob decides which kind of line it takes, so pointing a line at a switch knob turns the
    /// line into one — and a switch has nowhere to keep either half of what was authored: its value is
    /// pinned to one, and the bucket the number was written in (Flat, Increase, Multiplicative) has no
    /// meaning without a number. Walking a line through a switch and back is a thing an author does
    /// while looking for the right knob, and it must not cost them the line: "+15%" has to come back
    /// as "+15%", not as a fresh Increase of nothing and not as the pinned one read as a quantity.</para>
    /// <para>Keyed by the line object and held weakly, so a deleted line takes its memory with it and
    /// nothing here needs telling when a node goes away.</para>
    /// </summary>
    public sealed class ContextLineValues
    {
        private readonly ConditionalWeakTable<ContextModifierLine, Authored> _authored = new();

        /// <summary>
        /// Points the line at another knob and settles what kind of line that makes it. The knob owns
        /// the answer — a switch knob takes nothing but a switch, a knob that reads an amount takes
        /// nothing else — so the line follows the knob rather than the other way round, and what it
        /// said as a number is put aside on the way in and handed back on the way out. A knob that
        /// accepts the line as it stands leaves it alone: re-picking a bucket the author chose would
        /// undo an edit nobody asked to undo.
        /// <para>Deciding it here and not in the control that calls it is what makes the rule readable:
        /// what a knob change costs the author is a plain method over plain data, not a branch inside a
        /// widget callback.</para>
        /// </summary>
        public void Retarget(ContextModifierLine line, ContextParameter parameter)
        {
            Remember(line);
            line.Parameter = parameter;

            if (ContextKnobs.WhyRefused(parameter, line.ValueType) is null) return;

            if (ContextKnobs.IsFlag(parameter)) line.ValueType = ModifierValueType.Flag;
            else Restore(line);
        }

        /// <summary>Takes down what the line says. A line that is already a switch says nothing worth
        /// recording, and the memory it made on the way in is the one worth keeping.</summary>
        private void Remember(ContextModifierLine line)
        {
            if (line.IsFlag) return;

            _authored.AddOrUpdate(line, new Authored(line.ValueType, line.Value));
        }

        /// <summary>Puts the number and its bucket back on a line whose knob reads one again. A line
        /// with no history here — one that arrived from the file already a switch — becomes a plain
        /// Increase, which is the shape most tree lines take.</summary>
        private void Restore(ContextModifierLine line)
        {
            if (_authored.TryGetValue(line, out Authored? authored))
            {
                line.ValueType = authored.ValueType;
                line.Value = authored.Value;
                return;
            }

            line.ValueType = ModifierValueType.Increase;
        }

        private sealed record Authored(ModifierValueType ValueType, float Value);
    }
}
