using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared;


[ApiController]
[Route("[controller]")]
public class BusStatusController : ControllerBase
{
    private readonly ILogger<BusStatusController> _logger;
    private readonly IBus _bus;
    private readonly IBusControl _busControl;

    public BusStatusController(ILogger<BusStatusController> logger, IBus bus, IBusControl busControl)
    {
        _logger = logger;
        _bus = bus;
        _busControl = busControl;
    }

    [HttpGet("BusStatus")]
    public async Task<object> GetBusStatus()
    {
        _logger.LogInformation("getting bus status for api sender");
        var health = _busControl.CheckHealth();
        var endpoints = health.Endpoints.Select(x => new
        {
            x.Key,
            x.Value.Description,
            x.Value.Status,
            x.Value.Exception,
            x.Value.ReceiveEndpoint.InputAddress,

        });

        var response = new
        {
            health.Description,
            health.Status,
            health.Exception,
            Endpoints = endpoints
        };
       
        return response;
    }
}
