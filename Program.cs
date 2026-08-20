using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Glibs.Learning.WebApp
{
    public class Program
    {
        public static readonly JsonSerializerOptions jsonOptionsDecode = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }; // Reserved for future
        public static readonly JsonSerializerOptions jsonOptionsEncode = new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        static IServiceCollection? serviceCollection = null;
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = Program.BuildApplication(args);
            WebApplication app = builder.Build();
            Program.SetupWebApp(app);
            Program.AddMappings(app);
            ICommonBuffer? commonBuffer = app?.Services.GetRequiredService<ICommonBuffer>();
            Console.WriteLine($"{commonBuffer?.ServiceStartTime.ToString()} # Starting web service at {commonBuffer?.Host}...");
            app?.Run();
        }
        static void SetupWebApp(WebApplication _app)
        {
            _app.UseResponseCompression();
        }
        static WebApplicationBuilder BuildApplication(string[] _args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(_args);
            //CommonBuffer commonBuffer = new CommonBuffer() { Host = builder.Configuration["Urls"] ?? "http://localhost:5050" };
            CommonBuffer commonBuffer = new CommonBuffer() { Host = builder.Configuration["Urls"] ?? "http://127.0.0.1:5050" };
            builder.Services.ConfigureHttpJsonOptions(options => // For: Results.Ok();
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.WriteIndented = true;
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            });
            builder.Services.AddResponseCompression( options => options.EnableForHttps = true);
            builder.Services.AddMemoryCache();
            //builder.Services.AddScoped<ILogger>();
            serviceCollection = builder.Services;
            builder.Services.AddSingleton<ICommonBuffer>(commonBuffer);
            builder.Services.AddSingleton<IConsoleLogService, LogService>();
            builder.Services.AddHostedService<TestBackgroundService>();
            return builder;
        }
        public static void AddMappings(WebApplication _app)
        {
            // / [GET]
            _app.MapGet("/api/v1", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received root command.", DateTime.Now.ToString());
                //app.Services.GetService<IConsoleLogService>().Log("Test logging!");
                //return Results.Text(ResponseHelper.GetHelp(""), contentType: "text/plain");
                return Results.Text(ResponseHelper.GetHelp(""));
            });
            // debug/appl-services [GET]
            _app.MapGet("/api/v1/debug/appl-services", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received 'debug/appl-services' command.", DateTime.Now.ToString());
                List<ApplicationService> applicationServicesList = new List<ApplicationService>();
                foreach (ServiceDescriptor serviceDescriptor in serviceCollection)
                    applicationServicesList.Add(new ApplicationService() { Name = serviceDescriptor.ServiceType.Name, LifeTime = $"{serviceDescriptor.Lifetime}" });
                return Results.Json<ApplicationServices>(new ApplicationServices() { ApplicationService = applicationServicesList }, Program.jsonOptionsEncode);
            });
            // debug/threads [GET]
            _app.MapGet("/api/v1/debug/threads", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received 'debug/threads' command.", DateTime.Now.ToString());

                Process currentProcess = Process.GetCurrentProcess();

                int totalThreadCount = currentProcess.Threads.Count;
                int managedThreadCount = ThreadPool.ThreadCount;

                double workingSetMb = currentProcess.WorkingSet64 / (1024.0 * 1024.0);
                double managedHeapMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

                // Get all OS-level threads
                var osThreadsList = new List<object>();
                foreach (ProcessThread thread in currentProcess.Threads)
                {
                    osThreadsList.Add(new
                    {
                        threadId = thread.Id,
                        state = thread.ThreadState.ToString(),
                        priorityLevel = thread.PriorityLevel.ToString(),
                        userProcessorTime = Math.Round(thread.UserProcessorTime.TotalMilliseconds, 2)
                    });
                }

                var response = new
                {
                    timestamp = DateTime.UtcNow,
                    threadInfo = new
                    {
                        totalThreadCount = totalThreadCount,
                        managedThreadCount = managedThreadCount,
                        threads = osThreadsList
                    },
                    memoryInfo = new
                    {
                        workingSetMb = Math.Round(workingSetMb, 2),
                        managedHeapMb = Math.Round(managedHeapMb, 2)
                    }
                };

                return Results.Json(response, Program.jsonOptionsEncode);
            });
            // debug/test [GET]
            _app.MapGet("/api/v1/debug/test", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received 'debug/test' command.", DateTime.Now.ToString());
                //return Results.Ok(new { Header = "Test link", Link = "http://localhost:5050/api/v1/debug/test-2" }); // Postman/Bruno do not follow links
                //return Results.Redirect("/api/v1/sys-info", permanent: true);
                return Results.Redirect("/api/v1/sys-info", permanent: false);
            });
            // debug/test-1 [GET]
            _app.MapGet("/api/v1/debug/test-1", (HttpContext _httpContext, ILogger<Program> _logger, IMemoryCache _cache) => 
            {
                _logger.LogInformation("{DateTime} # Received 'debug/test-1' command.", DateTime.Now.ToString());
                _cache.Set("001", "001");
                _cache.Set("002", 2);
                _cache.TryGetValue("001", out string? _value);
                Console.WriteLine(_value);
                return Results.Text("OK.");
            });
            // debug/test-2 [GET]
            _app.MapGet("/api/v1/debug/test-2", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received 'debug/test-2' command.", DateTime.Now.ToString());
                return Results.Text("OK.");
            });
            // debug/test-html [GET]
            _app.MapGet("/api/v1/debug/test-html", (HttpContext _httpContext, ILogger<Program> _logger) => // Produced by Gemini
            {
                _logger.LogInformation("{DateTime} # Received 'debug/test-html' command.", DateTime.Now.ToString());
                string html =   """
                                <!DOCTYPE html>
                                <html lang="en">
                                <head>
                                    <meta charset="UTF-8">
                                    <title>Hyperlink Test</title>
                                </head>
                                <body>
                                    <h2>Bruno Hyperlink Test</h2>
                                    <p>Click <a href="/api/v1/help">here</a> to navigate to the <code>/api/v1/help</code> endpoint.</p>
                                </body>
                                </html>
                                """;
                return Results.Content(html, contentType: "text/html");
            });
            // exit [POST]
            _app.MapPost("/api/v1/exit", (HttpContext _httpContext, ILogger<Program> _logger, ICommonBuffer _commonBuffer) =>
            {
                _logger.LogInformation("{DateTime} # Received 'exit' command.", DateTime.Now.ToString());
                _httpContext.RequestServices.GetService<IHostApplicationLifetime>()?.StopApplication();
                return Results.Text($"{DateTime.Now.ToString()} # Stopping web service {_commonBuffer.Host}...", statusCode: StatusCodes.Status202Accepted);
            });
            // help [GET]
            _app.MapGet("/api/v1/help", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received 'help' command.", DateTime.Now.ToString());
                return Results.Text(ResponseHelper.GetHelp(""), contentType: "text/plain");
            });
            // help/<command> [GET]
            _app.MapGet("/api/v1/help/{_helpCommand}", (string _helpCommand, HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received 'help/{_helpCommand}' command.", DateTime.Now.ToString(), _helpCommand);
                return Results.Text(ResponseHelper.GetHelp(_helpCommand), contentType: "text/plain");
            });
            // ping [GET]
            _app.MapGet("/api/v1/ping", async (HttpContext _httpContext, ILogger<Program> _logger, ICommonBuffer _commandBuffer, string? output, string? delay) =>
            {
                _logger.LogInformation("{DateTime} # Received '{path}' request with analyzed options: 'output={output}, 'delay={delay}'.", DateTime.Now.ToString(),
                    _httpContext.Request.Path, output, delay);
                string pingReply = ResponseHelper.PingReply(_commandBuffer.Host);
                if (int.TryParse(delay ?? "", out int delaySec))
                {
                    if (delaySec > 0)
                        await Task.Delay(delaySec * 1000);
                }
                if ((output ?? "").ToLower().Equals("json"))
                    return Results.Json(new ResponceJson { Response = pingReply }, Program.jsonOptionsEncode);
                else
                    return Results.Text($"{pingReply}");
            }
            );
            // ping-old [GET]
            _app.MapGet("/api/v1/ping-old", WebRequestProcessor.ProcessGetPing);
            // pwd [GET]
            _app.MapGet("/api/v1/pwd", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received 'pwd' command.", DateTime.Now.ToString());
                return Results.Text($"Current directory: {Directory.GetCurrentDirectory()}");
            });
            // sys-info [GET]
            _app.MapGet("/api/v1/sys-info", (HttpContext _httpContext, ILogger<Program> _logger, ICommonBuffer _commandBuffer) =>
            {
                _logger.LogInformation("{DateTime} # Received 'sys-info' command.", DateTime.Now.ToString());
                SysInfo sysInfo = new SysInfo(_httpContext.Connection.LocalIpAddress?.ToString(), 
                    _httpContext.Connection.LocalPort.ToString(),
                    _commandBuffer.MachineName,
                    _app.Environment.EnvironmentName,
                    _commandBuffer.ServiceStartTime.ToString(),
                    (DateTime.Now - _commandBuffer.ServiceStartTime).ToString()
                );
                return Results.Ok(sysInfo);
            });
        }
    }
    public interface ICommonBuffer
    {
        public DateTime ServiceStartTime { get; }
        public string MachineName { get; }
        public string? Host { get; init; }
    }
    public class CommonBuffer : ICommonBuffer
    {
        public DateTime ServiceStartTime { get; } = DateTime.Now ;
        public string MachineName { get; } = Environment.MachineName ;
        public string? Host { get; init; } 
    }
    public record SysInfo(string Host, string Port, string Machine, string Environment, string StartTime, string ActiveTime);
    public interface IConsoleLogService
    {
        void Log(string _message);
    }
    public class LogService : IConsoleLogService
    {
        public int Counter = 0;
        public void Log(string _message)
        {
            Counter++;
            Console.WriteLine($"{Counter}: {_message}");
        }
    }
    public class TestBackgroundService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken _ct)
        {
            while (!_ct.IsCancellationRequested)
            {
                Console.WriteLine($"{DateTime.Now.ToString()}    Background service is working.");
                try { await Task.Delay(60000, _ct); }
                catch (TaskCanceledException _) { }
            }
            await Task.CompletedTask;
        }
    }
    public static class ResponseHelper
    {
        public static string PingReply(string _host)
        {
            return $"""
                    {_host} is alive!
                    Use '/HELP' command for commands help.
                    """;
        }
        public static string GetHelp(string _command)
        {
            _command ??= "";
            string helpText = "";
            switch (_command.ToLower())
            {
                case "":
                    helpText = """
                                Welcome to Glibs's 'Hello world' web service!

                                Use commands:
                                DEBUG/APPL-SERVICES (get) -- list of active (automatically added) web application services.
                                DEBUG/TEST (get) -- command for testing something.
                                DEBUG/TEST1 (get) -- another command for testing something.
                                DEBUG/TEST2 (get) -- one more command for testing something.
                                DEBUG/THREADS (get) -- shows active thread count and memory usage.
                                EXIT (post) -- stop current web service.
                                HELP (get) -- get this help.
                                PING (get) -- test connection.
                                        More details on: help/ping.
                                PING-OLD (get) -- test connection, old version (delegate method instead of lambda).
                                        More details on: help/ping-old.
                                PWD (get) -- shows current directory of web service.
                                SYS-INFO (get) -- shows service state (start time and so on).
                                """;
                    break;
                case "ping":
                case "ping-old":
                    helpText = """
                                Query options:
                        
                                output=<text/json> -- sets responce format as text or JSON.
                                timeout=<seconds> -- returns responce with delay. <seconds> must be integer.
                                """;
                    break;
                default:
                    helpText = $"No help for command '{_command}' awailable.";
                    break;
            }
            return helpText;
        }
    }
    public struct ApplicationService
    {
        public string Name { get; set; }
        public string LifeTime { get; set; }
    }
    public struct ApplicationServices
    {
        public IEnumerable<ApplicationService> ApplicationService { get; set; }
    }
}
