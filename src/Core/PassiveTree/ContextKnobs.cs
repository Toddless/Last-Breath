namespace Core.PassiveTree
{
    using System;
    using System.Collections.Generic;
    using Enums;
    using Modifiers;
    using Modifiers.Context;

    /// <summary>
    /// Which context knobs actually reach a battle pipeline. The answer comes from the binding table
    /// itself instead of being restated as a second list, so wiring a knob — or dropping it — needs no
    /// edit here. Authoring leans on it twice: the editor offers only knobs that are wired, and the
    /// reader refuses a line naming one that is not. An unbound knob throws the moment it is attached,
    /// which is a failure a data file must not be able to plant.
    /// </summary>
    public static class ContextKnobs
    {
        private static readonly List<ContextParameter> s_bound = Probe();

        /// <summary>The wired knobs, in declaration order — the pick list of the authoring tools.</summary>
        public static IReadOnlyList<ContextParameter> Bound => s_bound;

        /// <summary>Also false for a value outside the enum: a numeric "parameter" in a file parses into
        /// a member that does not exist, and it has no binding either.</summary>
        public static bool IsBound(ContextParameter parameter) => s_bound.Contains(parameter);

        /// <summary>Builds every knob's binding once and keeps the ones that exist. Nothing is attached
        /// to an entity, so the probe leaves no trace on anything.</summary>
        private static List<ContextParameter> Probe()
        {
            var bound = new List<ContextParameter>();

            foreach (ContextParameter parameter in Enum.GetValues<ContextParameter>())
            {
                try
                {
                    ContextModifierBindings.Create(new ContextModifierEntry(parameter, ModifierValueType.Increase, 0f));
                }
                catch (NotSupportedException)
                {
                    // A knob nobody wired: it stays out of the list and out of every authoring surface.
                    continue;
                }

                bound.Add(parameter);
            }

            return bound;
        }
    }
}
