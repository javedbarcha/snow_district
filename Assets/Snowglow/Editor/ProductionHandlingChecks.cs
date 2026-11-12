using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor
{
    [InitializeOnLoad]
    public static class ProductionHandlingChecks
    {
        const string Folder = "Logs/ProductionHandling";
        static ProductionHandlingChecks() { EditorApplication.update += Pending; }
        static void Pending()
        {
            if (!File.Exists(Folder + ".request") || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(Folder + ".request");
            Directory.CreateDirectory(Folder);
            try { Run(); } catch (Exception error) { File.WriteAllText(Folder + "/Checks.txt", "FAIL: " + error); Debug.LogException(error); }
        }
        public static void Run()
        {
            string key = "Snowglow.CollisionPlay.Active";
            string profilePath = SessionState.GetString(key + ".savePath", "");
            string Hash(string path) { if (!File.Exists(path)) return "absent"; using (var sha = System.Security.Cryptography.SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))); }
            Directory.CreateDirectory("Logs/SuspensionReview");
            File.WriteAllText("Logs/SuspensionReview/Preservation.txt", "Scene before=" + SessionState.GetString(key + ".scene", "missing") + " after=" + Hash(SceneValidation.ScenePath) + "\nProfile before=" + SessionState.GetString(key + ".save", "missing") + " after=" + Hash(profilePath) + "\nBackup before=" + SessionState.GetString(key + ".backup", "missing") + " after=" + Hash(profilePath + ".bak"));
            int count = 0;
            void Require(bool condition, string text) { count++; if (!condition) throw new InvalidOperationException(text); }
            var scene = SceneManager.GetActiveScene(); var sceneBytes = File.ReadAllBytes(scene.path); bool dirty = scene.isDirty;
            var fixture = EditorSceneManager.NewPreviewScene();
            var states = new List<object>();
            var type = typeof(SnowField).Assembly.GetType("Snowglow.SnowSupportSurface", true);
            var update = type.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            var meshField = type.GetField("mesh", BindingFlags.NonPublic | BindingFlags.Instance);
            var clock = type.GetField("updated", BindingFlags.NonPublic | BindingFlags.Instance);
            MeshCollider Surface(SnowVolumeGrid grid, int x, bool immediate, object reuse = null)
            {
                var state = reuse ?? Activator.CreateInstance(type, true);
                if (reuse == null) states.Add(state);
                var go = new GameObject("Support fixture"); SceneManager.MoveGameObjectToScene(go, fixture);
                var collider = go.AddComponent<MeshCollider>();
                update.Invoke(state, new object[] { grid, x, 8, collider, immediate });
                return collider;
            }
            try
            {
                var grid = new SnowVolumeGrid();
                for (int i = 0; i < grid.depth.Length; i++) grid.depth[i] = 2.4f;
                string ledger = JsonUtility.ToJson(grid);
                var left = Surface(grid, 6, true); var right = Surface(grid, 7, true);
                Require(left.enabled && right.enabled, "Deep snow must retain physical support.");
                var a = left.sharedMesh.vertices; var b = right.sharedMesh.vertices;
                Require(a.Length == 289 && left.sharedMesh.triangles.Length == 1536, "Bounded support mesh geometry.");
                for (int z = 0; z < 17; z++) Require(Vector3.Distance(a[z * 17 + 16], b[z * 17]) < .00001f, "Support chunk seam mismatch.");
                Physics.SyncTransforms();
                Require(fixture.GetPhysicsScene().Raycast(new Vector3(-24, 15, 8), Vector3.down, out var hit, 20), "Support top must be physically raycastable.");
                Require(hit.normal.y > .99f && hit.point.y > SnowField.Ground + 1, "Support winding/height incorrect.");
                Require(JsonUtility.ToJson(grid) == ledger, "Collision support changed snow volume/credit data.");

                Array.Clear(grid.depth, 0, grid.depth.Length);
                for (int i = 0; i < grid.depth.Length; i++) if (i % SnowVolumeGrid.Size >= 208) grid.depth[i] = 2.4f;
                var step = Surface(grid, 6, true).sharedMesh.vertices;
                float biggest = 0;
                for (int z = 0; z < 17; z++) for (int x = 0; x < 16; x++) biggest = Mathf.Max(biggest, Mathf.Abs(step[z * 17 + x + 1].y - step[z * 17 + x].y));
                Require(biggest > .1f && biggest < 1.2f, "A vertical volume step must become a graded support surface with raised half-gap support.");

                Array.Clear(grid.depth, 0, grid.depth.Length);
                var rising = Activator.CreateInstance(type, true); states.Add(rising);
                Surface(grid, 6, true, rising);
                for (int i = 0; i < grid.depth.Length; i++) grid.depth[i] = 2.4f;
                clock.SetValue(rising, Time.time - 1);
                var growth = Surface(grid, 6, false, rising);
                float rise = growth.sharedMesh.vertices.Max(v => v.y - SnowField.Ground);
                Require(rise > .001f && rise <= .0351f, "New support must not pop into vehicles, even after a delayed update.");

                var session = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PrototypeSession>(true)).Single();
                foreach (var source in session.Fleet)
                {
                    var clone = UnityEngine.Object.Instantiate(source.gameObject); SceneManager.MoveGameObjectToScene(clone, fixture);
                    var machine = clone.GetComponent<MachineController>(); machine.Initialize();
                    Require(machine.Body.maxDepenetrationVelocity == 2 && machine.Body.maxAngularVelocity == 3.5f, "Vehicle impact limits not initialized.");
                    Require(machine.wheels.All(w => !w.suspensionExpansionLimited) && Mathf.Approximately(machine.Body.angularDamping, .5f), "Normal driving must retain freer suspension.");
                    var contact = typeof(MachineController).GetMethod("IsWorkingAgainstSnow", BindingFlags.NonPublic | BindingFlags.Instance);
                    var snowObject = new GameObject("Snow contact fixture"); SceneManager.MoveGameObjectToScene(snowObject, fixture);
                    var snow = snowObject.AddComponent<SnowField>();
                    machine.Tool.ResetRaised();
                    Require(!(bool)contact.Invoke(machine, new object[] { snow }), "Raised tool must not enable work stabilization.");
                    machine.Tool.ResetLowered();
                    Require(!(bool)contact.Invoke(machine, new object[] { snow }), "Lowered tool on clear road must retain freer suspension.");
                    machine.Tool.mouth.position = new Vector3(0, SnowField.Ground + .1f, 0);
                    for (int i = 0; i < snow.Grid.depth.Length; i++) snow.Grid.depth[i] = .3f;
                    Require((bool)contact.Invoke(machine, new object[] { snow }), "Lowered tool touching snow must stabilize.");
                    machine.Tool.ResetRaised();
                    Require(!(bool)contact.Invoke(machine, new object[] { snow }), "Raised tool on flat snow must retain freer suspension.");
                    UnityEngine.Object.DestroyImmediate(snowObject);
                    UnityEngine.Object.DestroyImmediate(clone);
                }
            }
            finally
            {
                foreach (var state in states) { var mesh = (Mesh)meshField.GetValue(state); if (mesh) UnityEngine.Object.DestroyImmediate(mesh); }
                EditorSceneManager.ClosePreviewScene(fixture); SceneManager.SetActiveScene(scene);
            }
            SceneValidation.Validate(scene); MechanicsChecks.Run();
            RecoveryPhysicsChecks.Run(); DirectionBufferChecks.Run(); LoaderCycleChecks.Run();
            Require(scene.isDirty == dirty && sceneBytes.SequenceEqual(File.ReadAllBytes(scene.path)), "Saved scene/dirty state changed.");
            File.WriteAllText(Folder + "/Checks.txt", "PASS: " + count + " support/vehicle assertions, physical support raycast, seam continuity, graded steps, bounded delayed growth, unchanged volume/credit data; scene validation, 19 snow checks and recovery/direction-buffer/loader-cycle regressions passed. No scene or profile writes. Actual Play acceptance recorded separately.");
        }
    }
}
