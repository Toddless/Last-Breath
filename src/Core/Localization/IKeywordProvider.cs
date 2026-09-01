namespace Core.Localization
{
    /// <summary>Reference card of a clickable keyword ({@Effect_X} in descriptions).
    /// <paramref name="TitleColorHex"/> tints the card's title (damage-type cards wear their
    /// type's color); null keeps the popup's themed title color.</summary>
    public sealed record KeywordTooltipView(string Id, string Name, string Description, string? TitleColorHex = null);

    /// <summary>Resolves a keyword payload (the url meta of a {@Key} link) into tooltip content.</summary>
    public interface IKeywordProvider
    {
        bool TryGetTooltip(string key, out KeywordTooltipView view);
    }
}
