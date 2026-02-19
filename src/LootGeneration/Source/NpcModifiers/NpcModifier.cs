namespace LootGeneration.Source.NpcModifiers
{
    using Godot;
    using System;
    using Utilities;
    using Core.Interfaces;
    using Core.Interfaces.Entity;

    public abstract class NpcModifier(string id, float weight, float difficultyMultiplier, bool isUnique, string npcBuffId) : INpcModifier
    {
        protected float TotalScale { get; private set; } = 1f;
        public string Id { get; } = id;
        public Texture2D? Icon { get; }
        public float Weight { get; set; } = weight;
        public float BaseDifficultyMultiplier { get; } = difficultyMultiplier;
        public float DifficultyMultiplier => BaseDifficultyMultiplier * TotalScale;
        public bool IsUnique { get; } =  isUnique;
        public string NpcBuffId { get; } = npcBuffId;
        public string DisplayName => Localization.Localize(Id);
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string Description => Localization.LocalizeDescription(Id);


        public virtual void Attach(IEntity to)
        {
        }

        public virtual void Detach(IEntity from)
        {
        }

        public virtual void ApplyModifier(IModifierApplyingContext context)
        {
        }

        public virtual void ScaleUp(IScaleModifier modifier)
        {
            TotalScale += modifier.ScaleFactor;
        }

        public virtual void ScaleDown(IScaleModifier modifier)
        {
            TotalScale -= modifier.ScaleFactor;
        }

        public abstract INpcModifier Copy();
        public bool IsSame(string otherId) => otherId == Id;
    }
}
