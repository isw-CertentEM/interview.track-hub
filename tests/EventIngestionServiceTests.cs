using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using TrackHub.Application.Connectors;
using TrackHub.Application.Services;
using TrackHub.Domain;
using TrackHub.Infrastructure;

namespace TrackHub.Tests;

public class EventIngestionServiceTests
{
    private const string OneRowCsv =
        "1Z999AA1,UPSX-E-78001,2026-05-18T08:14:00,IN_TRANSIT,LAX-DC1\n";

    [Fact]
    public async Task IngestAsync_records_a_batch_row()
    {
        var sp = BuildContainer();
        var service = new EventIngestionService(sp, new ConnectorFactory());

        using var payload = new MemoryStream(Array.Empty<byte>());
        await service.IngestAsync("UPSX", payload);

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrackHubDbContext>();
        Assert.Single(db.EventBatches);
    }

    [Fact]
    public void CsvConnector_parses_a_sample_row()
    {
        var parser = new CsvCarrierConnector();
        using var payload = new MemoryStream(Encoding.UTF8.GetBytes(OneRowCsv));

        var events = parser.Parse(payload).ToList();

        Assert.NotEmpty(events);
    }

    private static IServiceProvider BuildContainer()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TrackHubDbContext>(opt =>
            opt.UseInMemoryDatabase("test-" + Guid.NewGuid()));

        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrackHubDbContext>();
        db.Carriers.Add(new Carrier { CarrierId = 1, Code = "UPSX", Name = "UPS-X" });
        db.SaveChanges();

        return sp;
    }
}
