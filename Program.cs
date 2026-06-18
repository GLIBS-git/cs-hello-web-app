using Microsoft.AspNetCore.Http.Extensions;

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
            String hostAddress = "http://localhost:5050";
            //String hostAddress = "http://192.168.0.157:5050"; // Home MS VS VM
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
            app.MapGet("/api/v1/ping", (HttpRequest _httpRequest) =>
            {
                Console.WriteLine($"Received ping command.");
                //return Results.Ok("Ping is OK!");
                return Results.Text($"{hostAddress} is alive!\nUse /HELP command for commands help.\n", contentType: "text/plain");
            });
            app.MapGet("/api/v1/test", Program.TestDelegate);

            Console.WriteLine($"Starting web service at {hostAddress}...");
            app.Run();
        }
        public static IResult TestDelegate(HttpRequest _httpRequest)
        {
            Console.WriteLine($"Received test command.");
            Console.WriteLine(_httpRequest.Method);
            Console.WriteLine(_httpRequest.Host);
            Console.WriteLine(_httpRequest.QueryString);
            Console.WriteLine(_httpRequest.Query["a"]);
            foreach (string? _param in _httpRequest.Query["a"].ToArray())
            {
                Console.WriteLine(_param);
            }
            return Results.Ok("Test is OK!");
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
}
