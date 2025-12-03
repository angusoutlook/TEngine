param(
    [string]$FetchUrl = "https://github.com/Alex-Rachel/TEngine.git",
    [string]$PushUrl  = "https://github.com/angusoutlook/TEngine.git"
)

$scriptError = $false

# 如果只传了一个参数，视为错误（必须 0 个或者 2 个参数）
if ($PSBoundParameters.Count -eq 1) {
    Write-Host "Error: you must provide both fetch-urlA and push-urlB, or no arguments to use defaults."
    $scriptError = $true
} else {
    Write-Host "Setting origin fetch URL to: $FetchUrl"
    git remote set-url origin $FetchUrl
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to set fetch URL"
        $scriptError = $true
    }

    Write-Host "Setting origin push URL to: $PushUrl"
    git remote set-url --push origin $PushUrl
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to set push URL"
        $scriptError = $true
    }

    if (-not $scriptError) {
        Write-Host "Current remote configuration:"
        git remote -v
    }
}

Write-Host ""
Write-Host "Usage: .\init-git-remote.ps1 <fetch-urlA> <push-urlB>"
Write-Host "Sample: .\init-git-remote.ps1 https://github.com/user/repo-a.git https://github.com/user/repo-b.git"

if ($PSBoundParameters.Count -eq 0) {
    Write-Host "Note: no arguments provided, using defaults:"
    Write-Host "  fetch-urlA = https://github.com/Alex-Rachel/TEngine.git"
    Write-Host "  push-urlB  = https://github.com/angusoutlook/TEngine.git"
}

if ($scriptError) {
    exit 1
}


