using Logzio.DotNet.NLog;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Web;
using LogLevel = NLog.LogLevel;

namespace ApiSender;

public static class ConfigureLogging
{
    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Host.UseNLog(new NLogAspNetCoreOptions
        {
            CaptureMessageTemplates = true,
            CaptureMessageProperties = true,
            IncludeScopes = true
        });

        var logzioLoggingToken = builder.Configuration.GetValue<string>("LogzioLoggingToken");
        ConfigureLogzioTarget(logzioLoggingToken);
    }


    private static void ConfigureLogzioTarget(string logzioLoggingToken)
    {
        var applicationName = "ApiSender";
        var config = LogManager.Configuration;
        var logzioTarget = new CustomLogzioTarget
        {
            Name = "Logzio",
            Token = logzioLoggingToken,
            LogzioType = "nlog",
            ListenerUrl = "https://listener-au.logz.io:8071",
            BufferSize = 100,
            BufferTimeout = TimeSpan.Parse("00:00:05"),
            RetriesMaxAttempts = 3,
            RetriesInterval = TimeSpan.Parse("00:00:02"),
            Debug = false,
            JsonKeysCamelCase = false,
            ApplicationName = applicationName,
            RuntimeEnvironment = "CedricsMachine",
            UseGzip = true,
            //ActivityId=${activity:property=TraceId} is used to match traces and logs
            Layout = "${longdate}|ActivityId=${activity:property=TraceId}|${level:uppercase=true}|${logger}|${message}"
        };

        var rule = new LoggingRule("*", LogLevel.Trace, LogLevel.Fatal, logzioTarget);
        rule.RuleName = "logzioRule";
        config.LoggingRules.Add(rule);
        LogManager.Configuration = config;
        LogManager.ReconfigExistingLoggers();
    }


    [Target("CustomLogzioTarget")]
    internal class CustomLogzioTarget : LogzioTarget
    {
        [RequiredParameter]
        public string ApplicationName { get; set; }

        [RequiredParameter]
        public string RuntimeEnvironment { get; set; }

        protected override void ExtendValues(LogEventInfo logEvent, Dictionary<string, object> dictionary)
        {
            dictionary["localDt"] = logEvent.TimeStamp.ToLocalTime();
            dictionary["machineName"] = Environment.MachineName;
            dictionary["sequenceId"] = logEvent.SequenceID;
            dictionary["utc"] = logEvent.TimeStamp.ToUniversalTime();
            dictionary["programeName"] = ApplicationName;
            dictionary["programGroup"] = "Gateways";
            dictionary["runtimeEnvironment"] = RuntimeEnvironment;
            ScopeContext.TryGetProperty("TraceId", out var traceId);
            if (traceId != null)
                dictionary["traceId"] = traceId.ToString();

            dictionary["severity"] = logEvent.Level.Name.ToUpper();
            dictionary["sourceFilePath"] = logEvent.CallerFilePath;
            dictionary["sourceMemberName"] = logEvent.CallerMemberName;
            dictionary["sourceLineNumber"] = logEvent.CallerLineNumber;
            dictionary["sourceClassName"] = logEvent.CallerClassName;

            if (logEvent.Exception != null)
                dictionary["exception"] = logEvent.Exception.ToString();
        }
    }
}