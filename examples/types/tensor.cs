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

/// <summary>Source-backed tensor example. OP calls require --operations.</summary>
public static class TypeTensor
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Tensor("example:tensor");
        await handle.CreateAsync(
            JsonDocument
                .Parse(
                    "{\"shape\":[2,2,2],\"dtype\":\"float64\",\"data\":[0,1,2,3,4,5,6,7]}"
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
                await handle.GetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"indices\":[1,1,1]}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.SetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"indices\":[0,0,0],\"value\":99}"
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
