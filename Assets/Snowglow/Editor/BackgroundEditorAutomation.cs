using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Snowglow.Editor
{
    // Project-local transport. Watcher callbacks never call Unity APIs.
    [InitializeOnLoad]
    public static class BackgroundEditorAutomation
    {
        const string StateKey = "Snowglow.BackgroundEditorAutomation.Pending";
        static readonly string Root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static readonly string Folder = Path.Combine(Root, "Logs/EditorAutomation");
        static readonly List<FileSystemWatcher> Watchers = new List<FileSystemWatcher>();
        static long changed;
        static double nextTick;
        static Request pending;
        static string phase;
        static readonly List<string> Errors = new List<string>();
        static string Preference => "Snowglow.BackgroundRefresh." + Hash128.Compute(Root);

        [Serializable] class Request { public string id; public string action; }
        [Serializable] class Status
        {
            public string requestId, state, utc, project, unity, domain, error;
            public int processId;
            public bool playing, compiling, updating, automaticRefresh;
        }
        static readonly string Domain = Guid.NewGuid().ToString("N");

        static BackgroundEditorAutomation()
        {
            Directory.CreateDirectory(Folder);
            var saved = SessionState.GetString(StateKey, "");
            if (!string.IsNullOrEmpty(saved))
            {
                pending = JsonUtility.FromJson<Request>(saved);
                phase = "reloaded";
            }
            foreach (var relative in new[] { "Assets", "Packages", "ProjectSettings" })
            {
                var path = Path.Combine(Root, relative);
                if (!Directory.Exists(path)) continue;
                var watcher = new FileSystemWatcher(path) { IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite };
                watcher.Changed += Changed;
                watcher.Created += Changed;
                watcher.Deleted += Changed;
                watcher.Renamed += Changed;
                watcher.Error += (s, e) => Interlocked.Exchange(ref changed, DateTime.UtcNow.Ticks);
                watcher.EnableRaisingEvents = true;
                Watchers.Add(watcher);
            }
            CompilationPipeline.assemblyCompilationFinished += (path, messages) =>
            {
                foreach (var message in messages)
                    if (message.type == CompilerMessageType.Error) Errors.Add(message.message);
            };
            CompilationPipeline.compilationFinished += context =>
            {
                if (pending != null) phase = Errors.Count > 0 ? "failed" : "awaiting_reload";
            };
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting += Dispose;
            EditorApplication.update += Tick;
        }

        static void Changed(object sender, FileSystemEventArgs args)
        {
            if (args.FullPath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
                args.FullPath.EndsWith("~", StringComparison.Ordinal)) return;
            Interlocked.Exchange(ref changed, DateTime.UtcNow.Ticks);
        }
        static void Dispose()
        {
            foreach (var watcher in Watchers) watcher.Dispose();
            Watchers.Clear();
        }

        [MenuItem("Snowglow/Editor automation/Automatic background refresh")]
        static void Toggle() => EditorPrefs.SetBool(Preference, !EditorPrefs.GetBool(Preference, true));
        [MenuItem("Snowglow/Editor automation/Automatic background refresh", true)]
        static bool ToggleCheck()
        {
            Menu.SetChecked("Snowglow/Editor automation/Automatic background refresh", EditorPrefs.GetBool(Preference, true));
            return true;
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < nextTick) return;
            nextTick = EditorApplication.timeSinceStartup + 0.5;
            try
            {
                var requestPath = Path.Combine(Folder, "request.json");
                if (pending == null && File.Exists(requestPath))
                {
                    pending = JsonUtility.FromJson<Request>(File.ReadAllText(requestPath));
                    File.Delete(requestPath);
                    phase = "requested";
                    Errors.Clear();
                    if (pending == null || string.IsNullOrEmpty(pending.id) ||
                        (pending.action != "refresh" && pending.action != "compile"))
                    { WriteStatus("failed", "Invalid request; allowed actions: refresh, compile."); pending = null; return; }
                    // An automatic import may already be compiling. Preserve the request
                    // before its domain reload, even when the busy gate below defers us.
                    SessionState.SetString(StateKey, JsonUtility.ToJson(pending));
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                { WriteStatus("waiting_for_play_mode_to_stop"); return; }
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                { WriteStatus("busy"); return; }
                if (pending != null)
                {
                    if (phase == "failed" || (phase != "requested" && EditorUtility.scriptCompilationFailed))
                    { Finish("failed", string.Join("\n", Errors)); return; }
                    if (phase == "reloaded") { Finish("complete"); return; }
                    if (phase == "requested")
                    {
                        // Persist before Refresh: import can trigger a domain reload itself.
                        SessionState.SetString(StateKey, JsonUtility.ToJson(pending));
                        phase = "awaiting_reload";
                        Interlocked.Exchange(ref changed, 0);
                        WriteStatus("refreshing");
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                        // Explicit requests always force a compilation/reload acknowledgement.
                        CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
                        return;
                    }
                    WriteStatus(phase);
                    return;
                }
                var stamp = Interlocked.Read(ref changed);
                if (EditorPrefs.GetBool(Preference, true) && stamp != 0 &&
                    DateTime.UtcNow.Ticks - stamp > TimeSpan.TicksPerSecond * 2)
                {
                    Interlocked.Exchange(ref changed, 0);
                    AssetDatabase.Refresh();
                }
                WriteStatus("idle");
            }
            catch (Exception exception)
            { Finish("failed", exception.ToString()); }
        }
        static void Finish(string state, string error = null)
        {
            WriteStatus(state, error);
            if (pending != null)
                File.Copy(Path.Combine(Folder, "status.json"), Path.Combine(Folder, "result.json"), true);
            pending = null;
            phase = null;
            SessionState.EraseString(StateKey);
        }
        static void WriteStatus(string state, string error = null)
        {
            var status = new Status { requestId = pending?.id, state = state,
                utc = DateTime.UtcNow.ToString("O"), project = Root, unity = Application.unityVersion,
                domain = Domain, processId = System.Diagnostics.Process.GetCurrentProcess().Id,
                playing = EditorApplication.isPlayingOrWillChangePlaymode,
                compiling = EditorApplication.isCompiling, updating = EditorApplication.isUpdating,
                automaticRefresh = EditorPrefs.GetBool(Preference, true), error = error };
            var path = Path.Combine(Folder, "status.json");
            File.WriteAllText(path + ".tmp", JsonUtility.ToJson(status, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(path + ".tmp", path);
        }
    }
}
