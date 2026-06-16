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
                return Results.Text(Program.GetHelp(), contentType: "text/plain");
            });
            app.MapGet("/api/v1/help", (HttpContext _httpContext) =>
            {
                Console.WriteLine($"Received help command.");
                return Results.Text(Program.GetHelp(), contentType: "text/plain");
            });
            app.MapGet("/api/v1/ping", (HttpContext _httpContext) =>
            {
                Console.WriteLine($"Received ping command.");
                //return Results.Ok("Ping is OK!");
                return Results.Text($"{hostAddress} is alive!\nUse HELP command for commands help.\n", contentType: "text/plain");
            });

            Console.WriteLine($"Starting web service at {hostAddress}...");
            app.Run();
        }
        public static String GetHelp()
        {
            return "Welcome to Glibs's \"Hello world\" web service!\n\n" +
                "Use commands:\n" +
                "HELP -- get this help.\n" +
                "PING -- test connection.\n";
        }
    }
}
