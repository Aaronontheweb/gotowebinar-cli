# GoToWebinar CLI

A command-line interface for interacting with the GoToWebinar API, providing easy access to webinar management, attendee tracking, and registration handling.

## Features

- **Authentication Management**: Secure OAuth2 authentication flow with token management
- **Configuration Management**: Store and manage multiple API configurations
- **Rate Limiting**: Built-in rate limiting to respect API quotas
- **Auto-Update**: Automatic update checking for new CLI versions
- **Webinar Management**: Create, list, and manage webinars
- **Registrant Handling**: Export and manage webinar registrants
- **Attendee Tracking**: Track and export attendee information

## Installation

### Quick Install (Recommended)

#### Linux/macOS
```bash
curl -sSL https://raw.githubusercontent.com/Aaronontheweb/gotowebinar-cli/dev/scripts/install.sh | bash
```

The installer will:
- Install to `~/.local/bin` (no sudo required)
- Add the directory to your PATH if needed
- Guide you through PATH setup if manual configuration is required

#### Windows (PowerShell)
```powershell
iwr -useb https://raw.githubusercontent.com/Aaronontheweb/gotowebinar-cli/dev/scripts/install.ps1 | iex
```

The installer will:
- Install to `%LOCALAPPDATA%\Programs\GoToWebinarCLI` (no admin required)
- Add the directory to your user PATH automatically

### Manual Download
Download the latest release for your platform from the [releases page](https://github.com/Aaronontheweb/gotowebinar-cli/releases/latest).

### Build from Source

#### Prerequisites
- .NET 9.0 SDK or later (check `global.json` for exact version)

```bash
git clone https://github.com/Aaronontheweb/gotowebinar-cli.git
cd gotowebinar-cli
dotnet publish -c Release

# The executable will be in:
# Linux/macOS: src/GoToWebinarCLI/bin/Release/net9.0/linux-x64/publish/gotowebinar
# Windows: src/GoToWebinarCLI/bin/Release/net9.0/win-x64/publish/gotowebinar.exe
```

## Getting GoToWebinar API Credentials

To use this CLI, you'll need to obtain API credentials from GoToWebinar:

1. **Create a Developer Account**: 
   - Visit the [GoTo Developer Center](https://developer.goto.com/)
   - Sign up for a developer account if you don't already have one

2. **Create an OAuth Client**:
   - Navigate to the [OAuth Clients](https://developer.goto.com/oauth-clients) section
   - Click "Create OAuth Client"
   - Fill in the required information:
     - **Product**: Select "GoToWebinar"
     - **Application Name**: Enter a name for your application
     - **Application Description**: Provide a brief description
     - **Redirect URIs**: Add `http://localhost:7878/callback` (required for CLI authentication)
   
3. **Obtain Your Credentials**:
   - Once created, you'll receive:
     - **Client ID**: A unique identifier for your application
     - **Client Secret**: A secret key (keep this secure!)
   - Save these credentials securely - you'll need them to configure the CLI

4. **Important Notes**:
   - Keep your Client Secret confidential and never commit it to version control
   - The CLI stores credentials locally in your user configuration directory
   - You may need to have an active GoToWebinar subscription to access certain API features

## Usage

### Initial Configuration
Configure your API credentials obtained from the steps above:
```bash
gotowebinar config set --client-id YOUR_CLIENT_ID --client-secret YOUR_CLIENT_SECRET
```

### Authentication
Authenticate with GoToWebinar:
```bash
gotowebinar config auth
```

### Common Commands

List webinars:
```bash
gotowebinar webinar list
```

Copy a webinar:
```bash
gotowebinar webinar copy --webinar-id WEBINAR_ID --title "New Webinar Title"
```

Update a webinar:
```bash
gotowebinar webinar update --webinar-id WEBINAR_ID --title "Updated Title"
```

Export registrants:
```bash
gotowebinar registrant export --webinar-id WEBINAR_ID --output registrants.csv
```

Check for updates:
```bash
gotowebinar update check
```

### Examples and Workflows

For detailed examples and complete workflows, see the [Examples Documentation](docs/EXAMPLES.md), which includes:
- Step-by-step webinar copying workflows
- Batch operations and automation scripts
- Post-webinar analysis procedures
- Troubleshooting guides

### Managing Multiple Profiles
```bash
# Create a new profile
gotowebinar config set --client-id CLIENT_ID --client-secret SECRET --profile work

# Switch between profiles
gotowebinar config profiles switch work

# List all profiles
gotowebinar config profiles list
```

### Getting Help
```bash
gotowebinar --help
gotowebinar [command] --help
```

## Development

### Project Structure
- `src/GoToWebinarCLI/` - Main CLI application
  - `Commands/` - Command implementations
  - `Models/` - Data models for API interactions
  - `Services/` - Core services (API client, authentication, etc.)

### Building
```bash
dotnet build
```

### Running Tests
```bash
dotnet test
```

### Creating a Release
1. Update `RELEASE_NOTES.md` with version information
2. Run the build script to update version numbers:
   ```powershell
   ./build.ps1
   ```
3. Create and push a tag to trigger the release pipeline

## Configuration

The CLI stores configuration in the user's home directory:
- Windows: `%USERPROFILE%\.gotowebinar\config.json`
- macOS/Linux: `~/.gotowebinar/config.json`

## API Rate Limiting

The CLI includes built-in rate limiting to comply with GoToWebinar API limits. The rate limiter automatically handles:
- Request throttling
- Retry logic with exponential backoff
- Proper error handling for rate limit responses

## Auto-Update Feature

To manually check for updates:
```bash
gotowebinar update check
```

Note: Auto-update configuration is managed through the config file directly.

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the Apache License 2.0 - see the LICENSE file for details.

## Support

For issues, feature requests, or questions, please open an issue on GitHub.
