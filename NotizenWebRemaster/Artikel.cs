using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
namespace NotizenWebRemaster
{
    public record NeuerArtikel(string? name, decimal price);
    public record Statistik(int count, List<Artikel> ?list, decimal sum);
    public record Erledigt(bool erl);
    public class ArtikelHandler(NotizenContext context)
    {
        public async Task init()
        {
            await context.Database.EnsureCreatedAsync();
        }
        //fügt übergebenen Artikel zur table Artikel
        internal IResult add(NeuerArtikel art)
        {
            if (art == null || art.name == null || art.price == 0) return Results.BadRequest();
            context.Artikels.Add(new Artikel {name = art.name, price = art.price});
            context.SaveChanges();
            return Results.Ok();
        }

        //returned die Liste der Artikel
        internal IResult getList()
        {
            return Results.Ok(context.Artikels.ToList());
        }

        //berechnet und returned Statistik
        internal IResult getStatistic()
        {
            if (!context.Artikels.Any()) return Results.BadRequest(new Statistik(0,null,0));

            List<Artikel> arti = context.Artikels.Where(x => x.done == false).ToList();
            
            int c = 0;
            decimal sum = 0;

            foreach (Artikel art in arti)
            {
                sum += art.price;
                c++;
            }

            return Results.Ok(new Statistik(c,arti,sum));

        }

        //entfernt den Artikel mit übergebener id
        internal IResult remove(int id)
        {
            if(id<0) return Results.BadRequest();

            var art = context.Artikels.Find(id);
            if(art == null) return Results.NotFound();
             
            context.Artikels.Remove(art);
            context.SaveChanges();

            return Results.Ok();
        }


        //Setzt das done Attribut auf den übergebenen bool
        internal IResult SetzeErledigt(int id, bool erl)
        {
            var art = context.Artikels.Find(id);
            if( art == null) return Results.BadRequest();

            art.done = erl;
            context.SaveChanges();
            return Results.Ok();
        }


        
    }

}
