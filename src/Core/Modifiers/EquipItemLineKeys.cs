namespace Core.Modifiers
{
    using System.Collections.Generic;
    using System.Linq;
    using Items;

    /// <summary>Which line keys an item already occupies — the duplicate guard for any roll onto an
    /// EXISTING item (today: the reroll).</summary>
    public static class EquipItemLineKeys
    {
        /// <summary>Keys of the rolled lines that block a new roll. Deliberately out of the count:
        /// implicits (an authored base line must never eat the roll pool — they live in their own list),
        /// composite parts (a group renders as a single line, so it neither blocks nor is blocked), and
        /// the excluded instances — the caller's own line or group, which is being replaced and may
        /// legally come back as the same stat with a fresh value.</summary>
        public static HashSet<ModifierKey> OccupiedLineKeys(this IEquipItem item, IReadOnlyCollection<string> excludedInstanceIds)
        {
            var keys = new HashSet<ModifierKey>();
            foreach (var modifier in item.Modifiers)
                if (modifier is not SimpleModifier { GroupId: not null } && !excludedInstanceIds.Contains(modifier.InstanceId))
                    keys.Add(ModifierKey.From(modifier));

            foreach (var entry in item.ContextModifiers)
                if (entry.GroupId == null && !excludedInstanceIds.Contains(entry.InstanceId))
                    keys.Add(ModifierKey.From(entry));

            return keys;
        }
    }
}
