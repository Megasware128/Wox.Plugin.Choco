using System.Diagnostics;
using CommandLine;

namespace WoxRestarter
{
    public class Program
    {
        private static Options options;

        static void Main(string[] args)
        {
            options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                var process = Process.GetProcessById(options.PID);
                process.Kill();
                process.WaitForExit();
                var startInfo = new ProcessStartInfo(options.WorkingDirectory + '\\' + options.App, $"query {options.Query}"); // query doesn't seem to work anymore
                startInfo.WorkingDirectory = options.WorkingDirectory;
                startInfo.Verb = "runas";
                Process.Start(startInfo);
            }
        }
    }
}
