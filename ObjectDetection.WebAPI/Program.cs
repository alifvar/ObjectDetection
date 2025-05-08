using Microsoft.Extensions.Options;
using ObjectDetection.WebAPI.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSignalR(options =>
    {
        options.MaximumReceiveMessageSize = 1024 * 512; // ✅ افزایش به 512 کیلوبایت (پیش‌فرض: 32 کیلوبایت)
    }
 );

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins("https://localhost:7035") // آدرس Blazor WebAssembly
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseCors("AllowBlazorClient");

app.MapHub<VideoStreamHub>("/videohub");

app.Run();
