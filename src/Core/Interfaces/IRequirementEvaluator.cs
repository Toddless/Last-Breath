namespace Core.Interfaces
{
    using Core.Enums;
    using Core.Interfaces.Data;

    public interface IRequirementEvaluator
    {
        RequirementType Type { get; }

        bool Evaluate<T>(T requirement, IGameServiceProvider provider)
            where T : IRequirement;
        
    }
}
