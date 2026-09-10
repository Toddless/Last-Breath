namespace Core.World.Spaces
{
    using System;
    using Godot;

    /// <summary>Return address for a transient transfer while the source scene remains resident.</summary>
    public sealed class ParticipantPlacement
    {
        private readonly Node _parent;
        private readonly Transform2D _transform;

        public Node2D Body { get; }

        public ParticipantPlacement(Node2D body)
        {
            if (!body.IsInsideTree()) throw new InvalidOperationException("A participant must belong to a loaded space.");
            Body = body;
            _parent = body.GetParent();
            _transform = body.Transform;
        }

        public void Restore()
        {
            if (!GodotObject.IsInstanceValid(Body) || Body.IsQueuedForDeletion()) return;
            if (!GodotObject.IsInstanceValid(_parent) || !_parent.IsInsideTree())
                throw new InvalidOperationException("The battle origin must remain loaded until all participants return.");

            if (Body.GetParent() != _parent)
            {
                Body.GetParent()?.RemoveChild(Body);
                Body.Transform = _transform;
                _parent.AddChild(Body);
            }
            Body.Transform = _transform;
            if (Body is CharacterBody2D character) character.Velocity = Vector2.Zero;
        }
    }
}
