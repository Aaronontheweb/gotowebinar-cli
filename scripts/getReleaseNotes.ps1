function Get-ReleaseNotes {
    <#
    .SYNOPSIS
        Parses RELEASE_NOTES.md and returns the version and notes for a release entry.
    .DESCRIPTION
        Release entries are delimited by headers of the form

            #### <version> <date> ####

        and separated by a '---' horizontal rule. Returns a [PSCustomObject] with
        Version and ReleaseNotes properties. With no -TargetVersion the latest (top)
        entry is returned. Markdown links and inline code are flattened to plain text
        so the result is suitable for <PackageReleaseNotes>.
    #>
    param (
        [Parameter(Mandatory=$true)]
        [string]$MarkdownFile,

        [string]$TargetVersion = $null
    )

    $lines = Get-Content -Path $MarkdownFile

    # Parse every release entry into { Version; Lines }.
    $entries = @()
    $current = $null
    foreach ($line in $lines) {
        if ($line -match '^####\s+(\S+)') {
            if ($null -ne $current) { $entries += $current }
            $current = [PSCustomObject]@{ Version = $Matches[1]; Lines = @() }
        }
        elseif ($null -ne $current) {
            if ($line -match '^-{3,}\s*$') {
                # Horizontal rule terminates the current entry's notes.
                $entries += $current
                $current = $null
            }
            else {
                $current.Lines += $line
            }
        }
    }
    if ($null -ne $current) { $entries += $current }

    if ($entries.Count -eq 0) {
        throw "No release entries found in $MarkdownFile"
    }

    if ([string]::IsNullOrEmpty($TargetVersion)) {
        $entry = $entries[0]
    }
    else {
        $entry = $entries | Where-Object { $_.Version -eq $TargetVersion } | Select-Object -First 1
    }

    if ($null -eq $entry) {
        throw "Release notes for version '$TargetVersion' not found in $MarkdownFile"
    }

    # Flatten to plain text: [text](url) -> text, and drop inline-code backticks.
    $notes = ($entry.Lines -join "`n").Trim()
    $notes = [regex]::Replace($notes, '\[([^\]]+)\]\([^)]+\)', '$1')
    $notes = $notes -replace '`', ''

    return [PSCustomObject]@{
        Version      = $entry.Version
        ReleaseNotes = $notes
    }
}
