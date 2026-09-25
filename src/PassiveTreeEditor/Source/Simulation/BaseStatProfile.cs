namespace PassiveTreeEditor.Source.Simulation
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.PassiveTree.Summary;

    /// <summary>
    /// The character the tree is measured on top of. Seeded from the <c>PlayerStats</c> data catalog
    /// and editable, so a cluster can be judged against real gear instead of a naked character.
    /// </summary>
    public sealed class BaseStatProfile : IParameterBaseline
    {
        private readonly Dictionary<EntityParameter, float> _values = [];

        public float this[EntityParameter parameter]
        {
            get => _values.GetValueOrDefault(parameter);
            set => _values[parameter] = value;
        }

        public IReadOnlyDictionary<EntityParameter, float> Values => _values;

        /// <summary>The summator's one question, answered from the edited profile.</summary>
        public float Of(EntityParameter parameter) => this[parameter];

        /// <summary>An editable profile seeded from a read-only data baseline (the PlayerStats catalog).</summary>
        public static BaseStatProfile From(IReadOnlyDictionary<EntityParameter, float> values)
        {
            var profile = new BaseStatProfile();
            foreach (KeyValuePair<EntityParameter, float> pair in values) profile._values[pair.Key] = pair.Value;

            return profile;
        }

        /// <summary>An independent profile with the same values. A baseline that is handed out is
        /// edited in place by the panel, so it has to stop being the baseline as it leaves.</summary>
        public BaseStatProfile Copy()
        {
            var copy = new BaseStatProfile();
            foreach (KeyValuePair<EntityParameter, float> pair in _values) copy._values[pair.Key] = pair.Value;

            return copy;
        }
    }
}
