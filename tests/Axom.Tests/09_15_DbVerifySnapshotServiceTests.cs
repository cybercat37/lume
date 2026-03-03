using System.Globalization;
using Axom.Cli;

namespace Axom.Tests;

public class DbVerifySnapshotServiceTests
{
    [Fact]
    public void Write_metrics_snapshot_writes_query_ids_and_plan_hashes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_snapshot_service_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempDir);

            var queries = new List<DbVerifiedQuery>
            {
                new("q1"),
                new("q2")
            };
            var planHashes = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["q1"] = "hash-1"
            };

            DbVerifySnapshotService.WriteMetricsSnapshot(queries, planHashes);

            var snapshotPath = Path.Combine(tempDir, ".axom", "query-metrics.json");
            Assert.True(File.Exists(snapshotPath));
            var snapshot = File.ReadAllText(snapshotPath);
            Assert.Contains("\"query_id\": \"q1\"", snapshot, StringComparison.Ordinal);
            Assert.Contains("\"query_id\": \"q2\"", snapshot, StringComparison.Ordinal);
            Assert.Contains("\"plan_hash\": \"hash-1\"", snapshot, StringComparison.Ordinal);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Compare_prints_ok_when_snapshot_matches_current_queries()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_snapshot_service_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(tempDir, ".axom"));
        File.WriteAllText(
            Path.Combine(tempDir, ".axom", "query-metrics.json"),
            "[{\"query_id\":\"q1\",\"average_duration\":0,\"execution_count\":0,\"plan_hash\":null}]");

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var output = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            Console.SetOut(output);

            var success = DbVerifySnapshotService.TryPrintSnapshotComparison(
                new HashSet<string>(StringComparer.Ordinal) { "q1" },
                currentPlanHashes: null,
                verbose: false,
                out var error);

            Assert.True(success);
            Assert.Null(error);
            Assert.Contains("compare_status=ok", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Compare_returns_error_for_invalid_snapshot_json()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_snapshot_service_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(tempDir, ".axom"));
        File.WriteAllText(Path.Combine(tempDir, ".axom", "query-metrics.json"), "{ invalid");

        var originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempDir);

            var success = DbVerifySnapshotService.TryPrintSnapshotComparison(
                new HashSet<string>(StringComparer.Ordinal),
                currentPlanHashes: null,
                verbose: false,
                out var error);

            Assert.False(success);
            Assert.Contains("Invalid snapshot file", error, StringComparison.Ordinal);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
