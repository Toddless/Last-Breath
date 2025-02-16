namespace Playground.Script.UI
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Debug;
    using Playground.DIComponents;

    public partial class DevTools : Control
    {
        private TabBar? _playerVariableTabBar, _buttonsTabBar;
        private Player? _player;
        private Button? _updatePropertiesBtn;
        private readonly Dictionary<string, Action<double>> _fieldActions = [];

        public override void _Ready()
        {
            var root = GetNode<MarginContainer>(nameof(MarginContainer)).GetNode<HBoxContainer>(nameof(HBoxContainer)).GetNode<TabContainer>(nameof(TabContainer));
            _player = GameManager.Instance.Player;
            _playerVariableTabBar = root.GetNode<TabBar>("PlayerVariable");
            _buttonsTabBar = root.GetNode<TabBar>("Buttons");
            _updatePropertiesBtn = _buttonsTabBar.GetNode<Button>(nameof(Button));
            FillDictionary();
            SetPlayerVariables(_player);
        }

        private void FillDictionary()
        {
            // Need to change it
            _fieldActions.Add(nameof(HealthComponent.AdditionalHealth), delta => _player!.HealthComponent!.AdditionalHealth += (int)delta);
            _fieldActions.Add(nameof(HealthComponent.IncreaseHealth), delta => _player!.HealthComponent!.IncreaseHealth += (float)delta);
            _fieldActions.Add(nameof(AttackComponent.AdditionalMinDamage), delta => _player!.AttackComponent!.AdditionalMinDamage += (float)delta);
            _fieldActions.Add(nameof(AttackComponent.AdditionalMaxDamage), delta => _player!.AttackComponent!.AdditionalMaxDamage += (float)delta);
            _fieldActions.Add(nameof(AttackComponent.AdditionalExtraHitChance), delta => _player!.AttackComponent!.AdditionalExtraHitChance += (float)delta);
            _fieldActions.Add(nameof(AttackComponent.AdditionalCriticalChance), delta => _player!.AttackComponent!.AdditionalCriticalChance += (float)delta);
            _fieldActions.Add(nameof(AttackComponent.AdditionalCriticalDamage), delta => _player!.AttackComponent!.AdditionalCriticalDamage += (float)delta);
            _fieldActions.Add(nameof(AttackComponent.IncreaseDamage), delta => _player!.AttackComponent!.IncreaseDamage += (float)delta);
            _fieldActions.Add(nameof(AttackComponent.IncreaseCriticalChance), delta => _player!.AttackComponent!.IncreaseCriticalChance += (float)delta);
            _fieldActions.Add(nameof(AttackComponent.IncreaseExtraHitChance), delta => _player!.AttackComponent!.IncreaseExtraHitChance += (float)delta);
        }

        private void SetPlayerVariables(Player player)
        {
            var fields = PropertyGenerator.GetAllFields(player.GetType());
            var cont = _playerVariableTabBar!.GetNode<ScrollContainer>(nameof(ScrollContainer)).GetNode<VBoxContainer>(nameof(VBoxContainer));

            foreach (var field in fields)
            {
                var variable = Variable.InitializeAsPackedScene().Instantiate<Variable>();
                cont.AddChild(variable);
                variable!.Initialize(field);

                SetEvents(field, variable);
            }
        }

        private void SetEvents(string field, Variable variable)
        {
            if (_fieldActions.TryGetValue(field, out var action))
            {
                variable.Add += action;
                variable.Remove += delta => action(-delta);
            }
        }

        public override void _ExitTree()
        {
            _fieldActions.Clear();
            base._ExitTree();
        }
    }
}
