using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace ApiSender.Controllers;

[ApiController]
[Route("[controller]")]
public class SendBusMessageController : ControllerBase
{
    private readonly ILogger<SendBusMessageController> _logger;
    private readonly IBus _bus;
    private readonly IBusControl _busControl;
    private readonly IRequestClient<SynchronousMessageRequest> _requestClientGet;
    public SendBusMessageController(ILogger<SendBusMessageController> logger, IBus bus, IBusControl busControl, IRequestClient<SynchronousMessageRequest> requestClientGet)
    {
        _logger = logger;
        _bus = bus;
        _busControl = busControl;
        _requestClientGet = requestClientGet;
    }

    private static int SyncCounter;
    private static int AsyncCounter;
    
    private static readonly object _lockerSync = new();
    private static readonly object _lockerAsync = new();

    [HttpGet("SendSynchronousMessage")]
    public async Task<string> SendSynchronousMessage()
    {
        lock (_lockerAsync)
        {
            SyncCounter++;
        }

        _logger.LogInformation("Sending a msg with count:{count}", SyncCounter);
        var msg = new SynchronousMessageRequest
        {
            Count = SyncCounter,
            SyncPayload = "a msg im sending to you, i do expect a response"
        };
        var response = await _requestClientGet.GetResponse<SynchronousMessageResponse>(msg);
        
        return $"msg sent with count:{msg.Count} received response back from Other service: {response.Message.SyncResponsePayload}";
    }

    [HttpGet("SendAsynchronousMessage")]
    public async Task<string> SendAsynchronousMessage()
    {
        lock (_lockerAsync)
        {
            AsyncCounter++;
        }

        var msg = new AsynchronousMessageRequest()
        {
            Count = AsyncCounter,
            AsyncPayload = "a msg im sending to you i dont expect a response",
        };
        await _bus.Publish(
            msg
        );
        return "Msg Sent";
    }

    
    
}