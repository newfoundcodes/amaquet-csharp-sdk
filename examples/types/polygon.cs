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

/// <summary>Source-backed polygon example. OP calls require --operations.</summary>
public static class TypePolygon
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Polygon("example:polygon");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument
                .Parse(
                    "[{\"Lat\":14.5,\"Lon\":120.9},{\"Lat\":14.7,\"Lon\":120.9},{\"Lat\":14.6,\"Lon\":121.1}]"
                )
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.ContainsAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"point\":{\"Lat\":14.6,\"Lon\":121.0}}"
                    )!
                )
            ).GetRawText()
        );
    }
}
