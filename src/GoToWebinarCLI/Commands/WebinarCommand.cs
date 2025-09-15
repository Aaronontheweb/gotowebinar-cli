using System.CommandLine;
using System.Text.Json;
using GoToWebinarCLI.Models;
using GoToWebinarCLI.Services;

namespace GoToWebinarCLI.Commands;

public sealed class WebinarCommand : Command
{
    public WebinarCommand() : base("webinar", "Manage webinars")
    {
        AddCommand(CreateListCommand());
        AddCommand(CreateGetCommand());
        AddCommand(CreateCreateCommand());
        AddCommand(CreateDeleteCommand());
    }

    private static Command CreateListCommand()
    {
        var command = new Command("list", "List webinars");

        var allOption = new Option<bool>(
            new[] { "--all", "-a" },
            "Show all webinars (past and future)");

        var pastOption = new Option<bool>(
            new[] { "--past", "-p" },
            "Show only past webinars");

        var fromOption = new Option<DateTime?>(
            new[] { "--from", "-f" },
            "Start date (YYYY-MM-DD)");

        var toOption = new Option<DateTime?>(
            new[] { "--to", "-t" },
            "End date (YYYY-MM-DD)");

        var formatOption = new Option<string>(
            new[] { "--format" },
            () => "table",
            "Output format (table, json, csv)");

        command.AddOption(allOption);
        command.AddOption(pastOption);
        command.AddOption(fromOption);
        command.AddOption(toOption);
        command.AddOption(formatOption);

        command.SetHandler(async (all, past, from, to, format) =>
        {
            var configService = new ConfigurationService();
            using var apiClient = new GoToWebinarApiClient(configService);

            // Determine date range based on flags
            DateTime? startDate = from;
            DateTime? endDate = to;

            if (!from.HasValue || !to.HasValue)
            {
                if (all)
                {
                    startDate = from ?? DateTime.UtcNow.AddYears(-2);
                    endDate = to ?? DateTime.UtcNow.AddYears(2);
                }
                else if (past)
                {
                    startDate = from ?? DateTime.UtcNow.AddYears(-2);
                    endDate = to ?? DateTime.UtcNow;
                }
                else // Default: upcoming
                {
                    startDate = from ?? DateTime.UtcNow;
                    endDate = to ?? DateTime.UtcNow.AddYears(1);
                }
            }

            var webinars = await apiClient.GetWebinarsAsync(true, startDate, endDate);

            if (webinars == null || webinars.Count == 0)
            {
                Console.WriteLine("No webinars found.");
                return;
            }

            // Sort webinars by start time
            // For past webinars, show most recent first (descending)
            // For upcoming webinars, show soonest first (ascending)
            // For all webinars, show most recent first
            if (past || all)
            {
                webinars = webinars.OrderByDescending(w => w.Times?.FirstOrDefault()?.StartTime ?? DateTime.MinValue).ToList();
            }
            else
            {
                webinars = webinars.OrderBy(w => w.Times?.FirstOrDefault()?.StartTime ?? DateTime.MinValue).ToList();
            }

            switch (format.ToLowerInvariant())
            {
                case "json":
                    var jsonContext = new GoToWebinarJsonContext(new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(JsonSerializer.Serialize(webinars, jsonContext.ListWebinar));
                    break;

                case "csv":
                    Console.WriteLine("WebinarKey,Subject,StartTime,EndTime,Registrants,InSession");
                    foreach (var webinar in webinars)
                    {
                        var startTime = webinar.Times?.FirstOrDefault()?.StartTime.ToString("yyyy-MM-dd HH:mm") ?? "";
                        var endTime = webinar.Times?.FirstOrDefault()?.EndTime.ToString("yyyy-MM-dd HH:mm") ?? "";
                        Console.WriteLine($"{webinar.WebinarKey},{EscapeCsv(webinar.Subject)},{startTime},{endTime},{webinar.NumberOfRegistrants ?? 0},{webinar.InSession}");
                    }
                    break;

                default: // table
                    Console.WriteLine($"{"Key",-15} {"Subject",-40} {"Start Time",-20} {"Registrants",-12} {"Status",-10}");
                    Console.WriteLine(new string('-', 100));

                    foreach (var webinar in webinars)
                    {
                        var startTime = webinar.Times?.FirstOrDefault()?.StartTime.ToString("yyyy-MM-dd HH:mm") ?? "Not scheduled";
                        var status = webinar.InSession ? "In Session" : "Scheduled";
                        var subject = webinar.Subject.Length > 40 ? webinar.Subject[..37] + "..." : webinar.Subject;

                        Console.WriteLine($"{webinar.WebinarKey,-15} {subject,-40} {startTime,-20} {webinar.NumberOfRegistrants ?? 0,-12} {status,-10}");
                    }
                    break;
            }
        }, allOption, pastOption, fromOption, toOption, formatOption);

        return command;
    }

    private static Command CreateGetCommand()
    {
        var command = new Command("get", "Get webinar details");

        var keyArgument = new Argument<string>("webinar-key", "The webinar key");
        var formatOption = new Option<string>(
            new[] { "--format" },
            () => "detail",
            "Output format (detail, json)");

        command.AddArgument(keyArgument);
        command.AddOption(formatOption);

        command.SetHandler(async (key, format) =>
        {
            var configService = new ConfigurationService();
            using var apiClient = new GoToWebinarApiClient(configService);

            var webinar = await apiClient.GetWebinarAsync(key);

            if (webinar == null)
            {
                Console.WriteLine($"Webinar {key} not found.");
                Environment.Exit(1);
                return;
            }

            switch (format.ToLowerInvariant())
            {
                case "json":
                    var jsonContext = new GoToWebinarJsonContext(new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(JsonSerializer.Serialize(webinar, jsonContext.Webinar));
                    break;

                default: // detail
                    Console.WriteLine($"Webinar Details");
                    Console.WriteLine($"===============");
                    Console.WriteLine($"Key:              {webinar.WebinarKey}");
                    Console.WriteLine($"Subject:          {webinar.Subject}");
                    Console.WriteLine($"Description:      {webinar.Description ?? "N/A"}");
                    Console.WriteLine($"Time Zone:        {webinar.TimeZone}");
                    Console.WriteLine($"In Session:       {(webinar.InSession ? "Yes" : "No")}");
                    Console.WriteLine($"Registrants:      {webinar.NumberOfRegistrants ?? 0}");
                    Console.WriteLine($"Registration URL: {webinar.RegistrationUrl ?? "N/A"}");

                    if (webinar.Times != null && webinar.Times.Count > 0)
                    {
                        Console.WriteLine($"\nScheduled Times:");
                        foreach (var time in webinar.Times)
                        {
                            Console.WriteLine($"  {time.StartTime:yyyy-MM-dd HH:mm} - {time.EndTime:HH:mm}");
                        }
                    }

                    if (webinar.RegistrationLimit.HasValue)
                    {
                        Console.WriteLine($"\nRegistration Limit: {webinar.RegistrationLimit}");
                    }
                    break;
            }
        }, keyArgument, formatOption);

        return command;
    }

    private static Command CreateCreateCommand()
    {
        var command = new Command("create", "Create a new webinar");

        var subjectOption = new Option<string>(
            new[] { "--subject", "-s" },
            "Webinar subject/title")
        { IsRequired = true };

        var descriptionOption = new Option<string?>(
            new[] { "--description", "-d" },
            "Webinar description");

        var startTimeOption = new Option<DateTime>(
            new[] { "--start", "--start-time" },
            "Start time (YYYY-MM-DD HH:MM)")
        { IsRequired = true };

        var durationOption = new Option<int>(
            new[] { "--duration" },
            () => 60,
            "Duration in minutes");

        var timeZoneOption = new Option<string>(
            new[] { "--timezone", "-tz" },
            () => "America/New_York",
            "Time zone");

        command.AddOption(subjectOption);
        command.AddOption(descriptionOption);
        command.AddOption(startTimeOption);
        command.AddOption(durationOption);
        command.AddOption(timeZoneOption);

        command.SetHandler(async (subject, description, startTime, duration, timeZone) =>
        {
            var configService = new ConfigurationService();
            using var apiClient = new GoToWebinarApiClient(configService);

            var request = new CreateWebinarRequest
            {
                Subject = subject,
                Description = description,
                TimeZone = timeZone,
                Times = new List<WebinarTime>
                {
                    new()
                    {
                        StartTime = startTime,
                        EndTime = startTime.AddMinutes(duration)
                    }
                }
            };

            var webinar = await apiClient.CreateWebinarAsync(request);

            if (webinar == null)
            {
                Console.WriteLine("Failed to create webinar.");
                Environment.Exit(1);
                return;
            }

            Console.WriteLine($"✓ Webinar created successfully!");
            Console.WriteLine($"  Key: {webinar.WebinarKey}");
            Console.WriteLine($"  Subject: {webinar.Subject}");
            Console.WriteLine($"  Registration URL: {webinar.RegistrationUrl}");
        }, subjectOption, descriptionOption, startTimeOption, durationOption, timeZoneOption);

        return command;
    }

    private static Command CreateDeleteCommand()
    {
        var command = new Command("delete", "Delete a webinar");

        var keyArgument = new Argument<string>("webinar-key", "The webinar key to delete");
        var forceOption = new Option<bool>(
            new[] { "--force", "-f" },
            "Skip confirmation prompt");

        command.AddArgument(keyArgument);
        command.AddOption(forceOption);

        command.SetHandler(async (key, force) =>
        {
            if (!force)
            {
                Console.Write($"Are you sure you want to delete webinar {key}? [y/N]: ");
                var response = Console.ReadLine()?.Trim().ToLowerInvariant();

                if (response != "y" && response != "yes")
                {
                    Console.WriteLine("Deletion cancelled.");
                    return;
                }
            }

            var configService = new ConfigurationService();
            using var apiClient = new GoToWebinarApiClient(configService);

            var success = await apiClient.DeleteWebinarAsync(key);

            if (success)
            {
                Console.WriteLine($"✓ Webinar {key} deleted successfully.");
            }
            else
            {
                Console.WriteLine($"❌ Failed to delete webinar {key}.");
                Environment.Exit(1);
            }
        }, keyArgument, forceOption);

        return command;
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}

