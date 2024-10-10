using Godot;
using Playground;

public partial class BattleTurnManager : Node
{
    [Export]
    private Node _player;
    [Export]
    private Node _enemy;
    private Node _currentCharacter;
    [Export]
    private float _nextTurnDelay = 1.0f;
    private bool _gameOver = false;

    [Signal]
    public delegate void OnCharacterBeginTurnEventHandler(Node character);
    [Signal]
    public delegate void OnCharacterEndTurnEventHandler(Node character);

    private void BeginNextTurn()
    {
        if (_currentCharacter == _player)
        {
            _currentCharacter = _enemy;
        }
        else if (_currentCharacter == _enemy)
        {
            _currentCharacter = _player;
        }
        else
        {
            _currentCharacter = _player;
        }

        EmitSignal(SignalName.OnCharacterBeginTurn, _currentCharacter);
    }

    private async void EndCurrentTurn()
    {
        EmitSignal(SignalName.OnCharacterEndTurn, _currentCharacter);
        await ToSignal(GetTree().CreateTimer(_nextTurnDelay), "timeout");
        if (!_gameOver)
        {
            BeginNextTurn();
        }
    }

    private void CharacterDied(Node character)
    {
        _gameOver = true;
        if (character is Player)
        {
            GD.Print("Player died");
        }
        else
        {
            GD.Print("Enemy died");
        }
    }

    public override void _Ready()
    {
        BeginNextTurn();
    }
}
