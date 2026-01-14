namespace Core.Interfaces
{
    using Enums;
    using Data;

    public interface IRequirementEvaluator
    {
        RequirementType Type { get; }

        bool Evaluate<T>(T requirement, IGameServiceProvider provider)
            where T : IRequirement;
        
    }
}
