using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ApiSender;

public static class ConfigureOpenTracing
{
    public static void AddOpenTracing(this WebApplicationBuilder builder)
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        var serviceName = "ApiSender";
        var otlpExporterEndpoint = "http://127.0.0.1:4317";

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .AddSource(serviceName)
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(serviceName))
                    .AddHttpClientInstrumentation()
                    .AddMassTransitInstrumentation()
                    .AddAspNetCoreInstrumentation();
                
                tracerProviderBuilder.AddSource("ApiSender.*");
                tracerProviderBuilder.AddSource("MassTransit");


                tracerProviderBuilder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddOtlpExporter(o => { o.Endpoint = new Uri(otlpExporterEndpoint); });
            }).StartWithHost();

        builder.Services.AddSingleton(TracerProvider.Default.GetTracer(serviceName));
    }
}