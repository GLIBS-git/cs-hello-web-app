using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http.Extensions;
using System.Net.Mime;
using System.Text;
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
            WebApplicationOptions options = new WebApplicationOptions() { Args = _args, EnvironmentName="Test" };
            WebApplicationBuilder builder = WebApplication.CreateBuilder(options);
            builder.WebHost.UseUrls(hostAddress); // Only one address works
            //builder.Services.AddTransient<IConsoleLogService, LogService>();
            builder.Services.AddSingleton<IConsoleLogService, LogService>();
            WebApplication app = builder.Build();

            //WebRequestProcessor requestProcessor = new WebRequestProcessor(app);

            app.MapGet("/api/v1", (HttpRequest _httpRequest) =>
            {
                Console.WriteLine($"Received root command.");
                //app.Services.GetService<IConsoleLogService>().Log("Test logging!");
                return Results.Text(Program.GetHelp(""), contentType: "text/plain");
            });
            app.MapGet("/api/v1/current-directory", Program.ProcessGetCurDir);
            app.MapGet("/api/v1/help", (HttpRequest _httpRequest) =>
            {
                Console.WriteLine($"Received help command.");
                return Results.Text(Program.GetHelp(""), contentType: "text/plain");
            });
            app.MapGet("/api/v1/help/{_helpCommand:alpha}", (string _helpCommand, HttpRequest _httpRequest) =>
            {
                Console.WriteLine($"Received help/{_helpCommand} command.");
                return Results.Text(Program.GetHelp(_helpCommand), contentType: "text/plain");
            });
            app.MapGet("/api/v1/ping", Program.ProcessGetPing);
            app.MapGet("/api/v1/test", Program.ProcessGetTest);
            //app.MapGet("/api/v1/test", requestProcessor.ProcessGetTest);

            Console.WriteLine($"{DateTime.Now.ToString()} # Starting web service at {hostAddress}...");
            app.Run();
        }
        public static IResult? ProcessGetPing(HttpRequest _httpRequest)
        {
            IResult? result = null;
            Console.WriteLine($"{DateTime.Now.ToString()} # Received ping command.");
            //result = Results.Ok("Ping is OK!");
            string[] delayOpt = _httpRequest.Query["delay"].ToArray();
            string[] outputOpt = _httpRequest.Query["output"].ToArray();
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
                                  {_httpRequest.Host} is alive!
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
        public static IResult ProcessGetCurDir(HttpContext _httpContext)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} # Received current-directory command.");
            IResult? result = null;
            int statusCode = StatusCodes.Status200OK;
            string responceText = $"{Directory.GetCurrentDirectory()}";
            ContentType contentType = ContentType.Text;
            result = Program.PrepareTextResponce(responceText, contentType, statusCode);
            return result;
        }
        //public static IResult ProcessGetTest(HttpRequest _httpRequest)
        public static IResult ProcessGetTest(HttpContext _httpContext)
        //public static IResult ProcessGetTest(HttpContext _httpContext, WebApplication _app)
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
                    JsonSerializerOptions opt = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };
                    result = Results.Json(new ResponceJson { Response = _text }, opt, "application/json; charset=utf-8", _statusCode);
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
                    helpText =  """
                                Welcome to Glibs's \"Hello world\" web service!

                                Use commands:
                                CURRENT-DIRECTORY -- shows current directory of web service
                                HELP -- get this help.
                                HELP/<command> -- get getailed help about certain command.
                                PING -- test connection.
                                TEST -- command for testing something.

                                All commands accept query option output=<responce-format> where <responce-format> may be 'text' or 'json'. Default is text responce format.
                                """;
                    break;
                case "ping":
                    helpText =  """
                                Query options:
                        
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
    public enum ContentType
    {
        Text = 0,
        Json = 1
    }
    public struct ResponceJson
    {
        public string Response { get; set; }
    }
}
