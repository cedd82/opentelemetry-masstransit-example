using ApiReceiver;
using ApiSender;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("start app");

try
{
    var builder = WebApplication.CreateBuilder(args);
    ConfigureLogging.Configure(builder);
    builder.AddOpenTracing();
    builder.Services.AddMassTransitForApp(builder.Configuration, null, typeof(ConfigureMassTransit).Assembly);
    
    // Add services to the container.

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    logger.Fatal(ex, "Stopped app because of exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}