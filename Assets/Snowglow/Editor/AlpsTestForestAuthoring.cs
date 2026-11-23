using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor
{
    [InitializeOnLoad]
    public static class AlpsTestForestAuthoring
    {
        const string ScenePath = "Assets/Scenes/AlpsTerrainTest.unity";
        const string Request = "Logs/AlpsTestForest.request";
        const string Logs = "Logs/AlpsTestForest/";
        const string TownRootName = "ALPS TOWN | supplied 400m road map";
        const string GroupName = "Alpine forest | Pinefall trees and PNG distance cards";
        const int TotalTrees = 374;
        const int TownTrees = 56;
        const int OuterTrees = TotalTrees - TownTrees;
        const float TerrainRadius = 500f;
        const float TownRadius = 210f;
        const float MinSupportSpread = .18f;

        // Red perimeter traced from the user's top-down reference, in its 1 km world frame.
        static readonly Vector2[] Boundary =
        {
            new(-190,431), new(-346,409), new(-376,286), new(-402,72), new(-421,-150),
            new(-253,-301), new(-125,-438), new(18,-392), new(200,-356), new(320,-356),
            new(390,-199), new(396,-66), new(390,49), new(394,299), new(276,407),
            new(115,423), new(0,425)
        };

        sealed class TreeData
        {
            public string id;
            public GameObject prefab;
            public MeshFilter body;
            public Vector3[] support;
            public DistanceVisibility visibility;
            public float height;
        }

        sealed class Cell
        {
            public readonly List<Bounds> bounds = new();
        }

        static AlpsTestForestAuthoring() => EditorApplication.update += Poll;

        static void Poll()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var action = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            Directory.CreateDirectory(Logs);
            try
            {
                if (action != "build") throw new InvalidOperationException("Expected one-time action 'build'.");
                Build();
            }
            catch (Exception e)
            {
                File.WriteAllText(Logs + "Validation.txt", "FAIL: " + e);
                Debug.LogException(e);
            }
        }

        static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Alps test forest: " + message);
        }

        static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        static float Ground(Terrain terrain, Vector3 p) => terrain.SampleHeight(p) + terrain.transform.position.y;

        static bool InsideBoundary(float x, float z)
        {
            bool inside = false;
            for (int i = 0, j = Boundary.Length - 1; i < Boundary.Length; j = i++)
            {
                var a = Boundary[i]; var b = Boundary[j];
                if (((a.y > z) != (b.y > z)) && x < (b.x - a.x) * (z - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        static TreeData LoadTree(int index)
        {
            string id = "alpine-tree-" + index.ToString("D2");
            string path = "Assets/Snowglow/Catalog/Prefabs/" + id + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(prefab, "missing Pinefall tree prefab " + id);
            var visibility = prefab.GetComponent<DistanceVisibility>();
            Check(visibility && visibility.nearParts.Length > 0 && visibility.distantParts.Length > 0, "tree lacks near mesh / PNG card switch " + id);
            Check(Mathf.Abs(visibility.distance - 200) < .01f && Mathf.Abs(visibility.distantDistance - 2000) < .01f, "unexpected Pinefall card distances " + id);
            var filters = prefab.GetComponentsInChildren<MeshFilter>(true)
                .Where(f => f.sharedMesh && !f.name.Contains("SnowCap") && !f.name.Contains("Distant forest"))
                .OrderByDescending(f => f.sharedMesh.bounds.size.y).ToArray();
            Check(filters.Length > 0, "tree body mesh missing " + id);
            var body = filters[0];
            var local = prefab.transform.worldToLocalMatrix * body.transform.localToWorldMatrix;
            var vertices = body.sharedMesh.vertices.Select(v => local.MultiplyPoint3x4(v)).ToArray();
            float minY = vertices.Min(v => v.y), maxY = vertices.Max(v => v.y);
            var support = vertices.Where(v => v.y <= minY + Mathf.Max(.07f, (maxY - minY) * .02f)).Distinct().ToArray();
            Check(support.Length > 0, "tree base support vertices missing " + id);
            return new TreeData { id = id, prefab = prefab, body = body, support = support, visibility = visibility, height = maxY - minY };
        }

        static Bounds LocalBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            Check(renderers.Length > 0, "tree has no renderers " + root.name);
            bool initialized = false; var bounds = new Bounds();
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                var local = r.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var point = root.transform.InverseTransformPoint(r.transform.TransformPoint(corner));
                    if (!initialized) { bounds = new Bounds(point, Vector3.zero); initialized = true; }
                    else bounds.Encapsulate(point);
                }
            }
            Check(initialized, "tree render bounds missing " + root.name);
            return bounds;
        }

        static Bounds WorldBounds(Bounds local, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var matrix = Matrix4x4.TRS(position, rotation, scale);
            var result = new Bounds(matrix.MultiplyPoint3x4(local.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
                result.Encapsulate(matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            return result;
        }

        static bool OverlapXZ(Bounds a, Bounds b, float gap)
            => a.min.x < b.max.x + gap && a.max.x > b.min.x - gap && a.min.z < b.max.z + gap && a.max.z > b.min.z - gap;

        static Vector2Int CellKey(float x, float z) => new(Mathf.FloorToInt(x / 24f), Mathf.FloorToInt(z / 24f));

        static void AddToGrid(Dictionary<Vector2Int, Cell> grid, Bounds b)
        {
            var lo = CellKey(b.min.x, b.min.z); var hi = CellKey(b.max.x, b.max.z);
            for (int z = lo.y; z <= hi.y; z++) for (int x = lo.x; x <= hi.x; x++)
            {
                var key = new Vector2Int(x, z);
                if (!grid.TryGetValue(key, out var cell)) grid.Add(key, cell = new Cell());
                cell.bounds.Add(b);
            }
        }

        static bool ClearOfObstacles(Bounds candidate, Dictionary<Vector2Int, Cell> grid)
        {
            var lo = CellKey(candidate.min.x - 3, candidate.min.z - 3); var hi = CellKey(candidate.max.x + 3, candidate.max.z + 3);
            for (int z = lo.y; z <= hi.y; z++) for (int x = lo.x; x <= hi.x; x++)
                if (grid.TryGetValue(new Vector2Int(x, z), out var cell))
                    foreach (var blocker in cell.bounds) if (OverlapXZ(candidate, blocker, 3f)) return false;
            return true;
        }

        static bool ClearOfRoads(Bounds candidate, Collider[] roads)
        {
            float[] xs = { candidate.min.x - 1f, candidate.center.x, candidate.max.x + 1f };
            float[] zs = { candidate.min.z - 1f, candidate.center.z, candidate.max.z + 1f };
            foreach (float x in xs) foreach (float z in zs)
            {
                var ray = new Ray(new Vector3(x, 650, z), Vector3.down);
                foreach (var road in roads)
                    if (road && road.enabled && road.Raycast(ray, out _, 1300)) return false;
            }
            return true;
        }

        static Quaternion SlopeRotation(Terrain terrain, Vector3 at, float yaw)
        {
            float u = Mathf.Clamp01((at.x - terrain.transform.position.x) / terrain.terrainData.size.x);
            float v = Mathf.Clamp01((at.z - terrain.transform.position.z) / terrain.terrainData.size.z);
            var normal = terrain.terrainData.GetInterpolatedNormal(u, v);
            var tilt = Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, normal), 12f);
            return tilt * Quaternion.Euler(0, yaw, 0);
        }

        static bool Seat(TreeData data, Terrain terrain, float x, float z, Quaternion rotation, float scale, out Vector3 position, out float spread)
        {
            float lowest = float.PositiveInfinity, highest = float.NegativeInfinity;
            foreach (var foot in data.support)
            {
                var relative = rotation * (foot * scale);
                var sample = new Vector3(x + relative.x, 0, z + relative.z);
                float neededY = Ground(terrain, sample) - relative.y;
                lowest = Mathf.Min(lowest, neededY); highest = Mathf.Max(highest, neededY);
            }
            spread = highest - lowest;
            position = new Vector3(x, lowest - .015f, z);
            return spread <= MinSupportSpread;
        }

        static bool Accept(TreeData data, Bounds localBounds, Terrain terrain, float x, float z, float yaw, float scale,
            Dictionary<Vector2Int, Cell> blockers, Collider[] roads, List<Vector2> placed, float minSpacing,
            out Vector3 position, out Quaternion rotation, out Bounds bounds, out float spread, out string reason)
        {
            reason = ""; position = new Vector3(x, 0, z); rotation = SlopeRotation(terrain, position, yaw); bounds = default;
            if (!Seat(data, terrain, x, z, rotation, scale, out position, out spread)) { reason = "terrain"; return false; }
            bounds = WorldBounds(localBounds, position, rotation, Vector3.one * scale);
            if (bounds.min.x < -TerrainRadius + 8 || bounds.max.x > TerrainRadius - 8 || bounds.min.z < -TerrainRadius + 8 || bounds.max.z > TerrainRadius - 8) { reason = "terrain_bounds"; return false; }
            if (!ClearOfObstacles(bounds, blockers)) { reason = "objects"; return false; }
            if (!ClearOfRoads(bounds, roads)) { reason = "roads"; return false; }
            foreach (var prior in placed) if (Vector2.Distance(prior, new Vector2(x, z)) < minSpacing) { reason = "spacing"; return false; }
            return true;
        }

        static bool InTown(float x, float z) => Mathf.Abs(x) <= 193f && Mathf.Abs(z) <= 193f;

        static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            Check(scene.IsValid() && scene.path == ScenePath, "open AlpsTerrainTest first; no other scene will be changed");
            Check(!All<Transform>(scene).Any(t => t.name == GroupName), "forest already installed; refusing to duplicate");
            var terrain = All<Terrain>(scene).Single(t => t.drawHeightmap == false && t.terrainData.size.x >= 999);
            var town = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Single(t => t.name == TownRootName);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            if (scene.isDirty) Check(EditorSceneManager.SaveScene(scene), "save current test-scene edits before forest authoring");
            File.Copy(ScenePath, Logs + "AlpsTerrainTest-before-" + stamp + ".unity", true);
            try
            {
            Check(!All<Transform>(scene).Any(t => t.name == GroupName), "saved scene already has a forest group");

            var trees = Enumerable.Range(1, 16).Select(LoadTree).ToArray();
            var localBounds = trees.ToDictionary(t => t.id, t => LocalBounds(t.prefab));
            var treeRoots = All<CatalogPlacement>(scene).Where(p => (p.assetId ?? "").StartsWith("alpine-tree-", StringComparison.Ordinal)).ToArray();
            Check(treeRoots.Length == 0, "test scene unexpectedly contains a tree forest; review before adding");
            var roadGroup = town.Find("01 Roads and squares");
            Check(roadGroup, "town road group missing");
            var roads = roadGroup.GetComponentsInChildren<MeshCollider>(true).Cast<Collider>().ToArray();
            Check(roads.Length >= 10, "town road mesh colliders missing");
            var blockers = new Dictionary<Vector2Int, Cell>();
            foreach (var collider in All<Collider>(scene))
            {
                if (!collider || !collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy || collider is TerrainCollider) continue;
                if (collider is MeshCollider && collider.bounds.size.x > 900f && collider.bounds.size.z > 900f) continue;
                if (collider.transform.IsChildOf(roadGroup)) continue;
                if (collider.GetComponentInParent<MachineController>() || collider.GetComponentInParent<SnowField>()) continue;
                AddToGrid(blockers, collider.bounds);
            }

            var forest = new GameObject(GroupName).transform;
            SceneManager.MoveGameObjectToScene(forest.gameObject, scene); forest.SetParent(town, false);
            var inTown = new GameObject("01 Trees within town blocks | 56 of 374").transform; inTown.SetParent(forest, false);
            var outskirts = new GameObject("02 Trees from town outskirts to red boundary | 318 of 374").transform; outskirts.SetParent(forest, false);
            var random = new System.Random(23112026);
            var occupied = new List<Vector2>();
            var byId = new Dictionary<string, TreeData>();
            foreach (var data in trees) byId[data.id] = data;
            var variantCounts = trees.ToDictionary(t => t.id, _ => 0);
            int inside = 0, outside = 0, slopeAligned = 0, cardCount = 0, attemptsTown = 0, attemptsOuter = 0;
            float worstSpread = 0, minTreeRadius = float.PositiveInfinity, maxTreeRadius = 0;

            void PlaceOne(bool townTree, int ordinal)
            {
                Transform parent = townTree ? inTown : outskirts;
                bool found = false;
                var rejected = new Dictionary<string, int>();
                for (int attempt = 0; attempt < 250000; attempt++)
                {
                    if (townTree) attemptsTown++; else attemptsOuter++;
                    float x = townTree ? (float)(random.NextDouble() * 386 - 193) : (float)(random.NextDouble() * 880 - 440);
                    float z = townTree ? (float)(random.NextDouble() * 386 - 193) : (float)(random.NextDouble() * 880 - 440);
                    if (townTree && !InTown(x, z)) continue;
                    if (!townTree && (!InsideBoundary(x, z) || new Vector2(x, z).magnitude < TownRadius)) continue;
                    int variant = random.Next(trees.Length);
                    var data = trees[variant];
                    float scale = Mathf.Lerp(.78f, 1.32f, (float)random.NextDouble());
                    float yaw = (float)random.NextDouble() * 360;
                    float minSpacing = townTree ? 10.5f : 13.5f;
                    if (!Accept(data, localBounds[data.id], terrain, x, z, yaw, scale, blockers, roads, occupied, minSpacing,
                        out var position, out var rotation, out var bounds, out var spread, out var reason))
                    { rejected.TryGetValue(reason, out int count); rejected[reason] = count + 1; continue; }
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(data.prefab, scene);
                    go.name = (townTree ? "TOWN TREE | " : "OUTSKIRT TREE | ") + ordinal.ToString("D3") + " | " + data.id;
                    go.transform.SetParent(parent, true); go.transform.SetPositionAndRotation(position, rotation); go.transform.localScale = Vector3.one * scale;
                    var placement = go.GetComponent<CatalogPlacement>();
                    Check(placement, "catalog placement component missing " + data.id);
                    placement.placementId = "alps-test-tree-" + (townTree ? "town-" : "outskirts-") + ordinal.ToString("D3");
                    Check(go.GetComponentsInChildren<Collider>(true).Length == 1 && go.GetComponentsInChildren<BoxCollider>(true).Length == 1, "expected Pinefall's single enclosing box " + data.id);
                    var visibility = go.GetComponent<DistanceVisibility>();
                    Check(visibility && visibility.nearParts.Length > 0 && visibility.distantParts.Length > 0, "far PNG tree cards missing " + data.id);
                    occupied.Add(new Vector2(x, z)); AddToGrid(blockers, bounds);
                    variantCounts[data.id]++; worstSpread = Mathf.Max(worstSpread, spread);
                    if (Vector3.Angle(Vector3.up, rotation * Vector3.up) > .25f) slopeAligned++;
                    cardCount++;
                    float radius = new Vector2(x, z).magnitude; minTreeRadius = Mathf.Min(minTreeRadius, radius); maxTreeRadius = Mathf.Max(maxTreeRadius, radius);
                    found = true;
                    if (townTree) inside++; else outside++;
                    return;
                }
                Check(found, "placement capacity exhausted for " + (townTree ? "town blocks" : "outer boundary") + " at " + ordinal + "; attempted=" + (townTree ? attemptsTown : attemptsOuter) + "; rejected=" + string.Join(",", rejected.Select(kv => kv.Key + ":" + kv.Value)) + "; roads=" + roads.Length + "; treeBounds=" + localBounds[trees[0].id]);
            }

            for (int i = 1; i <= TownTrees; i++) PlaceOne(true, i);
            for (int i = 1; i <= OuterTrees; i++) PlaceOne(false, i);
            Check(inside == TownTrees && outside == OuterTrees && cardCount == TotalTrees, "tree cohort totals");
            foreach (var data in trees) Check(variantCounts[data.id] > 0, "unused Pinefall variant " + data.id);

            Physics.SyncTransforms();
            AssetDatabase.SaveAssets();
            Check(EditorSceneManager.SaveScene(scene), "save AlpsTerrainTest forest");
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Validate(scene, trees, roadsExpected: 10);
            Capture(scene);
            File.WriteAllText(Logs + "Validation.txt",
                "PASS: AlpsTerrainTest saved/reopened with " + inside + "/" + TotalTrees + " town-block trees (" + (inside * 100f / TotalTrees).ToString("F2") + "%) and " + outside + " outside, inside the user-traced red perimeter. " +
                "Shared Pinefall alpine-tree-01..16 prefabs; all " + cardCount + " retain near geometry plus crossed PNG atlas cards (200m/2000m DistanceVisibility); each has one enclosing box collider. " +
                "Base support sampled against terrain height queries with <=" + MinSupportSpread + "m footprint relief; maximum observed " + worstSpread.ToString("F3") + "m. " + slopeAligned + " trees slope-aligned. " +
                "Town tree root radius " + minTreeRadius.ToString("F1") + ".." + new Vector2(193,193).magnitude.ToString("F1") + "m; outer tree radii " + TownRadius + ".." + maxTreeRadius.ToString("F1") + "m. Roads, building/facility/landmark/mountain colliders cleared. " +
                "Seed 23112026, variants=" + string.Join(",", variantCounts.Select(kv => kv.Key + ":" + kv.Value)) + ". No Play, gameplay, production scene, Pinefall source or terrain mesh changes. Captures: TopPlan.png and GameplayIso.png.");
            }
            catch
            {
                File.Copy(Logs + "AlpsTerrainTest-before-" + stamp + ".unity", ScenePath, true);
                AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                throw;
            }
        }

        static void Validate(Scene scene, TreeData[] trees, int roadsExpected)
        {
            Check(scene.path == ScenePath, "test scene reopened");
            var forest = All<Transform>(scene).Single(t => t.name == GroupName);
            var town = forest.Find("01 Trees within town blocks | 56 of 374");
            var outskirts = forest.Find("02 Trees from town outskirts to red boundary | 318 of 374");
            Check(town && outskirts, "organized forest cohorts saved");
            var entries = trees.ToDictionary(t => t.id, t => t);
            var townTrees = town.GetComponentsInChildren<CatalogPlacement>(true);
            var outerTrees = outskirts.GetComponentsInChildren<CatalogPlacement>(true);
            Check(townTrees.Length == TownTrees && outerTrees.Length == OuterTrees, "saved cohort counts");
            var terrain = All<Terrain>(scene).Single(t => t.drawHeightmap == false && t.terrainData.size.x >= 999);
            foreach (var p in townTrees.Concat(outerTrees))
            {
                Check(entries.ContainsKey(p.assetId), "unapproved asset " + p.assetId);
                var data = entries[p.assetId]; var lod = p.GetComponent<DistanceVisibility>();
                Check(PrefabUtility.IsPartOfPrefabInstance(p) && lod && lod.nearParts.Length > 0 && lod.distantParts.Length > 0 && lod.distance == 200 && lod.distantDistance == 2000,
                    "saved prefab or far-card switch " + p.placementId);
                var colliders = p.GetComponentsInChildren<Collider>(true);
                Check(colliders.Length == 1 && colliders[0] is BoxCollider, "saved single box collision " + p.placementId);
                float minClear = float.PositiveInfinity, maxClear = float.NegativeInfinity;
                foreach (var foot in data.support)
                {
                    var at = p.transform.TransformPoint(foot); var clear = at.y - Ground(terrain, at);
                    minClear = Mathf.Min(minClear, clear); maxClear = Mathf.Max(maxClear, clear);
                }
                Check(maxClear <= .005f && minClear >= -MinSupportSpread - .04f, "terrain contact bounds " + p.placementId + " " + minClear + ".." + maxClear);
                var xz = new Vector2(p.transform.position.x, p.transform.position.z);
                if (p.transform.parent == town) Check(InTown(xz.x, xz.y), "town cohort escaped blocks");
                else Check(InsideBoundary(xz.x, xz.y) && xz.magnitude >= TownRadius, "outer cohort escaped red boundary");
            }
            Check(All<CatalogPlacement>(scene).Count(p => (p.placementId ?? "").StartsWith("alps-test-tree-town-")) == TownTrees, "town IDs");
            Check(All<CatalogPlacement>(scene).Count(p => (p.placementId ?? "").StartsWith("alps-test-tree-outskirts-")) == OuterTrees, "outer IDs");
            Check(All<Transform>(scene).Single(t => t.name == TownRootName).Find("01 Roads and squares").GetComponentsInChildren<MeshCollider>(true).Length >= roadsExpected,
                "town road network retained");
        }

        static void Capture(Scene scene)
        {
            var cameraObject = new GameObject("Temporary Alps forest review camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            var rt = new RenderTexture(1440, 1440, 24); var previous = RenderTexture.active;
            var image = new Texture2D(1440, 1440, TextureFormat.RGB24, false);
            try
            {
                camera.enabled = false; camera.targetTexture = rt; camera.aspect = 1; camera.nearClipPlane = .1f; camera.farClipPlane = 3000;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.orthographic = true; camera.orthographicSize = 515;
                camera.transform.SetPositionAndRotation(new Vector3(0, 1150, 0), Quaternion.Euler(90, 0, 0)); camera.Render();
                RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 1440, 1440), 0, 0); image.Apply(); File.WriteAllBytes(Logs + "TopPlan.png", image.EncodeToPNG());
                camera.orthographic = false; camera.fieldOfView = 58; camera.transform.position = new Vector3(-570, 370, -570); camera.transform.LookAt(new Vector3(0, 30, 0)); camera.Render();
                RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 1440, 1440), 0, 0); image.Apply(); File.WriteAllBytes(Logs + "GameplayIso.png", image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
