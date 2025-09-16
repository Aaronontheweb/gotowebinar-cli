#!/bin/bash

# Quick script to copy registration fields from a good source webinar to affected ones

echo "======================================="
echo "Fix Registration Fields - Quick Method"
echo "======================================="
echo

# Step 1: Find source webinar
echo "Step 1: Finding source webinar with good settings..."
echo "Listing webinars from April-May 2025:"
echo
dotnet run --project src/GoToWebinarCLI -- webinar list --from 2025-04-01 --to 2025-05-31
echo

echo "Enter the webinar key to use as source (one with good registration settings):"
read SOURCE_KEY

# Step 2: Verify source
echo
echo "Checking registration fields for source webinar $SOURCE_KEY:"
dotnet run --project src/GoToWebinarCLI -- webinar fields get "$SOURCE_KEY"
echo

echo "Does this look correct? Should have jobTitle and organization as required. (y/n):"
read CONFIRM
if [ "$CONFIRM" != "y" ]; then
    echo "Exiting. Please find a better source webinar."
    exit 1
fi

# Step 3: Find affected webinars
echo
echo "Step 2: Finding affected webinars..."
echo "Listing webinars from October-December 2025:"
echo
dotnet run --project src/GoToWebinarCLI -- webinar list --from 2025-10-01 --to 2025-12-31
echo

echo "Enter ALL affected webinar keys (comma-separated, no spaces):"
echo "Example: 1234567890,0987654321,1122334455"
read TARGET_KEYS

# Step 4: Apply fix
echo
echo "Step 3: Copying registration fields from $SOURCE_KEY to targets..."
echo "Targets: $TARGET_KEYS"
echo
dotnet run --project src/GoToWebinarCLI -- webinar fields copy "$SOURCE_KEY" "$TARGET_KEYS"

# Step 5: Verify
echo
echo "Step 4: Verifying fixes..."
echo "Enter one of the target keys to verify:"
read VERIFY_KEY

dotnet run --project src/GoToWebinarCLI -- webinar fields get "$VERIFY_KEY"

echo
echo "======================================="
echo "Fix complete!"
echo "======================================="
echo
echo "To verify other webinars, run:"
echo "  dotnet run --project src/GoToWebinarCLI -- webinar fields get <KEY>"