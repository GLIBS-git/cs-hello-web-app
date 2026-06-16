namespace Glibs.Learning.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            WebApplication app = builder.Build();

            app.MapGet("/api/v1/ping", (HttpContext _httpContext) =>
            {
                return Results.Ok("Ping is OK!");
            });


            app.Run();
        }
    }
}
