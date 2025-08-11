namespace LastBreath.addons.Tools.TagGenerator
{
    public static class TagUtils
    {
        public static string Normalize(string tag) => (tag ?? string.Empty).Trim().ToLowerInvariant().Replace(' ', '_');
    }
}
