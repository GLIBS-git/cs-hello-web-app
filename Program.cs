using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Glibs.Learning.WebApp
{
    public class Program
    {
        static WebApplication? app = null;
        static readonly string hostAddress = "http://localhost:5050"; // Office
        //static readonly string hostAddress = "http://192.168.0.157:5050"; // Home MS VS VM
        static readonly JsonSerializerOptions jsonOptionsDecode = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
        static readonly JsonSerializerOptions jsonOptionsEncode = new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        static IServiceCollection? serviceCollection = null;
        public static void Main(string[] args)
        {
            Program.BuildApplication(args);
            Program.StartWebService();
            Console.WriteLine($"{app.Services.GetRequiredService<ICommonBuffer>().ServiceStartTime.ToString()} # Starting web service at {hostAddress}...");
            app.Run();
        }
        static void BuildApplication(string[] _args)
        {
            CommonBuffer commonBuffer = new CommonBuffer();
            WebApplicationOptions options = new WebApplicationOptions() { Args = _args, EnvironmentName = "Test" };
            WebApplicationBuilder builder = WebApplication.CreateBuilder(options);
            builder.WebHost.UseUrls(hostAddress); // Only one address works
            builder.Services.ConfigureHttpJsonOptions(options => // For: Results.Ok();
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.WriteIndented = true;
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            });
            //builder.Services.AddScoped<ILogger>();
            serviceCollection = builder.Services;
            builder.Services.AddSingleton<ICommonBuffer>(commonBuffer);
            builder.Services.AddSingleton<IConsoleLogService, LogService>();
            builder.Services.AddHostedService<TestBackgroungService>();
            builder.Services.AddMemoryCache();
            app = builder.Build();
        }
        public static void StartWebService()
        {
            // / [GET]
            app.MapGet("/api/v1", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received root command.", DateTime.Now.ToString());
                //app.Services.GetService<IConsoleLogService>().Log("Test logging!");
                //return Results.Text(Program.GetHelp(""), contentType: "text/plain");
                return Results.Text(Program.GetHelp(""));
            });
            // debug/appl-services [GET]
            app.MapGet("/api/v1/debug/appl-services", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received 'debug/appl-services' command.", DateTime.Now.ToString());
                List<ApplicationService> applicationServicesList = new List<ApplicationService>();
                foreach (ServiceDescriptor serviceDescriptor in serviceCollection)
                {
                    applicationServicesList.Add(new ApplicationService() { Name = serviceDescriptor.ServiceType.Name, LifeTime = $"{serviceDescriptor.Lifetime}" });
                }
                return Results.Json<ApplicationServices>(new ApplicationServices() { ApplicationService = applicationServicesList }, Program.jsonOptionsEncode);
            });
            // debug/test [GET]
            app.MapGet("/api/v1/debug/test", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received 'debug/test' command.", DateTime.Now.ToString());
                return Results.Ok(new { Header = "Test link", Link = "http://localhost:5050/api/v1/debug/test-2" });
            });
            // debug/test-1 [GET]
            app.MapGet("/api/v1/debug/test-1", (HttpContext _httpContext, ILogger<Program> _logger, IMemoryCache _cache) => 
            {
                _logger.LogInformation("{DateTime} # Received 'debug/test-1' command.", DateTime.Now.ToString());
                _cache.Set("001", "001");
                _cache.Set("002", 2);
                _cache.TryGetValue("001", out string? _value);
                Console.WriteLine(_value);
                return Results.Text("OK.");
            });
            // debug/test-2 [GET]
            app.MapGet("/api/v1/debug/test-2", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received 'debug/test-2' command.", DateTime.Now.ToString());
                return Results.Text("OK.");
            });
            // exit [POST]
            app.MapPost("/api/v1/exit", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received 'exit' command.", DateTime.Now.ToString());
                _httpContext.RequestServices.GetService<IHostApplicationLifetime>()?.StopApplication();
                return Results.Text("{DateTime} # Stopping web service...", DateTime.Now.ToString());
            });
            // help [GET]
            app.MapGet("/api/v1/help", () =>
            {
                Console.WriteLine($"Received help command.");
                return Results.Text(Program.GetHelp(""), contentType: "text/plain");
            });
            // help/<command> [GET]
            app.MapGet("/api/v1/help/{_helpCommand}", (string _helpCommand, HttpContext _httpContext) =>
            {
                Console.WriteLine($"Received 'help/{_helpCommand}' command.");
                return Results.Text(Program.GetHelp(_helpCommand), contentType: "text/plain");
            });
            // ping [GET]
            app.MapGet("/api/v1/ping", async (HttpContext _httpContext, ILogger<Program> _logger, string? output, string? delay) =>
            {
                _logger.LogInformation("{DateTime} # Received '{path}' request with analyzed options: 'output={output}, 'delay={delay}'.", DateTime.Now.ToString(),
                    _httpContext.Request.Path, output, delay);
                string pingReply = ResponseHelper.PingReply(hostAddress);
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
            app.MapGet("/api/v1/ping-old", WebRequestProcessor.ProcessGetPing);
            // pwd [GET]
            app.MapGet("/api/v1/pwd", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received 'current-directory' command.", DateTime.Now.ToString());
                return Results.Text($"Current directory: {Directory.GetCurrentDirectory()}");
            });
            // sys-info [GET]
            app.MapGet("/api/v1/sys-info", (HttpContext _httpContext, ILogger<Program> _logger, ICommonBuffer _commandBuffer) =>
            {
                _logger.LogInformation("{DateTime} # Received 'sys-info' command.", DateTime.Now.ToString());
                SysInfo sysInfo = new SysInfo(_httpContext.Connection.LocalIpAddress?.ToString(), 
                    _httpContext.Connection.LocalPort.ToString(),
                    _commandBuffer.MachineName,
                    app.Environment.EnvironmentName,
                    _commandBuffer.ServiceStartTime.ToString(),
                    (DateTime.Now - _commandBuffer.ServiceStartTime).ToString()
                );
                return Results.Ok(sysInfo);
            });
        }
        // ############################################################################################################################################
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
    public interface ICommonBuffer
    {
        public DateTime ServiceStartTime { get; }
        public string MachineName { get; }
    }
    public class CommonBuffer : ICommonBuffer
    {
        public DateTime ServiceStartTime { get; } = DateTime.Now ;
        public string MachineName { get; } = Environment.MachineName ;
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
    public class TestBackgroungService : BackgroundService
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
