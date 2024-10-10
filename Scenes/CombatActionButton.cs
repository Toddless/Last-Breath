using Godot;
using Playground.Script;

public partial class CombatActionButton : Button
{

    private CombatAction _combatAction;


    private void OnPressed()
    {
        var mainNode = GetNode("/root");
        // получаем скрипт менеджера боя
        var characterCast = mainNode.GetNode<Character>("Character");
        // подписываемся на событие начала хода
        characterCast.CastCombatAction(_combatAction);
    }
}
