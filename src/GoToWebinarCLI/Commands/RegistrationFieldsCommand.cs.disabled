using System.CommandLine;
using System.Text.Json;
using GoToWebinarCLI.Models;
using GoToWebinarCLI.Services;

namespace GoToWebinarCLI.Commands;

public sealed class RegistrationFieldsCommand : Command
{
    public RegistrationFieldsCommand() : base("fields", "Manage webinar registration fields")
    {
        AddCommand(CreateGetCommand());
        AddCommand(CreateSetCommand());
        AddCommand(CreateCopyCommand());
        AddCommand(CreateEnableLeadGenCommand());
    }

    private static Command CreateGetCommand()
    {
        var command = new Command("get", "Get registration fields for a webinar");

        var keyArgument = new Argument<string>("webinar-key", "The webinar key");
        var formatOption = new Option<string>(
            new[] { "--format", "-f" },
            () => "table",
            "Output format (table, json)");

        command.AddArgument(keyArgument);
        command.AddOption(formatOption);

        command.SetHandler(async (key, format) =>
        {
            var configService = new ConfigurationService();
            using var apiClient = new GoToWebinarApiClient(configService);

            var fields = await apiClient.GetRegistrationFieldsAsync(key);

            if (fields == null)
            {
                Console.WriteLine($"❌ Failed to get registration fields for webinar {key}");
                Environment.Exit(1);
                return;
            }

            switch (format.ToLowerInvariant())
            {
                case "json":
                    var jsonContext = new GoToWebinarJsonContext(new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(JsonSerializer.Serialize(fields, jsonContext.RegistrationFields));
                    break;

                default: // table
                    Console.WriteLine("Registration Fields");
                    Console.WriteLine("==================");
                    Console.WriteLine();

                    if (fields.Fields?.Any() == true)
                    {
                        Console.WriteLine($"{"Field",-25} {"Required",-10} {"Visible",-10}");
                        Console.WriteLine(new string('-', 45));

                        foreach (var field in fields.Fields.OrderBy(f => f.Field))
                        {
                            var required = field.Required ? "Yes" : "No";
                            var visible = field.Visible ? "Yes" : "No";
                            Console.WriteLine($"{field.Field,-25} {required,-10} {visible,-10}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No registration fields configured.");
                    }

                    if (fields.Questions?.Any() == true)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Custom Questions");
                        Console.WriteLine("================");
                        foreach (var question in fields.Questions)
                        {
                            Console.WriteLine($"- {question.Question} ({(question.Required ? "Required" : "Optional")})");
                        }
                    }
                    break;
            }
        }, keyArgument, formatOption);

        return command;
    }

    private static Command CreateSetCommand()
    {
        var command = new Command("set", "Set registration field requirements");

        var keyArgument = new Argument<string>("webinar-key", "The webinar key");
        var fieldOption = new Option<string>(
            new[] { "--field", "-f" },
            "The field name (e.g., jobTitle, organization)")
        { IsRequired = true };

        var requiredOption = new Option<bool>(
            new[] { "--required", "-r" },
            "Make the field required");

        var visibleOption = new Option<bool>(
            new[] { "--visible", "-v" },
            () => true,
            "Make the field visible");

        command.AddArgument(keyArgument);
        command.AddOption(fieldOption);
        command.AddOption(requiredOption);
        command.AddOption(visibleOption);

        command.SetHandler(async (key, fieldName, required, visible) =>
        {
            var configService = new ConfigurationService();
            using var apiClient = new GoToWebinarApiClient(configService);

            // Get current fields
            var fields = await apiClient.GetRegistrationFieldsAsync(key);
            if (fields == null)
            {
                Console.WriteLine($"❌ Failed to get registration fields for webinar {key}");
                Environment.Exit(1);
                return;
            }

            // Update or add the field
            if (fields.Fields == null)
            {
                fields.Fields = new List<RegistrationField>();
            }

            var existingField = fields.Fields.FirstOrDefault(f => f.Field.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (existingField != null)
            {
                existingField.Required = required;
                existingField.Visible = visible;
            }
            else
            {
                fields.Fields.Add(new RegistrationField
                {
                    Field = fieldName,
                    Required = required,
                    Visible = visible
                });
            }

            // Update the fields
            var success = await apiClient.UpdateRegistrationFieldsAsync(key, fields);
            if (success)
            {
                Console.WriteLine($"✓ Updated field '{fieldName}' - Required: {required}, Visible: {visible}");
            }
            else
            {
                Console.WriteLine($"❌ Failed to update registration fields");
                Environment.Exit(1);
            }
        }, keyArgument, fieldOption, requiredOption, visibleOption);

        return command;
    }

    private static Command CreateCopyCommand()
    {
        var command = new Command("copy", "Copy registration fields from one webinar to another");

        var sourceArgument = new Argument<string>("source-key", "The source webinar key");
        var targetArgument = new Argument<string>("target-key", "The target webinar key (or comma-separated list)");

        command.AddArgument(sourceArgument);
        command.AddArgument(targetArgument);

        command.SetHandler(async (source, targetKeys) =>
        {
            var configService = new ConfigurationService();
            using var apiClient = new GoToWebinarApiClient(configService);

            // Get source fields
            var sourceFields = await apiClient.GetRegistrationFieldsAsync(source);
            if (sourceFields == null)
            {
                Console.WriteLine($"❌ Failed to get registration fields from source webinar {source}");
                Environment.Exit(1);
                return;
            }

            // Parse target keys (support comma-separated list)
            var targets = targetKeys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var successCount = 0;
            var failCount = 0;

            foreach (var target in targets)
            {
                Console.Write($"Copying fields to {target}... ");
                var success = await apiClient.UpdateRegistrationFieldsAsync(target, sourceFields);

                if (success)
                {
                    Console.WriteLine("✓");
                    successCount++;
                }
                else
                {
                    Console.WriteLine("❌");
                    failCount++;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Summary: {successCount} succeeded, {failCount} failed");

            if (failCount > 0)
            {
                Environment.Exit(1);
            }
        }, sourceArgument, targetArgument);

        return command;
    }

    private static Command CreateEnableLeadGenCommand()
    {
        var command = new Command("enable-leadgen", "Enable standard lead generation fields (jobTitle and organization)");

        var keyArgument = new Argument<string>("webinar-key", "The webinar key (or comma-separated list)");

        command.AddArgument(keyArgument);

        command.SetHandler(async (keys) =>
        {
            var configService = new ConfigurationService();
            using var apiClient = new GoToWebinarApiClient(configService);

            // Parse target keys (support comma-separated list)
            var targets = keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var target in targets)
            {
                Console.WriteLine($"Enabling lead generation fields for {target}...");

                // Get current fields
                var fields = await apiClient.GetRegistrationFieldsAsync(target);
                if (fields == null)
                {
                    // Create new fields structure
                    fields = new RegistrationFields
                    {
                        Fields = new List<RegistrationField>()
                    };
                }

                if (fields.Fields == null)
                {
                    fields.Fields = new List<RegistrationField>();
                }

                // Ensure jobTitle is required
                var jobTitle = fields.Fields.FirstOrDefault(f => f.Field == StandardRegistrationFields.JobTitle);
                if (jobTitle == null)
                {
                    fields.Fields.Add(new RegistrationField
                    {
                        Field = StandardRegistrationFields.JobTitle,
                        Required = true,
                        Visible = true
                    });
                }
                else
                {
                    jobTitle.Required = true;
                    jobTitle.Visible = true;
                }

                // Ensure organization is required
                var organization = fields.Fields.FirstOrDefault(f => f.Field == StandardRegistrationFields.Organization);
                if (organization == null)
                {
                    fields.Fields.Add(new RegistrationField
                    {
                        Field = StandardRegistrationFields.Organization,
                        Required = true,
                        Visible = true
                    });
                }
                else
                {
                    organization.Required = true;
                    organization.Visible = true;
                }

                // Update the fields
                var success = await apiClient.UpdateRegistrationFieldsAsync(target, fields);
                if (success)
                {
                    Console.WriteLine($"  ✓ Enabled jobTitle (required)");
                    Console.WriteLine($"  ✓ Enabled organization (required)");
                }
                else
                {
                    Console.WriteLine($"  ❌ Failed to update fields");
                }
            }
        }, keyArgument);

        return command;
    }
}