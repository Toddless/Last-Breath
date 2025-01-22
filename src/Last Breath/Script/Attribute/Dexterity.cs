namespace Playground.Script.Attribute
{
    using Playground.Components;

    public partial class Dexterity : Attribute
    {
        private float _criticalStrikeChance = 0.01f;
        private float _additionalAttackChance = 0.01f;
        private float _dodgeChance = 0.01f;

        public float CriticalStrikeChance
        {
            get => _criticalStrikeChance;
            set => _criticalStrikeChance = value;
        }

        public float AdditionalAttackChance
        {
            get => _additionalAttackChance;
            set => _additionalAttackChance = value;
        }

        public float DodgeChance
        {
            get => _dodgeChance;
            set => _dodgeChance = value;
        }

        public float TotalCriticalStrikeChance() => _criticalStrikeChance * Total;

        public float TotalAdditionalAttackChance() => _additionalAttackChance * Total;

        public float TotalDodgeChance() => _dodgeChance * Total;
    }
}
