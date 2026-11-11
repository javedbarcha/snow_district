using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor
{
    // Opt-in diagnostic only. Never invoked by ordinary gameplay or authoring.
    [InitializeOnLoad]
    public static class CollisionPlayReproduction
    {
        static bool Audit => SessionState.GetBool(Key + ".audit", false);
        static string Folder => Audit ? "Logs/CollisionAudit" : "Logs/CollisionPlay";
        const string Key = "Snowglow.CollisionPlay.Active";
        const string Request = "Logs/CollisionPlay.request";
        static PrototypeSession game;
        static Collider[] targets;
        static readonly List<string> Results = new List<string>();
        static MachineController machine;
        static CollisionPlayProbe probe;
        static int trial = -1;
        static float start;
        static string saveHash, backupHash, sceneHash;
        static bool finished;
        static readonly FieldInfo Ready = typeof(PrototypeSession).GetField("ready", BindingFlags.Instance | BindingFlags.NonPublic);

        static CollisionPlayReproduction()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Key, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode) Setup();
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    var path = SessionState.GetString(Key + ".savePath", "");
                    var preserved = Hash(path) == SessionState.GetString(Key + ".save", "") &&
                        Hash(path + ".bak") == SessionState.GetString(Key + ".backup", "") &&
                        Hash(SceneValidation.ScenePath) == SessionState.GetString(Key + ".scene", "");
                    File.AppendAllText(Folder + "/Summary.txt", "\nExited Play. Scene/profile/backup bytes preserved: " + preserved + "\n");
                    SessionState.SetBool(Key, false);
                }
            };
        }

        static string Hash(string path)
        {
            if (!File.Exists(path)) return "absent";
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
        }
        static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && (File.Exists(Request) || File.Exists("Logs/CollisionAudit.request")) && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
                {
                    bool audit = File.Exists("Logs/CollisionAudit.request");
                    File.Delete(audit ? "Logs/CollisionAudit.request" : Request);
                    SessionState.SetBool(Key + ".audit", audit);
                    if (SceneManager.GetActiveScene().path != SceneValidation.ScenePath || SceneManager.GetActiveScene().isDirty)
                        throw new InvalidOperationException("Open a clean saved Pinefall scene before the reproduction; no automatic scene save.");
                    Directory.CreateDirectory(Folder);
                    saveHash = Hash(SaveStore.PathName); backupHash = Hash(SaveStore.PathName + ".bak"); sceneHash = Hash(SceneValidation.ScenePath);
                    SessionState.SetString(Key + ".savePath", SaveStore.PathName);
                    SessionState.SetString(Key + ".save", saveHash); SessionState.SetString(Key + ".backup", backupHash); SessionState.SetString(Key + ".scene", sceneHash);
                    SessionState.SetBool(Key, true);
                    File.WriteAllText(Folder + "/Summary.txt", "Actual Play-mode repeated collision diagnostic. No handling edits.\n");
                    EditorApplication.isPlaying = true;
                }
                if (!EditorApplication.isPlaying || !game || finished) return;
                if (Audit) { CollisionAuditSuite.Tick(); return; }
                if (Time.time - start > 9) Next();
                if (machine && probe) game.Input.Throttle = Time.time - start > 1.5f ? 1 : 0;
            }
            catch (Exception error) { Fail(error); }
        }

        static void Setup()
        {
            try
            {
                game = PrototypeSession.Instance;
                if (!game) throw new InvalidOperationException("No live PrototypeSession.");
                // EnteredPlayMode precedes normal frame updates: prevent all periodic/menu/quit saves.
                Ready.SetValue(game, false);
                game.enabled = false;
                game.Input.Clear(); game.Input.enabled = false;
                game.Profile = new ProfileSave { inJob = true, freeWork = true, assists = true };
                game.Panel = "";
                game.HUD.RefreshMenu();
                Time.timeScale = 1;
                Application.runInBackground = true;
                foreach (var vehicle in game.Fleet) { vehicle.Assistance = true; vehicle.Condition = 100; vehicle.Fuel = vehicle.definition.fuelCapacity; vehicle.SetActiveMachine(false); }
                if (Audit) { CollisionAuditSuite.Start(game, () => { finished = true; EditorApplication.isPlaying = false; }); return; }
                var boxes = UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None)
                    .Where(c => c.enabled && !c.isTrigger && !c.attachedRigidbody && c.bounds.center.y < 15 && c.bounds.size.y > .4f && c.bounds.size.y < 15)
                    .Where(c => Mathf.Abs(c.bounds.center.x) < 120 && Mathf.Abs(c.bounds.center.z) < 120).ToArray();
                string PathOf(Transform transform) => transform.parent ? PathOf(transform.parent) + "/" + transform.name : transform.name;
                var fence = boxes.Where(c => PathOf(c.transform).ToLowerInvariant().Contains("fence")).OrderBy(c => c.bounds.center.sqrMagnitude).FirstOrDefault();
                var home = boxes.Where(c => PathOf(c.transform).ToLowerInvariant().Contains("foundation")).OrderBy(c => c.bounds.center.sqrMagnitude).FirstOrDefault();
                if (!fence || !home) throw new InvalidOperationException("Could not identify authored fence and home foundation colliders.");
                targets = new Collider[] { fence, home };
                Results.Clear(); trial = -1; finished = false;
                Next();
            }
            catch (Exception error) { Fail(error); }
        }
        static void Next()
        {
            if (trial >= 0 && probe)
            {
                Results.Add(probe.Report());
                File.WriteAllLines(Folder + "/Trials.txt", Results);
                UnityEngine.Object.DestroyImmediate(probe);
                machine.SetActiveMachine(false);
                machine.RecoverToDepot();
            }
            trial++;
            if (trial >= game.Fleet.Length * targets.Length * 3)
            {
                File.AppendAllText(Folder + "/Summary.txt", "Completed " + trial + " actual Play-mode trials, 9 simulated seconds each.\n" + string.Join("\n", Results));
                finished = true; EditorApplication.isPlaying = false; return;
            }
            int vehicle = trial / 6, target = trial / 3 % 2, repeat = trial % 3;
            game.Profile.active = vehicle;
            machine = game.Fleet[vehicle];
            var obstacle = targets[target]; var bounds = obstacle.bounds;
            var outward = bounds.size.x < bounds.size.z ? Vector3.right : Vector3.forward;
            if (Vector3.Dot(bounds.center, outward) > 0) outward = -outward;
            var tangent = Vector3.Cross(Vector3.up, outward);
            var position = bounds.center + outward * (Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(outward.x), 0, Mathf.Abs(outward.z))) + 9);
            position += tangent * (target == 0 ? -.65f + repeat * .08f : (repeat - 1) * .08f);
            if (!Physics.Raycast(position + Vector3.up * 40, Vector3.down, out var ground, 100, 1 << 0, QueryTriggerInteraction.Ignore))
                throw new InvalidOperationException("No ground at approach position.");
            position.y = ground.point.y + .08f;
            machine.SetActiveMachine(true);
            machine.Body.position = position; machine.Body.rotation = Quaternion.LookRotation(-outward);
            machine.ResetMotion();
            machine.Tool.Load = 0;
            if (repeat == 2) machine.Tool.ResetRaised(); else machine.Tool.ResetLowered();
            game.Input.Clear();
            Physics.SyncTransforms();
            probe = machine.gameObject.AddComponent<CollisionPlayProbe>();
            probe.Capture = path => ScreenCapture.CaptureScreenshot(path);
            probe.Begin(machine, obstacle, Folder, trial, repeat, position.y);
            game.CameraRig.ResetView(); start = Time.time;
        }
        static void Fail(Exception error)
        {
            if (Audit) CollisionAuditSuite.Cleanup();
            Directory.CreateDirectory(Folder);
            File.AppendAllText(Folder + "/Summary.txt", "\nFAILED: " + error);
            Debug.LogException(error); finished = true;
            if (game) Ready.SetValue(game, false);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            else SessionState.SetBool(Key, false);
        }
    }

}
