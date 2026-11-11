param(
    [ValidateSet('Status', 'Refresh', 'Compile')][string]$Action = 'Status',
    [ValidateRange(1, 900)][int]$TimeoutSeconds = 600
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$automationFolder = Join-Path $projectRoot 'Logs/EditorAutomation'
[IO.Directory]::CreateDirectory($automationFolder) | Out-Null
$statusPath = Join-Path $automationFolder 'status.json'
function Read-JsonFile($path) {
    if (Test-Path -LiteralPath $path) {
        try { return (Get-Content -LiteralPath $path -Raw | ConvertFrom-Json) } catch { return $null }
    }
    return $null
}
if ($Action -eq 'Status') {
    $status = Read-JsonFile $statusPath
    if (!$status) { throw 'No Editor heartbeat. Open this project in Unity and import the bridge once.' }
    $status | ConvertTo-Json -Depth 5
    if (([DateTime]::UtcNow - [DateTime]::Parse($status.utc).ToUniversalTime()).TotalSeconds -gt 15) {
        throw 'Editor heartbeat is stale; Unity may be reloading, blocked, closed or frozen.'
    }
    exit 0
}
$lockPath = Join-Path $automationFolder 'client.lock'
$clientLock = $null
try {
    $clientLock = [IO.File]::Open($lockPath, 'OpenOrCreate', 'ReadWrite', 'None')
    $requestPath = Join-Path $automationFolder 'request.json'
    $status = Read-JsonFile $statusPath
    if ((Test-Path -LiteralPath $requestPath) -or ($status.requestId -and $status.state -notin @('idle', 'complete', 'failed'))) {
        throw 'An Editor request is already pending. Inspect Status before retrying.'
    }
    $requestId = [Guid]::NewGuid().ToString('N')
    $request = @{ id = $requestId; action = $Action.ToLowerInvariant() } | ConvertTo-Json
    [IO.File]::WriteAllText($requestPath + '.tmp', $request)
    Move-Item -LiteralPath ($requestPath + '.tmp') -Destination $requestPath
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $result = Read-JsonFile (Join-Path $automationFolder 'result.json')
        if ($result.requestId -eq $requestId) {
            $result | ConvertTo-Json -Depth 5
            if ($result.state -ne 'complete') { throw 'Unity compilation failed. See result.json and Editor.log.' }
            exit 0
        }
        Start-Sleep -Milliseconds 500
    }
    throw 'Timed out waiting for Unity. Request remains queued; inspect Status. Do not queue scene authoring until completion.'
} finally {
    if ($clientLock) { $clientLock.Dispose() }
}
