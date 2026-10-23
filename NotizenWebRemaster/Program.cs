using NotizenWebRemaster;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
app.MapGet("/api/artikel", async (ArtikelHandler handler, ClaimsPrincipal user ) => handler.getList(user.FindFirst("sub")!.Value)).RequireAuthorization();

app.MapPost("api/artikel", (NeuerArtikel art, ArtikelHandler handler, ClaimsPrincipal user) => handler.add(art, user.FindFirst("sub")!.Value)).RequireAuthorization();

app.MapPut("/api/artikel/{id}", (int id,Erledigt erl, ArtikelHandler handler, ClaimsPrincipal user) => handler.SetzeErledigt(id, erl.erl, user.FindFirst("sub")!.Value)).RequireAuthorization();

app.MapDelete("/api/artikel/{id}", (int id, ArtikelHandler handler, ClaimsPrincipal user) => handler.remove(id, user.FindFirst("sub")!.Value)).RequireAuthorization();

app.MapGet("/api/statistik", (ArtikelHandler handler, ClaimsPrincipal user) => handler.getStatistic(user.FindFirst("sub")!.Value)).RequireAuthorization();
app.Run();

