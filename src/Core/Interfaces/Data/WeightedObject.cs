namespace Core.Interfaces.Data
{
    public record WeightedObject<T>(T Obj, float From, float To, float Weight);
}
