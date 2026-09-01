namespace LastBreath.UI.Modules
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Items;
    using Core.Localization;
    using Godot;

    /// <summary>
    /// The bag filter bar: search field, one coloured chip per rarity and the type dropdown. The
    /// bar owns the filter state and the match rule; it only reports that the filter changed —
    /// what to do with the verdict is the window's business.
    /// </summary>
    public partial class FilterBar : HBoxContainer
    {
        private const int ChipSide = 26;
        private const float ChipFillAlpha = 0.35f;

        private static readonly Rarity[] s_chipOrder =
            [Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Legendary, Rarity.Unique, Rarity.Mythic];

        private static readonly string[] s_typeKeys =
            ["UI_Inv_AllTypes", "UI_Inv_Type_Weapon", "UI_Inv_Type_Armor", "UI_Inv_Type_Jewellery", "UI_Inv_Type_Resources"];

        [Export] private LineEdit? _search;
        [Export] private HBoxContainer? _chips;
        [Export] private OptionButton? _typeFilter;

        private readonly HashSet<Rarity> _rarityFilter = [];
        private readonly Dictionary<Rarity, Button> _chipButtons = [];
        private string _query = string.Empty;
        private int _typeIndex;

        public event Action? FilterChanged;

        /// <summary>Whether any of the three filters narrows the bag right now.</summary>
        public bool FilterActive => _query.Length > 0 || _rarityFilter.Count > 0 || _typeIndex > 0;

        public override void _Ready()
        {
            _search?.PlaceholderText = Localization.Localize("UI_Inv_Search");
            _search?.TextChanged += text => { _query = text; FilterChanged?.Invoke(); };
            _typeFilter?.ItemSelected += index => { _typeIndex = (int)index; FilterChanged?.Invoke(); };

            BuildTypeOptions();
            BuildRarityChips();
        }

        /// <summary>Whether the item passes the current search/rarity/type filter.</summary>
        public bool Matches(IItem item)
        {
            if (_query.Length > 0 && !item.DisplayName.Contains(_query, StringComparison.OrdinalIgnoreCase)) return false;
            if (_rarityFilter.Count > 0 && !_rarityFilter.Contains(item.Rarity)) return false;
            return MatchesType(item);
        }

        /// <summary>Puts every filter back to "show everything".</summary>
        public void Reset()
        {
            _query = string.Empty;
            _search?.Text = string.Empty;
            _typeIndex = 0;
            _typeFilter?.Selected = 0;
            ClearRarityFilter();
        }

        private void BuildTypeOptions()
        {
            if (_typeFilter == null) return;
            _typeFilter.Clear();
            foreach (string key in s_typeKeys)
                _typeFilter.AddItem(Localization.Localize(key));
            _typeFilter.Selected = 0;
        }

        /// <summary>An "all" pill plus one coloured dot per rarity. No pressed dot = no rarity filter.</summary>
        private void BuildRarityChips()
        {
            if (_chips == null) return;

            var all = new Button { Text = Localization.Localize("UI_Inv_All"), FocusMode = FocusModeEnum.None };
            all.Pressed += ClearRarityFilter;
            _chips.AddChild(all);

            foreach (var rarity in s_chipOrder)
            {
                var chip = MakeChip(Color.FromHtml(TextPalette.RarityColor(rarity)), rarity.ToString());
                chip.Toggled += pressed =>
                {
                    if (pressed) _rarityFilter.Add(rarity);
                    else _rarityFilter.Remove(rarity);
                    FilterChanged?.Invoke();
                };
                _chipButtons[rarity] = chip;
                _chips.AddChild(chip);
            }
        }

        private static Button MakeChip(Color color, string tooltip)
        {
            var chip = new Button
            {
                ToggleMode = true,
                FocusMode = FocusModeEnum.None,
                CustomMinimumSize = new Vector2(ChipSide, ChipSide),
                TooltipText = tooltip,
            };

            var normal = new StyleBoxFlat { BgColor = new Color(color.R, color.G, color.B, ChipFillAlpha) };
            normal.SetBorderWidthAll(1);
            normal.BorderColor = color;
            var pressed = new StyleBoxFlat { BgColor = color };
            pressed.SetBorderWidthAll(2);
            pressed.BorderColor = Colors.White;

            chip.AddThemeStyleboxOverride("normal", normal);
            chip.AddThemeStyleboxOverride("hover", pressed);
            chip.AddThemeStyleboxOverride("pressed", pressed);
            chip.AddThemeStyleboxOverride("hover_pressed", pressed);
            return chip;
        }

        private void ClearRarityFilter()
        {
            _rarityFilter.Clear();
            foreach (var chip in _chipButtons.Values)
                chip.SetPressedNoSignal(false);
            FilterChanged?.Invoke();
        }

        private bool MatchesType(IItem item) => _typeIndex switch
        {
            1 => item is IEquipItem { EquipmentPiece: EquipmentPiece.Weapon },
            2 => item is IEquipItem
            {
                EquipmentPiece: EquipmentPiece.Body or EquipmentPiece.Belt or EquipmentPiece.Gloves
                or EquipmentPiece.Boots or EquipmentPiece.Helmet or EquipmentPiece.Cloak,
            },
            3 => item is IEquipItem { EquipmentPiece: EquipmentPiece.Amulet or EquipmentPiece.Ring },
            4 => item is not IEquipItem,
            _ => true,
        };
    }
}
