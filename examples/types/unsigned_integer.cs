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

/// <summary>Source-backed unsigned_integer example. OP calls require --operations.</summary>
public static class TypeUnsignedInteger
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.UnsignedInteger("example:unsigned_integer");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument.Parse("42").RootElement
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
                        "{\"delta\":8}"
                    )!
                )
            ).GetRawText()
        );
    }
}
