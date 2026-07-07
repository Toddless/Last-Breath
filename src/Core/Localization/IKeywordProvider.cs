namespace Core.Localization
{
    /// <summary>Reference card of a clickable keyword ({@Effect_X} in descriptions).</summary>
    public sealed record KeywordTooltipView(string Id, string Name, string Description);

    /// <summary>Resolves a keyword payload (the url meta of a {@Key} link) into tooltip content.</summary>
    public interface IKeywordProvider
    {
        bool TryGetTooltip(string key, out KeywordTooltipView view);
    }
}
