using System.Diagnostics;

namespace Server.DockerHelper;

public static class DockerContainerHelperClass
{
    public static void StopAllRunningContainers()
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = "ps -q";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            string[] containerIds = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string containerId in containerIds)
            {
                StopContainer(containerId);
            }
        }
    }

    public static void StopContainer(string containerId)
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = $"stop {containerId}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
        }
    }

    public static void StartSqlServerContainer()
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = "start dazzling_cartwright";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
        }
    }

    public static bool IsSqlServerContainerRunning()
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = "ps --format '{{.Names}}' --filter name=dazzling_cartwright";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return !string.IsNullOrWhiteSpace(output) && output.Contains("dazzling_cartwright");
        }
    }
}