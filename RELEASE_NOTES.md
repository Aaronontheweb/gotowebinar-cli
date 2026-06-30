#### 1.1.2 June 30th 2026 ####

`registrant list` empty-webinar output now respects `--format`

**Bug Fixes:**
- **`registrant list --format json` now emits `[]` for a webinar with no registrants** - Previously the empty case printed the human sentence `No registrants found for webinar <key>.` to stdout regardless of `--format`, so JSON consumers received non-JSON and broke. The empty case is now format-aware and serializes through the same path as the non-empty case.
- **`registrant list --format csv` now emits the header row only when there are no registrants** - The empty case previously emitted the human sentence instead of a valid CSV document; it now outputs the `RegistrantKey,FirstName,...` header with no data rows.
- **`table` format keeps the friendly empty message** - For human-readable output, the empty case still prints `No registrants found for webinar <key>.`
- A null registrants result is treated as empty rather than throwing.

---

#### 1.1.1 June 30th 2026 ####

`registrant list --status` validation

**Bug Fixes:**
- **`registrant list --status` now validates its argument and fails loudly on unrecognized values** - Previously, passing an unknown status (e.g. `--status pending`) silently returned an empty list and exited 0, giving no indication that the filter was invalid. The command now prints a clear error message (`❌ Error: Unknown status '...'. Valid values: waiting, approved, denied.`) and exits 1.
- **Help text corrected: `pending` is not a valid GoToWebinar status** - The `--status` option help text previously advertised `pending` as an accepted value, but GoToWebinar has no such status. Unapproved registrants appear as `waiting`. The help text now correctly lists `waiting`, `approved`, and `denied`.

---

#### 1.1.0 June 24th 2026 ####

Headless and container-friendly credential management

