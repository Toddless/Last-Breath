namespace Crafting.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data;
    using Core.Interfaces;
    using Godot;
    using Godot.Collections;

    [Tool]
    [GlobalClass]
    public partial class CraftingRequirementsUi : Control
    {
        [Export] private Array<ClickableRequirement> _topLineRequirements = [];
        [Export] private Array<ClickableRequirement> _bottomLineRequirements = [];
        [Export] private Array<ClickableRequirement> _optionalRequirements = [];

        private readonly List<ClickableRequirement> _usedSlots = [];
        public event Action<bool>? ItemCanBeCrafted;

        public void SetRequirements(IRequirement requirement, IGameServiceProvider provider)
        {
            var slot = FindEmptySlot() ?? throw new ArgumentOutOfRangeException($"No slot found for requirement {requirement}");
            slot.RequirementChanges += OnRequirementChanges;
            slot.SetConfiguration(new CraftingRequirementConfiguration(requirement, provider, GetAllResourcesAlreadyInSlots));
            _usedSlots.Add(slot);
        }

        public void SetOptional(string[] resourceCategories, IGameServiceProvider provider)
        {
            foreach (ClickableRequirement slot in _optionalRequirements)
            {
                slot.RequirementChanges += OnRequirementChanges;
                slot.SetConfiguration(new OptionalCraftingResourceRequirementConfiguration(resourceCategories, provider, GetAllResourcesAlreadyInSlots));
                _usedSlots.Add(slot);
            }
        }

        public System.Collections.Generic.Dictionary<string, int> GetRequirements()
        {
            var resources = new System.Collections.Generic.Dictionary<string, int>();
            _usedSlots.ForEach(slot =>
            {
                if (slot.IsResource) resources.Add(slot.Id, slot.Amount);
            });
            return resources;
        }

        public void ClearSlots()
        {
            ClearResources(_topLineRequirements);
            ClearResources(_bottomLineRequirements);
            ClearResources(_optionalRequirements);
            _usedSlots.ForEach(slot => slot.RequirementChanges -= OnRequirementChanges);
            _usedSlots.Clear();
        }

        private void OnRequirementChanges() => ItemCanBeCrafted?.Invoke(_usedSlots.All(slot => slot.IsResourcesEnough));

        private ClickableRequirement? FindEmptySlot()
        {
            var topSlot = _topLineRequirements.FirstOrDefault(slot => string.IsNullOrWhiteSpace(slot.Id));
            return topSlot ?? _bottomLineRequirements.FirstOrDefault(slot => string.IsNullOrWhiteSpace(slot.Id));
        }

        private List<string> GetAllResourcesAlreadyInSlots()
        {
            List<string> resourceIds = [];
            resourceIds.AddRange(_topLineRequirements.Select(slot => slot.Id).Where(id => !string.IsNullOrWhiteSpace(id)));
            resourceIds.AddRange(_bottomLineRequirements.Select(slot => slot.Id).Where(id => !string.IsNullOrWhiteSpace(id)));
            resourceIds.AddRange(_optionalRequirements.Select(slot => slot.Id).Where(id => !string.IsNullOrWhiteSpace(id)));
            return resourceIds;
        }

        private void ClearResources(Array<ClickableRequirement> resources)
        {
            foreach (var resource in resources)
                resource.ClearConfiguration();
        }
    }
}
