using InteractiveMap.Infrastructure.Helpers;
public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddTransient<JsonHelper>();
        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline
        app.UseHttpsRedirection();
        app.UseCors("AllowAll");
        app.MapControllers();

        app.Run();
    }
}