namespace Crafting.Source.UIElements.Modules
{
    using System;
    using Core.Enums;
    using Core.Items;
    using Core.Localization;
    using Core.Views.UI;
    using Godot;

    /// <summary>The bench preview: item (or recipe result) header with icon, name and subtitle, and
    /// the modifier line list below. A live item prints its rolled blocks (pickable in Reroll, with
    /// the sharpening preview in Upgrade); a recipe prints its blueprint descriptors with value
    /// spreads. Clicks travel up as events — the window opens pickers and refreshes the pane.</summary>
    [GlobalClass]
    public partial class ItemPreviewPane : VBoxContainer
    {
        private const int GrantDescriptionMinWidth = 320;

        [Export] private Label? _tag;
        [Export] private Control? _itemHeader;
        [Export] private TextureRect? _icon;
        [Export] private Label? _name;
        [Export] private Label? _subtitle;
        [Export] private VBoxContainer? _mods;

        private ModifierFormatter? _formatter;
        private bool _showsItem;

        /// <summary>The item header was clicked while an item is up — the "change item" gesture.</summary>
        public event Action? ChangeItemRequested;

        /// <summary>A rerollable line was picked in Reroll mode; carries the line's instance id.</summary>
        public event Action<string>? ModifierPicked;

        public override void _Ready() => _itemHeader?.GuiInput += OnHeaderInput;

        public void SetFormatter(ModifierFormatter? formatter) => _formatter = formatter;

        /// <summary>The bench item with its full line list for the given mode and picked line.</summary>
        public void ShowItem(IEquipItem item, CraftingMode mode, string? selectedModifierInstanceId)
        {
            _showsItem = true;
            _icon?.Texture = item.Icon;
            SetTitle(item.DisplayName, item.Rarity);
            _subtitle?.Text = $"{Localization.Localize(item.Rarity.ToString())}   +{item.UpdateLevel} / {item.MaxUpdateLevel}";
            BeginLines(visible: true, tagKey: "UI_Craft_Modifiers");
            RenderItemModifiers(item, mode, selectedModifierInstanceId);
        }

        /// <summary>Recipe result preview straight from the blueprint.</summary>
        public void ShowRecipe(string title, Texture2D? icon, EquipItemBlueprint? blueprint)
        {
            _showsItem = false;
            _icon?.Texture = icon;
            SetTitle(title, blueprint?.Rarity);
            _subtitle?.Text = blueprint == null ? string.Empty : BlueprintSubtitle(blueprint);
            BeginLines(visible: blueprint != null, tagKey: "UI_Craft_Result");
            if (blueprint != null) RenderBlueprintModifiers(blueprint);
        }

        /// <summary>Nothing on the bench yet: the pick-a-recipe placeholder.</summary>
        public void ShowEmpty()
        {
            _showsItem = false;
            _icon?.Texture = null;
            SetTitle(Localization.Localize("UI_Craft_PickRecipe"), rarity: null);
            _subtitle?.Text = string.Empty;
            BeginLines(visible: false, tagKey: "UI_Craft_Result");
        }

        /// <summary>The bench item header doubles as a "change item" button in the item modes.</summary>
        private void OnHeaderInput(InputEvent @event)
        {
            if (!_showsItem) return;
            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            ChangeItemRequested?.Invoke();
        }

        private void SetTitle(string text, Rarity? rarity)
        {
            if (_name == null) return;
            _name.Text = text;
            if (rarity == null) _name.RemoveThemeColorOverride("font_color");
            else _name.AddThemeColorOverride("font_color", Color.FromHtml(TextPalette.RarityColor(rarity.Value)));
        }

        /// <summary>A weapon recipe names the actual weapon (type + grip) next to the rarity,
        /// mirroring the item tooltip's subtitle; everything else shows the bare rarity.</summary>
        private static string BlueprintSubtitle(EquipItemBlueprint blueprint) =>
            blueprint.Weapon is { } weapon
                ? $"{Localization.Localize(blueprint.Rarity.ToString())} · {Localization.Localize($"WeaponType_{weapon.WeaponType}")} · {Localization.Localize($"Handedness_{weapon.Handedness}")}"
                : Localization.Localize(blueprint.Rarity.ToString());

