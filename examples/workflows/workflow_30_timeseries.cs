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

namespace Amaquet.Examples;

/// <summary>Workflow 30: workflow_30_timeseries. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow30Timeseries
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Timeseries("example:workflow:timeseries");
        await handle.CreateAsync(
            JsonDocument
                .Parse(
                    "{\"labels\":{\"service\":\"api\",\"env\":\"prod\"},\"retention_ms\":3600000}"
                )
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.AddAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{\"timestamp\":1730000000000,\"value\":10}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
