namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Effects;

    /// <summary>Battle-side effect factory for item grants (mirror of <see cref="PassiveSkillProvider"/>):
    /// maps an effect id to a constructor fed by numeric properties from item JSON — balance lives in
    /// data, this registry only wires ids to code. Item-borne effects are battle-scoped: the grant
    /// re-applies them each battle, so "lasts the whole battle" is a high "duration" in the payload.
    /// A missing property refuses the effect loudly instead of constructing a mis-tuned instance.</summary>
    public class GrantEffectProvider : IGrantEffectProvider
    {
        private static readonly Dictionary<string, Func<SkillProperties, IEffect>> s_factories = new()
        {
            ["Effect_Evade_First_Death"] = properties =>
                new EvadeFirstDeath(properties.GetInt("duration"), maxStacks: 1, properties.Get("percentHealthToRecover")),
            ["Effect_Regeneration"] = properties =>
                new RegenerationEffect(properties.Get("amount"), properties.GetInt("duration"), properties.GetInt("maxStacks")),
            ["Effect_Lucky_Crit_Chance"] = properties =>
                new LuckyCritChanceEffect(properties.GetInt("duration"), properties.GetInt("maxStacks")),
            ["Effect_Execution"] = properties =>
                new ExecutionEffect(properties.GetInt("duration"), properties.GetInt("maxStacks"), properties.Get("percentage")),
            ["Effect_Curse"] = properties =>
                new CurseEffect(properties.GetInt("duration"), properties.GetInt("maxStacks"), properties.Get("costIncrease")),
            ["Effect_Life_Giving_Shade"] = properties =>
                new LifeGivingShadeEffect(properties.Get("lifeToRecover"), properties.GetInt("duration"), properties.GetInt("activations")),
        };

        public IEffect? CreateEffect(string id, SkillProperties properties)
        {
            if (!s_factories.TryGetValue(id, out var create))
            {
                Tracker.TrackNotFound($"Grant effect factory for '{id}'", this);
                return null;
            }

            try
            {
                return create(properties);
            }
            catch (KeyNotFoundException e)
            {
                Tracker.TrackError($"Effect '{id}' not granted: {e.Message}");
                return null;
            }
        }
    }
}
