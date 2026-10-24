using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 15: workflow_15_admin_metrics. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow15AdminMetrics
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        using var admin = new AdminClient(
            Environment.GetEnvironmentVariable("AMAQUET_ADMIN_URL")
                ?? "http://127.0.0.1:13379",
            new AdminOptions
            {
                BearerToken = Environment.GetEnvironmentVariable(
                    "AMAQUET_ADMIN_TOKEN"
                ),
            }
        );

        Console.WriteLine(await admin.MetricsAsync());
    }
}
