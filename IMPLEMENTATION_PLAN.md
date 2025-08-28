# GoToWebinar CLI Implementation Plan

## Executive Summary

This document outlines the implementation plan for a GoToWebinar CLI tool modeled after the successful Freshdesk CLI architecture. The tool will provide a fast, lightweight command-line interface for GoToWebinar using .NET 9 AOT compilation, featuring self-updating capabilities, comprehensive help system, and configuration testing.

## Architecture Overview

### Core Technologies
- **.NET 9** with Native AOT Compilation
- **System.Text.Json** with Source Generators (reflection-free)
- **System.CommandLine** for command parsing
- **HttpClient** with Polly for resilient API calls
- **GitHub Actions** for CI/CD and release automation

### Key Design Principles
1. **Zero Reflection** - Full AOT compatibility
2. **Small Binary Size** - Target <10MB per platform
3. **Fast Startup** - <50ms startup time
4. **Cross-Platform** - Linux, macOS, Windows support
5. **Self-Updating** - Automatic update checking and installation
6. **Secure Configuration** - Encrypted credential storage
7. **Rate Limit Handling** - Automatic retry with exponential backoff
8. **Read-Only Mode** - Safe exploration mode

## Project Structure

```
gotowebinar-cli/
├── src/
│   └── GoToWebinarCLI/
│       ├── Commands/           # Command handlers
│       │   ├── ConfigCommand.cs
│       │   ├── WebinarCommand.cs
│       │   ├── RegistrantCommand.cs
│       │   ├── RecordingCommand.cs
│       │   ├── AttendeeCommand.cs
│       │   ├── PanelistCommand.cs
│       │   ├── PollCommand.cs
│       │   ├── SurveyCommand.cs
│       │   ├── AnalyticsCommand.cs
│       │   ├── TranscriptCommand.cs
│       │   └── WebhookCommand.cs
│       ├── Models/              # Data models
│       │   ├── Webinar.cs
│       │   ├── Registrant.cs
│       │   ├── Attendee.cs
│       │   ├── Recording.cs
│       │   ├── Session.cs
│       │   ├── Panelist.cs
│       │   ├── Poll.cs
│       │   ├── Survey.cs
│       │   ├── Question.cs
│       │   ├── Analytics.cs
│       │   ├── Transcript.cs
│       │   ├── CustomField.cs
│       │   ├── Webhook.cs
│       │   └── ConfigFile.cs
│       ├── Services/            # Business logic
│       │   ├── GoToWebinarApiClient.cs
│       │   ├── ConfigurationService.cs
│       │   ├── UpdateService.cs
│       │   ├── ExportService.cs
│       │   ├── RateLimitHandler.cs
│       │   ├── AuthenticationService.cs
│       │   └── BulkOperationService.cs
│       ├── Helpers/             # Utilities
│       │   ├── OutputFormatter.cs
│       │   ├── DateTimeHelper.cs
│       │   └── ValidationHelper.cs
│       ├── Program.cs
│       ├── GoToWebinarJsonContext.cs
│       └── GoToWebinarCLI.csproj
├── tests/
│   └── GoToWebinarCLI.Tests/
├── scripts/
│   ├── install.sh
│   └── install.ps1
├── .github/
│   └── workflows/
│       ├── build.yml
│       └── release.yml
├── README.md
├── LICENSE
└── RELEASE_NOTES.md
```

## Phase 1: Foundation (Week 1-2)

### 1.1 Project Setup
- [ ] Initialize .NET 9 project with AOT configuration
- [ ] Set up GitHub repository with proper .gitignore
- [ ] Configure CI/CD pipeline with GitHub Actions
- [ ] Set up code quality tools (analyzers, formatters)
- [ ] Create project structure

### 1.2 Core Infrastructure
- [ ] Implement GoToWebinarJsonContext for AOT serialization
- [ ] Create base models (Webinar, Registrant, Attendee, etc.)
- [ ] Implement ConfigurationService with secure storage
- [ ] Build RateLimitHandler with exponential backoff
- [ ] Create OutputFormatter for multiple formats (table, json, csv)

### 1.3 Authentication & API Client
- [ ] Implement OAuth 2.0 authentication flow
- [ ] Create GoToWebinarApiClient with retry policies
- [ ] Add API response caching mechanism
- [ ] Implement error handling and logging
- [ ] Add connection testing functionality

