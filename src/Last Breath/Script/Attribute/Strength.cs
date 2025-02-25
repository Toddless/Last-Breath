namespace Playground.Script.Attribute
{
    public class Strength : Attribute
    {
        private float _damageIncrease = 0.01f;
        private float _healthIncrease = 0.1f;
        private float _defenceIncrease = 0.01f;


        public float DamageIncrease
        {
            get => _damageIncrease;
            set
            {
                _damageIncrease = value;
            }
        }

        public float HealthIncrease
        {
            get => _healthIncrease;
            set
            {
                _healthIncrease = value;
            }
        }

        public float DefenceIncrease
        {
            get => _defenceIncrease;
            set
            {
                _defenceIncrease = value;
            }
        }

        public float TotalDamageIncrese() => _damageIncrease * Total;

        public float TotalHealthIncrese() => _healthIncrease * Total + 1;


        public float TotalDefenceIncrease() => _defenceIncrease * Total;
    }
}
