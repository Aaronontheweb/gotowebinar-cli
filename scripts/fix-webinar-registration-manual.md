# Manual Steps to Fix Webinar Registration Fields

## Issue
The GoToWebinar API v2 does not expose a direct endpoint for managing registration fields. The `/registrationFields` endpoint we implemented doesn't exist in the actual API.

## Solution
Registration fields must be configured through the GoToWebinar web interface. Here's how to fix the affected webinars:

## Affected Webinars (October-December 2025)

### October 2025
- `1027854706014082144` - Akka.NET End to End Application Design Training - October 28, 2025
- `5777069837866945375` - Real-World Distributed Systems with Akka.Cluster - October 16, 2025
- `4602165797285970522` - Building Streaming Systems with Akka.NET Streams - October 7, 2025

### November 2025
(Run this command to get the list)
```bash
dotnet run --project src/GoToWebinarCLI -- webinar list --all --format json | \
  jq -r '.[] | select(.times[0].startTime >= "2025-11-01" and .times[0].startTime < "2025-12-01") | "\(.webinarKey) | \(.subject) | \(.times[0].startTime)"'
```

### December 2025
(Run this command to get the list)
```bash
dotnet run --project src/GoToWebinarCLI -- webinar list --all --format json | \
  jq -r '.[] | select(.times[0].startTime >= "2025-12-01" and .times[0].startTime < "2026-01-01") | "\(.webinarKey) | \(.subject) | \(.times[0].startTime)"'
```

## Manual Fix Steps for Each Webinar

1. **Login to GoToWebinar**
   - Go to https://global.gotowebinar.com
   - Sign in with your account

2. **For each webinar key above:**

   a. Navigate to the webinar:
      - Go to "My Webinars"
      - Find the webinar by date/title
      - Click "Edit"

   b. Configure Registration Fields:
      - Click on "Registration" tab
      - Click "Customize registration form"
      - Enable these fields and mark as REQUIRED:
        - ✅ **Job Title** (Required)
        - ✅ **Company/Organization** (Required)
        - ✅ First Name (Required - usually default)
        - ✅ Last Name (Required - usually default)
        - ✅ Email (Required - usually default)

   c. Configure Registration Settings:
      - Set "Registration approval" to: **Manual approval required**
      - Save changes

3. **Verify the changes:**
   - Open the registration URL in an incognito browser
   - Confirm that Job Title and Organization are required fields

## Verification Script

After making the manual changes, you can verify by attempting to register:

```bash
# Test registration with all fields
curl -X POST "https://api.getgo.com/G2W/rest/v2/organizers/$(dotnet run --project src/GoToWebinarCLI -- config get | grep 'Organizer Key:' | awk '{print $3}')/webinars/WEBINAR_KEY/registrants" \
  -H "Authorization: Bearer $(dotnet run --project src/GoToWebinarCLI -- config get | grep 'Access Token:' | awk '{print $3}')" \
  -H "Content-Type: application/json" \
  -H "Accept: application/vnd.citrix.g2wapi-v1.1+json" \
  -d '{
    "firstName": "Test",
    "lastName": "User",
    "email": "test@example.com",
    "jobTitle": "Software Engineer",
    "organization": "Test Company"
  }'
```

## Alternative: Copy Settings from Template

If you have a webinar with the correct settings already configured, you can:
1. Use the "Copy Webinar" function in the UI
2. Copy from a good template webinar
3. Update the date/time for the new webinar

## Prevention for Future

When copying webinars in the future, always use the GoToWebinar UI's "Copy Webinar" function instead of creating new ones via API, as it will preserve all registration field settings.

## CLI Command Updates Needed

Since the registration fields API doesn't exist, we should:
1. Remove the `webinar fields` commands
2. Add documentation about this limitation
3. Consider adding a command that tests registration to verify fields are configured

## Workaround for Automation

We can create a test registration command that attempts to register with all fields to verify they're configured:

```bash
dotnet run --project src/GoToWebinarCLI -- registrant test-fields WEBINAR_KEY
```

This would attempt to register with jobTitle and organization fields and report whether they're accepted.