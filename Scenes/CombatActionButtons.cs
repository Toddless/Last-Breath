using Godot;
using Godot.Collections;
using Playground;

public partial class CombatActionButtons : VBoxContainer
{
    [Export]
    private Array _buttons;

    public override void _Ready()
    {
        var mainNode = GetNode("/root");
        // получаем скрипт менеджера боя
        var battleManager = mainNode.GetNode<BattleTurnManager>("BattleScene");
        // подписываемся на событие начала хода
        battleManager.OnCharacterBeginTurn +=OnCharacterBeginTurn;
        battleManager.OnCharacterEndTurn +=OnCharacterEndTurn;
    }

    private void OnCharacterBeginTurn(Node character)
    {
       Visible = character is Player;
    }

    private void OnCharacterEndTurn(Node character)
    {
        Visible = false;
    }
}
