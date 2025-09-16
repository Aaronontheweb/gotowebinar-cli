#!/bin/bash

# Script to fix registration fields for webinars that were copied without proper settings
# This script will:
# 1. Find a source webinar with proper settings (from before June 1st)
# 2. Apply those settings to target webinars

echo "=========================================="
echo "Fix Registration Fields for Copied Webinars"
echo "=========================================="
echo

# Check if gotowebinar CLI is available
if ! command -v gotowebinar &> /dev/null; then
    echo "Error: gotowebinar CLI is not installed or not in PATH"
    echo "Please build and install the CLI first:"
    echo "  dotnet publish -c Release"
    echo "  dotnet tool install --global --add-source ./nupkg gotowebinar"
    exit 1
fi

# Function to find a good source webinar
find_source_webinar() {
    echo "Step 1: Finding source webinar with proper registration settings..."
    echo "Looking for webinars from May 2025 (before the issue)..."
    echo

    # List webinars from May 2025
    echo "Available source webinars:"
    gotowebinar webinar list --from 2025-05-01 --to 2025-05-31 --format table
    echo

    read -p "Enter the webinar key to use as source (or 'manual' to set fields manually): " SOURCE_KEY

    if [ "$SOURCE_KEY" = "manual" ]; then
        return 1
    fi

    # Verify the source has proper fields
    echo
    echo "Checking registration fields for webinar $SOURCE_KEY..."
    gotowebinar webinar fields get "$SOURCE_KEY"
    echo

    read -p "Use this webinar as source? (y/n): " CONFIRM
    if [ "$CONFIRM" != "y" ]; then
        return 1
    fi

    return 0
}

# Function to find affected webinars
find_affected_webinars() {
    echo
    echo "Step 2: Finding affected webinars (October-December 2025)..."
    echo

    # List recent webinars
    echo "Webinars created recently that may need fixing:"
    gotowebinar webinar list --from 2025-10-01 --to 2025-12-31 --format table
    echo

    read -p "Enter the webinar keys to fix (comma-separated): " TARGET_KEYS
}

# Function to apply registration fields
apply_registration_fields() {
    local source=$1
    local targets=$2

    echo
    echo "Step 3: Applying registration fields..."
    echo "Source: $source"
    echo "Targets: $targets"
    echo

    if [ -n "$source" ]; then
        # Copy fields from source to targets
        gotowebinar webinar fields copy "$source" "$targets"
    else
        # Manually enable lead generation fields
        echo "Enabling standard lead generation fields (jobTitle and organization)..."
        gotowebinar webinar fields enable-leadgen "$targets"
    fi
}

# Main execution
main() {
    # Try to find a source webinar
    if find_source_webinar; then
        # Found a good source
        find_affected_webinars
        apply_registration_fields "$SOURCE_KEY" "$TARGET_KEYS"
    else
        # Manual mode - just enable lead gen fields
        echo
        echo "Manual mode: Will enable jobTitle and organization as required fields"
        find_affected_webinars
        apply_registration_fields "" "$TARGET_KEYS"
    fi

    echo
    echo "=========================================="
    echo "Verification"
    echo "=========================================="
    echo

    # Verify the changes
    IFS=',' read -ra TARGETS <<< "$TARGET_KEYS"
    for target in "${TARGETS[@]}"; do
        target=$(echo "$target" | xargs)  # Trim whitespace
        echo "Registration fields for $target:"
        gotowebinar webinar fields get "$target" | head -10
        echo
    done

    echo "Fix complete! Please verify the registration fields are correct."
    echo
    echo "You can also manually check each webinar with:"
    echo "  gotowebinar webinar fields get <webinar-key>"
}

# Run the script
main