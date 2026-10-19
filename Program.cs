using NotizenWebRemaster;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.AspNetCore.Mvc;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<NotizenContext>();
builder.Services.AddScoped<ArtikelHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var c = scope.ServiceProvider.GetRequiredService<ArtikelHandler>();
    await c.init(); 
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/artikel", async (ArtikelHandler handler) => handler.getList());

app.MapPost("api/artikel", (NeuerArtikel art, ArtikelHandler handler) => handler.add(art));

app.MapPut("/api/artikel/{id}", (int id,Erledigt erl, ArtikelHandler handler) => handler.SetzeErledigt(id, erl.erl));

app.MapDelete("/api/artikel/{id}", (int id, ArtikelHandler handler) => handler.remove(id));

app.MapGet("/api/statistik", (ArtikelHandler handler) => handler.getStatistic());
app.Run();

