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

/// <summary>Source-backed gauge_series example. OP calls require --operations.</summary>
public static class TypeGaugeSeries
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.GaugeSeries("example:gauge_series");
        await handle.CreateAsync(
            JsonDocument.Parse("{\"labels\":{\"metric\":\"cpu\"}}").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.SetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"timestamp\":1000,\"value\":42.5}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.LastAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RangeAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"from\":0,\"to\":3000}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.AggregateAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"from\":0,\"to\":3000,\"bucket_ms\":1000,\"aggregation\":\"avg\"}"
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
