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

            app.MapGet("/api/v1", (HttpContext _httpContext) =>
            {
                Console.WriteLine($"Received root command.");
                return Results.Text(Program.GetHelp(""), contentType: "text/plain");
            });
            app.MapGet("/api/v1/help", (HttpContext _httpContext) =>
            {
                Console.WriteLine($"Received help command.");
                return Results.Text(Program.GetHelp(""), contentType: "text/plain");
            });
            app.MapGet("/api/v1/help/{_helpCommand:alpha}", (String _helpCommand, HttpContext _httpContext) =>
            {
                Console.WriteLine($"Received help/{_helpCommand} command.");
                return Results.Text(Program.GetHelp(_helpCommand), contentType: "text/plain");
            });
            app.MapGet("/api/v1/ping", (HttpContext _httpContext) =>
            {
                Console.WriteLine($"Received ping command.");
                //return Results.Ok("Ping is OK!");
                return Results.Text($"{hostAddress} is alive!\nUse /HELP command for commands help.\n", contentType: "text/plain");
            });
            app.MapGet("/api/v1/test", Program.TestDelegate);

            Console.WriteLine($"Starting web service at {hostAddress}...");
            app.Run();
        }
        public static IResult TestDelegate(HttpContext _httpContext)
        {
            Console.WriteLine($"Received test command.");
            return Results.Ok("Test is OK!");
        }
        public static String GetHelp(String _command)
        {
            _command ??= "";
            String helpText = "";
            switch (_command.ToLower())
            {
                case "":
                    helpText = "Welcome to Glibs's \"Hello world\" web service!\n\n" +
                        "Use commands:\n" +
                        "HELP -- get this help.\n" +
                        "HELP/<command> -- get getailed help about certain command.\n" +
                        "PING -- test connection.\n" +
                        "TEST -- command for testing something.\n";
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
