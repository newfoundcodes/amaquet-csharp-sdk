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

/// <summary>Source-backed event_log example. OP calls require --operations.</summary>
public static class TypeEventLog
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.EventLog("example:event_log");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.AppendAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"event_type\":\"created\",\"aggregate_id\":\"agg1\",\"payload\":{\"type\":\"utf8_string\",\"value\":\"born\"}}"
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

        Console.WriteLine(
            (
                await handle.ReadAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"from\":1,\"count\":100}"
                    )!
                )
            ).GetRawText()
        );
    }
}
