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

/// <summary>Source-backed counter_series example. OP calls require --operations.</summary>
public static class TypeCounterSeries
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.CounterSeries("example:counter_series");
        await handle.CreateAsync(
            JsonDocument
                .Parse("{\"labels\":{\"metric\":\"requests\"}}")
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.AddAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"timestamp\":1000,\"value\":5}"
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
                await handle.AggregateAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"from\":0,\"to\":3000,\"bucket_ms\":1000,\"aggregation\":\"sum\"}"
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
                await handle.RangeAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"from\":0,\"to\":3000}"
                    )!
                )
            ).GetRawText()
        );
    }
}