## Phase 2: Core Commands (Week 3-4)

### 2.1 Configuration Management
```bash
gotowebinar config set --client-id <id> --client-secret <secret>
gotowebinar config auth                  # Interactive OAuth flow
gotowebinar config test                  # Test API connection
gotowebinar config get                   # Show current config
gotowebinar config profiles list         # List saved profiles
gotowebinar config profiles switch <name>
```

### 2.2 Webinar Management
```bash
# List webinars
gotowebinar webinar list [--upcoming|--past|--all]
gotowebinar webinar list --from 2024-01-01 --to 2024-12-31
gotowebinar webinar list --format json

# Get webinar details
gotowebinar webinar get <webinar-id>
gotowebinar webinar get <webinar-id> --include-sessions

# Create webinar
gotowebinar webinar create --title "..." --description "..." --time "2024-01-15T10:00:00"
gotowebinar webinar create --from-file webinar.json

# Clone webinar
gotowebinar webinar clone <webinar-id> --title "New Title" --time "2024-02-01T14:00:00"

# Update webinar
gotowebinar webinar update <webinar-id> --title "..." --description "..."

# Delete webinar
gotowebinar webinar delete <webinar-id> [--confirm]
```

### 2.3 Registrant Management
```bash
# List registrants
gotowebinar registrant list <webinar-id>
gotowebinar registrant list <webinar-id> --status approved
gotowebinar registrant list <webinar-id> --format csv > registrants.csv

# Get registrant details
gotowebinar registrant get <webinar-id> <registrant-id>

# Add registrant
gotowebinar registrant add <webinar-id> --email "..." --name "..."
gotowebinar registrant add <webinar-id> --from-csv registrants.csv

# Bulk operations
gotowebinar registrant approve <webinar-id> <registrant-id>
gotowebinar registrant approve-all <webinar-id>
gotowebinar registrant deny <webinar-id> <registrant-id>

# Export registrants
gotowebinar registrant export <webinar-id> --format xlsx --output registrants.xlsx
```

## Phase 3: Advanced Features (Week 5-6)

### 3.1 Session & Attendance Management
```bash
# List sessions
gotowebinar session list <webinar-id>

# Get session performance
gotowebinar session get <session-id> --include-attendees
gotowebinar session performance <session-id>

# Attendee operations
gotowebinar attendee list <session-id>
gotowebinar attendee list <session-id> --format json
gotowebinar attendee export <session-id> --format csv --output attendees.csv

# Attendance reports
gotowebinar report attendance <webinar-id> --format pdf
gotowebinar report engagement <session-id>
gotowebinar report questions <session-id>
```

### 3.2 Recording Management
```bash
# List recordings
gotowebinar recording list
gotowebinar recording list --webinar <webinar-id>

# Download recordings
gotowebinar recording get <recording-id>
gotowebinar recording download <recording-id> --output webinar.mp4
gotowebinar recording download-all <webinar-id> --output-dir ./recordings/

# Recording management
gotowebinar recording share <recording-id> --public
gotowebinar recording delete <recording-id> [--confirm]
```

### 3.3 Panelist Management
```bash
# List panelists
gotowebinar panelist list <webinar-id>

# Add panelist
gotowebinar panelist add <webinar-id> --email "expert@example.com" --name "Jane Expert"
gotowebinar panelist add-bulk <webinar-id> --from-csv panelists.csv

# Remove panelist
gotowebinar panelist remove <webinar-id> <panelist-id>

# Promote attendee to panelist
gotowebinar panelist promote <webinar-id> <attendee-id>

# Manage co-organizers (up to 49 allowed)
gotowebinar coorganizer add <webinar-id> --email "coorg@example.com"
gotowebinar coorganizer list <webinar-id>
```

