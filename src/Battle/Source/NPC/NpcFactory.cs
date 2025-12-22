namespace Battle.Source.NPC
{
    using Godot;

    public abstract class NpcFactory
    {
        public static CharacterBody2D CreateEntity(string type)
        {
            return type switch
            {
                "Necromancer" => Necromancer.Initialize().Instantiate<Necromancer>(),
                "Bat" => Bat.Initialize().Instantiate<Bat>(),
            };
        }
    }
}
