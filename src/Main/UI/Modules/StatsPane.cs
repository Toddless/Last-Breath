namespace LastBreath.UI.Modules
{
    using Core.Entity;
    using Core.Enums;
    using Core.Localization;
    using Core.Views.UI;
    using Godot;
    using SharedUi;

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

        [Export] private Label? _header;
        [Export] private VBoxContainer? _stats;
        [Export] private Label? _fireResist, _coldResist, _lightningResist, _poisonResist;

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

        /// <summary>One stat row — the shared <see cref="KeyValueRow"/> scene in its plain-value tone.</summary>
        private void AddStatRow(string name, string value)
        {
            var row = KeyValueRow.Initialize().Instantiate<KeyValueRow>();
            row.Set(name, value);
            row.UsePlainValue();
            _stats?.AddChild(row);
        }

        /// <summary>The four resist chips are authored in the scene — only their texts update.</summary>
        private void RenderResists(IPlayer player)
        {
            SetResist(_fireResist, EntityParameter.FireResistance, player);
            SetResist(_coldResist, EntityParameter.ColdResistance, player);
            SetResist(_lightningResist, EntityParameter.LightningResistance, player);
            SetResist(_poisonResist, EntityParameter.PoisonResistance, player);
        }

        private void SetResist(Label? label, EntityParameter parameter, IPlayer player) =>
            label?.Text = $"{Localization.Localize(parameter.ToString())} {FormatValue(parameter, player)}";

        private string FormatValue(EntityParameter parameter, IPlayer player) =>
            ParameterValueText.Format(_formats, parameter, player.Parameters);
    }
}
