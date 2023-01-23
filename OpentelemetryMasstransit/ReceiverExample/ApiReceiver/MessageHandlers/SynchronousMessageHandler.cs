using MassTransit;
using Shared;

namespace ApiReceiver.MessageHandlers;

public class SynchronousMessageHandler : IConsumer<SynchronousMessageRequest>
{
    private readonly ILogger<SynchronousMessageHandler> _logger;

    public SynchronousMessageHandler(ILogger<SynchronousMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SynchronousMessageRequest> context)
    {
        _logger.LogInformation("received a new sync message with messageId:{messageId}", context.MessageId);
        var msg = context.Message;

        if (msg.Count % 2 == 0)
            throw new Exception("i dont like this sync message because its even");
        
        var responseMessage = $"received sync msg with Count: {msg.Count} i like it because its odd";

        var response = new SynchronousMessageResponse
        {
            SyncResponsePayload = responseMessage,
            
        };
        await context.RespondAsync<SynchronousMessageResponse>(response);
    }
}