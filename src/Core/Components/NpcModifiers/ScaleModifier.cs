namespace Core.Components.NpcModifiers
{
    using Interfaces.Entity;

    public class ScaleModifier(
        string id,
        float weight,
        float difficultyMultiplier,
        float scaleFactor,
        bool isUnique,
        string npcBuffId) : NpcModifier(id, weight, difficultyMultiplier, isUnique, npcBuffId), IScaleModifier
    {
        public float ScaleFactor { get; } = scaleFactor;

        public override void Attach(IFightable to)
        {
            if (to is not IFightableNpc npc) return;
            foreach (INpcModifier modifier in npc.NpcModifiers.AllModifiers)
                modifier.ScaleUp(this);
            npc.NpcModifiers.ModifierAdded += OnModifierAdded;
        }

        public override void Detach(IFightable from)
        {
            if (from is not IFightableNpc npc) return;
            foreach (INpcModifier modifier in npc.NpcModifiers.AllModifiers)
                modifier.ScaleDown(this);
            npc.NpcModifiers.ModifierAdded -= OnModifierAdded;
        }

        public override void ScaleUp(IScaleModifier modifier)
        {
        }

        public override void ScaleDown(IScaleModifier modifier)
        {
        }

        public override INpcModifier Copy() => new ScaleModifier(Id, Weight, BaseDifficultyMultiplier, ScaleFactor, IsUnique, NpcBuffId);

        private void OnModifierAdded(INpcModifier modifier)
        {
            if (modifier is IScaleModifier) return;
            modifier.ScaleUp(this);
        }
    }
}
