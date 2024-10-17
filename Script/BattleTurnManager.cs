using Godot;

//[Signal]
//public delegate void OnCharacterBeginTurnEventHandler(Character character);
//[Signal]
//public delegate void OnCharacterEndTurnEventHandler(Character character);

//public void BeginNextTurn()
//{
//    if (_currentCharacter == _player)
//    {
//        _currentCharacter = _enemy;
//    }
//    else if (_currentCharacter == _enemy)
//    {
//        _currentCharacter = _player;
//    }
//    else
//    {
//        _currentCharacter = _player;
//    }

//    EmitSignal(SignalName.OnCharacterBeginTurn, _currentCharacter);
//}

//public async void EndCurrentTurn()
//{
//    EmitSignal(SignalName.OnCharacterEndTurn, _currentCharacter);
//    await ToSignal(GetTree().CreateTimer(_nextTurnDelay), "timeout");
//    if (!_gameOver)
//    {
//        BeginNextTurn();
//    }
//}

//private void CharacterDied(Character character)
//{
//    _gameOver = true;
//    if (character == _player)
//    {
//        GD.Print("Player died");
//    }
//    else
//    {
//        GD.Print("Enemy died");
//    }
//}

//public override void _Ready()
//{
//    BeginNextTurn();
//    _player.OnCharacterDied += CharacterDied;
//}

public partial class BattleTurnManager : Node
{
    [Export]
    private float _nextTurnDelay = 1.0f;
    private bool _gameOver = false;
    private CharacterBody2D? _player;

    public override void _Ready()
    {
        _player = GetNode<CharacterBody2D>(nameof(CharacterBody2D));
    }
}
