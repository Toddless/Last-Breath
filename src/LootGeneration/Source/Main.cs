namespace LootGeneration.Source
{
    using Godot;
    using System;
    using Services;
    using Test.NpcModifiers;

    internal partial class Main : Node2D
    {
        private readonly ItemDataProvider _itemDataProvider = new();
        private readonly NpcModifiersFactory _factory = new();
        private NpcModifierProvider? _modifierProvider;

        public override void _Ready()
        {
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                _itemDataProvider.LoadData();
                _modifierProvider = new NpcModifierProvider(_factory);
                await _modifierProvider.LoadDataAsync();
            }
            catch (Exception e)
            {
                GD.Print($"error loading data: {e.Message}, {e.StackTrace}");
            }
        }
    }
}
