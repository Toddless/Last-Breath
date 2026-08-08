namespace Battle.Source.RequestHandlers
{
    using System.Threading.Tasks;
    using Abilities;
    using Core.Battle.Abilities;
    using Core.MessageBus;
    using Core.MessageBus.Requests;

    /// <summary>
    /// The bus door onto the install gate, and nothing besides. The order of the checks and the moving
    /// of the copy live in <see cref="IAugmentInstallGate"/> because a window has to ask the same
    /// question synchronously while a drag is in the air, and a handler with a copy of that order would
    /// be the second reading of it the whole design is built to avoid.
    /// </summary>
    public class InstallAugmentRequestHandler(IAugmentInstallGate gate)
        : IRequestHandler<InstallAugmentRequest, AugmentInstallResult>
    {
        public Task<AugmentInstallResult> HandleRequest(InstallAugmentRequest request) =>
            Task.FromResult(gate.Install(request.SocketId, request.ItemInstanceId));
    }
}
