namespace Core.Views
{
    using Localization;
    using UI;
    using Godot;

    public interface IKeywordTooltipPopup: IPopup
    {
        void ShowKeyword(KeywordTooltipView view, Vector2 globalPosition);
    }
}
