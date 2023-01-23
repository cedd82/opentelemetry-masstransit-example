using MassTransit;
using Shared;

namespace ApiReceiver.MessageHandlers;

public class AsynchronousMessageHandler : IConsumer<AsynchronousMessageRequest>
{
    private readonly ILogger<AsynchronousMessageHandler> _logger;

    public AsynchronousMessageHandler(ILogger<AsynchronousMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AsynchronousMessageRequest> context)
    {
        _logger.LogInformation("received a new async message with messageId:{messageId}", context.MessageId);
        var msg = context.Message;

        if (msg.Count % 2 == 0)
            throw new Exception("i dont like this async message because its even");
        
        // nothing to respond with an async msg
    }
}