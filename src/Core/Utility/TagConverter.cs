namespace Core.Utility
{
    using System;

    public abstract class TagConverter
    {
        public static string ConvertToString(object obj)
        {
            if (obj is Enum || obj is string) return obj?.ToString()?.Trim().ToLower() ?? string.Empty;
            return string.Empty;
        }
    }
}
