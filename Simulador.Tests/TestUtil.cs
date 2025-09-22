using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Simulador.Core.Data;
using Simulador.Web.Controllers;
using Moq;
using Microsoft.Data.Sqlite;

namespace Simulador.Tests
{
    public class Util
    {
        public static string EncodeJson(object obj)
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        }


        public static ILogger<T> LoggerDeTeste<T>()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(o =>
                {
                    o.SingleLine = true;
                    o.TimestampFormat = "hh:mm:ss ";
                });
            });
            return loggerFactory.CreateLogger<T>();
        }

        public static async Task TesteController<T>(Func<Mock<SimuladorDbContext>, ILogger<T>, Task> action)
        {
            // UseInMemoryDatabase não suporta ExecuteDeleteAsync...
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<SimuladorDbContext>()
                    .UseSqlite(connection)
                    .Options;

            // loucura, loucura, loucura
            var mockCtx = new Mock<SimuladorDbContext>(options)
            {
                CallBase = true
            };
            mockCtx.Object.Database.EnsureCreated();

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(o =>
                {
                    o.SingleLine = true;
                    o.TimestampFormat = "hh:mm:ss ";
                });
            });
            var logger = LoggerDeTeste<T>();

            await action(mockCtx, logger);

            connection.Close();
        }
    }
}