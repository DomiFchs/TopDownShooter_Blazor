using Domain.Repositories.Implementations;
using Domain.Repositories.Interfaces;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Model.Configurations;
using Model.Entities;
using MySqlConnector;
using TopDownShooter.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddSignalR();
builder.Services.AddCors();

builder.AddSqlServerDbContext<ShooterDbContext>("shooterdb");
builder.Services.AddTransient<IHighScoreRepository, HighScoreRepository>();
builder.Services.AddSingleton<SessionHandler>();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});




var app = builder.Build();

app.UseExceptionHandler();

app.UseResponseCompression();
app.UseRouting();

app.MapGet("/highscores", async (ShooterDbContext context) =>
    Results.Ok((object?)await context.HighScores.ToListAsync()));

app.MapHub<ChatHub>("/chathub");
app.MapHub<MatchmakingHub>("/matchmakinghub");
app.MapHub<GameHub>("/gamehub");



app.UseCors(b => b.AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
);

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment()) {
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ShooterDbContext>();
    await context.Database.EnsureCreatedAsync();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days.
    // You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Run();