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

### Prerequisites
- .NET 9.0 SDK or later (check `global.json` for exact version)

### Build from Source
```bash
git clone https://github.com/stannardlabs/gotowebinar-cli.git
cd gotowebinar-cli
dotnet build
```

### Install as Global Tool
```bash
dotnet pack
dotnet tool install --global --add-source ./nupkg GoToWebinarCLI
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
     - **Redirect URIs**: Add `http://localhost:8080/callback` (required for CLI authentication)
   
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
gotowebinar config --client-id YOUR_CLIENT_ID --client-secret YOUR_CLIENT_SECRET
```

### Authentication
Authenticate with GoToWebinar:
```bash
gotowebinar auth login
```

### Common Commands

List webinars:
```bash
gotowebinar webinar list
```

Export registrants:
```bash
gotowebinar registrant export --webinar-id WEBINAR_ID --output registrants.csv
```

Check for updates:
```bash
gotowebinar update check
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

The CLI can automatically check for updates. To enable:
```bash
gotowebinar config --enable-auto-update
```

To manually check for updates:
```bash
gotowebinar update check
```

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