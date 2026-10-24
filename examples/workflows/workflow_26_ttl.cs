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

/// <summary>Workflow 26: workflow_26_ttl. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow26Ttl
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        await client.SetAsync("example:workflow:ttl", "utf8_string", "soon");
        await client.ExpireAsync("example:workflow:ttl", 10000);

        Console.WriteLine(
            (await client.TtlAsync("example:workflow:ttl")).GetRawText()
        );
    }
}
