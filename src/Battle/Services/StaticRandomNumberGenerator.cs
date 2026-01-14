namespace Battle.Services
{
    using Godot;

    public static class StaticRandomNumberGenerator
    {
        public static RandomNumberGenerator Rnd { get; } = new RandomNumberGenerator();

        static StaticRandomNumberGenerator()
        {
            Rnd.Randomize();
        }
    }
}