### 3.4 Polls & Surveys
```bash
# Poll management
gotowebinar poll list <webinar-id>
gotowebinar poll create <webinar-id> --question "What's your experience level?" --options "Beginner,Intermediate,Advanced"
gotowebinar poll create <webinar-id> --from-file poll.json
gotowebinar poll launch <webinar-id> <poll-id>  # During live session
gotowebinar poll results <webinar-id> <poll-id>
gotowebinar poll export <webinar-id> --format csv

# Survey management
gotowebinar survey create <webinar-id> --questions-file survey.json
gotowebinar survey list <webinar-id>
gotowebinar survey results <webinar-id>
gotowebinar survey export <webinar-id> --format xlsx --output survey-results.xlsx
gotowebinar survey send <webinar-id> --to attendees  # Post-webinar survey
```

### 3.5 Q&A Management
```bash
# Questions and answers
gotowebinar qa list <session-id>
gotowebinar qa export <session-id> --format json
gotowebinar qa answer <session-id> <question-id> --response "Answer text"
gotowebinar qa moderate <session-id> --auto-approve
```

### 3.6 Analytics & Insights
```bash
# Analytics commands
gotowebinar analytics overview --from 2024-01-01 --to 2024-12-31
gotowebinar analytics webinar <webinar-id>
gotowebinar analytics engagement <session-id>
gotowebinar analytics source-tracking <webinar-id>  # Track registration sources

# Performance metrics
gotowebinar analytics attendance-rate <webinar-id>
gotowebinar analytics drop-off <session-id>  # When attendees left
gotowebinar analytics attention <session-id>  # Attendee attention scores

# Export analytics
gotowebinar analytics export --format pdf --output analytics-report.pdf
gotowebinar analytics export --format excel --output metrics.xlsx
```

### 3.7 Transcript Management
```bash
# Transcript operations
gotowebinar transcript generate <recording-id>
gotowebinar transcript get <recording-id> --format srt
gotowebinar transcript get <recording-id> --format txt
gotowebinar transcript get <recording-id> --format vtt
gotowebinar transcript search <recording-id> --keyword "important topic"
gotowebinar transcript enable --auto  # Auto-generate for all recordings
```

### 3.8 Custom Fields & Registration
```bash
# Custom registration fields
gotowebinar customfield create <webinar-id> --name "Company Size" --type dropdown --options "1-10,11-50,51-200,200+"
gotowebinar customfield list <webinar-id>
gotowebinar customfield required <webinar-id> <field-id> --set true

# Registration form customization
gotowebinar registration customize <webinar-id> --fields "firstName,lastName,email,company,customField1"
gotowebinar registration preview <webinar-id>
```

### 3.9 Branding & Customization
```bash
# Branding commands
gotowebinar branding set --logo ./logo.png --primary-color "#0066CC"
gotowebinar branding set --banner ./banner.jpg
gotowebinar branding apply <webinar-id>
gotowebinar branding template save --name "Corporate Brand"
gotowebinar branding template apply <webinar-id> "Corporate Brand"
```

### 3.10 Webhook Management
```bash
# Webhook configuration
gotowebinar webhook create --url "https://your-api.com/webhook" --events "registrant.added,attendee.joined"
gotowebinar webhook list
gotowebinar webhook test <webhook-id>
gotowebinar webhook logs <webhook-id> --last 100
gotowebinar webhook delete <webhook-id>

# Webhook events available:
# - webinar.created, webinar.changed, webinar.cancelled
# - registrant.added, registrant.approved, registrant.denied
# - attendee.joined, attendee.left
# - recording.ready
# - poll.completed, survey.completed
```

### 3.11 Email & Communication
```bash
# Email management
gotowebinar email preview <webinar-id> --type confirmation
gotowebinar email preview <webinar-id> --type reminder
gotowebinar email customize <webinar-id> --template ./email-template.html
gotowebinar email send-reminder <webinar-id> --when "1 hour before"
gotowebinar email disable <webinar-id> --type followup

# Automated communications
gotowebinar automation setup <webinar-id> --reminder "1day,1hour" --followup "immediately,1day"
```

### 3.12 Bulk Operations & Automation
```bash
# Bulk webinar creation
gotowebinar bulk create --from-csv webinars.csv
gotowebinar bulk create --from-json webinars.json

# Bulk clone
gotowebinar bulk clone <webinar-id> --count 5 --interval weekly

# Bulk export
gotowebinar bulk export --from 2024-01-01 --to 2024-12-31 --output ./exports/

# Templates
gotowebinar template save <webinar-id> --name "Monthly Meeting"
gotowebinar template list
gotowebinar template use "Monthly Meeting" --time "2024-03-01T10:00:00"
```

