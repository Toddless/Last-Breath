namespace LastBreath.Script.UI.View
{
    using System;
    using Godot;

    public partial class FractionReputation : HBoxContainer
    {
        private const string Path = "res://Script/UI/FractionReputation.tscn";
        private TextureRect? _icon;
        private Label? _label;
        private TextureProgressBar? _negativeRep;
        private TextureProgressBar? _positiveRep;

        public event Action<int>? UpdatePositive, UpdateNegative;
        public event Action<string>? UpdateRank;

        public override void _Ready()
        {
            _icon = GetNode<TextureRect>("Icon");
            _label = GetNode<Label>(nameof(Label));
            _negativeRep = GetNode<TextureProgressBar>("Negative");
            _positiveRep = GetNode<TextureProgressBar>("Positive");
        }

        public void InitialValues(int positive, int negative, string? rank)
        {
            _label!.Text = rank;
            _positiveRep!.Value = positive;
            _negativeRep!.Value = negative;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(Path);
    }
}
