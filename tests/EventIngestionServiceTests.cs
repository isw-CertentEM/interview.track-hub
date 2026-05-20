using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using TrackHub.Application.Connectors;
using TrackHub.Application.Services;
using TrackHub.Infrastructure;

namespace TrackHub.Tests;

public class EventIngestionServiceTests
{
    private const string OneRowCsv =
        "1Z999AA1,UPSX-E-78001,2026-05-18T08:14:00,IN_TRANSIT,LAX-DC1\n";

    [Fact]
    public async Task IngestAsync_records_a_batch_row()
    {
        var db = TestDb.Create();
        var sp = TestServiceProvider.For(db);
        var service = new EventIngestionService(sp, new ConnectorFactory());

        using var payload = new MemoryStream(Encoding.UTF8.GetBytes(OneRowCsv));
        await service.IngestAsync("UPSX", payload);

        Assert.Single(db.EventBatches);
    }

    [Fact]
    public async Task IngestAsync_counts_parsed_rows()
    {
        var db = TestDb.Create();
        var sp = TestServiceProvider.For(db);
        var service = new EventIngestionService(sp, new ConnectorFactory());

        using var payload = new MemoryStream(Encoding.UTF8.GetBytes(OneRowCsv));
        await service.IngestAsync("UPSX", payload);

        Assert.Equal(1, db.EventBatches.Single().RowCount);
    }

    [Fact]
    public void CsvConnector_parses_a_sample_row()
    {
        var parser = new CsvCarrierConnector();

        using var payload = new MemoryStream(Encoding.UTF8.GetBytes(OneRowCsv));
        var events = parser.Parse(payload).ToList();

        Assert.NotEmpty(events);
    }
}
