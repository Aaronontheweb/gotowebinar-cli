#### 1.2.3 September 2nd 2025 ####

Configuration validation improvements

**Bug Fixes:**
- **Enhanced config set command validation** - Improved OAuth credential configuration with better validation and user guidance ([#21](https://github.com/Aaronontheweb/gotowebinar-cli/pull/21))
  - Now requires at least one OAuth credential (client-id or client-secret) when using `config set`
  - Added helpful warning messages when only partial credentials are provided
  - Provides clear usage instructions when validation fails
  - Prevents setting empty configuration states that would cause authentication failures

**User Experience Improvements:**
- Better error messaging guides users through proper OAuth credential setup
- Clear validation prevents common configuration mistakes
- Improved command usability with helpful warnings and usage instructions

---

#### 1.0.0-beta4 September 2nd 2025 ####

Repository migration and installation fix update

**Critical Bug Fixes:**
- **Fixed repository references for installation and updates** - Resolved installation failures caused by outdated repository references ([#19](https://github.com/Aaronontheweb/gotowebinar-cli/pull/19))
  - Updated installation scripts (install.ps1 and install.sh) to use new repository location: Aaronontheweb/gotowebinar-cli
  - Modified UpdateService to use correct GitHub API endpoint for the new repository
  - Enhanced UpdateService to fetch all releases instead of just the latest for better version comparison
  - Ensures users can successfully install and update the tool from the new repository location

**Impact for Users:**
- Users with existing installations will now receive updates from the correct repository
- New installations will download from the proper location
- All installation scripts now point to the active repository

---

#### 1.0.0-beta3 September 2nd 2025 ####

Critical update service fix with improved reliability

**Bug Fixes:**
- **Fixed update service to use GitHub Releases API** - Resolved update check failures caused by non-existent version.json endpoint ([#17](https://github.com/Aaronontheweb/gotowebinar-cli/pull/17))
  - Now correctly queries GitHub's Releases API for version checks
  - Added proper User-Agent header required by GitHub API
  - Improved semantic versioning comparison with pre-release support
  - Fixed asset names to match actual release artifacts
  - Removed unused VersionManifest and DownloadUrls classes

**Technical Improvements:**
- Enhanced version comparison logic to properly handle beta, alpha, and other pre-release tags
- Streamlined update service architecture for better maintainability

---

#### 1.0.0-beta2 September 2nd 2025 ####

Major feature expansion with complete webinar and registrant management functionality

**New Features:**
- **Complete webinar management commands** - Full CRUD operations for webinars
  - `list` - List webinars with support for --all, --past flags and date filtering
  - `get` - Get detailed webinar information
  - `create` - Create new webinars with scheduling
  - `delete` - Delete webinars with confirmation prompts
- **Complete registrant management commands** - Comprehensive registrant handling
  - `list` - List registrants with status filtering
  - `get` - Get detailed registrant information
  - `add` - Add new registrants to webinars
  - `remove` - Remove registrants from webinars
- **Multiple output formats** - Support for table, JSON, and CSV output formats
- **Comprehensive API credential documentation** - Step-by-step guide for obtaining GoToWebinar API credentials

**Improvements:**
- **Enhanced command usability** - Replaced confusing `--upcoming` flag with clearer `--all` and `--past` flags
- **Robust testing infrastructure** - Added 16 unit tests with Moq and FluentAssertions for comprehensive coverage
- **AOT compilation compatibility** - Ensured all new commands work with native AOT compilation

**Security Updates:**
- Updated Microsoft.Extensions.Http.Polly from 9.0.0 to 9.0.8
- Updated System.Security.Cryptography.ProtectedData from 9.0.0 to 9.0.8

**Developer Experience:**
- Added interfaces (IGoToWebinarApiClient, IConfigurationService) for dependency injection
- Implemented test data builders for consistent test fixtures
- Enhanced error handling for API failures

---

#### 1.0.0-beta1 September 2nd 2025 ####

Initial beta release of the completely rewritten GoToWebinar CLI

**New Architecture:**
- Modern .NET 9 runtime with AOT compilation for improved performance
- Complete rewrite removing legacy dependencies and technical debt
- Cross-platform support with optimized native binaries

**Features:**
- OAuth2 authentication with secure credential storage
- Self-update functionality with automatic version checking  
- Core webinar management commands (list, details, create)
- Registrant export capabilities with flexible formatting
- API rate limiting for GoToWebinar compliance
- Improved installation scripts for streamlined setup
- Configuration management for API credentials

**Technical Improvements:**
- Native AOT compilation for faster startup and smaller memory footprint
- Enhanced error handling and user experience
- Streamlined build and deployment process

**Known Issues:**
- This is a beta release for testing and feedback
- Some advanced features may be added in future releases

---

#### 0.1.0-beta1 August 29th 2025 ####

Initial beta release of GoToWebinar CLI

**Features:**
- OAuth2 authentication flow with secure token storage
- Configuration management for API credentials
- Auto-update functionality with version checking
- Rate limiting for API compliance
- Basic webinar management commands
- Registrant export capabilities

**Known Issues:**
- This is a beta release for testing purposes
- Some commands may not be fully implemented

---

#### 0.1.0 TBD ####

First stable release

**Features:**
- Complete webinar management functionality
- Full attendee and registrant handling
- Robust error handling and retry logic
- Cross-platform support (Windows, macOS, Linux)