namespace LastBreath.UI.Modules
{
    using Core.Entity;
    using Core.Enums;
    using Core.Localization;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The key-stat readout under the paperdoll: header, the stat rows and the resist chips. The
    /// pane rebuilds whole from the player it is handed — small enough that nothing is diffed.
    /// </summary>
    public partial class StatsPane : VBoxContainer
    {
        private static readonly EntityParameter[] s_statParameters =
        [
            EntityParameter.PhysicalDamage,
            EntityParameter.CriticalChance,
            EntityParameter.CriticalDamage,
            EntityParameter.Armor,
            EntityParameter.Evade
        ];

        private static readonly EntityParameter[] s_resistParameters =
        [
            EntityParameter.FireResistance, EntityParameter.ColdResistance, EntityParameter.LightningResistance,
            EntityParameter.PoisonResistance
        ];

        [Export] private Label? _header;
        [Export] private VBoxContainer? _stats;
        [Export] private Container? _resists;

        private IParameterFormatProvider? _formats;

        public override void _Ready() => _header?.Text = Localization.Localize("UI_Inv_Stats");

        public void SetFormats(IParameterFormatProvider? formats) => _formats = formats;

        /// <summary>Rebuilds the stat rows and resist chips from the player's live parameters.</summary>
        public void Refresh(IPlayer player)
        {
            RenderStats(player);
            RenderResists(player);
        }

        private void RenderStats(IPlayer player)
        {
            if (_stats == null) return;
            _stats.QueueFreeChildren();

            AddStatRow(Localization.Localize("Health"), $"{Mathf.CeilToInt(player.CurrentHealth)} / {Mathf.CeilToInt(player.Parameters.MaxHealth)}");
            foreach (var parameter in s_statParameters)
                AddStatRow(Localization.Localize(parameter.ToString()), FormatValue(parameter, player));
        }

        private void AddStatRow(string name, string value)
        {
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = name, ThemeTypeVariation = "DimLabel", SizeFlagsHorizontal = SizeFlags.ExpandFill });
            row.AddChild(new Label { Text = value, HorizontalAlignment = HorizontalAlignment.Right });
            _stats?.AddChild(row);
        }

        private void RenderResists(IPlayer player)
        {
            if (_resists == null) return;
            _resists.QueueFreeChildren();

            foreach (var parameter in s_resistParameters)
            {
                var chip = new PanelContainer();
                chip.AddChild(new Label
                {
                    Text = $"{Localization.Localize(parameter.ToString())} {FormatValue(parameter, player)}",
                    ThemeTypeVariation = "DimLabel",
                });
                _resists.AddChild(chip);
            }
        }

        private string FormatValue(EntityParameter parameter, IPlayer player) =>
            ParameterValueText.Format(_formats, parameter, player.Parameters);
    }
}
