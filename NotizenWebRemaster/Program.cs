using NotizenWebRemaster;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.AspNetCore.Mvc;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<NotizenContext>();
builder.Services.AddScoped<ArtikelHandler>();
builder.Services.AddKeycloakAuth(builder.Configuration);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var c = scope.ServiceProvider.GetRequiredService<ArtikelHandler>();
    await c.init(); 
}



app.MapAuthEndpoints();
app.MapGet("/api/artikel", async (ArtikelHandler handler) => handler.getList()).RequireAuthorization();

app.MapPost("api/artikel", (NeuerArtikel art, ArtikelHandler handler) => handler.add(art)).RequireAuthorization();

app.MapPut("/api/artikel/{id}", (int id,Erledigt erl, ArtikelHandler handler) => handler.SetzeErledigt(id, erl.erl)).RequireAuthorization();

app.MapDelete("/api/artikel/{id}", (int id, ArtikelHandler handler) => handler.remove(id)).RequireAuthorization();

app.MapGet("/api/statistik", (ArtikelHandler handler) => handler.getStatistic()).RequireAuthorization();
app.Run();

