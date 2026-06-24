function UpdateVersionAndReleaseNotes {
    param (
        [Parameter(Mandatory=$true)]
        [PSCustomObject]$ReleaseNotesResult,

        [Parameter(Mandatory=$true)]
        [string]$XmlFilePath
    )

    # Refuse to write empty values — this is what previously corrupted the file when a
    # plain string (no .Version / .ReleaseNotes members) was passed in.
    if ([string]::IsNullOrWhiteSpace($ReleaseNotesResult.Version)) {
        throw "Refusing to update '$XmlFilePath': release version is empty."
    }
    if ([string]::IsNullOrWhiteSpace($ReleaseNotesResult.ReleaseNotes)) {
        throw "Refusing to update '$XmlFilePath': release notes are empty."
    }

    # Load XML
    $xmlContent = New-Object XML
    $xmlContent.Load($XmlFilePath)

    # Update VersionPrefix and PackageReleaseNotes
    $versionPrefixElement = $xmlContent.SelectSingleNode("//VersionPrefix")
    if ($null -eq $versionPrefixElement) {
        throw "VersionPrefix element not found in '$XmlFilePath'."
    }
    $versionPrefixElement.InnerText = $ReleaseNotesResult.Version

    $packageReleaseNotesElement = $xmlContent.SelectSingleNode("//PackageReleaseNotes")
    if ($null -eq $packageReleaseNotesElement) {
        throw "PackageReleaseNotes element not found in '$XmlFilePath'."
    }
    $packageReleaseNotesElement.InnerText = $ReleaseNotesResult.ReleaseNotes

    # Save the updated XML
    $xmlContent.Save($XmlFilePath)
}

# Usage example:
# $notes = Get-ReleaseNotes -MarkdownFile "$PSScriptRoot\RELEASE_NOTES.md"
# $propsPath = Join-Path -Path (Get-Item $PSScriptRoot).Parent.FullName -ChildPath "Directory.Build.props"
# UpdateVersionAndReleaseNotes -ReleaseNotesResult $notes -XmlFilePath $propsPath
