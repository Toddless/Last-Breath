namespace Battle.Source.Abilities.PassiveSkills
{
    using Godot;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Events.GameEvents;

    public class MulticastPassiveSkill(string id) : Skill(id)
    {
        private readonly float[] _baseChances = [0.55f, 0.30f, 0.15f];

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<AbilityActivatedGameEvent>(OnAbilityActivated);
        }

        private void OnAbilityActivated(AbilityActivatedGameEvent obj)
        {
            if (Owner == null) return;
            var ability = obj.Ability;
            float m = Owner.Parameters.MulticastChance;
            using var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            int stage = 1;

            int GetStage()
            {
                if (rnd.Randf() <= _baseChances[0] * m) stage++;
                else return stage;

                if (rnd.Randf() <= _baseChances[1] * m) stage++;
                else return stage;

                if (rnd.Randf() <= _baseChances[2] * m) stage++;

                return stage;
            }

            stage = GetStage();
        }

        public override void Detach(IEntity owner)
        {
            Owner = null;
            owner.CombatEvents.Unsubscribe<AbilityActivatedGameEvent>(OnAbilityActivated);
        }

        public override ISkill Copy() => throw new System.NotImplementedException();

        public override bool IsStronger(ISkill skill) => throw new System.NotImplementedException();
    }
}
