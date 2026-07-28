namespace PassiveTreeEditor.Source.View
{
    using System;
    using Core.Enums;
    using Godot;
    using Simulation;

    /// <summary>
    /// The character the totals are measured against. Defaults to the unarmed player; editing a row
    /// answers "what does this cluster do on top of that gear" without touching the tree itself.
    /// </summary>
    public partial class BaseStatsPanel : VBoxContainer
    {
        private BaseStatProfile _profile = BaseStatProfile.Unarmed();

        public event Action? ProfileChanged;

        public BaseStatProfile Profile => _profile;

        public void SetProfile(BaseStatProfile profile)
        {
            _profile = profile;
            Rebuild();
        }

        public void Rebuild()
        {
            foreach (Node child in GetChildren())
            {
                RemoveChild(child);
                child.QueueFree();
            }

            SizeFlagsHorizontal = SizeFlags.ExpandFill;

            var caption = new Label { Text = "BASE VALUES" };
            caption.AddThemeFontSizeOverride("font_size", 13);
            AddChild(caption);

            AddChild(new Label
            {
                Text = "Seeded from the unarmed player profile.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            });

            var reset = new Button { Text = "Reset to unarmed" };
            reset.Pressed += () =>
            {
                _profile = BaseStatProfile.Unarmed();
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

                var box = new SpinBox
                {
                    MinValue = -100000,
                    MaxValue = 100000,
                    Step = 0.01,
                    Value = _profile[parameter],
                    SizeFlagsHorizontal = SizeFlags.ExpandFill
                };

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
