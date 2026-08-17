namespace Battle.Source.RequestHandlers
{
    using System.Threading.Tasks;
    using Abilities;
    using Core.Battle.Abilities;
    using Core.MessageBus;
    using Core.MessageBus.Requests;

    /// <summary>The bus door onto the attach gate, and nothing besides: the order of the checks and the
    /// moving of the thing live in <see cref="IOrnamentAttachGate"/>, so an interface mirrors the gate
    /// instead of reading the rule a second time.</summary>
    public class AttachOrnamentRequestHandler(IOrnamentAttachGate gate)
        : IRequestHandler<AttachOrnamentRequest, OrnamentAttachResult>
    {
        public Task<OrnamentAttachResult> HandleRequest(AttachOrnamentRequest request) =>
            Task.FromResult(gate.Attach(request.ItemInstanceId, request.AbilityId));
    }

    /// <inheritdoc cref="AttachOrnamentRequestHandler"/>
    public class DetachOrnamentRequestHandler(IOrnamentAttachGate gate)
        : IRequestHandler<DetachOrnamentRequest, OrnamentDetachResult>
    {
        public Task<OrnamentDetachResult> HandleRequest(DetachOrnamentRequest request) =>
            Task.FromResult(gate.Detach(request.OrnamentId));
    }
}
