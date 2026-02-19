namespace LootGeneration.Internal
{
    using temp;
    using Godot;
    using System;
    using Source;
    using Core.Enums;
    using System.Linq;
    using System.Globalization;
    using Core.Interfaces.Events;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    internal partial class LootGenerationHud : Control
    {
        private readonly List<DataAmountLabel<int>> _tierLabelsCurrentNpc = [];
        private readonly List<DataAmountLabel<int>> _tierLabelsTotal = [];
        private readonly List<DataAmountLabel<Rarity>> _rarityLabelsCurrentNpc = [];
        private readonly List<DataAmountLabel<Rarity>> _rarityLabelsTotal = [];
        [Export] private VBoxContainer? _currentTierContainer, _totalTierContainer;
        [Export] private Label? _npcBudgetAmountLabel, _totalBudgetAmountLabel, _itemsOnFloor;
        [Export] private VBoxContainer? _chosenItemsIds;
        [Export] private OptionButton? _fractionList, _entityTypeList, _rarityList;
        [Export] private SpinBox? _levelBox;
        [Export] private Button? _createSingle, _setAsDefault, _random, _startBattle, _endBattle;
        [Export] private ItemList? _npcModifiers;

        private IGameEventBus? _eventBus;
        private float _totalBudget;
        public event Action<Rarity, Fractions, int, EntityType, List<string>>? CreateSingleNpc, SetAsDefault;
        public event Action? SetRandomNpcCreation;

        public override void _Ready()
        {
            SetCurrentNpcTierItemsStats();
            SetTotalNpcTierItemsStats();
            SetCurrentRarities();
            SetTotalRarities();
            ConvertEnumToList<Fractions>(_fractionList);
            ConvertEnumToList<EntityType>(_entityTypeList);
            ConvertEnumToList<Rarity>(_rarityList);
            _startBattle?.Pressed += () => _eventBus?.Publish(new ExampleBattleStart(850f, 450f));
            _endBattle?.Pressed += () => _eventBus?.Publish(new BattleEndEvent());
            _createSingle?.Pressed += OnCreateSinglePressed;
            _setAsDefault?.Pressed += OnSetAsDefault;
            _random?.Pressed += () => SetRandomNpcCreation?.Invoke();
        }

        public void UpdateItemsOnFloor(int amount) => _itemsOnFloor?.Text = $"{amount}";

        public void SetNpcModifiers(List<string> npcModifierIds)
        {
            if (_npcModifiers == null) return;
            foreach (string npcModifierId in npcModifierIds)
                _npcModifiers.AddItem(npcModifierId);
        }

        public void SetEventBus(IGameEventBus eventBus)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe<ItemTierChosenEvent>(OnItemTierChosen);
            _eventBus.Subscribe<BudgetCalculatedEvent>(OnBudgetCalculated);
            _eventBus.Subscribe<EquipRarityChosenEvent>(OnRarityChosen);
            _eventBus.Subscribe<ChosenItemIds>(OnChosenItems);
        }

        private void OnChosenItems(ChosenItemIds obj)
        {
            foreach (Node child in _chosenItemsIds?.GetChildren() ?? [])
                child.QueueFree();
            for (int index = 0; index < obj.Items.Count; index++)
            {
                string objItem = obj.Items[index];
                _chosenItemsIds?.AddChild(new Label { Text = $"{index + 1}. {objItem}" });
            }
        }

        private void ConvertEnumToList<TEnum>(OptionButton optionButton)
            where TEnum : struct, Enum
        {
            int index = 0;
            foreach (var listItem in Enum.GetValues<TEnum>())
            {
                optionButton.AddItem(listItem.ToString(), index);
                optionButton.SetItemMetadata(index, Convert.ToByte(listItem));
                index++;
            }
        }

        private void OnRarityChosen(EquipRarityChosenEvent obj)
        {
            var rarity = obj.RarityAmount;
            foreach (KeyValuePair<Rarity, int> rarityAmount in rarity)
            {
                var current = _rarityLabelsCurrentNpc.FirstOrDefault(x => x.Data == rarityAmount.Key);
                var total = _rarityLabelsTotal.FirstOrDefault(x => x.Data == rarityAmount.Key);
                current?.Label?.Text = $"{rarityAmount.Key}: {rarityAmount.Value}";
                total?.Amount += rarityAmount.Value;
                total?.Label?.Text = $"{total.Data}: {total.Amount}";
            }
        }

        private void OnBudgetCalculated(BudgetCalculatedEvent obj)
        {
            _npcBudgetAmountLabel?.Text = obj.Budget.ToString(CultureInfo.InvariantCulture);
            _totalBudget += obj.Budget;
            _totalBudgetAmountLabel?.Text = _totalBudget.ToString(CultureInfo.InvariantCulture);
        }

        private void OnItemTierChosen(ItemTierChosenEvent obj)
        {
            foreach (KeyValuePair<int, int> tierAmount in obj.ChosenTiersAmount)
            {
                var tierLabel = _tierLabelsCurrentNpc.FirstOrDefault(x => x.Data == tierAmount.Key);
                var totalTierLabel = _tierLabelsTotal.FirstOrDefault(x => x.Data == tierAmount.Key);
                tierLabel?.Label?.Text = $"{tierAmount.Key}: {tierAmount.Value}";
                totalTierLabel?.Amount += tierAmount.Value;
                totalTierLabel?.Label?.Text = $"{totalTierLabel.Data}: {totalTierLabel.Amount}";
            }
        }

        private void SetCurrentNpcTierItemsStats() => CreateLabels(_tierLabelsCurrentNpc, _currentTierContainer);
        private void SetTotalNpcTierItemsStats() => CreateLabels(_tierLabelsTotal, _totalTierContainer);
        private void SetCurrentRarities() => CreateRarityLabels(_rarityLabelsCurrentNpc, _currentTierContainer);
        private void SetTotalRarities() => CreateRarityLabels(_rarityLabelsTotal, _totalTierContainer);

        private void OnSetAsDefault()
        {
            if (_rarityList == null || _entityTypeList == null || _fractionList == null) return;
            SetAsDefault?.Invoke(ConvertByteToEnum<Rarity>(_rarityList), ConvertByteToEnum<Fractions>(_fractionList), Mathf.RoundToInt(_levelBox?.Value ?? 1),
                ConvertByteToEnum<EntityType>(_entityTypeList), ChosenNpcModifiers());
        }

        private void OnCreateSinglePressed()
        {
            if (_rarityList == null || _entityTypeList == null || _fractionList == null) return;
            CreateSingleNpc?.Invoke(ConvertByteToEnum<Rarity>(_rarityList), ConvertByteToEnum<Fractions>(_fractionList), Mathf.RoundToInt(_levelBox?.Value ?? 1),
                ConvertByteToEnum<EntityType>(_entityTypeList), ChosenNpcModifiers());
        }

        private List<string> ChosenNpcModifiers()
        {
            if (_npcModifiers == null) return [];
            int[] indexes = _npcModifiers.GetSelectedItems();
            var strings = indexes.Select(index => _npcModifiers.GetItemText(index)).ToList();
            return strings;
        }

        private TEnum ConvertByteToEnum<TEnum>(OptionButton option) where TEnum : struct, Enum =>
            (TEnum)Enum.ToObject(typeof(TEnum), option.GetItemMetadata(option.Selected).AsByte());


        private void CreateLabels(List<DataAmountLabel<int>> labels, VBoxContainer? container)
        {
            for (int i = 0; i < 4; i++)
            {
                var hbox = new HBoxContainer();
                var tierLabel = new Label { Text = $"Tier {i} items:" };
                var tierAmountLabel = new Label();
                hbox.AddChild(tierLabel);
                hbox.AddChild(tierAmountLabel);
                labels.Add(new(i) { Label = tierAmountLabel });
                container?.AddChild(hbox);
            }
        }

        private void CreateRarityLabels(List<DataAmountLabel<Rarity>> labels, VBoxContainer? container)
        {
            foreach (Rarity rarity in Enum.GetValues<Rarity>())
            {
                var hbox = new HBoxContainer();
                var rarityLabel = new Label { Text = $"{rarity}:" };
                var rarityAmount = new Label();
                hbox.AddChild(rarityLabel);
                hbox.AddChild(rarityAmount);
                labels.Add(new(rarity) { Label = rarityLabel });
                container?.AddChild(hbox);
            }
        }


        private class DataAmountLabel<TData>(TData data)
        {
            public Label? Label { get; init; }
            public TData Data { get; } = data;
            public int Amount { get; set; }
        }
    }
}
