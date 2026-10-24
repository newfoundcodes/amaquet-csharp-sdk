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

/// <summary>Source-backed rate_limit_bucket example. OP calls require --operations.</summary>
public static class TypeRateLimitBucket
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.RateLimitBucket("example:rate_limit_bucket");
        await handle.CreateAsync(
            JsonDocument
                .Parse("{\"capacity\":5,\"refill_per_second\":2}")
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.AllowAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"cost\":1}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RemainingAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );
    }
}
