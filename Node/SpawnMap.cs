using Godot;

public partial class SpawnMap : TileMapLayer
{
    public override void _Draw()
    {
        int gridSize = 64;
        Vector2 screenSize = GetViewportRect().Size;

        for (int i = 0; i <= screenSize.Y / gridSize; i++)
        {
            DrawLine(new Vector2(0, i * gridSize), new Vector2(screenSize.X, i * gridSize), Colors.White);
        }

        for (int i = 0; i <= screenSize.X / gridSize; i++)
        {
            DrawLine(new Vector2(i * gridSize, 0), new Vector2(i * gridSize, screenSize.Y), Colors.White);
        }
    }
    
}
