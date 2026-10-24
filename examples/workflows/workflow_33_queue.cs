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

/// <summary>Workflow 33: workflow_33_queue. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow33Queue
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.FifoQueue("example:workflow:fifo_queue");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.EnqueueAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{\"value\":{\"type\":\"utf8_string\",\"value\":\"first\"}}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
