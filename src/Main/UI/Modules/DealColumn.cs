namespace LastBreath.UI.Modules
{
    using System;
    using System.Collections.Generic;
    using Core.Localization;
    using Godot;

    /// <summary>What the deal card shows about the chosen item; the direction (buy/sell) is the
    /// window's call, made by which Show* it invokes.</summary>
    public record DealItemView(Texture2D? Icon, string DisplayName, Color NameColor, int UnitPrice, int MaxAmount);

    /// <summary>
    /// The middle "deal" column: empty it explains itself, with an item it shows the card, the
    /// quantity spinner (stackables), the running total and the one confirm button. The column
    /// gates the button on the gold it is told about and reports the confirmed quantity up —
    /// the request handlers stay the real gates.
    /// </summary>
    public partial class DealColumn : VBoxContainer
    {
        public const string PickHintKey = "UI_Trade_PickHint";
        public const string DirectionHintKey = "UI_Trade_DirectionHint";
        public const string BuyKey = "UI_Trade_Buy";
        public const string SellKey = "UI_Trade_Sell";
        public const string TotalKey = "UI_Trade_Total";
        public const string NotEnoughGoldKey = "UI_Trade_NotEnoughGold";
        public const string NoPriceKey = "UI_Trade_NoPrice";
        private const string PriceKey = "UI_Trade_Price";

        [Export] private Control? _emptyState, _dealState;
        [Export] private Label? _pickHint, _directionHint;
        [Export] private TextureRect? _icon;
        [Export] private Label? _name, _unitPrice, _total, _reason;
        [Export] private SpinBox? _amount;
        [Export] private Button? _confirm;

        private bool _isPurchase;
        private int _unitPriceValue;
        private int _gold;

        /// <summary>The confirm press with the chosen quantity; the window turns it into a request.</summary>
        public event Action<int>? Confirmed;

        public override void _Ready()
        {
            _pickHint?.Text = Localization.Localize(PickHintKey);
            _directionHint?.Text = Localization.Localize(DirectionHintKey);
            _confirm?.Pressed += () => Confirmed?.Invoke(Amount);
            _amount?.ValueChanged += _ => RefreshTotals();
        }

        public void ShowEmpty()
        {
            _emptyState?.Visible = true;
            _dealState?.Visible = false;
        }

        public void ShowPurchase(DealItemView view) => Show(view, isPurchase: true);

        public void ShowSale(DealItemView view) => Show(view, isPurchase: false);

        /// <summary>The wallet as the window last heard it — re-gates a shown purchase in place.</summary>
        public void SetAvailableGold(int gold)
        {
            _gold = gold;
            if (_dealState?.Visible == true) RefreshTotals();
        }

        private int Amount => (int)(_amount?.Value ?? 1);

        private void Show(DealItemView view, bool isPurchase)
        {
            _isPurchase = isPurchase;
            _unitPriceValue = view.UnitPrice;

            _icon?.Texture = view.Icon;
            if (_name != null)
            {
                _name.Text = view.DisplayName;
                _name.AddThemeColorOverride("font_color", view.NameColor);
            }

            _unitPrice?.Text = Localization.Render(PriceKey, new Dictionary<string, object?> { ["Amount"] = view.UnitPrice });
            if (_amount != null)
            {
                _amount.MinValue = 1;
                _amount.MaxValue = Math.Max(1, view.MaxAmount);
                _amount.SetValueNoSignal(1);
                _amount.Visible = view.MaxAmount > 1;
            }

            _confirm?.Text = Localization.Localize(isPurchase ? BuyKey : SellKey);
            _emptyState?.Visible = false;
            _dealState?.Visible = true;
            RefreshTotals();
        }

        /// <summary>Mirrors availability only (unpriced item, thin wallet) — the buy/sell handlers
        /// are the gates that actually refuse.</summary>
        private void RefreshTotals()
        {
            int total = _unitPriceValue * Amount;
            _total?.Text = Localization.Render(TotalKey, new Dictionary<string, object?> { ["Amount"] = total });

            bool priced = _unitPriceValue > 0;
            bool affordable = !_isPurchase || total <= _gold;
            _confirm?.Disabled = !priced || !affordable;
            if (_reason != null)
            {
                _reason.Visible = !priced || !affordable;
                _reason.Text = Localization.Localize(priced ? NotEnoughGoldKey : NoPriceKey);
            }
        }
    }
}
