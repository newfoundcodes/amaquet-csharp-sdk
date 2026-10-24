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

/// <summary>Source-backed inverted_index example. OP calls require --operations.</summary>
public static class TypeInvertedIndex
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.InvertedIndex("example:inverted_index");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.IndexAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"id\":\"d1\",\"text\":\"amaquet in-memory database\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.SearchAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"query\":\"memory\",\"limit\":10}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RemoveAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"id\":\"d1\"}"
                    )!
                )
            ).GetRawText()
        );
    }
}
