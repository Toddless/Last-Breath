namespace LastBreath.Source.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces.Abilities;
    using Godot;

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

        public void UnbindAbilityButtons()
        {
            foreach (var button in _buttons.Where(button => button.AbilitySet()))
            {
                button.UnbindAbility();
            }
        }

        // TODO: Handle no more free ability sockets
        public void BindAbilitysButton(IAbility ability) => _buttons.First(x => !x.AbilitySet()).BindAbility(ability);
    }
}
