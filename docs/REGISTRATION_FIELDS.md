# Managing Registration Fields

This guide explains how to manage registration fields for GoToWebinar webinars using the CLI.

## Background

Registration fields control what information attendees must provide when registering for a webinar. Common fields include:

- **firstName** (always required)
- **lastName** (always required)
- **email** (always required)
- **jobTitle** (can be optional or required)
- **organization** (can be optional or required)
- **phone** (can be optional or required)
- And many others...

## New Commands

### View Registration Fields

```bash
# View fields for a webinar
gotowebinar webinar fields get <webinar-key>

# Output in JSON format
gotowebinar webinar fields get <webinar-key> --format json
```

### Set Registration Fields

```bash
# Make jobTitle required
gotowebinar webinar fields set <webinar-key> --field jobTitle --required

# Make organization required and visible
gotowebinar webinar fields set <webinar-key> --field organization --required --visible

# Make phone optional
gotowebinar webinar fields set <webinar-key> --field phone --visible
```

### Copy Registration Fields

Copy all registration field settings from one webinar to another:

```bash
# Copy to single webinar
gotowebinar webinar fields copy <source-key> <target-key>

# Copy to multiple webinars
gotowebinar webinar fields copy <source-key> "key1,key2,key3"
```

### Enable Lead Generation Fields

Quickly enable standard lead generation fields (jobTitle and organization as required):

```bash
# Single webinar
gotowebinar webinar fields enable-leadgen <webinar-key>

# Multiple webinars
gotowebinar webinar fields enable-leadgen "key1,key2,key3"
```

## Enhanced Copy Command

The webinar copy command now copies registration fields and email settings by default:

```bash
# Full copy (includes all settings)
gotowebinar webinar copy <source-key> --start "2025-10-07 16:00"

# Copy without settings (old behavior)
gotowebinar webinar copy <source-key> --start "2025-10-07 16:00" --copy-settings false
```

## Fixing Webinars Missing Registration Fields

If you have webinars that were copied without proper registration fields, you can fix them using:

### Option 1: Use the Fix Script

```bash
# Run the interactive fix script
./scripts/fix-webinar-registration-fields.sh
```

This script will:
1. Help you find a source webinar with proper settings
2. Identify affected webinars
3. Copy the settings to all affected webinars

### Option 2: Manual Fix

```bash
# 1. Find a good source webinar (from before the issue)
gotowebinar webinar list --from 2025-05-01 --to 2025-05-31

# 2. Check its registration fields
gotowebinar webinar fields get <source-webinar-key>

# 3. Find affected webinars
gotowebinar webinar list --from 2025-10-01 --to 2025-12-31

# 4. Copy fields to affected webinars
gotowebinar webinar fields copy <source-key> "affected-key-1,affected-key-2,affected-key-3"

# 5. Verify the fix
gotowebinar webinar fields get <affected-key>
```

### Option 3: Quick Lead Gen Fix

If you just need to enable jobTitle and organization:

```bash
# List affected webinars
gotowebinar webinar list --from 2025-10-01 --to 2025-12-31

# Enable lead gen fields for all
gotowebinar webinar fields enable-leadgen "key1,key2,key3,key4,key5,key6"
```

## Best Practices

1. **Always verify registration fields after copying a webinar**
   ```bash
   gotowebinar webinar fields get <new-webinar-key>
   ```

2. **Use templates**: Keep a webinar with perfect settings as a template
   ```bash
   # Save a template webinar key
   TEMPLATE_KEY="your-perfect-webinar-key"

   # Always copy from template
   gotowebinar webinar copy $TEMPLATE_KEY --start "2025-10-07 16:00"
   ```

3. **Batch operations**: When setting up multiple webinars, use comma-separated keys
   ```bash
   # Enable lead gen for all October webinars at once
   gotowebinar webinar fields enable-leadgen "oct7-key,oct16-key,oct28-key"
   ```

## Common Field Names

| Field | Description | Typically Required? |
|-------|-------------|-------------------|
| firstName | First name | Always |
| lastName | Last name | Always |
| email | Email address | Always |
| jobTitle | Job title/position | For lead gen |
| organization | Company/organization | For lead gen |
| phone | Phone number | Optional |
| questionsAndComments | Open text field | Optional |
| industry | Industry selection | Optional |
| numberOfEmployees | Company size | Optional |
| purchasingTimeFrame | Buying timeline | Optional |
| purchasingRole | Decision maker role | Optional |

## Troubleshooting

### Fields not updating?
- Verify you're authenticated: `gotowebinar config whoami`
- Check the webinar exists: `gotowebinar webinar get <key>`
- Some fields may not be available in all GoToWebinar plans

### Email settings not copying?
- Email settings API endpoints may not be available in all API versions
- The CLI will show a warning but continue with other operations

### Need to verify what was copied?
```bash
# Check all settings for a webinar
gotowebinar webinar get <key> --format json
gotowebinar webinar fields get <key>
```