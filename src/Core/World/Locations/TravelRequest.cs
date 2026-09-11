namespace Core.World.Locations
{
    using System.Threading.Tasks;
    using MessageBus;
    using Save;
    using Data.SaveData;

    public enum TravelResult { Completed, Busy, Unavailable, OutOfReach, InvalidConnection, Failed }
    public record TravelRequest(string SourceLocationId, string EndpointId) : IRequest<TravelResult>;
    public record LocationChangedMessage(string LocationId) : IMessage;

    public interface ILocationTravelService
    {
        bool IsTransitioning { get; }
        Task<TravelResult> TravelAsync(TravelRequest request);
    }

    public interface ILocationSaveCoordinator
    {
        bool IsTransitioning { get; }
        Task PrepareLoadAsync(SaveFile file);
        void CompleteLoad(bool success);
        PlayerPlacementSaveData CapturePlacement();
        void RestorePlacement(PlayerPlacementSaveData data);
    }

    public sealed class TravelRequestHandler(ILocationTravelService travel) : IRequestHandler<TravelRequest, TravelResult>
    {
        public Task<TravelResult> HandleRequest(TravelRequest request) => travel.TravelAsync(request);
    }
}
