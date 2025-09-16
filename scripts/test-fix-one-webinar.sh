#!/bin/bash

echo "============================================"
echo "TEST: Fix ONE Webinar - Complete Settings"
echo "============================================"
echo

# Check authentication
echo "Testing authentication..."
dotnet run --project src/GoToWebinarCLI -- config test
if [ $? -ne 0 ]; then
    echo "Not authenticated. Please run:"
    echo "  dotnet run --project src/GoToWebinarCLI -- config auth"
    exit 1
fi
echo

# Step 1: Find a good source webinar
echo "STEP 1: Finding source webinars with good settings"
echo "======================================="
echo "Listing webinars from April-May 2025 (before the issue):"
echo

dotnet run --project src/GoToWebinarCLI -- webinar list --from 2025-04-01 --to 2025-05-31 --format table

echo
echo "Enter the KEY of a webinar with good settings (check for Akka.Streams, Cluster, or App Design):"
read SOURCE_KEY

# Step 2: Examine source webinar
echo
echo "STEP 2: Examining source webinar"
echo "======================================="
echo "Getting full details of source webinar..."
echo

echo "Basic info:"
dotnet run --project src/GoToWebinarCLI -- webinar get "$SOURCE_KEY"

echo
echo "Registration fields:"
dotnet run --project src/GoToWebinarCLI -- webinar fields get "$SOURCE_KEY"

echo
echo "Does this source webinar have:"
echo "  ✓ jobTitle as required?"
echo "  ✓ organization as required?"
echo "  ✓ Other settings you expect?"
echo
echo "Continue? (y/n):"
read CONFIRM
if [ "$CONFIRM" != "y" ]; then
    echo "Please find a different source webinar."
    exit 1
fi

# Step 3: Find ONE affected webinar to test
echo
echo "STEP 3: Select ONE affected webinar to test"
echo "======================================="
echo "Listing recent webinars (October-December 2025):"
echo

dotnet run --project src/GoToWebinarCLI -- webinar list --from 2025-10-01 --to 2025-12-31 --format table

echo
echo "Enter the KEY of ONE webinar to test the fix on:"
read TEST_KEY

# Step 4: Check current state
echo
echo "STEP 4: Current state of test webinar"
echo "======================================="
echo "Current registration fields for $TEST_KEY:"
dotnet run --project src/GoToWebinarCLI -- webinar fields get "$TEST_KEY"

echo
echo "As expected, jobTitle and organization are probably missing or not required."
echo "Press Enter to continue with the fix..."
read

# Step 5: Apply the fix
echo
echo "STEP 5: Applying fix"
echo "======================================="
echo "Copying registration fields from source to test webinar..."
echo

dotnet run --project src/GoToWebinarCLI -- webinar fields copy "$SOURCE_KEY" "$TEST_KEY"

if [ $? -eq 0 ]; then
    echo "✓ Registration fields copied successfully"
else
    echo "❌ Failed to copy registration fields"
    echo "Note: The API endpoint might not exist or might have a different path"
fi

# Step 6: Verify the fix
echo
echo "STEP 6: Verifying the fix"
echo "======================================="
echo "New registration fields for $TEST_KEY:"
dotnet run --project src/GoToWebinarCLI -- webinar fields get "$TEST_KEY"

echo
echo "============================================"
echo "TEST COMPLETE"
echo "============================================"
echo
echo "If the fix worked, you should see:"
echo "  - jobTitle: Required = Yes"
echo "  - organization: Required = Yes"
echo
echo "If it worked, you can now fix all webinars by running:"
echo "  dotnet run --project src/GoToWebinarCLI -- webinar fields copy $SOURCE_KEY \"KEY1,KEY2,KEY3,KEY4,KEY5,KEY6\""
echo
echo "If it didn't work, we may need to:"
echo "  1. Check if the API endpoint path is correct"
echo "  2. Use the GoToWebinar web interface instead"
echo "  3. Contact GoToWebinar support about their API"