using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http.Extensions;
using System.Collections;
using System.Net.Mime;
using System.Text;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Glibs.Learning.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Program.StartWebService(args);
        }
        public static void StartWebService(string[] _args)
        {
            //String hostAddress = "http://localhost:5050"; // Office
            String hostAddress = "http://192.168.0.157:5050"; // Home MS VS VM
            //WebApplicationOptions options = new WebApplicationOptions() { ApplicationName = "Test applicatin" }; // Not working. What is this? Loads file.
            WebApplicationOptions options = new WebApplicationOptions() { Args = _args, EnvironmentName="Test"};
            WebApplicationBuilder builder = WebApplication.CreateBuilder(options);
            builder.WebHost.UseUrls(hostAddress); // Only one address works
            //builder.Services.AddScoped<ILogger>();
            IServiceCollection serviceCollection = builder.Services;
            builder.Services.AddSingleton<IConsoleLogService, LogService>();
            builder.Services.AddHostedService<TestBackgroungService>();
            WebApplication app = builder.Build();

            WebRequestProcessor requestProcessor = new WebRequestProcessor(app);

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
                return Results.Json<ApplicationServices>(new ApplicationServices() { ApplicationService = applicationServicesList }, ResponseHelper.JsonSerializingOptions());
            });
            // exit [POST]
            app.MapPost("/api/v1/exit", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received 'exit' command.", DateTime.Now.ToString());
                _httpContext.RequestServices.GetService<IHostApplicationLifetime>()?.StopApplication();
                return Results.Text("Stopping web service...");
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
            app.MapGet("/api/v1/ping", Program.ProcessGetPing);
            // pwd [GET]
            app.MapGet("/api/v1/pwd", (HttpContext _httpContext, ILogger<Program> _logger) =>
            {
                _logger.LogInformation("{DateTime} # Received 'current-directory' command.", DateTime.Now.ToString());
                return Results.Text($"Current directory: {Directory.GetCurrentDirectory()}");
            });
            // test [GET]
            app.MapGet("/api/v1/test", Program.ProcessGetTest);
            // test-1 [GET]
            app.MapGet("/api/v1/test-1", requestProcessor.ProcessGetTest);

            Console.WriteLine($"{DateTime.Now.ToString()} # Starting web service at {hostAddress}...");
            app.Run();
        }
        // ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public struct ApplicationService
        {
            public string Name { get; set; }
            public string LifeTime { get; set; }
        }
        public struct ApplicationServices
        {
            public IEnumerable<ApplicationService> ApplicationService { get; set; }   
        }

        public static IResult? ProcessGetPing(HttpContext _httpContext)
        {
            IResult? result = null;
            Console.WriteLine($"{DateTime.Now.ToString()} # Received ping command.");
            //result = Results.Ok("Ping is OK!");
            string[] delayOpt = _httpContext.Request.Query["delay"].ToArray();
            string[] outputOpt = _httpContext.Request.Query["output"].ToArray();
            int delaySec = 0;
            //Array.ForEach(delayOpt, (string delay) => { if (int.TryParse(delay, out int _delaySec)) { delaySec = Math.Max(delaySec, _delaySec); } });
            foreach (string delay in delayOpt)
            {
                if (int.TryParse(delay, out int _delaySec))
                {
                    delaySec = Math.Max(delaySec, _delaySec);
                }
            }
            int statusCode = StatusCodes.Status200OK;
            string responceText = $"""
                                  {_httpContext.Request.Host} is alive!
                                  Use '/HELP' command for commands help.
                                  """;
            ContentType contentType = ContentType.Text;
            if (Program.ParseContentType(outputOpt, ref contentType, ref responceText, ref statusCode))
            {
                if (delaySec > 0)
                {
                    Thread.Sleep(delaySec * 1000);
                }
            }
            result = Program.PrepareTextResponce(responceText, contentType, statusCode);
            return result;
        }
        public static IResult ProcessGetTest(HttpContext _httpContext)
        {
            Console.WriteLine();
            Console.WriteLine(Directory.GetCurrentDirectory());
            Console.WriteLine($"Received test command.");
            Console.WriteLine(_httpContext.Request.Method);
            Console.WriteLine(_httpContext.Request.Host);
            Console.WriteLine(_httpContext.Request.QueryString);
            //Console.WriteLine(_httpContext.Request.Query["a"]);
            DateTime dt = DateTime.Now;
            //Console.WriteLine($"{dt.ToShortDateString()} {dt.ToLongTimeString()}");
            Console.WriteLine(dt.ToString());
            Console.WriteLine();
            _httpContext.RequestServices.GetService<IConsoleLogService>().Log("Test logging!");

            //_httpContext.Response.WriteAsync("").Wait();
            //foreach (string? _param in _httpRequest.Query["a"].ToArray())
            //{
            //    Console.WriteLine(_param);
            //}
            //Array.ForEach<string>(_httpRequest.Query["a"].ToArray(), (_param) => { Console.WriteLine(_param); } );
            Array.ForEach<string>(_httpContext.Request.Query["a"].ToArray(), _param => Console.WriteLine(_param) );
            return Results.Ok("Test is OK!");
        }
        public static bool ParseContentType(string[] _outputOpt, ref ContentType _contentType, ref string _responceText, ref int _statusCode)
        {
            _contentType = ContentType.Text;
            bool ret = true;
            if (_outputOpt.Length > 1)
            {
                ret = false;
                _statusCode = StatusCodes.Status400BadRequest;
                _responceText = "Only one option 'output' is allowed.";
            }
            else if (_outputOpt.Length == 1)
            {
                switch (_outputOpt[0].ToLower())
                {
                    case "text":
                        break;
                    case "json":
                        _contentType = ContentType.Json;
                        break;
                    default:
                        ret = false;
                        _statusCode = StatusCodes.Status400BadRequest;
                        _responceText = $"Not allowed output {_outputOpt[0]}.";
                        break;
                }
            }
            return ret;
        }
        public static IResult? PrepareTextResponce(string _text, ContentType _contentType, int _statusCode)
        {
            IResult? result = null;
            switch (_contentType)
            {
                case ContentType.Text:
                    result = Results.Text(_text, "text/plain; charset=utf-8", Encoding.UTF8, _statusCode);
                    break;
                case ContentType.Json:
                    result = Results.Json(new ResponceJson { Response = _text }, ResponseHelper.JsonSerializingOptions(), "application/json; charset=utf-8", _statusCode);
                    break;
            }
            return result;
        }
        public static string GetHelp(string _command)
        {
            _command ??= "";
            string helpText = "";
            switch (_command.ToLower())
            {
                case "":
                    helpText = """
                                Welcome to Glibs's \"Hello world\" web service!

                                Use commands:
                                EXIT -- stop current web service.
                                HELP -- get this help.
                                HELP/<command> -- get detailed help about certain command.
                                PING -- test connection.
                                PWD -- shows current directory of web service
                                TEST -- command for testing something.
                                TEST1 -- another command for testing something.
                                TEST2 -- one more command for testing something.
                                
                                All commands accept query option output=<responce-format> where <responce-format> may be 'text' or 'json'. Default is text responce format.
                                """;
                    break;
                case "ping":
                    helpText =  """
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
    public class WebRequestProcessor
    {
        public WebApplication? WebApp = null;
        public WebRequestProcessor(WebApplication _webApp)
        {
            this.WebApp = _webApp;
        }
        public IResult ProcessGetTest(HttpContext _httpContext)
        {
            Console.WriteLine();
            Console.WriteLine(Directory.GetCurrentDirectory());
            Console.WriteLine($"Received test command.");
            Console.WriteLine(_httpContext.Request.Method);
            Console.WriteLine(_httpContext.Request.Host);
            Console.WriteLine(_httpContext.Request.QueryString);
            //Console.WriteLine(_httpContext.Request.Query["a"]);
            DateTime dt = DateTime.Now;
            //Console.WriteLine($"{dt.ToShortDateString()} {dt.ToLongTimeString()}");
            Console.WriteLine(dt.ToString());
            Console.WriteLine();
            this.WebApp.Services.GetService<IConsoleLogService>().Log("Test logging!");
            //_httpContext.Response.WriteAsync("").Wait();
            //foreach (string? _param in _httpRequest.Query["a"].ToArray())
            //{
            //    Console.WriteLine(_param);
            //}
            //Array.ForEach<string>(_httpRequest.Query["a"].ToArray(), (_param) => { Console.WriteLine(_param); } );
            Array.ForEach<string>(_httpContext.Request.Query["a"].ToArray(), _param => Console.WriteLine(_param));
            return Results.Ok("Test is OK!");
        }
    }
    public class TestBackgroungService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken _ct)
        {
            while (!_ct.IsCancellationRequested)
            {
                Console.WriteLine($"{DateTime.Now.ToString()}    Background service is working.");
                try
                {
                    await Task.Delay(60000, _ct);
                }
                catch (TaskCanceledException _)
                {
                }
            }
            await Task.CompletedTask;
        }
    }
    public enum ContentType
    {
        Text = 0,
        Json = 1
    }
    public struct ResponceJson
    {
        public string Response { get; set; }
    }
    public static class ResponseHelper
    {
        public static JsonSerializerOptions JsonSerializingOptions()
        {
            return new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };

        }
        public static ContentType? GetContentType(HttpContext _context, out bool _isOK, out string _responseText)
        {
            ContentType? contentType = null;
            _isOK = false;
            _responseText = "";
            string[] outputOpt = _context.Request.Query["output"].ToArray();
            if (outputOpt.Length == 0)
            {
                _isOK = true;
                contentType = null;
            }
            else if (outputOpt.Length > 1)
            {
                _isOK = false;
                contentType = null;
                _responseText = "Only one 'output' option is allowed";
            }
            else if (outputOpt.Length == 1)
            {
                switch (outputOpt[0].ToLower())
                {
                    case "text":
                        break;
                    case "json":
                        contentType = ContentType.Json;
                        break;
                    default:
                        //ret = false;
                        //statusCode = StatusCodes.Status400BadRequest;
                        //responceText = $"Not allowed output {_outputOpt[0]}.";
                        break;
                }
            }
            return contentType;
        }
    }
}
