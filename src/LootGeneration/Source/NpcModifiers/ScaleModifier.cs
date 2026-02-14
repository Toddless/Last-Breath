namespace LootGeneration.Source.NpcModifiers
{
    using Core.Interfaces.Entity;

    public class ScaleModifier(
        string id,
        float weight,
        float difficultyMultiplier,
        float scaleFactor,
        bool isUnique,
        string npcBuffId) : NpcModifier(id, weight, difficultyMultiplier, isUnique, npcBuffId), IScaleModifier
    {
        public float ScaleFactor { get; } = scaleFactor;

        public override void Attach(IEntity to)
        {
            if(to is not INpc npc) return;
            foreach (INpcModifier modifier in npc.NpcModifiers.AllModifiers)
                modifier.ScaleUp(this);
            npc.NpcModifiers.ModifierAdded += OnModifierAdded;
        }

        private void OnModifierAdded(INpcModifier modifier)
        {
            if(modifier is IScaleModifier scaleModifier) return;
            modifier.ScaleUp(this);
        }

        public override void Detach(IEntity from)
        {
            if(from is not INpc npc) return;
            foreach (INpcModifier modifier in npc.NpcModifiers.AllModifiers)
                modifier.ScaleDown(this);
            npc.NpcModifiers.ModifierAdded -= OnModifierAdded;
        }

        public override INpcModifier Copy() => new ScaleModifier(Id, Weight, BaseDifficultyMultiplier, ScaleFactor, IsUnique, NpcBuffId);
    }
}
