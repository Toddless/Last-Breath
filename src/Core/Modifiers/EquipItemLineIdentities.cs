namespace Core.Modifiers
{
    using System.Collections.Generic;
    using System.Linq;
    using Items;

    /// <summary>Which line identities an item already occupies — the duplicate guard for any roll onto an
    /// EXISTING item (today: the reroll and its pool preview).</summary>
    public static class EquipItemLineIdentities
    {
        /// <summary>Identities of the rolled lines that block a new roll: atoms as singleton identities,
        /// a composite group folded into ONE whole-set identity (so its parts still neither block nor are
        /// blocked by standalone atoms — only a full set match is a duplicate). Deliberately out of the
        /// count: implicits (an authored base line must never eat the roll pool — they live in their own
        /// list) and the excluded instances — the caller's own line, which is being replaced and may
        /// legally come back as the same stat with a fresh value. An excluded id lifts its WHOLE group.</summary>
        public static HashSet<LineIdentity> OccupiedLineIdentities(this IEquipItem item, IReadOnlyCollection<string> excludedInstanceIds)
        {
            var identities = new HashSet<LineIdentity>();
            var groups = new Dictionary<string, List<ModifierKey>>();
            var excludedGroups = new HashSet<string>();

            foreach (var modifier in item.Modifiers)
                AddLine(modifier.InstanceId, (modifier as SimpleModifier)?.GroupId, ModifierKey.From(modifier));

            foreach (var entry in item.ContextModifiers)
                AddLine(entry.InstanceId, entry.GroupId, ModifierKey.From(entry));

            foreach ((string groupId, var keys) in groups)
                if (!excludedGroups.Contains(groupId))
                    identities.Add(LineIdentity.OfKeys(keys));

            return identities;

            void AddLine(string instanceId, string? groupId, ModifierKey key)
            {
                if (groupId == null)
                {
                    if (!excludedInstanceIds.Contains(instanceId)) identities.Add(LineIdentity.Of(key));
                    return;
                }

                if (excludedInstanceIds.Contains(instanceId)) excludedGroups.Add(groupId);
                if (!groups.TryGetValue(groupId, out var keys)) groups[groupId] = keys = [];
                keys.Add(key);
            }
        }
    }
}
