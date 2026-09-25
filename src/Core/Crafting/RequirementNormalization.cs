namespace Core.Crafting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using Interfaces;

    /// <summary>
    /// Authoring tolerance for recipe requirements: a category requirement may be declared either
    /// as <see cref="RequirementType.ResourceCategory"/> or as a plain Resource line whose id names
    /// a material category ("Category_Metal"). Consumers read requirements through here so both
    /// spellings behave identically — the line becomes a pickable category slot, never a literal
    /// item lookup that can't be satisfied.
    /// </summary>
    public static class RequirementNormalization
    {
        public static List<IRequirement> Normalize(this IEnumerable<IRequirement> requirements, Func<string, bool> isMaterialCategory) =>
            requirements
                .Select(requirement => requirement.Type == RequirementType.Resource && isMaterialCategory(requirement.Id)
                    ? new Requirement(RequirementType.ResourceCategory, requirement.Id, requirement.Amount)
                    : requirement)
                .ToList();
    }
}
