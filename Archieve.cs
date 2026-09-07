using Microsoft.AspNetCore.Builder;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Glibs.Learning.WebApp
{
    public class Archieve
    {
        //static readonly string hostAddress = "http://localhost:5050"; // Office
        //static readonly string hostAddress = "http://192.168.0.157:5050"; // Home MS VS VM
        //WebApplicationOptions options = new WebApplicationOptions() { Args = _args, EnvironmentName = "Test" }; // From launchSettings.json now comes "ASPNETCORE_ENVIRONMENT": "Development"
        //WebApplicationBuilder builder = WebApplication.CreateBuilder(options);
        //builder.WebHost.UseUrls(hostAddress); // Only one address works
        //builder.Services.AddScoped<ILogger>();
        //IServiceCollection serviceCollection = builder.Services;
        //builder.Services.AddSingleton<ICommonBuffer>(commonBuffer);
        //builder.Services.AddSingleton<IConsoleLogService, LogService>();
        //builder.Services.AddHostedService<TestBackgroungService>();
        //static WebApplication app = builder.Build();
        //app?.Services.GetRequiredService<ILogger<Program>>().LogInformation("Starting web service at {Host}", commonBuffer?.Host);
        //WebRequestProcessor requestProcessor = new WebRequestProcessor(app);
        //string? hostAddress = "";
        //app?.Services.GetRequiredService<IMemoryCache>().TryGetValue("HostAddress", out hostAddress);
        //Console.WriteLine($"{commonBuffer?.ServiceStartTime.ToString()} # Starting web service at {hostAddress}...");
        // test [GET]
        //app.MapGet("/api/v1/debug/test", WebRequestProcessor.ProcessGetTest_2);
        // test-1 [GET]
        //app.MapGet("/api/v1/debug/test-1", requestProcessor.ProcessGetTest);
        //app.Use(async (context, next) => {
        //        Console.WriteLine("A-la logging before...");
        //        await next.Invoke();
        //        Console.WriteLine("A-la logging after...");
        //});
        //        app.Use(async (context, next) => {
        //        Console.WriteLine("A-la one more logging before...");
        //        await next.Invoke();
        //        Console.WriteLine("A-la one more logging after...");
        //});
        // / [GET]
        //_app.MapGet("", (HttpContext _httpContext, ILogger<Program> _logger) =>
        //    {
        //        _logger.LogInformation("{DateTime} # Received root command.", DateTime.Now.ToString());
        //        app.Services.GetService<IConsoleLogService>().Log("Test logging!");
        //        return Results.Text(ResponseHelper.GetHelp(""), contentType: "text/plain");
        //        return Results.Text(ResponseHelper.GetHelp(""));
        //    });



    }
    // ########################### Obsolete ##################################
    public class WebRequestProcessor
    {
        public WebApplication? WebApp = null;
        public WebRequestProcessor(WebApplication _webApp)
        {
            this.WebApp = _webApp;
            // ping-old [GET]
            _webApp.MapGet("/ping-old", WebRequestProcessor.ProcessGetPing);
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
            this.WebApp.Services.GetService<IConsoleLogService>()?.Log("Test logging!");
            //_httpContext.Response.WriteAsync("").Wait();
            //foreach (string? _param in _httpRequest.Query["a"].ToArray())
            //{
            //    Console.WriteLine(_param);
            //}
            //Array.ForEach<string>(_httpRequest.Query["a"].ToArray(), (_param) => { Console.WriteLine(_param); } );
            Array.ForEach<string>(_httpContext.Request.Query["a"].ToArray(), _param => Console.WriteLine(_param));
            return Results.Ok("Test is OK!");
        }
        public static IResult? ProcessGetPing(HttpContext _httpContext)
        {
            IResult? result = null;
            Console.WriteLine($"{DateTime.Now.ToString()} # Received 'ping-old' command.");
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
            if (WebRequestProcessor.ParseContentType(outputOpt, ref contentType, ref responceText, ref statusCode))
            {
                if (delaySec > 0)
                {
                    Thread.Sleep(delaySec * 1000);
                }
            }
            result = WebRequestProcessor.PrepareTextResponce(responceText, contentType, statusCode);
            return result;
        }
        public static IResult ProcessGetTest_2(HttpContext _httpContext)
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
            _httpContext.RequestServices.GetService<IConsoleLogService>()?.Log("Test logging!");
            //_httpContext.Response.WriteAsync("").Wait();
            //foreach (string? _param in _httpRequest.Query["a"].ToArray())
            //{
            //    Console.WriteLine(_param);
            //}
            //Array.ForEach<string>(_httpRequest.Query["a"].ToArray(), (_param) => { Console.WriteLine(_param); } );
            Array.ForEach<string>(_httpContext.Request.Query["a"].ToArray(), _param => Console.WriteLine(_param));
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
                    result = Results.Json(new ResponseJson(_text), Program.jsonOptionsEncode, "application/json; charset=utf-8", _statusCode);
                    break;
            }
            return result;
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
    }
    public enum ContentType
    {
        Text = 0,
        Json = 1
    }
}
