namespace Battle.Source.UIElements
{
    using Core.Views.UI;
    using Godot;

    // TODO: не используется нигде (очередь ходов в BattleHud рисуется серыми Label'ами; у бойцов нет иконок) — кандидат на удаление вместе со сценой.
    [GlobalClass]
    [Tool]
    public partial class QueueSlot : Control, IInitializable
    {
        private const string UID = "uid://bt5ovr71f3vu1";
        [Export] private TextureRect? Icon { get; set; }

        public void SetIcon(Texture2D icon) => Icon?.Texture = icon;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
