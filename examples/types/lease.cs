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

/// <summary>Source-backed lease example. OP calls require --operations.</summary>
public static class TypeLease
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Lease("example:lease");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.AcquireAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"owner\":\"worker-a\",\"token\":\"t1\",\"ttl_ms\":5000}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.HeldAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.ReleaseAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"token\":\"t1\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RenewAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"token\":\"t1\",\"ttl_ms\":5000}"
                    )!
                )
            ).GetRawText()
        );
    }
}
