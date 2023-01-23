using System.Reflection;
using Azure.Messaging.ServiceBus;
using MassTransit;
using MassTransit.Audit;
using NLog;

namespace ApiReceiver;

public static class ConfigureMassTransit
{
    public static IServiceCollection AddMassTransitForApp(this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator> registrationAction,
        params Assembly[] assemblies)
    {
        services.AddMassTransit(mt =>
        {
            mt.AddConsumers(assemblies);
            registrationAction?.Invoke(mt);
            var serviceProvider = services.BuildServiceProvider();
            //var auditStore = serviceProvider.GetService<IMessageAuditStore>();

            //note should we implement observers to create logs on the events where an error has occurred?

            //mt.AddBusObserver<BusObserver>();
            //mt.AddReceiveObserver<ReceiveObserver>();
            //mt.AddConsumeObserver<ConsumeObserver>();
            //mt.AddSendObserver<SendObserver>();
            //mt.AddPublishObserver<PublishObserver>();

            if (configuration.GetValue<bool>("AzureServiceBusEnabled"))
            {
                mt.UsingAzureServiceBus((context, cfg) =>
                {
                    var connectionString = configuration["AzureServiceBusConnection"];
                    cfg.UseServiceBusMessageScheduler();
                    //cfg.Host(connectionString);
                    cfg.Host(connectionString, x =>
                    {
                        //x.TransportType = ServiceBusTransportType.AmqpTcp;
                        x.TransportType = ServiceBusTransportType.AmqpWebSockets;
                    }); 
                    //cfg.MessageTopology.SetEntityNameFormatter(new MamboEntityNameFormatter(cfg.MessageTopology.EntityNameFormatter));
                    //cfg.ConnectSendAuditObservers(auditStore);
                    //cfg.ConnectConsumeAuditObserver(auditStore);
                    cfg.ConfigureEndpoints(context);

                });
            }
            else
            {
                mt.UsingRabbitMq((context, cfg) =>
                {
                    //cfg.MessageTopology.SetEntityNameFormatter(new MamboEntityNameFormatter(cfg.MessageTopology.EntityNameFormatter));
                    cfg.UseDelayedMessageScheduler();
                    //cfg.ConnectSendAuditObservers(auditStore);
                    //cfg.ConnectConsumeAuditObserver(auditStore);
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }
}


public class BusObserver : IBusObserver
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public void PostCreate(IBus bus)
    {
        // called after the bus has been created, but before it has been started.
    }

    public void CreateFaulted(Exception exception)
    {
        // called if the bus creation fails for some reason
        _logger.Log(NLog.LogLevel.Error, exception, "ExceptionType:{exceptionType}", "massTransit");
    }

    public Task PreStart(IBus bus)
    {
        // called just before the bus is started
        return Task.CompletedTask;
    }

    public Task PostStart(IBus bus, Task<BusReady> busReady)
    {
        // called once the bus has been started successfully. The task can be used to wait for
        // all of the receive endpoints to be ready.
        return Task.CompletedTask;
    }

    public Task StartFaulted(IBus bus, Exception exception)
    {
        // called if the bus fails to start for some reason (dead battery, no fuel, etc.)
        _logger.Log(NLog.LogLevel.Error, exception, "ExceptionType:{exceptionType}", "massTransit");
        return Task.CompletedTask;
    }

    public Task PreStop(IBus bus)
    {
        // called just before the bus is stopped
        return Task.CompletedTask;
    }

    public Task PostStop(IBus bus)
    {
        // called after the bus has been stopped
        return Task.CompletedTask;
    }

    public Task StopFaulted(IBus bus, Exception exception)
    {
        // called if the bus fails to stop (no brakes)
        _logger.Log(NLog.LogLevel.Error, exception, "ExceptionType:{exceptionType}", "massTransit");
        return Task.CompletedTask;
    }
}

public class ReceiveObserver : IReceiveObserver
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public Task PreReceive(ReceiveContext context)
    {
        // called immediately after the message was delivery by the transport
        return Task.CompletedTask;
    }

    public Task PostReceive(ReceiveContext context)
    {
        // called after the message has been received and processed
        return Task.CompletedTask;
    }

    public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class
    {
        // called when the message was consumed, once for each consumer
        return Task.CompletedTask;
    }

    public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan elapsed, string consumerType, Exception exception) where T : class
    {
        // called when the message is consumed but the consumer throws an exception
        var destinationAddress = context.DestinationAddress;
        var msgType = context.GetType();
        _logger.Log(NLog.LogLevel.Error, exception, "ExceptionType:{exceptionType} msgId:{msgId} destinationAddress:{destinationAddress} msgType:{msgType}",
            "massTransit", context.MessageId?.ToString(), destinationAddress?.ToString(), msgType.ToString());
        return Task.CompletedTask;
    }

    public Task ReceiveFault(ReceiveContext context, Exception exception)
    {
        // called when an exception occurs early in the message processing, such as deserialization, etc.

        var inputAddress = context.InputAddress;
        var msgType = context.GetType();
        _logger.Log(NLog.LogLevel.Error, exception, "ExceptionType:{exceptionType} msgId:{msgId} inputAddress:{inputAddress} msgType:{msgType}",
            "massTransit", context.GetMessageId()?.ToString(), inputAddress?.ToString(), msgType.ToString());

        return Task.CompletedTask;
    }
}

public class PublishObserver : IPublishObserver
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public Task PrePublish<T>(PublishContext<T> context)
        where T : class
    {
        // called right before the message is published (sent to exchange or topic)
        return Task.CompletedTask;
    }

    public Task PostPublish<T>(PublishContext<T> context)
        where T : class
    {
        // called after the message is published (and acked by the broker if RabbitMQ)
        return Task.CompletedTask;
    }

    public Task PublishFault<T>(PublishContext<T> context, Exception exception)
        where T : class
    {
        // called if there was an exception publishing the message
        var destinationAddress = context.DestinationAddress;
        var msgType = context.GetType();
        _logger.Log(NLog.LogLevel.Error, exception, "ExceptionType:{exceptionType} msgId:{msgId} destinationAddress:{destinationAddress} msgType:{msgType}",
            "massTransit", context.MessageId.ToString(), destinationAddress?.ToString(), msgType.ToString());

        return Task.CompletedTask;
    }
}

public class SendObserver : ISendObserver
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public Task PreSend<T>(SendContext<T> context)
        where T : class
    {
        // called just before a message is sent, all the headers should be setup and everything
        var t = context.Message.GetType();
        return Task.CompletedTask;
    }

