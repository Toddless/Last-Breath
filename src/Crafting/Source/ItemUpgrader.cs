namespace Crafting.Source
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Crafting.TestResources;
    using Godot;
    using Utilities;

    public class ItemUpgrader
    {
        private const string UpgradeData = "uid://br3am4hnn5iqg";
        // 1. Количество опциональных ресурсов для рекрафта не должно быть меньше количества выбранных модификаторов
        // 2. Количество основных ресурсов увеличивается пропроционально кол-ву выбранных модификаторов
        // 3. НЕ выбранные модификаторы остаются неизменными
        // 4. Кнопка рекрафта доступна только при наличии необходимого кол-ва ресурсов
        // Для апгрейдов можно добавить опциональные ресурсы увеличивающие шансы на успех, дополнительную заточку либо шанс не потерять уровень заточки при неудаче.
        // решить позже сколько опциональных ресурсов можно использовтаь одновременно
        private Dictionary<string, List<IResourceRequirement>> _upgradeRequirements = [];
        private ItemUpgradeMode _currentUpgradeMode = ItemUpgradeMode.None;

        public ItemUpgrader()
        {
            LoadData();
        }

        public List<IResourceRequirement> SetCost(string itemId, ItemUpgradeMode mode)
        {
            if (!_upgradeRequirements.TryGetValue(itemId, out var reqs))
            {
                Logger.LogNotFound($"Requirements: {itemId}", this);
                return [];
            }
            _currentUpgradeMode = mode;
            switch (mode)
            {
                case ItemUpgradeMode.Normal:
                    return reqs;
                case ItemUpgradeMode.Double:
                    var doubl = new List<IResourceRequirement>();
                    foreach (var req in reqs) doubl.Add(new ResourceRequirement() { ResourceId = req.ResourceId, Amount = req.Amount * 2 });
                    return doubl;
                case ItemUpgradeMode.Lucky:
                    var lucky = new List<IResourceRequirement>();
                    foreach (var req in reqs) lucky.Add(new ResourceRequirement() { ResourceId = req.ResourceId, Amount = req.Amount * 5 });
                    return lucky;
                default:
                    return [];
            }
        }

        public List<IResourceRequirement> GetResourceRequirements(string itemId) => _upgradeRequirements.GetValueOrDefault(itemId) ?? [];

        public bool TryUpgradeItem(IEquipItem item)
        {
            using var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            switch (_currentUpgradeMode)
            {
                case ItemUpgradeMode.Normal:
                    return item.Upgrade();
                case ItemUpgradeMode.Double:
                    return item.Upgrade(2);
                case ItemUpgradeMode.Lucky:
                    if (rnd.Randf() >= 0.99f)
                        return item.Upgrade(12);
                    else
                        return !item.Downgrade(12);
                default:
                    return false;
            }
        }


        private void LoadData()
        {
            var data = ResourceLoader.Load<UpgradeRequirement>(UpgradeData);

            foreach (var req in data.UpgradeRequrements)
                _upgradeRequirements.Add(req.Key, [.. req.Requirements]);

        }
    }
}
