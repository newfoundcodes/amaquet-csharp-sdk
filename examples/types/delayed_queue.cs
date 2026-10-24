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

/// <summary>Source-backed delayed_queue example. OP calls require --operations.</summary>
public static class TypeDelayedQueue
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.DelayedQueue("example:delayed_queue");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.EnqueueAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"id\":\"job1\",\"value\":{\"type\":\"utf8_string\",\"value\":\"payload\"},\"available_at_ms\":0}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.PopReadyAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.LenAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );
    }
}
