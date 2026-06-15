# Deploy Unity Cloud Build WebGL output to a GitHub Pages repo.
# For local/manual Windows use only. Unity Cloud Build must use deploy-ghpages.sh
# (UBA runs bash on all builders, including Windows).
# Expects env vars: GITHUB_TOKEN, GITHUB_USER, GITHUB_REPO, GITHUB_EMAIL
# Optional: USER or USERNAME, UCB_BUILD_NUMBER

$ErrorActionPreference = "Stop"

Write-Host "====================DEPLOYMENT_TO_GITHUB_PAGES_START============================="

$requiredVars = @("GITHUB_TOKEN", "GITHUB_USER", "GITHUB_REPO", "GITHUB_EMAIL")
foreach ($name in $requiredVars) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
        Write-Error "Required environment variable is not set: $name"
        exit 1
    }
}

$gitAuthorName = if (-not [string]::IsNullOrWhiteSpace($env:USER)) { $env:USER }
                 elseif (-not [string]::IsNullOrWhiteSpace($env:USERNAME)) { $env:USERNAME }
                 else { "unity-cloud-build" }
$buildNumber = if (-not [string]::IsNullOrWhiteSpace($env:UCB_BUILD_NUMBER)) { $env:UCB_BUILD_NUMBER }
                 elseif (-not [string]::IsNullOrWhiteSpace($env:GITHUB_RUN_NUMBER)) { $env:GITHUB_RUN_NUMBER }
                 else { "unknown" }
$commitMessage = if (-not [string]::IsNullOrWhiteSpace($env:DEPLOY_COMMIT_MESSAGE)) { $env:DEPLOY_COMMIT_MESSAGE }
                  else { "CI build $buildNumber" }

function Resolve-BuildFolder {
    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_PLAYER_PATH)) {
        if (Test-Path $env:UNITY_PLAYER_PATH -PathType Container) {
            return (Resolve-Path $env:UNITY_PLAYER_PATH).Path
        }
        if (Test-Path $env:UNITY_PLAYER_PATH -PathType Leaf) {
            return (Resolve-Path (Split-Path $env:UNITY_PLAYER_PATH -Parent)).Path
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($env:OUTPUT_DIRECTORY) -and (Test-Path $env:OUTPUT_DIRECTORY)) {
        return (Resolve-Path $env:OUTPUT_DIRECTORY).Path
    }

    return Get-ChildItem -Path . -Directory |
        Where-Object { $_.Name -match '^temp' } |
        ForEach-Object {
            Get-ChildItem -Path $_.FullName -Directory |
                Where-Object { $_.Name -match '^default-webgl' } |
                Select-Object -First 1 -ExpandProperty FullName
        } |
        Select-Object -First 1
}

$buildfolder = Resolve-BuildFolder
if (-not $buildfolder) {
    Write-Error "Could not find build folder (checked UNITY_PLAYER_PATH, OUTPUT_DIRECTORY, ./temp*/default-webgl*)"
    exit 1
}

Write-Host "Build folder: $buildfolder"

$tmpDir = Join-Path (Get-Location) "tmp"
if (-not (Test-Path $tmpDir)) {
    $cloneUrl = "https://$($env:GITHUB_TOKEN)@github.com/$($env:GITHUB_USER)/$($env:GITHUB_REPO)"
    Write-Host "Cloning https://***@github.com/$($env:GITHUB_USER)/$($env:GITHUB_REPO) -> tmp"
    git clone $cloneUrl $tmpDir
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "Copying build output into tmp..."
Copy-Item -Path (Join-Path $buildfolder "*") -Destination $tmpDir -Recurse -Force

Push-Location $tmpDir
try {
    Get-ChildItem

    git config --global user.email $env:GITHUB_EMAIL
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    git config --global user.name $gitAuthorName
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    git add Build
    git add StreamingAssets/aa/catalog.json
    git add StreamingAssets/aa/settings.json
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $status = git diff --cached --quiet; $diffExit = $LASTEXITCODE
    if ($diffExit -eq 0) {
        Write-Host "No changes to commit; skipping push."
        exit 0
    }

    git commit -m $commitMessage
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    git log -1
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    git push --force
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

Write-Host "====================DEPLOYMENT_TO_GITHUB_PAGES_END============================="
