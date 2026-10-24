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

/// <summary>Workflow 38: workflow_38_trie. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow38Trie
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Trie("example:workflow:trie");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.InsertAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{\"value\":\"amaquet\",\"id\":\"1\"}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
