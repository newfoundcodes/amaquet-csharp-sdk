using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 23: workflow_23_timeouts. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow23Timeouts
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        using var deadline = new CancellationTokenSource(
            TimeSpan.FromSeconds(5)
        );

        Console.WriteLine(
            (await client.PingAsync(deadline.Token)).GetRawText()
        );
    }
}
