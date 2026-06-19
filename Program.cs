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
            //String hostAddress = "http://localhost:5050";
            String hostAddress = "http://192.168.0.157:5050"; // Home MS VS VM
            WebApplicationBuilder builder = WebApplication.CreateBuilder(_args);
            WebApplication app = builder.Build();
            builder.WebHost.UseUrls(hostAddress); // Only one address works

            app.MapGet("/api/v1", (HttpRequest _httpRequest) =>
            {
                Console.WriteLine($"Received root command.");
                return Results.Text(Program.GetHelp(""), contentType: "text/plain");
            });
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

            Console.WriteLine($"Starting web service at {hostAddress}...");
            app.Run();
        }
        public static IResult? ProcessGetPing(HttpRequest _httpRequest)
        {
            IResult? result = null;
            Console.WriteLine($"Received ping command.");
            //result = Results.Ok("Ping is OK!");
            string[] delayOpt = _httpRequest.Query["delay"].ToArray();
            string[] outputOpt = _httpRequest.Query["output"].ToArray();
            int delaySec = 0;
            foreach (string delay in delayOpt)
            {
                if (int.TryParse(delay, out int _delaySec) )
                {
                    delaySec = Math.Max(delaySec, _delaySec);
                }
            }
            int statusCode = StatusCodes.Status200OK;
            string responceText = $"{_httpRequest.Host} is alive!\nUse /HELP command for commands help.\n";
            ContentType contentType = ContentType.Text;
            if (Program.ParseContentType(outputOpt, ref contentType, ref responceText, ref statusCode))
            {

            }
            
            if (delaySec > 0)
            {
                Thread.Sleep(delaySec * 1000);
            }

            result = Program.PrepareTextResponce(responceText, ContentType.Text, statusCode);
            return result;
        }
        public static bool ParseContentType(string[] _outputOpt, ref ContentType _contentType, ref string _responceText, ref int statusCode)
        {
            _contentType = ContentType.Text;
            
            return true;
        }
        public static IResult ProcessGetTest(HttpRequest _httpRequest)
        {
            Console.WriteLine($"Received test command.");
            Console.WriteLine(_httpRequest.Method);
            Console.WriteLine(_httpRequest.Host);
            Console.WriteLine(_httpRequest.QueryString);
            Console.WriteLine(_httpRequest.Query["a"]);
            //foreach (string? _param in _httpRequest.Query["a"].ToArray())
            //{
            //    Console.WriteLine(_param);
            //}
            //Array.ForEach<string>(_httpRequest.Query["a"].ToArray(), (_param) => { Console.WriteLine(_param); } );
            Array.ForEach<string>(_httpRequest.Query["a"].ToArray(), _param => Console.WriteLine(_param) );
            return Results.Ok("Test is OK!");
        }
        public static IResult? PrepareTextResponce(string _text, ContentType _contentType, int _statusCode)
        {
            IResult? result = null;
            switch (_contentType)
            {
                case ContentType.Text:
                    result = Results.Text("_text", "text/plain; charset=utf-8", Encoding.UTF8, _statusCode);
                    break;
                case ContentType.Json:
                    JsonSerializerOptions opt = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.Always };
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
                    helpText = "Welcome to Glibs's \"Hello world\" web service!\n\n" +
                        "Use commands:\n" +
                        "HELP -- get this help.\n" +
                        "HELP/<command> -- get getailed help about certain command.\n" +
                        "PING -- test connection.\n" +
                        "TEST -- command for testing something.\n\n" +
                        "All commands accept query option output=<responce-format> where <responce-format> may be 'text' or 'json'. Default is text responce format.";
                    break;
                case "ping":
                    helpText = "Query options:\n" +
                        "timeout=<seconds> -- returns responce with delay. <seconds> must be integer.";
                    break;
                default:
                    helpText = $"No help for command '{_command}' awailable.";
                    break;
            }
            return helpText;
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
