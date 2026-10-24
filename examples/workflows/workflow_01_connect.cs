using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 1: workflow_01_connect. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow01Connect
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        Console.WriteLine((await client.PingAsync()).GetRawText());
        Console.WriteLine((await client.InfoAsync()).GetRawText());
    }
}
