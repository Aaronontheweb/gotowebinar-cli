param (
    [Parameter(Mandatory=$false)]
    [string]$Version
)

function Get-ReleaseNotes {
    param (
        [Parameter(Mandatory=$true)]
        [string]$MarkdownFile,
        [string]$TargetVersion = $null
    )

    # Read markdown file content
    $content = Get-Content -Path $MarkdownFile -Raw

    # Split content based on headers
    $sections = $content -split "####"

    # If no target version specified, get the first release
    if ([string]::IsNullOrEmpty($TargetVersion)) {
        if ($sections.Count -ge 2) {
            $header = $sections[1].Trim()
            
            # Find the next header or use rest of content
            $endIndex = $sections[1].IndexOf("`n---")
            if ($endIndex -gt 0) {
                $releaseContent = $sections[1].Substring(0, $endIndex).Trim()
            } else {
                $releaseContent = $sections[1].Trim()
            }
            
            # Remove the version line and return just the notes
            $lines = $releaseContent -split "`n"
            if ($lines.Count -gt 1) {
                return ($lines[1..($lines.Count-1)] -join "`n").Trim()
            }
        }
        return ""
    }

    # Search for specific version
    foreach ($section in $sections) {
        if ($section.Trim() -match "^$([regex]::Escape($TargetVersion))\s") {
            # Found the target version
            $lines = $section.Trim() -split "`n"
            
            # Skip the version line and any following empty lines
            $noteLines = @()
            $foundContent = $false
            
            for ($i = 1; $i -lt $lines.Count; $i++) {
                $line = $lines[$i]
                
                # Stop at the next section delimiter
                if ($line -match "^---") {
                    break
                }
                
                # Start collecting after finding non-empty content
                if (-not [string]::IsNullOrWhiteSpace($line)) {
                    $foundContent = $true
                }
                
                if ($foundContent -or -not [string]::IsNullOrWhiteSpace($line)) {
                    $noteLines += $line
                }
            }
            
            return ($noteLines -join "`n").Trim()
        }
    }

    # Version not found, return empty
    return ""
}

# Script entry point
$releaseNotesPath = Join-Path $PSScriptRoot ".." "RELEASE_NOTES.md"

if (Test-Path $releaseNotesPath) {
    $notes = Get-ReleaseNotes -MarkdownFile $releaseNotesPath -TargetVersion $Version
    Write-Output $notes
} else {
    Write-Error "RELEASE_NOTES.md not found at $releaseNotesPath"
    exit 1
}