using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Data.SqlClient;

namespace Server.DockerHelper;

public static class DockerContainerHelperClass
{
    public static void StartTestSqlServerContainer()
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = "start textinger_test_database";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
        }
    }

    public static bool IsTestSqlServerContainerRunning()
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = "ps --format '{{.Names}}' --filter name=textinger_test_database";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return !string.IsNullOrWhiteSpace(output) && output.Contains("textinger_test_database");
        }
    }
}