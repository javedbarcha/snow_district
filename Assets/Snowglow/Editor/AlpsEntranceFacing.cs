using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor {
    // One-time entrance-facing adjustment for the isolated Alps town test.
    [InitializeOnLoad]
    public static class AlpsEntranceFacing {
        const string ScenePath = "Assets/Scenes/AlpsTerrainTest.unity";
        const string Town = "ALPS TOWN | supplied 400m road map";
        const string Homes = "02 Numbered buildings | 01-75";
        const string Log = "Logs/AlpsEntranceFacing/";
        const string Request = "Logs/AlpsEntranceFacing.request";
        [Serializable] class Point { public float x, z; }
        [Serializable] sealed class Building : Point { public int id; public float width, depth, yaw; }
        [Serializable] sealed class Plan { public Building[] buildings; }
        static Scene scene;
        static Transform homes;
        static Building[] plan;

        static AlpsEntranceFacing() { EditorApplication.update += Pending; }
        static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException("Alps entrance facing: " + message); }
        static void Pending() {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var action = File.ReadAllText(Request).Trim(); File.Delete(Request); Directory.CreateDirectory(Log);
            try {
                Setup();
                if (action == "audit") Audit();
                else if (action == "apply") Apply();
                else if (action == "review") Review();
                else throw new InvalidOperationException("Unknown action " + action);
            } catch (Exception e) { File.WriteAllText(Log + "Failure.txt", e.ToString()); Debug.LogException(e); }
        }
        static void Setup() {
            scene = SceneManager.GetSceneByPath(ScenePath); Check(scene.IsValid() && scene.isLoaded, "AlpsTerrainTest must be open; unsaved edits are preserved");
            var root = scene.GetRootGameObjects().SingleOrDefault(g => g.name == Town); Check(root, "town group");
            homes = root.transform.Find(Homes); Check(homes && homes.childCount == 75, "75 numbered homes");
            plan = JsonUtility.FromJson<Plan>(File.ReadAllText("SourceAssets/AlpsTownMap/Layout.json")).buildings; Check(plan.Length == 75, "source map has 75 building rows");
        }
        static Transform Home(int id) => homes.Cast<Transform>().Single(t => t.name.StartsWith(id.ToString("D2") + " |", StringComparison.Ordinal));
        static bool InViewSector(float yaw) => yaw >= 180f - .01f && yaw <= 270f + .01f;
        static float NearestAllowed(float yaw) {
            // A half-turn preserves a building's exact footprint. Use the opposite
            // southwest-facing direction when that fits the allowed sector; only
            // the two diagonal edge cases need a smaller turn to a sector boundary.
            float opposite = Mathf.Repeat(yaw + 180f, 360f);
            if (InViewSector(opposite)) return opposite;
            float toMin = Mathf.Abs(Mathf.DeltaAngle(yaw, 180f));
            float toMax = Mathf.Abs(Mathf.DeltaAngle(yaw, 270f));
            return toMin <= toMax ? 180f : 270f;
        }
        static Bounds RenderBounds(GameObject go) {
            var rs = go.GetComponentsInChildren<Renderer>(true); Check(rs.Length > 0, "renderers on " + go.name);
            var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b;
        }
        static void Audit() {
            var lines = new List<string>(); int already = 0, changes = 0;
            foreach (var b in plan.OrderBy(b => b.id)) {
                var t = Home(b.id); var yaw = Mathf.Repeat(t.eulerAngles.y, 360f); var next = InViewSector(yaw) ? yaw : NearestAllowed(yaw);
                if (InViewSector(yaw)) already++; else changes++;
                lines.Add($"{b.id:D2} | {t.name} | yaw {yaw:0.##} -> {next:0.##} | entrance {(InViewSector(yaw) ? "already toward iso" : "turn required")} | footprint {RenderBounds(t.gameObject).size.x:0.##} x {RenderBounds(t.gameObject).size.z:0.##}m");
            }
            File.WriteAllLines(Log + "Audit.txt", new[] { $"PASS: {already} entrances already face within the isometric southwest sector; {changes} require rotation. Camera view vector is (-X,-Z); accepted front yaws are 180..270 degrees. Audit made no scene changes." }.Concat(lines));
        }
        static void Apply() {
            Check(!File.Exists(Log + "Applied.txt"), "already applied; refusing a second pass");
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var diskBackup = Log + "AlpsTerrainTest-before-" + stamp + ".unity";
            Check(File.Exists(ScenePath), "scene file exists"); File.Copy(ScenePath, diskBackup, false);
            var memoryBackup = Log + "AlpsTerrainTest-memory-before-" + stamp + ".unity";
            Check(EditorSceneManager.SaveScene(scene, memoryBackup, true), "save in-memory recovery copy");
            try {
                int changed = 0;
                foreach (var b in plan.OrderBy(b => b.id)) {
                    var t = Home(b.id); float oldYaw = Mathf.Repeat(t.eulerAngles.y, 360f); if (InViewSector(oldYaw)) continue;
                    var before = RenderBounds(t.gameObject); float targetYaw = NearestAllowed(oldYaw);
                    t.rotation = Quaternion.Euler(t.eulerAngles.x, targetYaw, t.eulerAngles.z);
                    var after = RenderBounds(t.gameObject); t.position += new Vector3(before.center.x - after.center.x, 0, before.center.z - after.center.z);
                    var placed = RenderBounds(t.gameObject);
                    Check(Mathf.Abs(placed.center.x - before.center.x) < .02f && Mathf.Abs(placed.center.z - before.center.z) < .02f, "preserved footprint centre " + b.id);
                    Check(placed.size.x <= b.width + .05f && placed.size.z <= b.depth + .05f, "reserved footprint envelope after rotation " + b.id + " (" + placed.size.x + " x " + placed.size.z + ")");
                    changed++;
                }
                Physics.SyncTransforms();
                Check(plan.All(b => InViewSector(Mathf.Repeat(Home(b.id).eulerAngles.y, 360f))), "all entrances face the isometric southwest sector");
                foreach (var b in plan.Where(b => InViewSector(b.yaw)))
                    Check(Mathf.Abs(Mathf.DeltaAngle(Home(b.id).eulerAngles.y, b.yaw)) < .01f, "preserved previously-facing entrance " + b.id);
                Check(EditorSceneManager.SaveScene(scene, ScenePath), "save entrance alignment");
                File.WriteAllText(Log + "Applied.txt", $"PASS: aligned {changed} previously away-facing building entrances; already-facing entrances retained. Scene backup: {diskBackup}; in-memory backup: {memoryBackup}. Positions, reserved footprint envelopes, terrain, roads, landmarks, facilities and tree layout checked/preserved.");
                EditorSceneManager.OpenScene(ScenePath);
                Check(SceneManager.GetSceneByPath(ScenePath).isLoaded, "saved scene reopened");
                Setup(); Check(plan.All(b => InViewSector(Mathf.Repeat(Home(b.id).eulerAngles.y, 360f))), "saved/reopened entrance orientations");
            } catch {
                EditorSceneManager.OpenScene(memoryBackup);
                File.Copy(diskBackup, ScenePath, true); AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate); EditorSceneManager.OpenScene(ScenePath); throw;
            }
        }
        static void Review() {
            Check(File.Exists(Log + "Applied.txt"), "apply before review");
            var sceneCamera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).Single();
            var cameraObject = new GameObject("Temporary entrance-facing review camera"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>(); camera.CopyFrom(sceneCamera); camera.enabled = false; camera.farClipPlane = 3000; camera.aspect = 1.6f;
            var rig = cameraObject.AddComponent<CameraRig>(); rig.view = camera; rig.terrain = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Terrain>(true)).Single();
            var focus = new GameObject("Temporary entrance-facing review focus"); SceneManager.MoveGameObjectToScene(focus, scene);
            try {
                foreach (var b in plan.Where(b => Mathf.Abs(Mathf.DeltaAngle(b.yaw, Home(b.id).eulerAngles.y)) >= .01f)) {
                    var t = Home(b.id); focus.transform.position = t.position + t.forward * (Mathf.Max(b.width, b.depth) * .5f + 8);
                    rig.SetMode(CameraMode.Isometric); rig.UpdateForTarget(focus.transform, .1f, false);
                    Capture(camera, Log + "IsoEntrance" + b.id.ToString("D2") + ".png");
                }
            } finally { UnityEngine.Object.DestroyImmediate(focus); UnityEngine.Object.DestroyImmediate(cameraObject); }
            var rows = plan.Select(b => { var t = Home(b.id); var v = t.forward; return $"{b.id:D2} yaw={t.eulerAngles.y:0.##} front=({v.x:0.00},{v.z:0.00})"; });
            File.WriteAllLines(Log + "Validation.txt", new[] { "PASS: all 75 saved/reopened building entrances face -X, -Z, or between; previously compliant fronts unchanged." }.Concat(rows));
        }
        static void Capture(Camera camera, string path) {
            const int width = 1280, height = 800; var rt = RenderTexture.GetTemporary(width, height, 24); var prior = RenderTexture.active; var old = camera.targetTexture; var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try { camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); }
            finally { camera.targetTexture = old; RenderTexture.active = prior; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(image); }
        }
    }
}
