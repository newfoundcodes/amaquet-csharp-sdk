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

/// <summary>Source-backed t_digest example. OP calls require --operations.</summary>
public static class TypeTDigest
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.TDigest("example:t_digest");
        await handle.CreateAsync(
            JsonDocument.Parse("{\"compression\":100}").RootElement
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
                        "{\"value\":5}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.QuantileAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"q\":0.5}"
                    )!
                )
            ).GetRawText()
        );
    }
}
