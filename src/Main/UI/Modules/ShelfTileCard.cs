namespace LastBreath.UI.Modules
{
    using System;
    using System.Collections.Generic;
    using Core.Localization;
    using Godot;

    /// <summary>
    /// One showcase tile as a scene: the ItemSlotPanel-style frame, the centered icon, the name in
    /// the rarity color, the price line and the xN counter. The shelf only instantiates and fills
    /// it — fonts and layout live in <c>ShelfTileCard.tscn</c>. The frame is either a solid border
    /// in the rarity color, or the hand-drawn dashed outline (rotation slots, empty buyback sockets).
    /// </summary>
    public partial class ShelfTileCard : PanelContainer
    {
        // The uid rather than the path, matching the SharedUi elements' loading convention.
        private const string UID = "uid://bq2vhx8mtile3";

        private const string PriceKey = "UI_Trade_Price";
        private static readonly Color s_neutralLine = new(0.42f, 0.341f, 0.188f); // Umbral GoldBorder

        [Export] private Control? _dashed;
        [Export] private Control? _layout;
        [Export] private TextureRect? _icon;
        [Export] private Label? _caption;
        [Export] private Label? _price;
        [Export] private Control? _countOverlay;
        [Export] private Label? _count;

        private Color _line = s_neutralLine;
        private StyleBox? _basePanel;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        /// <summary>The tile was left-clicked; the shelf translates it into the offer id.</summary>
        public event Action? Clicked;

        /// <summary>The scene's own panel stylebox, captured before any per-tile border override.</summary>
        private StyleBox? BasePanel => _basePanel ??= GetThemeStylebox("panel");

        public override void _Ready()
        {
            GuiInput += OnGuiInput;
            if (_dashed == null) return;
            _dashed.Draw += DrawDashes;
            _dashed.Resized += _dashed.QueueRedraw;
        }

        /// <summary>Fills the tile with an offer; the rarity color tints the name, the border and
        /// the dashed outline alike. The solid rarity border is the default — <see cref="SetDashed"/>
        /// swaps it for the dashed one.</summary>
        public void SetOffer(Texture2D? icon, string name, Color rarityColor, int unitPrice, int count)
        {
            _line = rarityColor;
            _icon?.Texture = icon;
            if (_caption != null)
            {
                _caption.Text = name;
                _caption.AddThemeColorOverride("font_color", rarityColor);
            }

            _price?.Text = Localization.Render(PriceKey, new Dictionary<string, object?> { ["Amount"] = unitPrice });
            _countOverlay?.Visible = count > 1;
            _count?.Text = $"x{count}";
            SetDashed(false);
        }

        /// <summary>Dashed mode: the frame loses its solid border and draws the dashed outline
        /// instead (the rotation slots of the special deliveries).</summary>
        public void SetDashed(bool dashed)
        {
            if (dashed) AddThemeStyleboxOverride("panel", BasePanel);
            else ApplyBorder(_line);
            _dashed?.Visible = dashed;
            _dashed?.QueueRedraw();
        }

        /// <summary>An empty buyback socket: no content, no clicks, the neutral dashed outline.</summary>
        public void SetEmpty()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            _layout?.Visible = false;
            _countOverlay?.Visible = false;
            _line = s_neutralLine;
            _dashed?.Visible = true;
            _dashed?.QueueRedraw();
        }

        private void ApplyBorder(Color color)
        {
            if (BasePanel is not StyleBoxFlat flat) return;
            var bordered = (StyleBoxFlat)flat.Duplicate();
            bordered.SetBorderWidthAll(1);
            bordered.BorderColor = color;
            AddThemeStyleboxOverride("panel", bordered);
        }

        private void OnGuiInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            Clicked?.Invoke();
            AcceptEvent();
        }

        /// <summary>The hairline the theme cannot draw: the dashed outline traced over the frame's
        /// content rect — the same inset the code-built tiles used.</summary>
        private void DrawDashes()
        {
            if (_dashed == null) return;
            var end = _dashed.Size - Vector2.One;
            _dashed.DrawDashedLine(Vector2.One, new Vector2(end.X, 1), _line);
            _dashed.DrawDashedLine(new Vector2(end.X, 1), end, _line);
            _dashed.DrawDashedLine(end, new Vector2(1, end.Y), _line);
            _dashed.DrawDashedLine(new Vector2(1, end.Y), Vector2.One, _line);
        }
    }
}
