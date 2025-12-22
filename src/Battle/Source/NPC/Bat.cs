namespace Battle.Source.NPC
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.UI;
    using Core.Interfaces.Entity;

    [GlobalClass]
    internal partial class Bat : Enemy, INpc, IInitializable
    {
        private const string UID = "uid://bssmtdwwycbpt";
        [Export] public EntityType EntityType { get; set; }
        [Export] public Fractions Fraction { get; set; }
        public static PackedScene Initialize() =>ResourceLoader.Load<PackedScene>(UID);
    }
}
