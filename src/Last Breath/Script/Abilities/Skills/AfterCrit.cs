namespace Playground.Script.Abilities.Skills
{
    using Godot;
    using Playground.Components;
    using Playground.Script.Enums;

    public class AfterCrit : ISkill
    {
        private float _afterCritChance;
        private ICharacter? _owner;
        public string Name { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public Texture2D? Icon { get; private set; }

        public SkillType SkillType { get; private set; }


        public void OnObtaining(ICharacter owner)
        {
            // Не нужно изменять DamageComponent для After Crit. Мы просто берем все значения выше 100% и используем их как основу шанса after crit
            // базовым уроном является критический удар. Множителем after crit равен 1.5 . Данное значение можно увеличить с помощью апгрейда
            _owner = owner;
            _owner.Damage.AddOverrideFuncForParameter(OnSomeName, Parameter.CriticalChance);
        }

        public void OnLoss()
        {
            _owner?.Damage.RemoveOverrideFuncForParameter(Parameter.CriticalChance);
            _owner = null;
        }


        private float OnSomeName(float value, ModifiersChangedEventArgs args)
        {
            var afterCritChance = Calculations.CalculateFloatValue(value, args.Modifiers);
            _afterCritChance = Mathf.Max(0, afterCritChance - 1.0f);
            return afterCritChance;
        }
    }
}
