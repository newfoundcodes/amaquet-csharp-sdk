using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 12: workflow_12_blob_upload. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow12BlobUpload
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var data = System.Text.Encoding.UTF8.GetBytes(
            "Amaquet chunked upload example"
        );

        await client.UploadBlobAsync("example:workflow:blob", data);
        Console.WriteLine("Uploaded: " + data.Length);
    }
}
