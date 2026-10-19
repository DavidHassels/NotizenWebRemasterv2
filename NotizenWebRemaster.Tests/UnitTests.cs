using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NotizenWebRemaster;
using Testcontainers.PostgreSql;

namespace NotizenWebRemaster
{
    public class UnitTests
    {
        public void TestGetList()
        {

        }
    }

    public class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

        public string ConnectionString => _container.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            await using var db = NeuerContext();
            await db.Database.EnsureCreatedAsync();
        }

        public Task DisposeAsync() => _container.DisposeAsync().AsTask();

        public NotizenContext NeuerContext()
        {
            var options = new DbContextOptionsBuilder<NotizenContext>()
                .UseNpgsql(ConnectionString)
                .Options;
            return new NotizenContext(options);
        }
       
    }

    public class ArtikelHandlerPostgresTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture _fx;
        public ArtikelHandlerPostgresTests(PostgresFixture fx) => _fx = fx;

        [Fact]
        public async Task TesteAddMethode_Postgres()
        {
            await using var db = _fx.NeuerContext();
            var handler = new ArtikelHandler(db);

            handler.add(new NeuerArtikel("Hammer", 9.99m));

            await using var pruefen = _fx.NeuerContext();
            var a = await pruefen.Artikels.FirstOrDefaultAsync(x => x.name == "Hammer");
            Assert.NotNull(a);
            Assert.Equal(9.99m, a!.price);
        }

        private async Task<int> HF_addArt(string name, decimal preis = 1m, bool erledigt = false)
        {
            await using var db = _fx.NeuerContext();
            var a = new Artikel { name = name, price = preis, done = erledigt };
            db.Artikels.Add(a);
            await db.SaveChangesAsync();
            return a.id;
        }

        [Fact]
        public async Task Remove_Test()
        {
            var id = await HF_addArt("remove" + Guid.NewGuid());
            await using var db = _fx.NeuerContext();
            var handler = new ArtikelHandler(db);

            handler.remove(id);

            await using var pruefen = _fx.NeuerContext();
            Assert.Null(await pruefen.Artikels.FindAsync(id));
        }

        [Fact]
        public async Task remove_test_unknownID()
        {
            await using var db = _fx.NeuerContext();
            var handler = new ArtikelHandler(db);

            var result = handler.remove(-1);

            Assert.IsType<BadRequest>(result);   // nur, wenn deine Methode NotFound zurückgibt
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ErledigtSetzen_GueltigeId_SetztWertUndGibtOk(bool neuerWert)
        {
            // Startwert ist das Gegenteil, damit die Änderung wirklich sichtbar wird
            var id = await HF_addArt("Erledigt_" + Guid.NewGuid(), erledigt: !neuerWert);
            await using var db = _fx.NeuerContext();
            var handler = new ArtikelHandler(db);

            var result = handler.SetzeErledigt(id, neuerWert);

            Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
            Assert.Equal(200, ((IStatusCodeHttpResult)result).StatusCode);

            await using var pruefen = _fx.NeuerContext();   // frischer Context, liest aus der DB
            var a = await pruefen.Artikels.FindAsync(id);
            Assert.Equal(neuerWert, a!.done);
        }

        [Fact]
        public async Task ErledigtSetzen_UnbekannteId_GibtBadRequest()
        {
            await using var db = _fx.NeuerContext();
            var handler = new ArtikelHandler(db);

            var result = handler.SetzeErledigt(999999, true);

            Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
            Assert.Equal(400, ((IStatusCodeHttpResult)result).StatusCode);
        }

        [Fact]
        public async Task Liste_GibtOkMitAngelegtenArtikeln()
        {
            var name = "Liste_" + Guid.NewGuid();
            await HF_addArt(name);
            await using var db = _fx.NeuerContext();
            var handler = new ArtikelHandler(db);

            var result = handler.getList();

            var ok = Assert.IsAssignableFrom<IValueHttpResult>(result);
            Assert.Equal(200, ((IStatusCodeHttpResult)result).StatusCode);
            var liste = Assert.IsAssignableFrom<IEnumerable<Artikel>>(ok.Value);
            Assert.Contains(liste, x => x.name == name);
        }

        [Fact]
        public async Task Statistik_LeereDatenbank_GibtBadRequestMitNullStatistik()
        {
            var fremd = new PostgresFixture();
            await fremd.InitializeAsync();
            try
            {
                await using var db = fremd.NeuerContext();
                var handler = new ArtikelHandler(db);

                var result = handler.getStatistic();

                Assert.Equal(400, ((IStatusCodeHttpResult)result).StatusCode);
                var stat = Assert.IsType<Statistik>(((IValueHttpResult)result).Value);
                Assert.Equal(0, stat.count);
                Assert.Null(stat.list);
                Assert.Equal(0m, stat.sum);
            }
            finally
            {
                await fremd.DisposeAsync();
            }
        }

        [Fact]
        public async Task Statistik_ZaehltNurOffeneArtikel()
        {
            var fremd = new PostgresFixture();
            await fremd.InitializeAsync();
            try
            {
                await using (var seed = fremd.NeuerContext())
                {
                    seed.Artikels.AddRange(
                        new Artikel { name = "Offen1", price = 10m, done = false },
                        new Artikel { name = "Offen2", price = 5.5m, done = false },
                        new Artikel { name = "Fertig", price = 100m, done = true });
                    await seed.SaveChangesAsync();
                }

                await using var db = fremd.NeuerContext();
                var handler = new ArtikelHandler(db);

                var result = handler.getStatistic();

                Assert.Equal(200, ((IStatusCodeHttpResult)result).StatusCode);
                var stat = Assert.IsType<Statistik>(((IValueHttpResult)result).Value);
                Assert.Equal(2, stat.count);
                Assert.Equal(15.5m, stat.sum);
                Assert.Equal(2, stat.list!.Count);
                Assert.DoesNotContain(stat.list, x => x.name == "Fertig");
            }
            finally
            {
                await fremd.DisposeAsync();
            }
        }

        [Fact]
        public async Task Statistik_AlleErledigt_GibtOkMitNull()
        {
            var fremd = new PostgresFixture();
            await fremd.InitializeAsync();
            try
            {
                await using (var seed = fremd.NeuerContext())
                {
                    seed.Artikels.Add(new Artikel { name = "Fertig", price = 9m, done = true });
                    await seed.SaveChangesAsync();
                }

                await using var db = fremd.NeuerContext();
                var result = new ArtikelHandler(db).getStatistic();

                Assert.Equal(200, ((IStatusCodeHttpResult)result).StatusCode);
                var stat = Assert.IsType<Statistik>(((IValueHttpResult)result).Value);
                Assert.Equal(0, stat.count);
                Assert.Equal(0m, stat.sum);
            }
            finally
            {
                await fremd.DisposeAsync();
            }
        }
    }
}
