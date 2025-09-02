using System.CommandLine;
using System.Text.Json;
using GoToWebinarCLI.Models;
using GoToWebinarCLI.Services;

namespace GoToWebinarCLI.Commands;

public sealed class RegistrantCommand : Command
{
    public RegistrantCommand() : base("registrant", "Manage webinar registrants")
    {
        AddCommand(CreateListCommand());
        AddCommand(CreateAddCommand());
        AddCommand(CreateRemoveCommand());
        AddCommand(CreateGetCommand());
    }

    private static Command CreateListCommand()
    {
        var command = new Command("list", "List registrants for a webinar");

        var webinarKeyArgument = new Argument<string>("webinar-key", "The webinar key");
        var formatOption = new Option<string>(
            new[] { "--format" },
            () => "table",
            "Output format (table, json, csv)");

        var statusOption = new Option<string?>(
            new[] { "--status", "-s" },
            "Filter by status (approved, denied, pending)");

        command.AddArgument(webinarKeyArgument);
        command.AddOption(formatOption);
        command.AddOption(statusOption);

        command.SetHandler(async (webinarKey, format, status) =>
        {
            var configService = new ConfigurationService();
            using var apiClient = new GoToWebinarApiClient(configService);

            var registrants = await apiClient.GetRegistrantsAsync(webinarKey);

            if (registrants == null || registrants.Count == 0)
            {
                Console.WriteLine($"No registrants found for webinar {webinarKey}.");
                return;
            }

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status))
            {
                registrants = registrants.Where(r =>
                    r.Status?.Equals(status, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            switch (format.ToLowerInvariant())
            {
                case "json":
                    var jsonContext = new GoToWebinarJsonContext(new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(JsonSerializer.Serialize(registrants, jsonContext.ListRegistrant));
                    break;

                case "csv":
                    Console.WriteLine("RegistrantKey,FirstName,LastName,Email,Status,RegisteredAt,JoinUrl");
                    foreach (var reg in registrants)
                    {
                        Console.WriteLine($"{reg.RegistrantKey},{EscapeCsv(reg.FirstName)},{EscapeCsv(reg.LastName)},{reg.Email},{reg.Status},{reg.RegistrationDate:yyyy-MM-dd HH:mm},{reg.JoinUrl ?? ""}");
                    }
                    break;

                default: // table
                    Console.WriteLine($"{"Key",-15} {"Name",-30} {"Email",-35} {"Status",-10} {"Registered",-20}");
                    Console.WriteLine(new string('-', 110));

                    foreach (var reg in registrants)
                    {
                        var name = $"{reg.FirstName} {reg.LastName}";
                        if (name.Length > 30) name = name[..27] + "...";
                        var email = reg.Email.Length > 35 ? reg.Email[..32] + "..." : reg.Email;

                        Console.WriteLine($"{reg.RegistrantKey,-15} {name,-30} {email,-35} {reg.Status ?? "N/A",-10} {reg.RegistrationDate:yyyy-MM-dd HH:mm}");
                    }

                    Console.WriteLine($"\nTotal: {registrants.Count} registrant(s)");
                    break;
            }
        }, webinarKeyArgument, formatOption, statusOption);

        return command;
    }

    private static Command CreateAddCommand()
    {
        var command = new Command("add", "Add a registrant to a webinar");

        var webinarKeyArgument = new Argument<string>("webinar-key", "The webinar key");
        var firstNameOption = new Option<string>(
            new[] { "--first-name", "-f" },
            "First name")
        { IsRequired = true };

        var lastNameOption = new Option<string>(
            new[] { "--last-name", "-l" },
            "Last name")
        { IsRequired = true };

        var emailOption = new Option<string>(
            new[] { "--email", "-e" },
            "Email address")
        { IsRequired = true };

        var organizationOption = new Option<string?>(
            new[] { "--organization", "-o" },
            "Organization name");

        var phoneOption = new Option<string?>(
            new[] { "--phone", "-p" },
            "Phone number");

        command.AddArgument(webinarKeyArgument);
        command.AddOption(firstNameOption);
        command.AddOption(lastNameOption);
        command.AddOption(emailOption);
        command.AddOption(organizationOption);
        command.AddOption(phoneOption);

        command.SetHandler(async (webinarKey, firstName, lastName, email, organization, phone) =>
        {
            var configService = new ConfigurationService();
            using var apiClient = new GoToWebinarApiClient(configService);

            var registrant = await apiClient.AddRegistrantAsync(webinarKey, new CreateRegistrantRequest
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Organization = organization,
                Phone = phone
            });

            if (registrant == null)
            {
                Console.WriteLine("Failed to add registrant.");
                Environment.Exit(1);
                return;
            }

            Console.WriteLine($"✓ Registrant added successfully!");
            Console.WriteLine($"  Registrant Key: {registrant.RegistrantKey}");
            Console.WriteLine($"  Name: {registrant.FirstName} {registrant.LastName}");
            Console.WriteLine($"  Email: {registrant.Email}");
            Console.WriteLine($"  Status: {registrant.Status}");

            if (!string.IsNullOrEmpty(registrant.JoinUrl))
            {
                Console.WriteLine($"  Join URL: {registrant.JoinUrl}");
            }
        }, webinarKeyArgument, firstNameOption, lastNameOption, emailOption, organizationOption, phoneOption);

        return command;
    }

    private static Command CreateRemoveCommand()
    {
        var command = new Command("remove", "Remove a registrant from a webinar");

        var webinarKeyArgument = new Argument<string>("webinar-key", "The webinar key");
        var registrantKeyArgument = new Argument<string>("registrant-key", "The registrant key");
        var forceOption = new Option<bool>(
            new[] { "--force", "-f" },
            "Skip confirmation prompt");

        command.AddArgument(webinarKeyArgument);
        command.AddArgument(registrantKeyArgument);
        command.AddOption(forceOption);

        command.SetHandler(async (webinarKey, registrantKey, force) =>
        {
            if (!force)
            {
                Console.Write($"Are you sure you want to remove registrant {registrantKey}? [y/N]: ");
                var response = Console.ReadLine()?.Trim().ToLowerInvariant();

                if (response != "y" && response != "yes")
                {
                    Console.WriteLine("Removal cancelled.");
                    return;
                }
            }

            var configService = new ConfigurationService();
            using var apiClient = new GoToWebinarApiClient(configService);

            var success = await apiClient.RemoveRegistrantAsync(webinarKey, registrantKey);

            if (success)
            {
                Console.WriteLine($"✓ Registrant {registrantKey} removed successfully.");
            }
            else
            {
                Console.WriteLine($"❌ Failed to remove registrant {registrantKey}.");
                Environment.Exit(1);
            }
        }, webinarKeyArgument, registrantKeyArgument, forceOption);

        return command;
    }

    private static Command CreateGetCommand()
    {
        var command = new Command("get", "Get registrant details");

        var webinarKeyArgument = new Argument<string>("webinar-key", "The webinar key");
        var registrantKeyArgument = new Argument<string>("registrant-key", "The registrant key");
        var formatOption = new Option<string>(
            new[] { "--format" },
            () => "detail",
            "Output format (detail, json)");

        command.AddArgument(webinarKeyArgument);
        command.AddArgument(registrantKeyArgument);
        command.AddOption(formatOption);

        command.SetHandler(async (webinarKey, registrantKey, format) =>
        {
            var configService = new ConfigurationService();
            using var apiClient = new GoToWebinarApiClient(configService);

            var registrant = await apiClient.GetRegistrantAsync(webinarKey, registrantKey);

            if (registrant == null)
            {
                Console.WriteLine($"Registrant {registrantKey} not found.");
                Environment.Exit(1);
                return;
            }

            switch (format.ToLowerInvariant())
            {
                case "json":
                    var jsonContext = new GoToWebinarJsonContext(new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(JsonSerializer.Serialize(registrant, jsonContext.Registrant));
                    break;

                default: // detail
                    Console.WriteLine($"Registrant Details");
                    Console.WriteLine($"==================");
                    Console.WriteLine($"Key:              {registrant.RegistrantKey}");
                    Console.WriteLine($"Name:             {registrant.FirstName} {registrant.LastName}");
                    Console.WriteLine($"Email:            {registrant.Email}");
                    Console.WriteLine($"Organization:     {registrant.Organization ?? "N/A"}");
                    Console.WriteLine($"Phone:            {registrant.Phone ?? "N/A"}");
                    Console.WriteLine($"Status:           {registrant.Status ?? "N/A"}");
                    Console.WriteLine($"Registered:       {registrant.RegistrationDate:yyyy-MM-dd HH:mm:ss}");

                    if (!string.IsNullOrEmpty(registrant.JoinUrl))
                    {
                        Console.WriteLine($"Join URL:         {registrant.JoinUrl}");
                    }

                    if (registrant.Responses != null && registrant.Responses.Count > 0)
                    {
                        Console.WriteLine($"\nCustom Responses:");
                        foreach (var response in registrant.Responses)
                        {
                            Console.WriteLine($"  {response.Question}: {response.Answer}");
                        }
                    }
                    break;
            }
        }, webinarKeyArgument, registrantKeyArgument, formatOption);

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

