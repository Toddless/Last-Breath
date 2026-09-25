namespace Core.Ai.World
{
    using Entity.Components;
    using Godot;

    public interface IWorldBrain
    {
        IWorldAgent Agent { get; }
        WorldBrainConfig Config { get; }
        IRandomNumberGenerator Rnd { get; }
        AlertnessState State { get; }
        bool IsNear(Vector2 point);
        void Tick(float delta);

        /// <summary>
        /// World events routed by the body (the adapter filters by hearing radius).
        /// The aggressive go looking; civilians run away from any commotion.
        /// </summary>
        void OnStimulus(Stimulus stimulus);

        /// <summary>Called by the body when its battle ends: calm down and hold aggression briefly.</summary>
        void OnBattleEnded();
    }
}
