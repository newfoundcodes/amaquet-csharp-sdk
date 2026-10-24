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

/// <summary>Workflow 6: workflow_06_scan. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow06Scan
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        string cursor = "";
        do
        {
            var page = await client.ScanAsync(cursor, "example:", 20);

            Console.WriteLine(page.GetRawText());
            cursor = page.GetProperty("cursor").GetString() ?? "";
        } while (cursor != "");
    }
}
