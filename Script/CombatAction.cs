using Godot;

[GlobalClass]
public partial class CombatAction : Resource
{
    [Export]
    private string _name;
    [Export]
    private float _damage;
    [Export]
    private float _heal;
}