        /// <summary>Clears the line list and shows/hides the tagged block under the header.</summary>
        private void BeginLines(bool visible, string tagKey)
        {
            _mods?.QueueFreeChildren();
            _tag?.Visible = visible;
            _mods?.Visible = visible;
            _tag?.Text = Localization.Localize(tagKey);
        }

        /// <summary>Live item lines: parts of one composite roll present as a single row (the row's
        /// id is the first part — the group-reroll target). The rolled rows arrive grouped by slot family
        /// and each family announces itself. Grants show their full description below the name.
        /// Upgrade mode appends the sharpening preview to every scaling part.</summary>
        private void RenderItemModifiers(IEquipItem item, CraftingMode mode, string? selectedModifierInstanceId)
        {
            if (item is IWeaponItem weapon)
            {
                AddWeaponStatRow(EntityParameter.PhysicalDamage, weapon);
                AddWeaponStatRow(EntityParameter.CriticalChance, weapon);
                AddWeaponStatRow(EntityParameter.CriticalDamage, weapon);
            }

            // The typed base channel, folded with its locals — same convention as the item tooltip.
            foreach (var parameter in item.BaseStats.Keys)
            {
                (float baseValue, float localBonus) = item.GetBaseStatBreakdown(parameter);
                AddBaseStatRow(parameter, baseValue + localBonus, highlighted: Mathf.Abs(localBonus) > 0.0001f);
            }

            float? previewScale = mode == CraftingMode.Upgrade && !item.IsSealed && item.UpdateLevel < item.MaxUpdateLevel
                ? item.NextUpgradeValueScale
                : null;
            var format = previewScale == null ? TextFormat.Plain : TextFormat.Rich;

            foreach (var line in EquipItemLines.ComposeImplicits(item, format, previewScale))
                _mods?.AddChild(LineRow(line.Text, rich: previewScale != null, dim: true));

            AffixKind? block = null;
            foreach (var line in EquipItemLines.ComposeRolled(item, format, previewScale))
            {
                if (line.Affix != block)
                {
                    block = line.Affix;
                    var header = ItemLineRows.AffixHeader(line.Affix);
                    if (header != null) _mods?.AddChild(header);
                }

                _mods?.AddChild(previewScale != null
                    ? LineRow(line.Text, rich: true, dim: false)
                    : RerollableOrLabel(item, mode, line.InstanceId, selectedModifierInstanceId, line.Text));
            }

            foreach (var grant in item.Grants)
                AddGrantBlock(grant);
        }

        /// <summary>An item line row: a plain Label normally, a RichTextLabel when the text carries
        /// the colored upgrade preview (BBCode). Dim rows keep their tone via a default_color override —
        /// theme variations only target Label.</summary>
        private static Control LineRow(string text, bool rich, bool dim)
        {
            if (!rich)
                return new Label { Text = text, ThemeTypeVariation = dim ? "DimLabel" : null, AutowrapMode = TextServer.AutowrapMode.WordSmart };

            var row = RichText(text);
            row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            if (dim) row.AddThemeColorOverride("default_color", Color.FromHtml(TextPalette.Muted));
            return row;
        }

        /// <summary>A grant on the bench mirrors the tooltip's Effect block: the name, then the rendered
        /// rich description below (a plain Label would print the raw BBCode tags).</summary>
        private void AddGrantBlock(IItemGrant grant)
        {
            _mods?.AddChild(new Label { Text = Localization.Localize(grant.Id), AutowrapMode = TextServer.AutowrapMode.WordSmart });
            if (string.IsNullOrEmpty(grant.Description)) return;
            var description = RichText(grant.Description);
            description.CustomMinimumSize = new Vector2(GrantDescriptionMinWidth, 0);
            description.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _mods?.AddChild(description);
        }

