namespace Glibs.Learning.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();


            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseAuthorization();

            app.MapGet("/api/v1/ping", (HttpContext httpContext) =>
            {
            });

            app.Run();
        }
    }
}