**New Features:**
- **Environment-variable credential injection** - The CLI now reads credentials directly from environment variables, enabling headless and containerized (Docker/Kubernetes) usage without a browser or config file ([#78](https://github.com/Aaronontheweb/gotowebinar-cli/issues/78), [#79](https://github.com/Aaronontheweb/gotowebinar-cli/pull/79))
  - Supported variables: `GOTOWEBINAR_REFRESH_TOKEN`, `GOTOWEBINAR_ACCESS_TOKEN`, `GOTOWEBINAR_CLIENT_ID`, `GOTOWEBINAR_CLIENT_SECRET`, and `GOTOWEBINAR_ORGANIZER_KEY`
  - Refresh-token-first: given a refresh token plus client credentials, the CLI mints access tokens on demand, so no interactive browser login or on-disk config file is required
- **`config export` command** - Extract credentials for secret injection and rotation workflows ([#79](https://github.com/Aaronontheweb/gotowebinar-cli/pull/79))
  - Secure-by-default: writes a `0600`-permissioned file via `--output`, or prints to stdout only when you pass the explicit `--reveal` flag
  - Supports `--format env|json` for different secret-store targets
  - Supports `--refresh` to capture GoTo's rolled refresh token, ideal for credential-rotation jobs

**Bug Fixes:**
- **Stored refresh token no longer wiped on ordinary token refreshes** - GoTo only returns a new refresh token when rotating one near expiry; the CLI now preserves the existing refresh token on routine access-token refreshes instead of clearing it ([#79](https://github.com/Aaronontheweb/gotowebinar-cli/pull/79))

**Improvements:**
- **Recoverable from a missing access token** - When a refresh token is present, a missing or absent access token is now recovered automatically rather than failing ([#79](https://github.com/Aaronontheweb/gotowebinar-cli/pull/79))
- **Non-fatal config file I/O in container mode** - Config file read/write failures are now treated as non-fatal when running in container/headless mode, so ephemeral or read-only filesystems do not break the CLI ([#79](https://github.com/Aaronontheweb/gotowebinar-cli/pull/79))

**Impact for Users:**
- Run the CLI in CI pipelines, Docker containers, and Kubernetes pods using only environment variables - no browser login or persistent config file needed
- Export and rotate credentials safely with secure-by-default file permissions
- Long-lived refresh tokens are no longer accidentally discarded during normal operation

---

#### 1.0.2 June 23rd 2026 ####

Bug fixes: complete registrant data hydration and concurrent access safety

**Bug Fixes:**
- **Fixed registrant list missing organization and jobTitle fields** - The GoToWebinar v2 list endpoint returns condensed records that omit profile fields such as organization and jobTitle ([#71](https://github.com/Aaronontheweb/gotowebinar-cli/issues/71), [#75](https://github.com/Aaronontheweb/gotowebinar-cli/pull/75))
  - `registrant list` now fans out parallel requests to the individual registrant endpoint to hydrate full details for each record
  - Falls back gracefully to the basic record if an individual fetch fails
  - Hydrated results are cached normally to avoid redundant API calls
- **Fixed thread-safety races in parallel registrant fetch** - Eliminated concurrent access bugs introduced by fanning out to the public `GetRegistrantAsync` from multiple tasks ([#75](https://github.com/Aaronontheweb/gotowebinar-cli/pull/75))
  - Extracted private `FetchRegistrantDetailsAsync` helper that reuses the already-authenticated `HttpClient` without re-running `EnsureAuthenticatedAsync` or `GetConfigAsync`
  - Prevents concurrent writes to `DefaultRequestHeaders.Authorization` (not thread-safe for concurrent mutation)
  - Prevents N concurrent `RefreshTokenAsync` calls from racing to overwrite the token config file when a token expires mid-flight
- **Fixed empty registrant list bypassing the cache** - An early return for zero-registrant webinars was skipping the cache write, causing every call for an empty webinar to re-hit the GoToWebinar API ([#75](https://github.com/Aaronontheweb/gotowebinar-cli/pull/75))
- **Fixed `OperationCanceledException` swallowed during registrant fetch** - The generic catch block after `Task.WhenAll` was absorbing cancellation exceptions; they are now re-thrown so callers can distinguish cancellation from a real failure ([#75](https://github.com/Aaronontheweb/gotowebinar-cli/pull/75))
- **Fixed concurrent config load race in `ConfigurationService`** - Added a `SemaphoreSlim` guard on `LoadConfigAsync` so concurrent callers cannot race on `_config` field initialization or issue simultaneous `SaveConfigAsync` file writes ([#75](https://github.com/Aaronontheweb/gotowebinar-cli/pull/75))

**Impact for Users:**
- `registrant list` now returns complete registrant profiles including organization and job title fields
- Eliminates intermittent token refresh errors and corrupted config files when multiple parallel operations run simultaneously
- Zero-registrant webinars no longer generate unnecessary API requests on repeated calls
- Cancellation (Ctrl+C) during long registrant fetches is handled correctly

---

#### 1.0.1 January 7th 2026 ####

Bug fixes and installation improvements

**Bug Fixes:**
- **Fixed registrant list JSON parsing error** - Resolved JSON deserialization error when registrantKey is numeric instead of string ([#64](https://github.com/Aaronontheweb/gotowebinar-cli/pull/64))
  - Fixed handling of GoToWebinar API's inconsistent registrantKey field type
  - Ensures registrant list command works reliably across all webinars
- **Fixed registration fields API issues** - Improved registration fields command and test isolation ([#46](https://github.com/Aaronontheweb/gotowebinar-cli/pull/46))
  - Resolved API integration issues with registration fields endpoint
  - Enhanced test isolation to prevent test interference
  - More reliable registration form field management

**Improvements:**
- **Simplified installation process** - Removed sudo requirements from install scripts ([#44](https://github.com/Aaronontheweb/gotowebinar-cli/pull/44))
  - Install scripts (install.sh/install.ps1) no longer require elevated privileges
  - Easier setup for users with restricted permissions
  - Updated documentation with correct CLI commands and installation instructions

**Impact for Users:**
- Registrant listing now works consistently across all webinars regardless of API response format
- More reliable registration field management capabilities
- Smoother installation experience without administrative privileges
- Better documentation accuracy for setup and usage

---

#### 1.0.0 September 15th 2025 ####

First stable release of GoToWebinar CLI

**Major New Features:**
- **Webinar copy and update commands** - Complete webinar management capabilities ([#37](https://github.com/Aaronontheweb/gotowebinar-cli/pull/37))
  - `webinar copy` - Clone existing webinars to new dates with optional field overrides
  - `webinar update` - Modify existing webinar properties (title, description, timing, etc.)
  - Support for multiple output formats (detail, key-only, json) for scripting workflows
  - Enables easy management of recurring webinars and training sessions

**Critical Bug Fixes:**
- **Fixed OAuth authentication flow** - Resolved multiple authentication issues for reliable setup ([#25](https://github.com/Aaronontheweb/gotowebinar-cli/pull/25), [#24](https://github.com/Aaronontheweb/gotowebinar-cli/pull/24), [#36](https://github.com/Aaronontheweb/gotowebinar-cli/pull/36))
  - Fixed OAuth redirect URI encoding that was preventing successful authentication
  - Implemented HTTP Basic Auth for token exchange to resolve invalid_client errors
  - Corrected redirect port documentation (7878, not 8080)
  - Enhanced auth command help text with clear setup instructions

**Performance & Usability Improvements:**
- **Enhanced webinar list functionality** - Better data retrieval and organization ([#36](https://github.com/Aaronontheweb/gotowebinar-cli/pull/36))
  - Implemented pagination to fetch all webinars (not just first page)
  - Added intelligent sorting: recent-first for past webinars, soonest-first for upcoming
  - Fixed date range handling to retrieve webinars from all time periods
  - Improved help documentation for configuration and authentication commands

**Impact for Users:**
- Complete webinar management workflow from creation to updates and duplication
- Reliable OAuth authentication setup with clear error messages and guidance
- Comprehensive webinar listing that shows all available webinars with proper sorting
- Professional tooling ready for production use with recurring webinar management

**Stability & Testing:**
- Comprehensive unit test coverage for all new functionality
- Validated API client integration with proper error handling
- Native AOT compilation compatibility maintained for optimal performance

This release represents the graduation from beta to stable status, providing a complete, reliable solution for GoToWebinar management via command line.

---

#### 1.0.0-beta5 September 2nd 2025 ####

Configuration command validation improvements

**Bug Fixes:**
- **Fixed config set command validation** - Improved configuration setup experience with proper validation ([#21](https://github.com/Aaronontheweb/gotowebinar-cli/pull/21))
  - Now requires at least one OAuth credential when using `config set`
  - Warns users when both client ID and secret aren't provided
  - Added helpful usage message when no credentials are specified
  - Improved validation to ensure proper authentication setup

**Impact for Users:**
- Users will now receive clear guidance when setting up OAuth credentials
- Prevents incomplete configuration that could lead to authentication failures
- Better user experience with informative error messages and usage instructions

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