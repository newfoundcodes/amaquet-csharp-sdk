using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 42: workflow_42_cleanup. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow42Cleanup
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        Console.WriteLine(
            (
                await client.DeleteAsync(
                    new[]
                    {
                        "example:workflow:ttl",
                        "example:workflow:cas",
                        "example:workflow:blob",
                    }
                )
            ).GetRawText()
        );
    }
}
