namespace Crafting.Internal.UIElements
{
    using System;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.UI;
    using Crafting.Source;
    using Crafting.Source.UIElements;
    using Godot;
    using Utilities;

    [GlobalClass]
    public partial class MasteryStats : Control, IInitializable, IRequireServices
    {
        private const string UID = "uid://dgkdi4yraut0h";

        private Dictionary<string, InteractiveLabel> _labels = [];
        [Export] private VBoxContainer? _masteryStatContainer;
        [Export] private Label? _masteryName;

        private string _masteryId = string.Empty;
        private IGameMessageBus? _mediator;
        private CraftingMastery? _craftingMastery;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public override void _Ready()
        {
            SetMasteryStats();
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _mediator = provider.GetService<IGameMessageBus>();
            _craftingMastery = provider.GetService<CraftingMastery>();
            _craftingMastery.ExperienceChange += OnExpirienceChange;
            _craftingMastery.BonusLevelChange += OnBonusLevelChanges;
            //_mediator.UpdateUi += OnUpdate;
        }

        private void OnUpdate()
        {
            _labels.TryGetValue("currentLevel", out var currentLevel);
            UpdateLabel(currentLevel, $"Current level: {_craftingMastery?.CurrentLevel}");
            _labels.TryGetValue("currentExp", out var currentExp);
            UpdateLabel(currentExp, $"Current exp: {_craftingMastery?.CurrentExperience}");
            _labels.TryGetValue("expNextLevel", out var expNext);
            UpdateLabel(expNext, $"Exp to next level: {_craftingMastery?.ExpToNextLevelRemain()}");
            _labels.TryGetValue("skillChance", out var skillChance);
            UpdateLabel(skillChance, $"Chance to get skill: {_craftingMastery?.GetCurrentSkillChance() * 100:0.##}%");
            _labels.TryGetValue("valueMultiplier", out var valueMulti);
            UpdateLabel(valueMulti, $"Value multiplier: {_craftingMastery?.GetCurrentValueMultiplier() * 100:0.##}%");
            _labels.TryGetValue("resourceMultiplier", out var resourceMulti);
            UpdateLabel(resourceMulti, $"Resource multiplier: {_craftingMastery?.GetCurrentResourceMultiplier() * 100:0.##}%");

            foreach (var (rarity, value) in _craftingMastery?.GetRarityProbabilities() ?? [])
            {
                if (rarity is Rarity.Mythic or Rarity.Unique)
                    continue;
                _labels.TryGetValue(rarity.ToString(), out var label);
                UpdateLabel(label, $"{rarity}: {value * 100:0.##}%");
            }
        }

        private void UpdateLabel(InteractiveLabel? label, string text)=> label?.SetText(text);

        private void OnBonusLevelChanges(int value)
        {
            if (_labels.TryGetValue("bonusLevel", out var label))
                label.SetText($"Bonus level: {value}");
        }

        private void OnExpirienceChange(int value)
        {
            if (_labels.TryGetValue("currentExp", out var expLabel))
                expLabel.SetText($"Current exp: {value}");

            if (_labels.TryGetValue("expNextLevel", out var expNextLabel))
                expNextLabel.SetText($"Exp to next level: {_craftingMastery?.ExpToNextLevelRemain()}");
        }

        private void SetMasteryStats()
        {
            ArgumentNullException.ThrowIfNull(_craftingMastery);

            _masteryName.Text = Localization.Localize("Passive_Skill_Crafting_Mastery");

            InteractiveLabel CreateLabel(string key, string text)
            {
                var label = new InteractiveLabel();
                label.SetText(text);
                _labels[key] = label;
                _masteryStatContainer?.AddChild(label);
                return label;
            }

            CreateLabel("currentLevel", $"Current level: {_craftingMastery.CurrentLevel}");
            CreateLabel("currentExp", $"Current exp: {_craftingMastery.CurrentExperience}");

            CreateLabel("expNextLevel", $"Exp to next level: {_craftingMastery.ExpToNextLevelRemain()}");
            CreateLabel("bonusLevel", $"Bonus level: {_craftingMastery.BonusLevel}");
            CreateLabel("skillChance", $"Chance to get skill: {_craftingMastery.GetCurrentSkillChance() * 100:0.##}%");
            CreateLabel("valueMultiplier", $"Value multiplier: {_craftingMastery.GetCurrentValueMultiplier() * 100:0.##}%");
            CreateLabel("resourceMultiplier", $"Resource multiplier: {_craftingMastery.GetCurrentResourceMultiplier() * 100:0.##}%");

            foreach (var (rarity, value) in _craftingMastery.GetRarityProbabilities())
            {
                if (rarity is Rarity.Mythic or Rarity.Unique)
                    continue;

                CreateLabel(rarity.ToString(), $"{rarity}: {value * 100:0.##}%");
            }
        }
    }
}
