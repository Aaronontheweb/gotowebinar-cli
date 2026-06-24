param (
    # Optional: release version to apply. When omitted, the latest (top) entry in
    # RELEASE_NOTES.md is used.
    [Parameter(Mandatory=$false)]
    [string]$Version
)

. (Join-Path $PSScriptRoot "scripts" "getReleaseNotes.ps1")
. (Join-Path $PSScriptRoot "scripts" "bumpVersion.ps1")

######################################################################
# Grab release notes and update solution metadata
######################################################################
$releaseNotes = Get-ReleaseNotes `
    -MarkdownFile (Join-Path $PSScriptRoot "RELEASE_NOTES.md") `
    -TargetVersion $Version

# Inject version + notes into Directory.Build.props
UpdateVersionAndReleaseNotes `
    -ReleaseNotesResult $releaseNotes `
    -XmlFilePath (Join-Path $PSScriptRoot "Directory.Build.props")

Write-Output "Set VersionPrefix to $($releaseNotes.Version) and updated PackageReleaseNotes in Directory.Build.props"
