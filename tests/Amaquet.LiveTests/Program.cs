/*
 * Amaquet C# SDK - A Newfoundcodes project.
 *
 * Copyright (C) 2026 Jonathan Eldy Baldivicio
 *
 * Author: Jonathan Eldy Baldivicio
 * Contact: jonathaneldy.baldivicio@newfoundcodes.com
 *
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;
using Amaquet;

var endpoint = Environment.GetEnvironmentVariable("AMAQUET_TEST_ENDPOINT");
if (string.IsNullOrWhiteSpace(endpoint))
{
    Console.WriteLine(
        "SKIP: Set AMAQUET_TEST_ENDPOINT to use a disposable Amaquet test server."
    );
    return;
}

string prefix = "sdk:csharp:live:" + Guid.NewGuid().ToString("N");
await using var client = await AmaquetClient.ConnectAsync(
    endpoint,
    new ClientOptions
    {
        ApiKey = Environment.GetEnvironmentVariable("AMAQUET_TEST_API_KEY"),
        RequireSignedIdentity =
            Environment.GetEnvironmentVariable("AMAQUET_TEST_REQUIRE_IDENTITY")
            == "1",
        ExpectedIdentityFingerprint = Environment.GetEnvironmentVariable(
            "AMAQUET_TEST_IDENTITY_FINGERPRINT"
        ),
    }
);

Console.WriteLine((await client.PingAsync()).GetRawText());

int constructed = 0,
    checkedOperations = 0,
    operationErrors = 0;
bool runOperations =
    Environment.GetEnvironmentVariable("AMAQUET_TEST_RUN_OPERATIONS") == "1";
var catalog = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
    await File.ReadAllTextAsync("tools/catalog.source.json")
)!;

foreach (var (type, operations) in catalog)
{
    var source = JsonWire.Decode(
        await File.ReadAllBytesAsync("examples/scenarios/" + type + ".json")
    );

    var create = source.GetProperty("construction");
    var args = create.GetProperty("args");
    string key = prefix + ":" + type;

    try
    {
        if (create.GetProperty("command").GetString() == "SET")
        {
            await client.SetAsync(key, type, args.GetProperty("value"));
        }
        else
        {
            await client.CreateAsync(key, type, args.GetProperty("options"));
        }

        var read = await client.GetAsync(key);
        if (read.GetProperty("type").GetString() != type)
        {
            throw new Exception("Type mismatch for " + type);
        }

        constructed++;
        if (runOperations)
        {
            foreach (
                var op in source.GetProperty("operations").EnumerateArray()
            )
            {
                var opArgs = op.GetProperty("args");
                string operation = opArgs.GetProperty("operation").GetString()!;
                var parameters = JsonSerializer.Deserialize<
                    Dictionary<string, object?>
                >(opArgs.GetProperty("args").GetRawText())!;

                using var timeout = new CancellationTokenSource(
                    TimeSpan.FromSeconds(10)
                );

                try
                {
                    await client.OperationAsync(
                        key,
                        operation,
                        parameters,
                        timeout.Token
                    );
                    checkedOperations++;
                }
                catch (Exception e)
                {
                    operationErrors++;
                    Console.WriteLine(
                        $"OP WARN {type}.{operation}: {e.Message}"
                    );
                }
            }
        }
    }
    finally
    {
        try
        {
            await client.DeleteAsync(new[] { key });
        }
        catch { }
    }
}

var binary = new byte[3 * 1024 * 1024 + 37];
new Random(1488).NextBytes(binary);

var blobKey = prefix + ":blob";
try
{
    await client.UploadBlobAsync(blobKey, binary);

    var actual = await client.DownloadBlobBytesAsync(blobKey);
    if (!actual.SequenceEqual(binary))
    {
        throw new Exception("Live binary round trip mismatch");
    }
}
finally
{
    try
    {
        await client.DeleteAsync(new[] { blobKey });
    }
    catch { }
}

Console.WriteLine(
    $"Live results: {constructed}/91 constructions+reads; {checkedOperations} operation calls succeeded, {operationErrors} operation calls failed."
);

if (constructed != 91)
{
    throw new Exception("Incomplete type coverage");
}

if (runOperations && operationErrors != 0)
{
    throw new Exception(
        "Some documented OP calls need prerequisites or failed on the server; inspect warnings"
    );
}
