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

/// <summary>Workflow 25: workflow_25_connection_pool. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow25ConnectionPool
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        await using (
            var pool = await ConnectionPool.ConnectAsync(
                Environment.GetEnvironmentVariable("AMAQUET_ENDPOINT")
                    ?? "amaquet://127.0.0.1:13378",
                size: 3,
                options: new ClientOptions
                {
                    ApiKey = Environment.GetEnvironmentVariable(
                        "AMAQUET_API_KEY"
                    ),
                }
            )
        )
        {
            Console.WriteLine((await pool.CommandAsync("PING")).GetRawText());
        }
    }
}