    public Task PostSend<T>(SendContext<T> context)
        where T : class
    {
        // called just after a message it sent to the transport and acknowledged (RabbitMQ)
        return Task.CompletedTask;
    }

    public Task SendFault<T>(SendContext<T> context, Exception exception)
        where T : class
    {
        // called if an exception occurred sending the message
        var destinationAddress = context.DestinationAddress;
        var msgType = context.GetType();
        _logger.Log(NLog.LogLevel.Error, exception, "ExceptionType:{exceptionType} msgId:{msgId} destinationAddress:{destinationAddress} msgType:{msgType}",
            "massTransit", context.MessageId.ToString(), destinationAddress?.ToString(), msgType.ToString());
        return Task.CompletedTask;
    }
}

public class ConsumeObserver : IConsumeObserver
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    Task IConsumeObserver.PreConsume<T>(ConsumeContext<T> context)
    {
        // called before the consumer's Consume method is called
        return Task.CompletedTask;
    }

    Task IConsumeObserver.PostConsume<T>(ConsumeContext<T> context)
    {
        // called after the consumer's Consume method is called
        // if an exception was thrown, the ConsumeFault method is called instead
        return Task.CompletedTask;
    }

    Task IConsumeObserver.ConsumeFault<T>(ConsumeContext<T> context, Exception exception)
    {
        // called if the consumer's Consume method throws an exception
        var destinationAddress = context.DestinationAddress;
        var msgType = context.GetType();
        _logger.Log(NLog.LogLevel.Error, exception, "ExceptionType:{exceptionType} msgId:{msgId} destinationAddress:{destinationAddress} msgType:{msgType}",
            "massTransit", context.MessageId?.ToString(), destinationAddress?.ToString(), msgType.ToString());
        return Task.CompletedTask;
    }
}