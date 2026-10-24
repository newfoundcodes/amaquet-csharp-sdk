using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 2: workflow_02_key_lifecycle. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow02KeyLifecycle
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var key = "example:workflow:lifecycle";
        await client.SetAsync(key, "utf8_string", "hello");

        Console.WriteLine((await client.GetAsync(key)).GetRawText());
        await client.ExpireAsync(key, 5000);

        Console.WriteLine((await client.TtlAsync(key)).GetRawText());
        await client.PersistAsync(key);
        await client.DeleteAsync(new[] { key });
    }
}
