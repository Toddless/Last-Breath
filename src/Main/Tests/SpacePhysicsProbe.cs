namespace LastBreath.Tests
{
    using Godot;

    public partial class SpacePhysicsProbe : CharacterBody2D
    {
        public int PhysicsTicks { get; private set; }
        public override void _PhysicsProcess(double delta)
        {
            PhysicsTicks++;
            Velocity = Vector2.Right * 10;
            MoveAndSlide();
        }
    }
}
