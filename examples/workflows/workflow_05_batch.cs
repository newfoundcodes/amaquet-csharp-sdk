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

/// <summary>Workflow 5: workflow_05_batch. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow05Batch
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var commands = new object[]
        {
            JsonWire.Map(
                "command",
                "SET",
                "args",
                JsonWire.Map(
                    "key",
                    "example:workflow:batch",
                    "type",
                    "integer",
                    "value",
                    1
                )
            ),
            JsonWire.Map(
                "command",
                "GET",
                "args",
                JsonWire.Map("key", "example:workflow:batch")
            ),
        };
        Console.WriteLine((await client.BatchAsync(commands)).GetRawText());
    }
}
