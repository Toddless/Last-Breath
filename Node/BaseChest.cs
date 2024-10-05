using Godot;

public partial class BaseChest : Node2D
{
    [Signal]
    public delegate void PlayerEnteredChestZoneEventHandler();


    public override void _Ready()
    {
        this.PlayerEnteredChestZone += PlayerEnteredZone;
    }

    private void PlayerEnteredZone()
    {

    }
}
