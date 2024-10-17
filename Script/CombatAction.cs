using Godot;

[GlobalClass]
public partial class CombatAction : Resource
{
    [Export]
    public string? Name;
    [Export]
    public float Damage;
    [Export]
    public float Heal;
}
