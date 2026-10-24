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

/// <summary>Workflow 36: workflow_36_hnsw. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow36Hnsw
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.HnswIndex("example:workflow:hnsw_index");
        await handle.CreateAsync(
            JsonDocument
                .Parse(
                    "{\"dim\":3,\"metric\":\"cosine\",\"m\":8,\"ef_search\":32}"
                )
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.AddAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{\"id\":\"v1\",\"vector\":[1,0,0],\"metadata\":{}}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
