namespace Core.Crafting
{
    using System.Collections.Generic;
    using Enums;
    using Interfaces;

    /// <summary>One rollable bonus effect a crafted item may receive on creation (the mastery
    /// "extra effect" channel). Carries the full grant payload — the strict skill factories refuse
    /// a grant with missing properties, so an entry without its numbers never lands silently.</summary>
    public sealed record CraftingEffectOption(GrantKind Kind, string Id, IReadOnlyDictionary<string, float> Properties) : IWeightable
    {
        public float Weight { get; set; }
    }

    /// <summary>Catalog of effects the crafting "extra effect" roll draws from (ItemEffects.json).</summary>
    public interface ICraftingEffectProvider
    {
        IReadOnlyList<CraftingEffectOption> Effects { get; }
    }
}
