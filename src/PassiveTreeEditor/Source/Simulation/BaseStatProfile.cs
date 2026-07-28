namespace PassiveTreeEditor.Source.Simulation
{
    using System.Collections.Generic;
    using Core.Enums;

    /// <summary>
    /// The character the tree is measured on top of. Seeded from the unarmed profile in
    /// <c>Main/Player/Player.cs</c> (<c>GetUnarmedBaseValue</c>) and editable, so a cluster can be
    /// judged against real gear instead of a naked character.
    /// </summary>
    public sealed class BaseStatProfile
    {
        private readonly Dictionary<EntityParameter, float> _values = [];

        public float this[EntityParameter parameter]
        {
            get => _values.GetValueOrDefault(parameter);
            set => _values[parameter] = value;
        }

        public IReadOnlyDictionary<EntityParameter, float> Values => _values;

        /// <summary>The player's unarmed baseline, kept in sync with the game by hand — the tool must
        /// not construct a Player, which would drag the whole battle stack into the editor.</summary>
        public static BaseStatProfile Unarmed()
        {
            var profile = new BaseStatProfile();
            profile[EntityParameter.Health] = 1000;
            profile[EntityParameter.Barrier] = 100;
            profile[EntityParameter.Mana] = 500;
            profile[EntityParameter.Intelligence] = 5f;
            profile[EntityParameter.Strength] = 5f;
            profile[EntityParameter.Dexterity] = 5f;
            profile[EntityParameter.Evade] = 300;
            profile[EntityParameter.Armor] = 300;
            profile[EntityParameter.Accuracy] = 300;
            profile[EntityParameter.CriticalChance] = 0.05f;
            profile[EntityParameter.AdditionalHitChance] = 0.05f;
            profile[EntityParameter.CriticalDamage] = 1.5f;
            profile[EntityParameter.MulticastChance] = 0f;
            profile[EntityParameter.PhysicalDamage] = 100;
            profile[EntityParameter.SpellDamage] = 50;
            profile[EntityParameter.MoveSpeed] = 500;
            return profile;
        }
    }
}
