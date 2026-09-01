namespace Crafting.Source.UIElements.Modules
{
    using System.Collections.Generic;
    using Core.Localization;
    using Core.Views.UI;
    using Godot;
    using SharedUi;

    /// <summary>One forecast row: an already localized channel name and its BBCode value.</summary>
    public sealed record ForecastLineView(string Name, string ValueBbcode);

    /// <summary>The craft forecast under the resource slots: for the current operation the abstract
    /// mastery channels turn into concrete odds. The window computes the lines; the panel only
    /// prints them, hiding itself when there is nothing to say.</summary>
    [GlobalClass]
    public partial class ForecastPanel : PanelContainer
    {
        private const string HeaderKey = "UI_Craft_Forecast";
        private const string HintKey = "UI_Craft_Forecast_Hint";
        private const int ValueWidth = 130;

        [Export] private Label? _header;
        [Export] private Label? _hint;
        [Export] private VBoxContainer? _rows;

        public override void _Ready()
        {
            _header?.Text = Localization.Localize(HeaderKey).ToUpper();
            _hint?.Text = Localization.Localize(HintKey);
        }

        /// <summary>Repaints the forecast rows; an empty list hides the whole box.</summary>
        public void SetForecast(IReadOnlyList<ForecastLineView> lines)
        {
            if (_rows == null) return;

            _rows.QueueFreeChildren();
            Visible = lines.Count > 0;
            foreach (var line in lines)
                _rows.AddChild(Row(line.Name, line.ValueBbcode));
        }

        private static Control Row(string name, string valueBbcode)
        {
            var row = KeyValueRow.Initialize().Instantiate<KeyValueRow>();
            row.SetRich(name, valueBbcode, ValueWidth);
            row.EnableCaptionAutowrap();
            return row;
        }
    }
}
