namespace Battle.Source.NPC
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.UI;
    using Core.Interfaces.Entity;

    [GlobalClass]
    internal partial class Necromancer : Enemy, IInitializable, INpc
    {
        private const string UID = "uid://j5vmixgbii5n";
        [Export] public EntityType EntityType { get; set; }
        [Export] public Fractions Fraction { get; set; }


        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