        /// <summary>Recipe result preview straight from the blueprint: a weapon opens with its base
        /// combat stats, then descriptors with value spreads render through the range templates,
        /// fixed lines as usual.</summary>
        private void RenderBlueprintModifiers(EquipItemBlueprint blueprint)
        {
            if (blueprint.Weapon is { } weapon)
            {
                AddBaseStatRow(EntityParameter.PhysicalDamage, weapon.Damage);
                AddBaseStatRow(EntityParameter.CriticalChance, weapon.CriticalChance);
                AddBaseStatRow(EntityParameter.CriticalDamage, weapon.CriticalDamage);
            }

            // Unrolled base stats show their roll spread; a fixed base shows the single value.
            foreach (var stat in blueprint.BaseStats)
                AddBaseStatSpreadRow(stat);

            foreach (var descriptor in blueprint.Implicits)
                _mods?.AddChild(new Label { Text = Localization.Format(descriptor), ThemeTypeVariation = "DimLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart });

            foreach (var descriptor in blueprint.Modifiers)
                _mods?.AddChild(new Label { Text = Localization.Format(descriptor), AutowrapMode = TextServer.AutowrapMode.WordSmart });

            foreach (var grant in blueprint.Grants)
                _mods?.AddChild(new Label { Text = Localization.Localize(grant.Id), AutowrapMode = TextServer.AutowrapMode.WordSmart });
        }

        /// <summary>A bench-item weapon stat: the effective value with local lines folded in; a
        /// locally modified stat glows brighter — same convention as the item tooltip.</summary>
        private void AddWeaponStatRow(EntityParameter parameter, IWeaponItem weapon)
        {
            (float baseValue, float localBonus) = weapon.GetStatBreakdown(parameter);
            AddBaseStatRow(parameter, baseValue + localBonus, highlighted: Mathf.Abs(localBonus) > 0.0001f);
        }

        /// <summary>Blueprint base-stat row: the roll spread ("500–900") instead of a value — the
        /// bench previews what the mint can produce, not a concrete roll.</summary>
        private void AddBaseStatSpreadRow(BaseStatBlueprint stat)
        {
            string Bound(float value) =>
                _formatter?.FormatValue(ModifierValueType.Flat, stat.Parameter, value)
                ?? value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

            var row = new HBoxContainer();
            row.AddChild(new Label
            {
                Text = Localization.Localize(stat.Parameter.ToString()),
                ThemeTypeVariation = "DimLabel",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });
            var valueLabel = new Label
            {
                Text = stat.Value.IsFixed ? Bound(stat.Value.Min) : $"{Bound(stat.Value.Min)}–{Bound(stat.Value.Max)}",
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            valueLabel.AddThemeColorOverride("font_color", Color.FromHtml(TextPalette.BaseStat));
            row.AddChild(valueLabel);
            _mods?.AddChild(row);
        }

        /// <summary>One base-stat row: dim parameter name, unit-aware value on the right.</summary>
        private void AddBaseStatRow(EntityParameter parameter, float value, bool highlighted = false)
        {
            var row = new HBoxContainer();
            row.AddChild(new Label
            {
                Text = Localization.Localize(parameter.ToString()),
                ThemeTypeVariation = "DimLabel",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });
            var valueLabel = new Label
            {
                Text = _formatter?.FormatValue(ModifierValueType.Flat, parameter, value) ?? value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture),
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            valueLabel.AddThemeColorOverride("font_color", Color.FromHtml(highlighted ? TextPalette.Number : TextPalette.BaseStat));
            row.AddChild(valueLabel);
            _mods?.AddChild(row);
        }

        /// <summary>In Recraft every additional row — entity, context or grouped — is pickable; the chosen
        /// one gets rerolled. A sealed item (unique/mythic) offers no rows at all — nothing on it rerolls.</summary>
        private Control RerollableOrLabel(IEquipItem item, CraftingMode mode, string instanceId, string? selectedModifierInstanceId, string text) =>
            mode == CraftingMode.Recraft && item is { IsSealed: false }
                ? RerollableRow(instanceId, selectedModifierInstanceId, text)
                : new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };

        private Button RerollableRow(string instanceId, string? selectedModifierInstanceId, string text)
        {
            var row = new Button
            {
                Text = (selectedModifierInstanceId == instanceId ? "» " : string.Empty) + text,
                Alignment = HorizontalAlignment.Left,
                FocusMode = FocusModeEnum.None,
                ClipText = true,
            };
            row.Pressed += () => ModifierPicked?.Invoke(instanceId);
            return row;
        }

        /// <summary>Shared BBCode label defaults; callers add size flags/minimums on top.</summary>
        private static RichTextLabel RichText(string text, TextServer.AutowrapMode autowrap = TextServer.AutowrapMode.WordSmart) =>
            new() { Text = text, BbcodeEnabled = true, FitContent = true, AutowrapMode = autowrap };
    }
}
