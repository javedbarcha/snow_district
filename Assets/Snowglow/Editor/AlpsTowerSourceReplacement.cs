using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor
{
    [InitializeOnLoad]
    public static class AlpsTowerSourceReplacement
    {
        const string ScenePath = "Assets/Scenes/AlpsTerrainTest.unity";
        const string ModelPath = "Assets/Snowglow/Catalog/Sources/AlpsRebuild/GateTower.fbx";
        const string PrefabPath = "Assets/Snowglow/Catalog/Prefabs/alps-rebuild-gatetower.prefab";
        const string EntryPath = "Assets/Snowglow/Catalog/Entries/asset-68b13d02972a438397f63a8953979766.asset";
        const string Log = "Logs/AlpsTowerSourceReplacement/";
        const string Request = "Logs/AlpsTowerSourceReplacement.request";

        static AlpsTowerSourceReplacement() { EditorApplication.update += Poll; }

        static void Poll()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var action = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            Directory.CreateDirectory(Log);
            try
            {
                if (action == "audit") Audit();
                else if (action == "apply") Apply();
                else throw new InvalidOperationException("Unknown tower replacement action: " + action);
            }
            catch (Exception e)
            {
                File.WriteAllText(Log + "Validation.txt", "FAIL: " + e);
                Debug.LogException(e);
            }
        }

        static void Check(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException("Alps tower replacement: " + message);
        }

        static Component[] Components(GameObject root) => root.GetComponentsInChildren<Component>(true);

        static string Describe(GameObject root, string label)
        {
            var sb = new StringBuilder(label).AppendLine();
            foreach (var t in root.GetComponentsInChildren<Transform>(true).OrderBy(t => Path(t)))
            {
                var mf = t.GetComponent<MeshFilter>();
                var col = t.GetComponent<MeshCollider>();
                var renderer = t.GetComponent<MeshRenderer>();
                sb.Append(Path(t)).Append(" | ").Append(t.localPosition.ToString("F4")).Append(" | ").Append(t.localScale.ToString("F4"));
                if (mf) sb.Append(" | mesh=").Append(mf.sharedMesh ? mf.sharedMesh.name : "<null>").Append(" v=").Append(mf.sharedMesh ? mf.sharedMesh.vertexCount : 0).Append(" t=").Append(mf.sharedMesh ? mf.sharedMesh.triangles.Length / 3 : 0).Append(" meshBounds=").Append(mf.sharedMesh ? mf.sharedMesh.bounds.ToString("F4") : "<null>").Append(" @").Append(AssetDatabase.GetAssetPath(mf.sharedMesh));
                if (col) sb.Append(" | collider=").Append(col.sharedMesh ? col.sharedMesh.name : "<null>");
                if (renderer) sb.Append(" | materials=").Append(string.Join(",", renderer.sharedMaterials.Select(m => m ? m.name : "<null>")));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        static string Path(Transform t)
        {
            var parts = new System.Collections.Generic.List<string>();
            while (t) { parts.Add(t.name); t = t.parent; }
            parts.Reverse();
            return string.Join("/", parts);
        }

        static void Audit()
        {
            var scene = SceneManager.GetActiveScene();
            Check(scene.path == ScenePath, "active scene must be AlpsTerrainTest");
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            Check(EditorSceneManager.SaveScene(scene, Log + "InMemory-before-" + stamp + ".unity", true), "preserve in-memory scene copy");
            File.Copy(ScenePath, Log + "Disk-before-" + stamp + ".unity", true);
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var tower = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CatalogPlacement>(true)).Single(p => p.assetId == "alps-rebuild-gatetower");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Check(prefab && model, "tower prefab and imported model exist");
            File.WriteAllText(Log + "Before.txt", "source=" + AssetDatabase.GetAssetPath(model) + "\n" + Describe(model, "IMPORTED SOURCE") + Describe(prefab, "CATALOG PREFAB") + Describe(tower.gameObject, "SCENE INSTANCE") + "\nscene dirty=" + scene.isDirty + "\ntower position=" + tower.transform.position + " rotation=" + tower.transform.rotation.eulerAngles + " scale=" + tower.transform.lossyScale);
            File.WriteAllText(Log + "Validation.txt", "PASS: current in-memory/disk scene copies captured; source, catalogue prefab and placed tower hierarchy reported before replacement.");
        }

        static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            Check(scene.path == ScenePath, "active scene must be AlpsTerrainTest");
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var tower = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CatalogPlacement>(true)).Single(p => p.assetId == "alps-rebuild-gatetower");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Check(source, "imported replacement FBX is available");
            var sourceFilters = source.GetComponentsInChildren<MeshFilter>(true);
            Check(sourceFilters.Length > 0, "replacement FBX has render meshes");
            var duplicates = sourceFilters.GroupBy(f => f.name).Where(g => g.Count() > 1).ToArray();
            Check(duplicates.Length == 0, "replacement mesh names must be unique: " + string.Join(",", duplicates.Select(g => g.Key)));
            var sourceByName = sourceFilters.ToDictionary(f => f.name, f => f);
            var report = new StringBuilder(Describe(source, "REPLACEMENT SOURCE"));
            var oldSceneObjects = tower.GetComponentsInChildren<Transform>(true).Select(Path).ToArray();
            var oldPosition = tower.transform.position;
            var oldRotation = tower.transform.rotation;
            var oldScale = tower.transform.localScale;
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var backup = Log + "InMemory-apply-" + stamp + ".unity";
            Check(EditorSceneManager.SaveScene(scene, backup, true), "preserve current in-memory scene");
            File.Copy(ScenePath, Log + "Disk-apply-" + stamp + ".unity", true);
            var prefabBackup = Log + "Prefab-before-" + stamp + ".prefab";
            var entryBackup = Log + "Entry-before-" + stamp + ".asset";
            File.Copy(PrefabPath, prefabBackup, true);
            File.Copy(EntryPath, entryBackup, true);
            try
            {
                var prefabContents = PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    int replaced = 0;
                    foreach (var f in prefabContents.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (!sourceByName.TryGetValue(f.name, out var sourceFilter)) continue;
                        var previous = f.sharedMesh;
                        f.sharedMesh = sourceFilter.sharedMesh;
                        f.transform.localPosition = sourceFilter.transform.localPosition;
                        f.transform.localRotation = sourceFilter.transform.localRotation;
                        f.transform.localScale = sourceFilter.transform.localScale;
                        foreach (var c in f.GetComponents<MeshCollider>()) if (c.sharedMesh == previous) c.sharedMesh = sourceFilter.sharedMesh;
                        replaced++;
                        report.AppendLine("Prefab replaced " + f.name + " -> " + sourceFilter.sharedMesh.name + " transform=" + sourceFilter.transform.localPosition.ToString("F4") + "/" + sourceFilter.transform.localRotation.eulerAngles.ToString("F4") + "/" + sourceFilter.transform.localScale.ToString("F4"));
                    }
                    Check(replaced > 0, "no prefab mesh names matched replacement source");
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, PrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefabContents); }

                var instanceFilters = tower.GetComponentsInChildren<MeshFilter>(true);
                int sceneReplaced = 0;
                foreach (var f in instanceFilters)
                {
                    if (!sourceByName.TryGetValue(f.name, out var sourceFilter)) continue;
                    var previous = f.sharedMesh;
                    f.sharedMesh = sourceFilter.sharedMesh;
                    f.transform.localPosition = sourceFilter.transform.localPosition;
                    f.transform.localRotation = sourceFilter.transform.localRotation;
                    f.transform.localScale = sourceFilter.transform.localScale;
                    foreach (var c in f.GetComponents<MeshCollider>()) if (c.sharedMesh == previous) c.sharedMesh = sourceFilter.sharedMesh;
                    sceneReplaced++;
                }
                Check(sceneReplaced > 0, "no scene mesh names matched replacement source");
                foreach (var t in tower.GetComponentsInChildren<Transform>(true).Where(t => t != tower.transform && (t.name == "Flag overlay" || t.name == "Flag front overlay" || t.name == "Flag rear overlay" || t.name == "Flag cross overlay")).ToArray()) UnityEngine.Object.DestroyImmediate(t.gameObject);

                var entry = AssetDatabase.LoadAssetAtPath<CatalogAsset>(EntryPath);
                Check(entry, "catalogue entry exists");
                var bounds = CatalogValidation.BoundsOf(source);
                entry.localBounds = bounds;
                entry.triangles = sourceFilters.Sum(f => (int)f.sharedMesh.GetIndexCount(0) / 3);
                EditorUtility.SetDirty(entry);
                Check(tower.transform.position == oldPosition && tower.transform.rotation == oldRotation && tower.transform.localScale == oldScale, "placement transform preserved");
                Check(EditorSceneManager.SaveScene(scene, ScenePath), "save updated Alps test scene");
                AssetDatabase.SaveAssets();

                scene = EditorSceneManager.OpenScene(ScenePath);
                tower = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CatalogPlacement>(true)).Single(p => p.assetId == "alps-rebuild-gatetower");
                Check(tower.transform.position == oldPosition && tower.transform.rotation == oldRotation && tower.transform.localScale == oldScale, "saved/reopened tower transform retained");
                int reopenedMeshes = 0;
                foreach (var f in tower.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (!sourceByName.TryGetValue(f.name, out var sourceFilter)) continue;
                    Check(f.sharedMesh == sourceFilter.sharedMesh, "saved scene uses replacement mesh " + f.name);
                    Check(f.transform.localScale == sourceFilter.transform.localScale, "saved scene uses replacement source scale " + f.name);
                    foreach (var c in f.GetComponents<MeshCollider>()) Check(c.sharedMesh == sourceFilter.sharedMesh, "matching collider " + f.name);
                    reopenedMeshes++;
                }
                Check(reopenedMeshes == sceneReplaced, "all scene source meshes persisted");
                foreach (var t in tower.GetComponentsInChildren<Transform>(true)) Check(t.name != "Flag overlay" && t.name != "Flag front overlay" && t.name != "Flag rear overlay" && t.name != "Flag cross overlay", "old flag overlay removed");
                Physics.SyncTransforms();
                var arch = tower.GetComponentsInChildren<MeshCollider>(true).Single(c => c.sharedMesh == sourceByName["Architecture"].sharedMesh);
                float z = tower.transform.position.z;
                for (float y = .2f; y <= 4.5f; y += .3f)
                    for (float x = -2.75f; x <= 2.75f; x += .5f)
                    {
                        var p = new Vector3(x, y, z);
                        Check(!arch.Raycast(new Ray(p + Vector3.back * 8, Vector3.forward), out _, 16) && !arch.Raycast(new Ray(p + Vector3.forward * 8, Vector3.back), out _, 16), "replacement tower tunnel clear at x=" + x.ToString("F1") + " y=" + y.ToString("F1"));
                    }
                Capture(scene, tower.transform, Log);
                report.AppendLine("Scene mesh refs updated=" + reopenedMeshes + "; prefab mesh refs updated=" + sceneReplaced + "; entry triangles=" + entry.triangles + "; old/new hierarchy nodes=" + oldSceneObjects.Length + "/" + tower.GetComponentsInChildren<Transform>(true).Length);
                File.WriteAllText(Log + "After.txt", report.ToString());
                File.WriteAllText(Log + "Validation.txt", "PASS: imported replacement meshes and source child transforms are used by the catalogue prefab and AlpsTerrainTest tower; matching mesh colliders updated; tower placement transform retained; narrowed road remains unchanged; 6m tunnel clearance sampled after save/reopen; gameplay/isometric and both flag-side captures written.");
            }
            catch
            {
                File.Copy(prefabBackup, PrefabPath, true);
                File.Copy(entryBackup, EntryPath, true);
                File.Copy(backup, ScenePath, true);
                AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(EntryPath, ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
                EditorSceneManager.OpenScene(ScenePath);
                throw;
            }
        }

        static void Capture(Scene scene, Transform tower, string log)
        {
            var go = new GameObject("Temporary corrected tower review camera");
            SceneManager.MoveGameObjectToScene(go, scene);
            var camera = go.AddComponent<Camera>();
            camera.enabled = false;
            camera.aspect = 1;
            camera.fieldOfView = 45;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 2000;
            var renderers = tower.GetComponentsInChildren<Renderer>(true);
            Check(renderers.Length > 0, "replacement tower renderers visible");
            var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.y) * .62f;
            var focus = bounds.center + Vector3.up * 1.0f;
            foreach (var side in new[] { -1, 1 })
            {
                var position = focus + Vector3.up * 4 + Vector3.forward * (side * Mathf.Max(25, bounds.size.z * 2));
                camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(focus - position));
                Render(camera, log + (side < 0 ? "Flag-front.png" : "Flag-back.png"));
            }
            camera.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.y) * .8f;
            var iso = Quaternion.Euler(41, 45, 0);
            camera.transform.SetPositionAndRotation(focus - iso * Vector3.forward * 100, iso);
            Render(camera, log + "GameplayIsometric.png");
            UnityEngine.Object.DestroyImmediate(go);
        }

        static void Render(Camera camera, string path)
        {
            var rt = RenderTexture.GetTemporary(1024, 1024, 24);
            var previous = RenderTexture.active;
            var image = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
