namespace Battle.Source.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Views;
    using Godot;

    /// <summary>
    /// What the ability is wearing, laid out by tier. Fed pure view data (no domain object) and read
    /// only: an ability is upgraded by exactly the augments in its sockets, so there is no choice to
    /// offer and nothing to report upwards. Seating and extracting belong to the socket window, which
    /// drives the install and extract gates.
    /// The three tier rows and the rows inside them come from the scene, so an ability wearing more
    /// augments of one tier than the scene has rows for shows the first of them — a limit this
    /// placeholder inherits and the socket window replaces.
    /// </summary>
    public partial class AbilityUpgrades : Control
    {
        private const string UID = "uid://cfjcrk0kkryi8";
        private const int TierCount = 3; // mirrors the three exported tier containers

        [Export] private BoxContainer? _tierOne, _tierTwo, _tierThree;

        public void SetWorn(IReadOnlyList<UpgradeOptionView> worn)
        {
            // Every tier renders, even an empty one: the window instance is reused between
            // abilities (OpenWindow returns the open one), so a skipped tier would keep the
            // previous ability's augments on screen (tracker #64).
            for (int tier = 1; tier <= TierCount; tier++)
                SetTier(tier, [.. worn.Where(augment => augment.Tier == tier)]);
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void SetTier(int tier, IReadOnlyList<UpgradeOptionView> worn)
        {
            var rows = GetButtonsInTier(tier);
            for (int row = 0; row < rows.Length; row++)
            {
                // A row without an augment hides: stale text must not read as something worn.
                bool filled = row < worn.Count;
                rows[row].Visible = filled;
                if (filled) rows[row].ShowWorn(worn[row].DisplayName, worn[row].Description);
            }
        }

        private UpgradeButton[] GetButtonsInTier(int tier) => tier switch
        {
            1 => _tierOne?.GetChildren().OfType<UpgradeButton>().ToArray() ?? [],
            2 => _tierTwo?.GetChildren().OfType<UpgradeButton>().ToArray() ?? [],
            3 => _tierThree?.GetChildren().OfType<UpgradeButton>().ToArray() ?? [],
            _ => []
        };
    }
}
