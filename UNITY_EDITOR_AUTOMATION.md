# Background Editor refresh and compilation

The Editor-only bridge watches Assets, Packages and ProjectSettings. After two
quiet seconds it asks the running Editor to refresh, even without a focus change.
It defers while playing, compiling or importing. No scene save, Play entry,
player-save write, build, or project rebake is performed by this bridge.

## Set up this or another PC

1. Copy/clone the repository, including the bridge script and its .meta file.
2. Open it with the version in ProjectSettings/ProjectVersion.txt. On first
   installation only, stop Play and choose Assets > Refresh if Unity has not
   imported the bridge. It cannot import itself before its first compilation.
3. Leave Unity open. Automatic refresh defaults on. Toggle it through Snowglow >
   Editor automation > Automatic background refresh. The preference is per PC
   and project path; no undocumented Unity preference keys are modified.
4. From a terminal in the project:

```powershell
powershell -NoProfile -File Tools/EditorAutomation/Invoke-UnityEditor.ps1 -Action Status
powershell -NoProfile -File Tools/EditorAutomation/Invoke-UnityEditor.ps1 -Action Compile
```

Refresh and Compile both explicitly refresh and force script recompilation so
success acknowledges a domain reload, even when no source changed. This costs
more than the automatic watcher, which refreshes only changed files. Status is
read-only. Paths resolve relative to the helper, so no username or Unity install
path needs editing. This helper requires PowerShell; Unity's bridge is Editor-only.

## Agent workflow

After source edits, run Compile and await exit 0 **before** creating any existing
authoring/check `.request` marker. This prevents queued scene work from using
old loaded assemblies. Then wait for that task's own validation evidence;
compilation alone does not prove an authoring operation ran or saved successfully.
Never call this helper from a menu callback that blocks Unity's main thread.

The ignored Logs/EditorAutomation directory contains a heartbeat, queued request
and the latest durable result. IDs prevent accepting a previous request's result;
the process ID and domain token identify the running Editor/reloaded assembly.
The helper serializes clients and refuses to overwrite an active request. A
timeout leaves its request pending, since it may still be running. Inspect Status
and Editor.log rather than repeatedly queuing work or deleting active requests.
Play mode is not stopped automatically: requests report that they are waiting.

Minimized/background execution still depends on Unity servicing Editor update
callbacks. This cannot unfreeze Unity, dismiss blocking dialogs, overcome OS
suspension, or execute after the Editor closes. A stale heartbeat or timeout is
not success. Do not launch a second Editor on the same project, kill Unity, or
delete Library as an automatic fallback. Ask for manual recovery when necessary.

## Acceptance

After the initial import, run Compile with Unity behind another window and again
while minimized. Each must return complete with a new domain token and a fresh
heartbeat. Repeat after editing an Editor script. Confirm Play requests defer,
then complete after stopping Play, and compiler errors return failed. Actual
background/minimized acceptance must be recorded separately from offline C# checks.

11 November 2026: following the user's confirmation that Unity was minimized
with Play stopped, the external Compile request completed at 15:11:59 UTC.
Unity 6000.3.8f1 on the Android target returned a matching request result, changed
its domain token and resumed the heartbeat in the same Editor process. No further
manual refresh was requested. The forced rebuild took approximately four minutes.
The minimized state was user-confirmed; native desktop inspection was unavailable.
The default client timeout is ten minutes (override with -TimeoutSeconds).
