namespace Battle.Source.PassiveSkills
{
    using Godot;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Events.GameEvents;

    public class MulticastPassiveSkill(float[] Chances) : Skill(id: "Passive_Skill_Multicast")
    {
        public override void Attach(IEntity owner)
        {
            Owner = owner;
            owner.CombatEvents.Subscribe<AbilityActivatedEvent>(OnAbilityActivated);
        }

        private void OnAbilityActivated(AbilityActivatedEvent obj)
        {
            if (Owner == null) return;
            var ability = obj.Ability;
            float m = Owner.Parameters.MulticastChance;
            using var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            int stage = 1;

            stage = GetStage();
            return;

            int GetStage()
            {
                if (rnd.Randf() <= Chances[0] * m) stage++;
                else return stage;

                if (rnd.Randf() <= Chances[1] * m) stage++;
                else return stage;

                if (rnd.Randf() <= Chances[2] * m) stage++;

                return stage;
            }
        }

        public override void Detach(IEntity owner)
        {
            Owner = null;
            owner.CombatEvents.Unsubscribe<AbilityActivatedEvent>(OnAbilityActivated);
        }

        public override ISkill Copy() => new MulticastPassiveSkill(Chances);

        public override bool IsStronger(ISkill skill) => false;
    }
}
