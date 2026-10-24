using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 24: workflow_24_concurrent_requests. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow24ConcurrentRequests
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var calls = Enumerable
            .Range(0, 20)
            .Select(_ => client.PingAsync())
            .ToArray();

        foreach (var result in await Task.WhenAll(calls))
        {
            Console.WriteLine(result.GetRawText());
        }
    }
}
