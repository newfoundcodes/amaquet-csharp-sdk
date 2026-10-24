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

/// <summary>Source-backed hnsw_index example. OP calls require --operations.</summary>
public static class TypeHnswIndex
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.HnswIndex("example:hnsw_index");
        await handle.CreateAsync(
            JsonDocument
                .Parse(
                    "{\"dim\":3,\"metric\":\"cosine\",\"m\":8,\"ef_search\":32}"
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
                await handle.AddAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"id\":\"v1\",\"vector\":[1,0,0],\"metadata\":{}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.SearchAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"vector\":[1,0,0],\"k\":1}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RemoveAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"id\":\"v1\"}"
                    )!
                )
            ).GetRawText()
        );
    }
}
