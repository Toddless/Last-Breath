namespace Crafting.Source
{
    using Godot;

    public static class UpgradeChances
    {
        private const float P0 = 0.95f;
        private const float P5 = 0.70f;
        private const float P6 = 0.35f;
        private const float P9 = 0.15f;
        private const float P12 = 0.01f;

        private static readonly float s_r = Mathf.Pow(P12 / P9, 1.0f / 3.0f);

        public static float GetChance(int level)
        {
            return true switch
            {
                var _ when level <= 5 => Mathf.Lerp(P0, P5, level / 3.0f),
                var _ when level <= 9 => Mathf.Lerp(P6, P9, (level - 5) / 3.0f),
                var _ when level >= 10 => P9 * Mathf.Pow(s_r, level - 9),
                _ => 0,
            };
        }

        public static void ShowChances()
        {
            for (int i = 0; i < 13; i++)
            {
                GD.Print($"+{i} => {GetChance(i)}%");
            }
        }

    }
}
