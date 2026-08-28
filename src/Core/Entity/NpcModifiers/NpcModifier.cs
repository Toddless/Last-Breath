namespace Core.Entity.NpcModifiers
{
    using System;
    using Context;
    using Entity;
    using Godot;

    public abstract class NpcModifier(string id, float weight, float difficultyMultiplier, bool isUnique, string npcBuffId) : INpcModifier
    {
        public float TotalScale { get; private set; } = 1f;
        public string Id { get; } = id;
        public Texture2D? Icon { get; }
        public float Weight { get; set; } = weight;
        public float BaseDifficultyMultiplier { get; } = difficultyMultiplier;
        public float DifficultyMultiplier => BaseDifficultyMultiplier * TotalScale;
        public bool IsUnique { get; } = isUnique;
        public string NpcBuffId { get; } = npcBuffId;

        /// <summary>Catalog section key, carried in by the factory (see <see cref="INpcModifier.Group"/>).
        /// An init property rather than a constructor argument so a modifier built by hand — a test, a
        /// summon put together in code — keeps compiling and simply has no group.</summary>
        public string Group { get; init; } = string.Empty;

        /// <summary>Section-authored, carried in beside <see cref="Group"/>. Defaults to Group, which is
        /// what a section that says nothing means.</summary>
        public Enums.NpcUniqueScope UniqueScope { get; init; }
        public string InstanceId { get; } = Guid.NewGuid().ToString();

        // Same key conventions as Utilities.Localization (which Core cannot reference).
        public string DisplayName => Localization.Localization.Localize(Id);
        public string Description => Localization.Localization.LocalizeDescription(Id);

        // The bearer's side of a modifier does NOT live here: NpcBuffId is bound by NpcBuffBinder, which
        // the modifiers component drives after every change to the list. It has to be a list-wide pass —
        // a scaling modifier arriving later raises TotalScale of everyone already attached, and a value
        // frozen at Attach time would keep the scale it happened to see.
        public virtual void Attach(IFightable to)
        {
        }
        public virtual void Detach(IFightable from)
        {
        }

        public virtual void ApplyModifier(IModifierApplyingContext context)
        {
        }

        public virtual void ScaleUp(IScaleModifier modifier) => TotalScale += modifier.ScaleFactor;

        public virtual void ScaleDown(IScaleModifier modifier) => TotalScale -= modifier.ScaleFactor;

        public abstract INpcModifier Copy();
        public bool IsSame(string otherId) => otherId == Id;
    }
}
