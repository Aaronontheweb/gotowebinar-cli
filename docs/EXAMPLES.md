# GoToWebinar CLI Examples

This guide provides practical examples of using the GoToWebinar CLI for common webinar management tasks.

## Table of Contents
- [Authentication & Setup](#authentication--setup)
- [Listing and Viewing Webinars](#listing-and-viewing-webinars)
- [Copying Webinars](#copying-webinars)
- [Updating Webinars](#updating-webinars)
- [Managing Registrants](#managing-registrants)
- [Tracking Attendees](#tracking-attendees)
- [Complete Workflows](#complete-workflows)

## Authentication & Setup

### Initial Configuration
Set up your API credentials (obtained from the GoTo Developer Center):
```bash
gotowebinar config set --client-id YOUR_CLIENT_ID --client-secret YOUR_CLIENT_SECRET
```

### Login
Authenticate with GoToWebinar (opens browser for OAuth flow):
```bash
gotowebinar config auth
```

### Check Authentication Status
Verify your configuration and authentication status:
```bash
gotowebinar config test
```

## Listing and Viewing Webinars

### List All Webinars
Display all webinars with pagination:
```bash
gotowebinar webinar list
```

### List with Specific Page Size
```bash
gotowebinar webinar list --page-size 50
```

### View Specific Webinar Details
```bash
gotowebinar webinar get --webinar-id 1234567890
```

### List Webinars with JSON Output
For scripting and automation:
```bash
gotowebinar webinar list --output json > webinars.json
```

## Copying Webinars

The copy command is perfect for duplicating webinar setups for recurring events.

### Basic Copy
Copy a webinar with a new title:
```bash
gotowebinar webinar copy --webinar-id 1234567890 --title "Product Demo - March 2024"
```

### Copy with New Date and Time
Copy a webinar and schedule it for a specific date:
```bash
gotowebinar webinar copy \
  --webinar-id 1234567890 \
  --title "Weekly Training Session - Week 12" \
  --start-time "2024-03-25T14:00:00" \
  --end-time "2024-03-25T15:30:00"
```

### Copy with Description Update
```bash
gotowebinar webinar copy \
  --webinar-id 1234567890 \
  --title "Q2 Quarterly Review" \
  --description "Join us for our Q2 2024 quarterly business review"
```

## Updating Webinars

Modify existing webinar details without creating a new one.

### Update Title
```bash
gotowebinar webinar update \
  --webinar-id 1234567890 \
  --title "Updated: Advanced Features Workshop"
```

### Update Time
Reschedule a webinar:
```bash
gotowebinar webinar update \
  --webinar-id 1234567890 \
  --start-time "2024-03-28T10:00:00" \
  --end-time "2024-03-28T11:00:00"
```

### Update Description
```bash
gotowebinar webinar update \
  --webinar-id 1234567890 \
  --description "New agenda: We'll cover advanced automation features"
```

### Update Multiple Fields
```bash
gotowebinar webinar update \
  --webinar-id 1234567890 \
  --title "Rescheduled: Customer Success Workshop" \
  --start-time "2024-04-01T15:00:00" \
  --end-time "2024-04-01T16:30:00" \
  --description "Note: This session has been rescheduled to April 1st"
```

## Managing Registrants

### Export All Registrants
Export registrant data to CSV:
```bash
gotowebinar registrant export \
  --webinar-id 1234567890 \
  --output registrants.csv
```

### Export to JSON
```bash
gotowebinar registrant export \
  --webinar-id 1234567890 \
  --format json \
  --output registrants.json
```

### List Registrants (View in Terminal)
```bash
gotowebinar registrant list --webinar-id 1234567890
```

## Tracking Attendees

### Export Attendee Report
Get attendance data for a completed webinar:
```bash
gotowebinar attendee export \
  --webinar-id 1234567890 \
  --output attendees.csv
```

### List Attendees with Details
```bash
gotowebinar attendee list --webinar-id 1234567890
```

## Complete Workflows

### Workflow 1: Weekly Webinar Series Setup

Create a series of weekly webinars by copying a template:

```bash
# First, find your template webinar
gotowebinar webinar list

# Copy for Week 1
gotowebinar webinar copy \
  --webinar-id 9876543210 \
  --title "Weekly Product Demo - Week 1" \
  --start-time "2024-03-04T14:00:00" \
  --end-time "2024-03-04T15:00:00"

# Copy for Week 2
gotowebinar webinar copy \
  --webinar-id 9876543210 \
  --title "Weekly Product Demo - Week 2" \
  --start-time "2024-03-11T14:00:00" \
  --end-time "2024-03-11T15:00:00"

# Copy for Week 3
gotowebinar webinar copy \
  --webinar-id 9876543210 \
  --title "Weekly Product Demo - Week 3" \
  --start-time "2024-03-18T14:00:00" \
  --end-time "2024-03-18T15:00:00"

# List all webinars to verify
gotowebinar webinar list
```

### Workflow 2: Reschedule and Notify

Reschedule a webinar and export registrants for notification:

```bash
# 1. Export current registrants
gotowebinar registrant export \
  --webinar-id 1234567890 \
  --output original_registrants.csv

# 2. Update the webinar with new time and notice
gotowebinar webinar update \
  --webinar-id 1234567890 \
  --title "RESCHEDULED: Annual Planning Session" \
  --start-time "2024-04-05T09:00:00" \
  --end-time "2024-04-05T12:00:00" \
  --description "IMPORTANT: This session has been rescheduled to April 5th at 9 AM"

# 3. Verify the update
gotowebinar webinar get --webinar-id 1234567890
```

### Workflow 3: Post-Webinar Analysis

After a webinar completes, gather all data for analysis:

```bash
# 1. Get webinar details
gotowebinar webinar get --webinar-id 1234567890 > webinar_details.json

# 2. Export all registrants
gotowebinar registrant export \
  --webinar-id 1234567890 \
  --format csv \
  --output registrants.csv

# 3. Export attendee data
gotowebinar attendee export \
  --webinar-id 1234567890 \
  --format csv \
  --output attendees.csv

# 4. Create a summary (using jq if available)
echo "Webinar Analysis for ID: 1234567890"
echo "Registrants: $(wc -l < registrants.csv)"
echo "Attendees: $(wc -l < attendees.csv)"
```

### Workflow 4: Clone and Customize Monthly Webinar

Copy last month's webinar and update for the current month:

```bash
# 1. List recent webinars to find last month's
gotowebinar webinar list --page-size 20

# 2. Copy the webinar with all new details
gotowebinar webinar copy \
  --webinar-id 5555555555 \
  --title "Monthly Customer Q&A - March 2024" \
  --start-time "2024-03-28T16:00:00" \
  --end-time "2024-03-28T17:00:00" \
  --description "Join our monthly Q&A session where we address customer questions and showcase new features released in March"

# 3. Get the new webinar ID from the output and verify
gotowebinar webinar get --webinar-id NEW_WEBINAR_ID
```

## Tips and Best Practices

### Using JSON Output for Automation

Many commands support JSON output for scripting:

```bash
# Get webinar ID programmatically (requires jq)
WEBINAR_ID=$(gotowebinar webinar list --output json | jq -r '.webinars[0].webinarId')

# Copy that webinar
gotowebinar webinar copy --webinar-id $WEBINAR_ID --title "Automated Copy"
```

### Batch Operations with Shell Scripts

Create multiple webinars from a CSV:

```bash
#!/bin/bash
# create_webinars.sh

while IFS=',' read -r title start_time end_time
do
  gotowebinar webinar copy \
    --webinar-id TEMPLATE_ID \
    --title "$title" \
    --start-time "$start_time" \
    --end-time "$end_time"

  sleep 2  # Be nice to the API
done < webinar_schedule.csv
```

### Error Handling

Always check command success in scripts:

```bash
if gotowebinar webinar update --webinar-id 1234567890 --title "New Title"; then
  echo "Webinar updated successfully"
else
  echo "Failed to update webinar"
  exit 1
fi
```

### Date/Time Format

Always use ISO 8601 format for dates and times:
- Format: `YYYY-MM-DDTHH:MM:SS`
- Example: `2024-03-25T14:30:00`
- Include timezone if needed: `2024-03-25T14:30:00-05:00`

## Troubleshooting

### Authentication Issues
If you encounter authentication errors:
```bash
# Check token status
gotowebinar config test

# Re-authenticate if needed
gotowebinar config auth
```

### Rate Limiting
The CLI handles rate limiting automatically, but for batch operations, add delays:
```bash
for id in 123 456 789; do
  gotowebinar webinar get --webinar-id $id
  sleep 1  # Prevent rate limiting
done
```

### Debug Output
Enable verbose output for troubleshooting:
```bash
gotowebinar --verbose webinar list
```

## Additional Resources

- [GoToWebinar API Documentation](https://developer.goto.com/GoToWebinarV2)
- [Main README](../README.md)
- [Release Notes](../RELEASE_NOTES.md)