using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 22: workflow_22_admin_audit. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow22AdminAudit
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

        Console.WriteLine((await admin.AuditAsync(25)).GetRawText());
    }
}