## Phase 4: Polish & Distribution (Week 7-8)

### 4.1 Self-Update System
```bash
gotowebinar update                    # Check and install updates
gotowebinar update --check            # Check only
gotowebinar update --channel beta     # Use beta channel
gotowebinar update --force            # Force update
```

### 4.2 Help System
```bash
gotowebinar --help                    # General help
gotowebinar webinar --help            # Command-specific help
gotowebinar help webinar create       # Detailed command help
gotowebinar help examples             # Show usage examples
```

### 4.3 Installation & Distribution
- [ ] Create install.sh for Linux/macOS
- [ ] Create install.ps1 for Windows
- [ ] Set up GitHub Releases automation
- [ ] Create Homebrew formula (macOS)
- [ ] Create Chocolatey package (Windows)
- [ ] Create snap package (Linux)

## GoToWebinar API Documentation

### API Overview
The GoToWebinar V2 API provides comprehensive webinar management capabilities through RESTful endpoints. The API uses OAuth 2.0 for authentication and returns JSON responses.

### Base URLs
- **API Base**: `https://api.getgo.com/G2W/rest/v2/`
- **OAuth Authorization**: `https://api.getgo.com/oauth/v2/authorize`
- **OAuth Token**: `https://api.getgo.com/oauth/v2/token`

### API Capabilities
1. **Webinar Management**: Create, update, delete, and clone webinars
2. **Registration**: Custom fields, approval workflows, bulk operations
3. **Panelists**: Up to 49 co-organizers and 50 panelists per webinar
4. **Engagement Tools**: Polls, surveys, Q&A sessions
5. **Analytics**: Attendance, engagement, source tracking
6. **Recordings**: Download, transcripts, sharing
7. **Webhooks**: Real-time event notifications
8. **Branding**: Custom logos, colors, templates

### Rate Limits
- **Per Second**: 10 requests maximum
- **Per Day**: 10,000 requests (can request increase to 50,000)
- **Response**: HTTP 429 with Retry-After header when exceeded

### Important Notes
- API parameters may be accepted but unused by the service
- Pagination support added for Get Registrants API (October 2023)
- Custom fields support for GetRegistrant API (July 2024)
- Not all features available via API may be documented

## Technical Implementation Details

### API Integration

#### Authentication Flow
1. OAuth 2.0 with refresh token support
2. Secure token storage in config file
3. Automatic token refresh on expiration
4. Multiple account profile support

#### Rate Limiting Strategy
```csharp
public class RateLimitHandler : DelegatingHandler
{
    // Per GoToWebinar API docs:
    // - 10 requests per second
    // - 10,000 requests per day (upgradeable to 50,000)
    // - HTTP 429 with Retry-After header when exceeded
    
    private readonly SemaphoreSlim _semaphore = new(10);
    private readonly Queue<DateTime> _requestTimes = new();
    private int _dailyRequestCount = 0;
    private DateTime _dailyResetTime = DateTime.UtcNow.Date.AddDays(1);
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        await ThrottleRequest(cancellationToken);
        
        var response = await base.SendAsync(request, cancellationToken);
        
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfter = GetRetryAfter(response);
            await Task.Delay(retryAfter, cancellationToken);
            return await SendAsync(request, cancellationToken);
        }
        
        return response;
    }
}
```

### AOT Serialization Context
```csharp
[JsonSerializable(typeof(Webinar))]
[JsonSerializable(typeof(Webinar[]))]
[JsonSerializable(typeof(Registrant))]
[JsonSerializable(typeof(Registrant[]))]
[JsonSerializable(typeof(Attendee))]
[JsonSerializable(typeof(Attendee[]))]
[JsonSerializable(typeof(Recording))]
[JsonSerializable(typeof(Recording[]))]
[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(Session[]))]
[JsonSerializable(typeof(GoToWebinarConfig))]
[JsonSerializable(typeof(OAuthToken))]
[JsonSerializable(typeof(ErrorResponse))]
public partial class GoToWebinarJsonContext : JsonSerializerContext
{
}
```

