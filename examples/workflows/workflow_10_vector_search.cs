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

/// <summary>Workflow 10: workflow_10_vector_search. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow10VectorSearch
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.VectorSet("example:workflow:vector_set");
        await handle.CreateAsync(
            JsonDocument.Parse("{\"dim\":3,\"metric\":\"cosine\"}").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.AddAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{\"id\":\"v1\",\"vector\":[1,0,0],\"metadata\":{\"class\":\"a\"}}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
