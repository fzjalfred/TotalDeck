param([string]$Project)

$ErrorActionPreference = 'Stop'
$tuanjie = 'C:\Program Files\Tuanjie\Hub\Editor\2022.3.62t14\Editor\Tuanjie.exe'
$pkg     = 'TotalDeck_v0.1'
$zip     = Join-Path $Project 'Builds\TotalDeck_Windows.zip'
$out     = Join-Path $Project 'Builds\Windows'
$bs      = Join-Path $Project 'Assets\Editor\BuildOnLoad.cs'
$log     = Join-Path $Project 'Logs\build_windows.log'

if (-not (Test-Path $tuanjie)) {
    Write-Host "[ERROR] Tuanjie editor not found: $tuanjie"
    Read-Host 'Press Enter to exit'
    exit 1
}

# ── 1. Generate the build bootstrap C# ──────────────────────
Write-Host '[1/3] Generating build bootstrap script...'
$cs = @'
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TotalDeck.EditorTools
{
    [InitializeOnLoad]
    public static class BuildOnLoad
    {
        static BuildOnLoad() { EditorApplication.delayCall += Run; }
        static void Run()
        {
            try
            {
                BuildGame.BuildWindows();
                string root = Directory.GetParent(Application.dataPath).FullName;
                string outDir = Path.Combine(root, "Builds", "Windows");
                string pkg = "PKGNAME";
                string zipPath = Path.Combine(root, "Builds", "TotalDeck_Windows.zip");
                string stage = Path.Combine(root, "Builds", pkg);
                if (Directory.Exists(stage)) Directory.Delete(stage, true);
                // Keep the build's internal structure under the top-level folder
                string inner = Path.Combine(stage, pkg);
                Directory.CreateDirectory(inner);
                CopyDir(outDir, inner);
                if (File.Exists(zipPath)) File.Delete(zipPath);
                System.IO.Compression.ZipFile.CreateFromDirectory(stage, zipPath);
                Directory.Delete(stage, true);
                Debug.Log("[BuildOnLoad] zip written: " + zipPath);
            }
            catch (System.Exception e) { Debug.LogError("[BuildOnLoad] " + e); }
            finally
            {
                EditorApplication.delayCall += () =>
                {
                    if (File.Exists("Assets/Editor/BuildOnLoad.cs"))
                    {
                        File.Delete("Assets/Editor/BuildOnLoad.cs");
                        if (File.Exists("Assets/Editor/BuildOnLoad.cs.meta")) File.Delete("Assets/Editor/BuildOnLoad.cs.meta");
                        AssetDatabase.Refresh();
                    }
                };
            }
        }
        static void CopyDir(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src)) File.Copy(f, Path.Combine(dst, Path.GetFileName(f)));
            foreach (var d in Directory.GetDirectories(src)) CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
        }
    }
}
'@
$cs = $cs -replace 'PKGNAME', $pkg
[System.IO.File]::WriteAllText($bs, $cs)
Write-Host "      wrote $bs"

# ── 2. Trigger the build inside the open editor ─────────────
Write-Host '[2/3] Building inside the open editor (synchronous wait, progress below)...'
if (Test-Path $zip) { Remove-Item $zip -Force }  # ensure we wait for a FRESH zip
$sw = [System.Diagnostics.Stopwatch]::StartNew()
& $tuanjie -projectPath $Project -logFile $log | Out-Null

$spin = '|/-\'
$i = 0
$timeoutSec = 600
while (-not (Test-Path $zip)) {
    if ($sw.Elapsed.TotalSeconds -gt $timeoutSec) {
        Write-Host ''
        Write-Host "[ERROR] Build timed out after ${timeoutSec}s - no zip produced."
        Write-Host "        Check $log and the editor Console."
        if (Test-Path $bs) { Remove-Item $bs -Force -ErrorAction SilentlyContinue }
        if (Test-Path "$bs.meta") { Remove-Item "$bs.meta" -Force -ErrorAction SilentlyContinue }
        Read-Host 'Press Enter to exit'
        exit 1
    }
    $c = $spin[$i % $spin.Length]
    $elapsed = [int]$sw.Elapsed.TotalSeconds
    Write-Host -NoNewline "`r      $c building... ${elapsed}s elapsed"
    Start-Sleep -Milliseconds 300
    $i++
}
Write-Host ''
Write-Host "      zip ready: $zip"

# ── 3. Done ─────────────────────────────────────────────────
$sw.Stop()
$sizeMB = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host "[3/3] Done in $([int]$sw.Elapsed.TotalSeconds)s ($sizeMB MB)."
Write-Host "      Send $zip to the target Windows PC, extract anywhere, run TotalDeck.exe."
Read-Host 'Press Enter to exit'
exit 0
