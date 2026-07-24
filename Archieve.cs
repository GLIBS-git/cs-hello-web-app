using System.Text;

namespace Glibs.Learning.WebApp
{
    public class Archieve
    {
        //WebRequestProcessor requestProcessor = new WebRequestProcessor(app);
        // test [GET]
        //app.MapGet("/api/v1/debug/test", WebRequestProcessor.ProcessGetTest_2);
        // test-1 [GET]
        //app.MapGet("/api/v1/debug/test-1", requestProcessor.ProcessGetTest);

    }
    // ########################### Obsolete ##################################
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
                    result = Results.Json(new ResponceJson { Response = _text }, ResponseHelper.JsonSerializingOptions(), "application/json; charset=utf-8", _statusCode);
                    break;
            }
            return result;
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
