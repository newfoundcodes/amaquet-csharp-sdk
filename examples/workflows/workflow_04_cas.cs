using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 4: workflow_04_cas. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow04Cas
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var key = "example:workflow:cas";
        await client.SetAsync(key, "integer", 10);

        var current = await client.GetAsync(key);
        ulong version = current.GetProperty("version").GetUInt64();

        Console.WriteLine(
            (await client.CasAsync(key, version, "integer", 11)).GetRawText()
        );
    }
}
