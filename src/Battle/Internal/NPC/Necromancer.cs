namespace Battle.Internal.NPC
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.UI;
    using Godot;

    [GlobalClass]
    internal partial class Necromancer : Enemy, IInitializable, INpc
    {
        private const string UID = "uid://j5vmixgbii5n";
        public int Level { get; }
        public Rarity Rarity { get; }
        [Export] public EntityType EntityType { get; set; }
        [Export] public Fractions Fraction { get; set; }
        public INpcModifiersComponent NpcModifiers { get; }


        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
