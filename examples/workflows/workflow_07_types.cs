using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 7: workflow_07_types. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow07Types
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        Console.WriteLine((await client.TypesAsync()).GetRawText());
    }
}
