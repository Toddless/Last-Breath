namespace PassiveTreeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Godot;
    using Simulation;

    /// <summary>
    /// The character the totals are measured against. Defaults to the unarmed player; editing a row
    /// answers "what does this cluster do on top of that gear" without touching the tree itself.
    /// </summary>
    public partial class BaseStatsPanel : VBoxContainer
    {
        private BaseStatProfile _profile = new();
        private BaseStatProfile _baseline = new();

        public event Action? ProfileChanged;

        public BaseStatProfile Profile => _profile;

        /// <summary>The profile to edit and the baseline "reset" returns to. Both come from the data
        /// catalog, so the panel never carries its own copy of the game's numbers.</summary>
        public void Initialize(BaseStatProfile profile, BaseStatProfile baseline)
        {
            _profile = profile;
            _baseline = baseline;
            Rebuild();
        }

        /// <summary>What this session actually changed. A value still equal to the baseline is left out, so
        /// a later edit to the PlayerStats catalog reaches an untouched stat instead of being shadowed by a
        /// stored copy of its own old value.</summary>
        public BaseStatProfile Overrides()
        {
            var overrides = new BaseStatProfile();

            // Approximate, not exact: the value round-trips through a double-valued SpinBox, so nudging
            // a stat and putting it back must not register as an edit.
            foreach (KeyValuePair<EntityParameter, float> pair in _profile.Values)
                if (!Mathf.IsEqualApprox(pair.Value, _baseline[pair.Key]))
                    overrides[pair.Key] = pair.Value;

            return overrides;
        }

        public void Rebuild()
        {
            this.ClearContent();

            SizeFlagsHorizontal = SizeFlags.ExpandFill;

            AddChild(EditorControls.Caption("BASE VALUES"));
            AddChild(EditorControls.Wrapped("Seeded from the unarmed player profile."));

            var reset = new Button { Text = "Reset to unarmed" };
            reset.Pressed += () =>
            {
                // A copy per reset: the baseline has to survive being edited afterwards.
                _profile = _baseline.Copy();
                Rebuild();
                ProfileChanged?.Invoke();
            };
            AddChild(reset);

            var grid = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            AddChild(grid);

            foreach (EntityParameter parameter in Enum.GetValues<EntityParameter>())
            {
                // Aggregates are buckets, never values: they have no base of their own.
                if (AggregateParameters.IsAggregate(parameter)) continue;

                grid.AddChild(new Label { Text = parameter.ToString() });

                SpinBox box = EditorControls.Number(_profile[parameter], 0.01);

                EntityParameter captured = parameter;
                box.ValueChanged += value =>
                {
                    _profile[captured] = (float)value;
                    ProfileChanged?.Invoke();
                };

                grid.AddChild(box);
            }
        }
    }
}
