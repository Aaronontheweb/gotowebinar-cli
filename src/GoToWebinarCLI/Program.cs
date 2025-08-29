using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using GoToWebinarCLI.Commands;

namespace GoToWebinarCLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("GoToWebinar CLI - Command-line interface for GoToWebinar API");

        rootCommand.AddCommand(new ConfigCommand());

        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseVersionOption("--version", "-v")
            .UseExceptionHandler((exception, context) =>
            {
                Console.Error.WriteLine($"Error: {exception.Message}");
                Environment.Exit(1);
            })
            .Build();

        return await parser.InvokeAsync(args);
    }
}
