using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Prometheus;

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
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = Program.BuildApplication(args);
            WebApplication app = builder.Build();
            Program.SetupWebApp(app);
            ICommonBuffer? commonBuffer = app?.Services.GetRequiredService<ICommonBuffer>();
            app?.Services.GetRequiredService<ILogger<Program>>().LogInformation("Starting web service at {Host}", commonBuffer?.Host);
            app?.Run();
        }
        static void SetupWebApp(WebApplication _app)
        {
            _app.UseResponseCompression();
            _app.UseHttpMetrics();
            _app.MapMetrics("/api/v1/metrics");
            _app.Use(async (context, next) => {
                ILogger<Program> logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Request received. Path: '{Path}'; Query: '{Query}'.", context.Request.Path, context.Request.QueryString);
                await next(context);
            });
            Program.AddMappings(_app);
        }
        static WebApplicationBuilder BuildApplication(string[] _args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(_args);
            string host = builder.Configuration["Urls"] ?? "http://127.0.0.1:5050"; // Or: http://localhost:5050
            CommonBuffer commonBuffer = new CommonBuffer() { Host = host };
            builder.Services.ConfigureHttpJsonOptions(options => // Format for Results.Ok();
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.WriteIndented = true;
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            });
            builder.Logging.AddSimpleConsole(options => 
            {
                options.TimestampFormat = "HH:mm:ss dd.MM.yyyy :: ";
                //options.IncludeScopes = true; // Reserved for future & debugging 
            });
            builder.Services.AddResponseCompression( options => options.EnableForHttps = true);
            builder.Services.AddMemoryCache();
            commonBuffer.ServiceCollection = builder.Services;
            builder.Services.AddSingleton<ICommonBuffer>(commonBuffer);
            builder.Services.AddHostedService<TestBackgroundService>();
            return builder;
        }
        public static void AddMappings(WebApplication _app)
        {
            RouteGroupBuilder apiGroup = _app.MapGroup("/api/v1");
            // / [GET]
            apiGroup.MapGet("", (HttpContext _httpContext) =>
            {
                return Results.Text(ResponseHelper.GetHelp(""));
            });
            RouteGroupBuilder debugGroup = apiGroup.MapGroup("/debug");
            // debug/appl-services [GET]
            debugGroup.MapGet("/appl-services", (HttpContext _httpContext, ICommonBuffer _commonBuffer) =>
            {
                List<ApplicationService> applicationServicesList = new List<ApplicationService>();
                if (_commonBuffer.ServiceCollection != null)
                {
                    int counter = 0;
                    foreach (ServiceDescriptor serviceDescriptor in _commonBuffer.ServiceCollection)
                        applicationServicesList.Add(new ApplicationService(Number: ++counter, Name: serviceDescriptor.ServiceType.Name, LifeTime: $"{serviceDescriptor.Lifetime}"));
                }
                return Results.Json<ApplicationServices>(new ApplicationServices( ApplicationService: applicationServicesList), Program.jsonOptionsEncode);
            });
            // debug/memory [GET]
            debugGroup.MapGet("/memory", ResponseHelper.ProcessMemoryReport); // Just once. A demo of using delegates.
            // debug/test-redirect [GET]
            debugGroup.MapGet("/test", (HttpContext _httpContext) =>
            {
                // Reserver for tests and debugging
                return Results.Ok(new { Header = "Test link", Link = "/api/v1/debug/test-redirect" }); // Postman/Bruno do not follow links
            });
            // debug/test-1 [GET]
            debugGroup.MapGet("/test-1", (HttpContext _httpContext, IMemoryCache _cache) => 
            {
                _cache.Set("001", "001"); // Reserved for tests
                _cache.Set("002", 2);
                _cache.TryGetValue("001", out string? _value);
                Console.WriteLine(_value);
                return Results.Text("OK.");
            });
            // debug/test-2 [GET]
            debugGroup.MapGet("/test-2", (HttpContext _httpContext) =>
            {
                return Results.Text("OK."); // Reserved for tests
            });
            // debug/test-html [GET]
            debugGroup.MapGet("/test-html", (HttpContext _httpContext) => // Produced by Gemini
            {
                return Results.Content(ResponseHelper.TestHtmlPage(), contentType: "text/html");
            });
            // debug/test-redirect [GET]
            debugGroup.MapGet("/test-redirect", (HttpContext _httpContext) =>
            {
                //return Results.Redirect("/api/v1/sys-info", permanent: true); // Another option
                return Results.Redirect("/api/v1/sys-info", permanent: false);
            });
            // debug/threads [GET]
            debugGroup.MapGet("/threads", (HttpContext _httpContext) =>
            {
                return Results.Json(ResponseHelper.ThreadReport(), Program.jsonOptionsEncode);
            });
            // exit [POST]
            apiGroup.MapPost("/exit", (HttpContext _httpContext, ICommonBuffer _commonBuffer) =>
            {
                _httpContext.RequestServices.GetService<IHostApplicationLifetime>()?.StopApplication();
                return Results.Text($"{DateTime.Now.ToString()} # Stopping web service {_commonBuffer.Host}...", statusCode: StatusCodes.Status202Accepted);
            });
            // help [GET]
            apiGroup.MapGet("/help", (HttpContext _httpContext) =>
            {
                return Results.Text(ResponseHelper.GetHelp(""), contentType: "text/plain");
            });
            // help/<command> [GET]
            apiGroup.MapGet("/help/{_helpCommand}", (string _helpCommand, HttpContext _httpContext) =>
            {
                return Results.Text(ResponseHelper.GetHelp(_helpCommand), contentType: "text/plain");
            });
            // ping [GET]
            apiGroup.MapGet("/ping", async (HttpContext _httpContext, ICommonBuffer _commonBuffer, string? output, string? delay) =>
            {
                string pingReply = ResponseHelper.PingReply(_commonBuffer.Host ?? "");
                if (int.TryParse(delay ?? "", out int delaySec))
                {
                    if (delaySec > 0)
                        await Task.Delay(delaySec * 1000);
                }
                if ((output ?? "").ToLower().Equals("json"))
                    return Results.Json(new ResponseJson(pingReply), Program.jsonOptionsEncode);
                else
                    return Results.Text($"{pingReply}");
            }
            );
            // pwd [GET]
            apiGroup.MapGet("/pwd", (HttpContext _httpContext) =>
            {
                return Results.Text($"Current directory: {Directory.GetCurrentDirectory()}");
            });
            // sys-info [GET]
            apiGroup.MapGet("/sys-info", (HttpContext _httpContext, ICommonBuffer _commandBuffer) =>
            {
                SysInfo sysInfo = new SysInfo(_httpContext.Connection.LocalIpAddress?.ToString() ?? "", 
                    _httpContext.Connection.LocalPort.ToString(),
                    _commandBuffer.MachineName,
                    _app.Environment.EnvironmentName,
                    _commandBuffer.ServiceStartTime,
                    (DateTime.Now - _commandBuffer.ServiceStartTime).ToString()
                );
                return Results.Ok(sysInfo);
            });
        }
    }
    public interface ICommonBuffer
    {
        public DateTime ServiceStartTime { get; }
        public string? MachineName { get; }
        public string? Host { get; init; }
        public IServiceCollection? ServiceCollection { get; set; }
    }
    public class CommonBuffer : ICommonBuffer
    {
        public DateTime ServiceStartTime { get; } = DateTime.Now ;
        public string? MachineName { get; } = Environment.MachineName ;
        public string? Host { get; init; }
        public IServiceCollection? ServiceCollection { get; set; }
    }
    public record SysInfo(string Host, string Port, string Machine, string Environment, DateTime StartTime, string ActiveTime);
    public class TestBackgroundService : BackgroundService
    {
        private readonly ILogger<TestBackgroundService> logger;
        public TestBackgroundService(ILogger<TestBackgroundService> _logger) => this.logger = _logger;
        protected override async Task ExecuteAsync(CancellationToken _ct)
        {
            while (!_ct.IsCancellationRequested)
            {
                this.logger.LogInformation("Background service is working.");
                //Console.WriteLine($"{DateTime.Now.ToString()}    Background service is working.");
                try { await Task.Delay(60000, _ct); }
                catch (TaskCanceledException _) { }
            }
            await Task.CompletedTask;
        }
    }
    public static class ResponseHelper
    {
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
                                DEBUG/MEMORY (get) -- shows memory usage report.
                                DEBUG/TEST (get) -- endpoint for testing something.
                                DEBUG/TEST-1 (get) -- another endpoint for testing something.
                                DEBUG/TEST-2 (get) -- one more endpoint for testing something.
                                DEBUG/TEST-HTML (get) -- returns tes page in HTML format to check Postman/Bruno follows links in HTML results.
                                DEBUG/TEST-REDIRECT (get) -- shows how works URL redirect.
                                DEBUG/THREADS (get) -- shows active thread count info.
                                EXIT (post) -- stop current web service.
                                HELP (get) -- get this help.
                                METRICS (get) -- get the web application Prometheus metrics.
                                PING (get) -- test connection.
                                        More details on: help/ping.
                                PWD (get) -- shows current directory of web service.
                                SYS-INFO (get) -- shows service state (start time and so on).
                                """;
                    break;
                case "ping":
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
        public static string PingReply(string _host)
        {
            return $"""
                    {_host} is alive!
                    Use '/HELP' command for commands help.
                    """;
        }
        public record ProcessMemoryData(double WorkingSetMb, double PrivateMemoryMb, double VirtualMemoryMb);
        public record ManagedHeapData(double TotalManagedHeapMb, double TotalAllocatedLifetimeMb, double GcHeapSizeBytesMb, double FragmentedMb, long MemoryLoadPercentage);
        public record GarbageCollectorData(int Gen0Collections, int Gen1Collections, int Gen2Collections, double PauseTimePercentage);
        public record MemoryReportData(ProcessMemoryData ProcessMemory, ManagedHeapData ManagedHeap, GarbageCollectorData GarbageCollector);
        public static IResult ProcessMemoryReport(HttpContext _httpContext)
        {
            Process process = Process.GetCurrentProcess();
            GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();
            ProcessMemoryData processMemory = new(
                WorkingSetMb: Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 2),
                PrivateMemoryMb: Math.Round(process.PrivateMemorySize64 / 1024.0 / 1024.0, 2),
                VirtualMemoryMb: Math.Round(process.VirtualMemorySize64 / 1024.0 / 1024.0, 2)
            );
            ManagedHeapData managedHeap = new(
                TotalManagedHeapMb: Math.Round(GC.GetTotalMemory(forceFullCollection: false) / 1024.0 / 1024.0, 2),
                TotalAllocatedLifetimeMb: Math.Round(GC.GetTotalAllocatedBytes() / 1024.0 / 1024.0, 2),
                GcHeapSizeBytesMb: Math.Round(gcInfo.HeapSizeBytes / 1024.0 / 1024.0, 2),
                FragmentedMb: Math.Round(gcInfo.FragmentedBytes / 1024.0 / 1024.0, 2),
                MemoryLoadPercentage: gcInfo.MemoryLoadBytes * 100 / Math.Max(1, gcInfo.TotalAvailableMemoryBytes)
            );
            GarbageCollectorData garbageCollector = new(
                Gen0Collections: GC.CollectionCount(0),
                Gen1Collections: GC.CollectionCount(1),
                Gen2Collections: GC.CollectionCount(2),
                PauseTimePercentage: Math.Round(gcInfo.PauseTimePercentage, 2)
            );
            MemoryReportData memoryReport = new(processMemory, managedHeap, garbageCollector);
            return Results.Json(memoryReport, Program.jsonOptionsEncode);
        }
        public static string TestHtmlPage()
        {
            return """
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
        }
        public record ThreadData(int TotalThreadCount, int ManagedThreadCount);
        public record MemoryData(double WorkingSetMb, double ManagedHeapMb);
        public record ThreadMemoryData(DateTime Timestamp, ThreadData ThreadInfo, MemoryData MemoryInfo);
        public static ThreadMemoryData ThreadReport()
        {
            Process currentProcess = Process.GetCurrentProcess();
            int totalThreadCount = currentProcess.Threads.Count;
            int managedThreadCount = ThreadPool.ThreadCount;
            double workingSetMb = Math.Round(currentProcess.WorkingSet64 / (1024.0 * 1024.0), 2);
            double managedHeapMb = Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 2);
            ThreadData threadInfo = new ThreadData(totalThreadCount, managedThreadCount);
            MemoryData memoryInfo = new MemoryData(workingSetMb, managedHeapMb);
            return new ThreadMemoryData(DateTime.UtcNow, threadInfo, memoryInfo);
        }
    }
    public record ResponseJson(string Response);
    public record ApplicationService(int? Number, string? Name, string? LifeTime);
    public record ApplicationServices(IEnumerable<ApplicationService>? ApplicationService);
}