### Configuration Schema
```json
{
  "version": "1.0.0",
  "profiles": {
    "default": {
      "clientId": "encrypted_client_id",
      "clientSecret": "encrypted_client_secret",
      "accessToken": "encrypted_access_token",
      "refreshToken": "encrypted_refresh_token",
      "tokenExpiry": "2024-01-15T10:00:00Z",
      "organizerKey": "1234567890",
      "accountKey": "0987654321"
    }
  },
  "currentProfile": "default",
  "settings": {
    "defaultFormat": "table",
    "pageSize": 100,
    "autoUpdate": true,
    "updateChannel": "stable"
  }
}
```

## Testing Strategy

### Unit Tests
- Model serialization/deserialization
- Command parsing and validation
- Date/time handling across timezones
- Configuration encryption/decryption
- Output formatting

### Integration Tests
- API client with mock server
- Rate limiting behavior
- Authentication flow
- Bulk operations
- Error handling

### End-to-End Tests
- Complete command workflows
- Cross-platform binary execution
- Update mechanism
- Configuration management

## Performance Targets

- **Startup Time**: <50ms
- **Binary Size**: <10MB per platform
- **Memory Usage**: <30MB typical, <100MB peak
- **API Response Caching**: 5-minute TTL for read operations
- **Concurrent Operations**: Up to 10 parallel API calls

## Security Considerations

1. **Credential Storage**
   - AES-256 encryption for stored credentials
   - File permissions 600 on Unix systems
   - Windows DPAPI on Windows systems

2. **API Key Handling**
   - Never log full API keys
   - Mask credentials in output
   - Support environment variables for CI/CD

3. **OAuth Security**
   - PKCE flow for OAuth
   - Secure token refresh
   - Token expiry handling

4. **Read-Only Mode**
   - Global flag to prevent write operations
   - Useful for auditing and reporting

## Monitoring & Telemetry

- Optional anonymous usage statistics
- Error reporting to GitHub Issues
- Performance metrics collection
- Update adoption tracking

## Documentation Requirements

1. **README.md** - Installation, quick start, examples
2. **API_REFERENCE.md** - Complete command reference
3. **CONTRIBUTING.md** - Development setup, guidelines
4. **SECURITY.md** - Security policies and reporting
5. **Man pages** - Unix manual pages for each command

## Release Process

1. Semantic versioning (MAJOR.MINOR.PATCH)
2. Automated builds for multiple platforms
3. Signed binaries for Windows and macOS
4. Automatic release notes generation
5. Update notification in CLI
6. Rollback capability

## Success Metrics

- **Adoption**: 100+ GitHub stars in first 3 months
- **Performance**: All operations complete in <2 seconds
- **Reliability**: 99.9% success rate for API operations
- **User Satisfaction**: <5% bug reports per release
- **Cross-Platform**: Works on 95% of target systems

## Risk Mitigation

| Risk | Mitigation Strategy |
|------|-------------------|
| API Changes | Version detection and compatibility layer |
| Rate Limiting | Intelligent throttling and queue management |
| Large Data Sets | Pagination and streaming support |
| Network Issues | Retry logic with exponential backoff |
| Security Vulnerabilities | Regular dependency updates, security scanning |

## Timeline

- **Week 1-2**: Foundation and core infrastructure
- **Week 3-4**: Core commands (webinar, registrant, attendee)
- **Week 5-6**: Engagement features (polls, surveys, Q&A, panelists)
- **Week 7-8**: Analytics, transcripts, and webhooks
- **Week 9**: Branding, custom fields, email automation
- **Week 10**: Bulk operations and templates
- **Week 11**: Testing, documentation, and polish
- **Week 12**: Release preparation and distribution

## Next Steps

1. Review and approve implementation plan
2. Set up GitHub repository and CI/CD
3. Begin Phase 1 development
4. Create initial test suite
5. Establish feedback loop with early users

## References

- [GoToWebinar API Documentation](https://developer.goto.com/GoToWebinarV2)
- [.NET 9 AOT Documentation](https://docs.microsoft.com/en-us/dotnet/core/deploying/native-aot)
- [Freshdesk CLI Source](https://github.com/Aaronontheweb/freshdesk-cli)
- [System.CommandLine Documentation](https://docs.microsoft.com/en-us/dotnet/standard/commandline/)