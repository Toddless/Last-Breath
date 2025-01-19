namespace Playground.Components
{
    using Godot;
    using Playground.Components.Interfaces;
    using Playground.Script.Helpers;

    [GlobalClass]
    public partial class ComponentBase : ObservableNode, IGameComponent
    {
    }
}
