using BattleshipsGame.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add CORS for Blazor client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowBlazorClient");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
