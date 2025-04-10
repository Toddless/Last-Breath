namespace Playground.Script.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Script.Abilities.Interfaces;

    public partial class AbilityButtons : HBoxContainer
    {
        private const int Slots = 8;

        private readonly List<AbilityButton> _buttons = [];


        public void Initialize()
        {
            for (int i = 0; i < Slots; i++)
            {
                var button = AbilityButton.InitializeButton().Instantiate<AbilityButton>();
                _buttons.Add(button);
                AddChild(button);
            }
        }

        public void UpdateAbilitiesTarget(ICharacter target)
        {
            foreach (var button in _buttons)
            {
                button.UpdateAbilityTarget(target);
            }
        }

        public void UpdateAbiliesCooldown() => _buttons.ForEach(x => x.UpdateAbilityCooldown());

        public void UnbindAbilityButtons() => _buttons.ForEach(button => button.UnbindAbility());
        // TODO: Handle no more free ability sockets
        public void BindAbilitysButton(IAbility ability) => _buttons.First(x => x.AbilitySet() == false).BindAbility(ability);
    }
}
