using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 13: workflow_13_blob_download. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow13BlobDownload
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var key = "example:workflow:blob";
        var data = System.Text.Encoding.UTF8.GetBytes("download me");
        await client.UploadBlobAsync(key, data);

        var restored = await client.DownloadBlobBytesAsync(key);
        Console.WriteLine(System.Text.Encoding.UTF8.GetString(restored));
    }
}
