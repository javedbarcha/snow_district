using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor
{
    public static class AlpsTerrainTestSignLayout
    {
        const string ScenePath = "Assets/Scenes/AlpsTerrainTest.unity";
        const string TownName = "ALPS TOWN | supplied 400m road map";
        const string FurnitureName = "05 Street lights and signboards";
        const string SourcePath = "SourceAssets/AlpineSignBoards/Alpine_Mobile_Sign_Boards_12_Variants.blend";
        const string SourceHash = "A533FFDEB2781572932C7FAC07F0FC6312B600D50133BD23607DAF399DD22A12";
        const string TextMaterialPath = "Assets/Snowglow/Catalog/Sources/AlpineSignBoards/WorldSignText.mat";
        const string RequestPath = "Logs/AlpsTerrainTestSigns.request";
        const string EvidenceFolder = "Logs/AlpsTerrainTestSigns";
        const string MarkerName = "Alps test-town sign variety v1";
        const string FacingMarkerName = "Alps sign front orientation correction v1";

        sealed class SignSnapshot
        {
            public CatalogPlacement placement;
            public string name;
            public string placementId;
            public Vector3 position;
            public Vector3 localScale;
            public Vector3 frontForward;
            public Transform parent;
        }

        static readonly Dictionary<string, string> ExistingVariants = new Dictionary<string, string>
        {
            { "S5 | TOWN LANDMARKS", "alpine-sign-01" },
            { "S6 | NORTH ENTRY", "alpine-sign-02" },
            { "S9 | UPPER VILLAGE", "alpine-sign-09" },
            { "S10 | UPPER VILLAGE", "alpine-sign-07" },
            { "S8 | THE ALPS", "alpine-sign-08" },
            { "S7 | THE ALPS", "alpine-sign-03" },
            { "S3 | SNOW DEPOT", "alpine-sign-07" },
            { "S4 | SNOW DISPOSAL", "alpine-sign-12" },
            { "S2 | FUEL + REPAIR", "alpine-sign-10" },
            { "S1 | SOUTH ENTRY", "alpine-sign-10" }
        };

        static readonly Vector2[] HazardBoundary = {
            new Vector2(-165, 220), new Vector2(-55, 220), new Vector2(55, 220), new Vector2(165, 220),
            new Vector2(220, 165), new Vector2(220, 55), new Vector2(220, -55), new Vector2(220, -165),
            new Vector2(165, -220), new Vector2(55, -220), new Vector2(-55, -220), new Vector2(-165, -220),
            new Vector2(-220, -165), new Vector2(-220, -55), new Vector2(-220, 55), new Vector2(-220, 165)
        };

        static T[] All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        static bool IsSign(CatalogPlacement placement) =>
            placement && (placement.assetId ?? "").StartsWith("alpine-sign-", StringComparison.Ordinal);

        static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("Alps test signs: " + message);
        }

        [InitializeOnLoadMethod]
        static void RegisterRequest()
        {
            EditorApplication.update += ProcessRequest;
        }

        static void ProcessRequest()
        {
            if (!File.Exists(RequestPath))
            {
                EditorApplication.update -= ProcessRequest;
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorApplication.update -= ProcessRequest;
            var action = File.ReadAllText(RequestPath).Trim();
            File.Delete(RequestPath);
            try
            {
                Require(action == "apply" || action == "correct-facing",
                    "request must contain only 'apply' or 'correct-facing'");
                if (action == "apply")
                    Apply();
                else
                    CorrectFacing();
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory(EvidenceFolder);
                File.WriteAllText(EvidenceFolder + "/Validation.txt", "FAIL: " + exception);
                Debug.LogException(exception);
            }
        }

        static float Ground(MeshCollider terrain, Vector3 position)
        {
            var ray = new Ray(new Vector3(position.x, 500, position.z), Vector3.down);
            Require(terrain.Raycast(ray, out var hit, 1000), "no physical terrain at " + position);
            return hit.point.y;
        }

        static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
        }

        static string Matrix(Transform transform) => transform.localToWorldMatrix.ToString("R");

        static CatalogAsset Entry(string id)
        {
            var entry = CatalogPreparation.Library().Find(id);
            Require(entry && entry.prefab && entry.status == PreparationStatus.NeedsReview,
                "prepared Needs Review prefab missing: " + id);
            return entry;
        }

        static AssetAnchor Face(GameObject sign)
        {
            return sign.GetComponentsInChildren<AssetAnchor>(true).Single(anchor => anchor.id == "label-face");
        }

        static void AimFace(GameObject sign, Vector3 direction)
        {
            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            Require(direction.sqrMagnitude > .99f, "invalid horizontal facing direction");
            var face = Face(sign);
            var desiredRotation = Quaternion.LookRotation(-direction, Vector3.up);
            var faceRelativeToRoot = Quaternion.Inverse(sign.transform.rotation) * face.transform.rotation;
            sign.transform.rotation = desiredRotation * Quaternion.Inverse(faceRelativeToRoot);
        }

        static TextMesh AddCaption(GameObject sign, string caption)
        {
            var anchor = Face(sign).transform;
            var text = new GameObject("Label | Alps town destination").AddComponent<TextMesh>();
            text.transform.SetParent(anchor, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = caption;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 48;
            text.characterSize = .13f;
            text.color = new Color(.96f, .96f, .87f);
            return text;
        }

        static void FitCaption(TextMesh text, float width, float height)
        {
            text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle);
            text.transform.localScale = Vector3.one;
            var bounds = text.GetComponent<MeshRenderer>().localBounds;
            Require(bounds.size.x > .001f && bounds.size.y > .001f, "invalid caption bounds");
            text.transform.localScale = Vector3.one * Mathf.Min(1, Mathf.Min(width / bounds.size.x, height / bounds.size.y));
            PrefabUtility.RecordPrefabInstancePropertyModifications(text.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }

        static void SetCaptions(GameObject sign, params string[] captions)
        {
            var labels = sign.GetComponentsInChildren<TextMesh>(true)
                .OrderByDescending(text => text.transform.position.y).ToArray();
            if (labels.Length == 0)
            {
                Require(captions.Length == 1, "blank sign needs one caption: " + sign.name);
                labels = new[] { AddCaption(sign, captions[0]) };
            }
            else
            {
                Require(labels.Length == captions.Length, "caption count mismatch on " + sign.name);
                for (var index = 0; index < labels.Length; index++)
                {
                    labels[index].text = captions[index];
                    PrefabUtility.RecordPrefabInstancePropertyModifications(labels[index]);
                }
            }

            foreach (var label in labels)
            {
                if (sign.GetComponent<CatalogPlacement>().assetId == "alpine-sign-07")
                    FitCaption(label, 2.4f, .95f);
                else if (sign.GetComponent<CatalogPlacement>().assetId == "alpine-sign-12")
                    FitCaption(label, 1.5f, .65f);
                else if (sign.GetComponent<CatalogPlacement>().assetId == "alpine-sign-09")
                    FitCaption(label, 1.65f, .28f);
                else
                    FitCaption(label, 2.2f, .65f);
            }
        }

        static Material TextMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(TextMaterialPath);
            Require(material && material.shader && material.shader.name == "Snowglow/World Sign Text",
                "existing depth-tested sign text material unavailable");
            return material;
        }

        static void BindCaptions(Scene scene)
        {
            var material = TextMaterial();
            foreach (var text in All<CatalogPlacement>(scene).Where(IsSign)
                         .SelectMany(sign => sign.GetComponentsInChildren<TextMesh>(true)))
            {
                var binding = text.GetComponent<WorldSignLabel>();
                if (!binding)
                    binding = text.gameObject.AddComponent<WorldSignLabel>();
                binding.caption = text;
                binding.material = material;
                binding.Refresh();
                EditorUtility.SetDirty(binding);
                PrefabUtility.RecordPrefabInstancePropertyModifications(text.GetComponent<MeshRenderer>());
                PrefabUtility.RecordPrefabInstancePropertyModifications(binding);
            }
        }

        static void Seat(GameObject sign, MeshCollider terrain, Vector3 position)
        {
            sign.transform.position = new Vector3(position.x, Ground(terrain, position), position.z);
            Physics.SyncTransforms();
            var collider = sign.GetComponentInChildren<Collider>();
            Require(collider, "sign has no conservative envelope collider: " + sign.name);
            sign.transform.position += Vector3.up * (Ground(terrain, position) - .015f - collider.bounds.min.y);
            Physics.SyncTransforms();
            PrefabUtility.RecordPrefabInstancePropertyModifications(sign.transform);
        }

        static MeshCollider[] RoadColliders(Transform town)
        {
            var roads = town.Find("01 Roads and squares").GetComponentsInChildren<MeshCollider>(true)
                .Where(collider => collider.enabled && collider.sharedMesh &&
                                   !collider.name.StartsWith("Snow depot |", StringComparison.Ordinal) &&
                                   !collider.name.StartsWith("Disposal yard |", StringComparison.Ordinal))
                .Where(collider =>
                {
                    var renderer = collider.GetComponent<MeshRenderer>();
                    return renderer && renderer.sharedMaterials.Any(material => material && material.name == "Town asphalt");
                }).ToArray();
            Require(roads.Length > 0, "town asphalt road geometry missing");
            return roads;
        }

        static List<Collider> Blockers(Scene scene, MeshCollider terrain, MeshCollider[] roads)
        {
            var roadSet = new HashSet<Collider>(roads);
            return All<Collider>(scene)
                .Where(collider => collider.enabled && collider.gameObject.activeInHierarchy && !collider.isTrigger &&
                                   collider != terrain && !roadSet.Contains(collider) &&
                                   !IsSign(collider.GetComponentInParent<CatalogPlacement>()))
                .ToList();
        }

        static bool SupportsPenetrationQuery(Collider collider)
        {
            return collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider ||
                   collider is MeshCollider meshCollider && meshCollider.convex;
        }

        static bool Penetrates(Collider first, Collider second)
        {
            if (!first || !second || !first.bounds.Intersects(second.bounds))
                return false;
            if (SupportsPenetrationQuery(first))
                return Physics.ComputePenetration(first, first.transform.position, first.transform.rotation,
                    second, second.transform.position, second.transform.rotation, out _, out _);
            if (SupportsPenetrationQuery(second))
                return Physics.ComputePenetration(second, second.transform.position, second.transform.rotation,
                    first, first.transform.position, first.transform.rotation, out _, out _);
            return true;
        }

        static bool Clear(GameObject sign, List<Collider> blockers, MeshCollider[] roads)
        {
            var collider = sign.GetComponentInChildren<Collider>();
            return collider && !roads.Any(road => Penetrates(collider, road)) &&
                   !blockers.Any(blocker => Penetrates(collider, blocker));
        }

        static Vector3[] SearchOffsets(float radius)
        {
            var offsets = new List<Vector3> { Vector3.zero };
            for (var ring = 1; ring <= Mathf.FloorToInt(radius / 2); ring++)
            {
                var distance = ring * 2f;
                for (var step = 0; step < 16; step++)
                {
                    var angle = step * Mathf.PI * 2 / 16;
                    offsets.Add(new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance));
                }
            }
            return offsets.ToArray();
        }

        static GameObject Spawn(
            CatalogAsset entry,
            Transform parent,
            string name,
            string placementId,
            Vector3 position,
            Vector3 faceDirection,
            MeshCollider terrain,
            List<Collider> blockers,
            MeshCollider[] roads,
            float searchRadius)
        {
            var sign = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab);
            sign.transform.SetParent(parent, false);
            sign.transform.localScale = Vector3.one;
            sign.name = name;
            AimFace(sign, faceDirection);
            var placement = sign.GetComponent<CatalogPlacement>();
            Require(placement, "catalogue placement component missing: " + entry.id);
            placement.placementId = placementId;
            PrefabUtility.RecordPrefabInstancePropertyModifications(placement);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sign.transform);

            var found = false;
            foreach (var offset in SearchOffsets(searchRadius))
            {
                var candidate = position + offset;
                Seat(sign, terrain, candidate);
                if (!Clear(sign, blockers, roads))
                    continue;
                found = true;
                break;
            }
            Require(found, "no clear roadside position within " + searchRadius + "m for " + placementId);
            blockers.Add(sign.GetComponentInChildren<Collider>());
            return sign;
        }

        static SignSnapshot Snapshot(CatalogPlacement placement)
        {
            return new SignSnapshot
            {
                placement = placement,
                name = placement.gameObject.name,
                placementId = placement.placementId,
                position = placement.transform.position,
                localScale = placement.transform.localScale,
                frontForward = -Face(placement.gameObject).transform.forward,
                parent = placement.transform.parent
            };
        }

        static void Replace(
            SignSnapshot snapshot,
            CatalogAsset entry,
            string[] captions,
            MeshCollider terrain,
            List<Collider> blockers,
            MeshCollider[] roads)
        {
            var replacement = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab);
            replacement.transform.SetParent(snapshot.parent, false);
            replacement.transform.localScale = snapshot.localScale;
            replacement.name = snapshot.name;
            replacement.transform.position = snapshot.position;
            AimFace(replacement, snapshot.frontForward);

            var placement = replacement.GetComponent<CatalogPlacement>();
            Require(placement, "catalogue placement component missing: " + entry.id);
            placement.placementId = snapshot.placementId;
            PrefabUtility.RecordPrefabInstancePropertyModifications(placement);
            PrefabUtility.RecordPrefabInstancePropertyModifications(replacement.transform);
            if (captions != null)
                SetCaptions(replacement, captions);

            var found = false;
            foreach (var offset in SearchOffsets(8))
            {
                var candidate = snapshot.position + offset;
                Seat(replacement, terrain, candidate);
                if (!Clear(replacement, blockers, roads))
                    continue;
                found = true;
                break;
            }
            Require(found, "no clear replacement position within 8m for " + snapshot.name);
            blockers.Add(replacement.GetComponentInChildren<Collider>());
            UnityEngine.Object.DestroyImmediate(snapshot.placement.gameObject);
        }

        static Dictionary<string, string> NonSignPlacements(Scene scene)
        {
            return All<CatalogPlacement>(scene).Where(placement => !IsSign(placement) && !string.IsNullOrEmpty(placement.placementId))
                .ToDictionary(placement => placement.placementId, placement => placement.assetId + "|" + Matrix(placement.transform));
        }

        static void Validate(Scene scene, Dictionary<string, string> protectedPlacements, SignSnapshot[] mapSnapshots)
        {
            var town = scene.GetRootGameObjects().Single(root => root.name == TownName).transform;
            var furniture = town.Find(FurnitureName);
            Require(furniture, "town sign and street-furniture group missing");
            Require(furniture.Find(MarkerName), "sign variety marker missing");
            var sceneSigns = furniture.GetComponentsInChildren<CatalogPlacement>(true).Where(IsSign).ToArray();
            Require(sceneSigns.Length == 30, "expected 10 map signs, 16 boundary hazards and 4 road closures");
            Require(sceneSigns.Select(sign => sign.placementId).Distinct().Count() == 30, "sign placement IDs are not unique");
            var disconnected = sceneSigns.FirstOrDefault(sign => !PrefabUtility.IsPartOfPrefabInstance(sign));
            Require(!disconnected, "disconnected catalogue sign: " +
                    (disconnected ? disconnected.placementId : "unknown"));
            Require(sceneSigns.All(sign => sign.GetComponentsInChildren<Light>(true).Length == 0), "unexpected sign light");
            Require(sceneSigns.Select(sign => sign.assetId).Distinct().Count() >= 9, "signboard variety was not retained");

            foreach (var snapshot in mapSnapshots)
            {
                var placed = sceneSigns.SingleOrDefault(sign => sign.placementId == snapshot.placementId);
                Require(placed && placed.name == snapshot.name, "original map sign identity changed: " + snapshot.placementId);
                Require(Vector2.Distance(new Vector2(placed.transform.position.x, placed.transform.position.z),
                    new Vector2(snapshot.position.x, snapshot.position.z)) <= 8.1f,
                    "map sign moved too far from its supplied X/Z: " + snapshot.placementId);
                Require(Vector3.Dot(-Face(placed.gameObject).transform.forward, snapshot.frontForward) > .999f,
                    "original map sign reading direction changed: " + snapshot.placementId);
            }

            var terrainRoot = scene.GetRootGameObjects().Single(root => root.name == "Revised attached terrain | 1:1 metres");
            var terrain = terrainRoot.GetComponentInChildren<MeshCollider>();
            Require(terrain, "physical terrain collider missing");
            var roads = RoadColliders(town);
            var blockers = Blockers(scene, terrain, roads);

            foreach (var sign in sceneSigns)
            {
                var collider = sign.GetComponentInChildren<Collider>();
                Require(collider, "sign collision envelope missing: " + sign.placementId);
                var bounds = collider.bounds;
                Require(!roads.Any(road => Penetrates(collider, road)), "sign obstructs asphalt: " + sign.placementId);
                var placementRoot = sign.transform;
                var ground = Ground(terrain, placementRoot.position);
                Require(Mathf.Abs(bounds.min.y - (ground - .015f)) < .035f, "unseated sign: " + sign.placementId);
            }

            var hazardSigns = sceneSigns.Where(sign => sign.placementId.StartsWith("alps-boundary-hazard-", StringComparison.Ordinal)).ToArray();
            var closedSigns = sceneSigns.Where(sign => sign.placementId.StartsWith("alps-road-closed-", StringComparison.Ordinal)).ToArray();
            Require(hazardSigns.Length == 16 && hazardSigns.All(sign => sign.assetId == "alpine-sign-04"), "boundary hazard count/type mismatch");
            Require(closedSigns.Length == 4 && closedSigns.All(sign => sign.assetId == "alpine-sign-05"), "road-closed count/type mismatch");

            for (var index = 0; index < HazardBoundary.Length; index++)
            {
                var sign = hazardSigns.Single(candidate => candidate.placementId == "alps-boundary-hazard-" + (index + 1).ToString("D2"));
                var expected = new Vector3(HazardBoundary[index].x, 0, HazardBoundary[index].y);
                Require(Vector2.Distance(new Vector2(sign.transform.position.x, sign.transform.position.z),
                    new Vector2(expected.x, expected.z)) <= 6.1f, "hazard sign moved off the marked town boundary");
                var inward = new Vector3(-sign.transform.position.x, 0, -sign.transform.position.z).normalized;
                var front = -Face(sign.gameObject).transform.forward;
                Require(Vector3.Dot(front, inward) > .98f, "hazard sign does not face into town: " + sign.placementId);
            }

            var closedTargets = new Dictionary<string, Vector3>
            {
                { "alps-road-closed-north", Vector3.back },
                { "alps-road-closed-south", Vector3.forward },
                { "alps-road-closed-east", Vector3.left },
                { "alps-road-closed-west", Vector3.right }
            };
            foreach (var pair in closedTargets)
            {
                var sign = closedSigns.Single(candidate => candidate.placementId == pair.Key);
                Require(Vector3.Dot(-Face(sign.gameObject).transform.forward, pair.Value) > .98f,
                    "road-closed sign faces away from its approach: " + pair.Key);
                var target = pair.Key.EndsWith("north", StringComparison.Ordinal) ? new Vector3(8.25f, 0, 190) :
                    pair.Key.EndsWith("south", StringComparison.Ordinal) ? new Vector3(8.25f, 0, -190) :
                    pair.Key.EndsWith("east", StringComparison.Ordinal) ? new Vector3(190, 0, 28.25f) :
                    new Vector3(-190, 0, 28.25f);
                Require(Vector2.Distance(new Vector2(sign.transform.position.x, sign.transform.position.z),
                    new Vector2(target.x, target.z)) <= 4.1f, "road-closed sign is not at its road terminus: " + pair.Key);
            }

            var directory = sceneSigns.Single(sign => sign.placementId == "alps-map-s5");
            Require(directory.assetId == "alpine-sign-01", "four-board route directory is not in the town centre");
            var directoryCaptions = directory.GetComponentsInChildren<TextMesh>(true)
                .OrderByDescending(text => text.transform.position.y).Select(text => text.text).ToArray();
            Require(directoryCaptions.SequenceEqual(new[] { "UPPER VILLAGE", "TOWN SQUARE", "SNOW DEPOT", "SOUTH ENTRY" }),
                "four-board directory labels are incomplete");
            Require(sceneSigns.Any(sign => sign.assetId == "alpine-sign-09") &&
                    sceneSigns.Any(sign => sign.assetId == "alpine-sign-07") &&
                    sceneSigns.Any(sign => sign.assetId == "alpine-sign-12"),
                "single/multi-board timber variants missing");

            var signBounds = sceneSigns.Select(sign => sign.GetComponentInChildren<Collider>().bounds).ToArray();
            for (var first = 0; first < signBounds.Length; first++)
                for (var second = first + 1; second < signBounds.Length; second++)
                    Require(!signBounds[first].Intersects(signBounds[second]), "sign envelopes overlap each other");
            foreach (var sign in sceneSigns)
            {
                var collider = sign.GetComponentInChildren<Collider>();
                Require(!blockers.Any(blocker => Penetrates(collider, blocker)),
                    "sign overlaps another collider: " + sign.placementId);
            }

            foreach (var pair in protectedPlacements)
            {
                var placement = All<CatalogPlacement>(scene).SingleOrDefault(candidate => candidate.placementId == pair.Key);
                Require(placement && pair.Value == placement.assetId + "|" + Matrix(placement.transform),
                    "non-sign town placement changed: " + pair.Key);
            }
        }

        static void Capture(Camera camera, string filename)
        {
            var renderTexture = RenderTexture.GetTemporary(1600, 1000, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
                image.Apply();
                File.WriteAllBytes(EvidenceFolder + "/" + filename, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        static void CaptureEvidence(Scene scene, MeshCollider terrain)
        {
            var originalCamera = All<Camera>(scene).FirstOrDefault(camera => camera.enabled);
            Require(originalCamera, "scene camera unavailable for evidence capture");
            var temporary = new GameObject("Temporary Alps sign review camera");
            SceneManager.MoveGameObjectToScene(temporary, scene);
            var camera = temporary.AddComponent<Camera>();
            camera.CopyFrom(originalCamera);
            camera.enabled = false;
            camera.targetTexture = null;
            camera.farClipPlane = 1500;
            camera.cullingMask = originalCamera.cullingMask;

            try
            {
                camera.orthographic = true;
                camera.orthographicSize = 245;
                camera.transform.SetPositionAndRotation(new Vector3(0, 620, 0), Quaternion.Euler(90, 0, 0));
                Capture(camera, "TopPlan.png");

                camera.orthographicSize = 235;
                camera.transform.position = new Vector3(-420, 560, -420);
                camera.transform.LookAt(new Vector3(0, Ground(terrain, Vector3.zero), 0));
                Capture(camera, "IsometricOverview.png");

                camera.orthographic = false;
                camera.fieldOfView = 50;
                foreach (var item in new[]
                {
                    ("alps-map-s5", "CenterDirectory.png"),
                    ("alps-map-s9", "TimberTwinArrows.png"),
                    ("alps-boundary-hazard-01", "BoundaryHazard.png"),
                    ("alps-road-closed-north", "NorthRoadClosed.png")
                })
                {
                    var placement = All<CatalogPlacement>(scene).Single(sign => sign.placementId == item.Item1);
                    var face = Face(placement.gameObject).transform;
                    var focus = face.position + Vector3.up * .2f;
                    camera.transform.position = face.position - face.forward * 9 + Vector3.up * 2;
                    camera.transform.LookAt(focus);
                    Capture(camera, item.Item2);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporary);
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        static Vector3 IntendedFront(CatalogPlacement sign)
        {
            if (sign.placementId.StartsWith("alps-boundary-hazard-", StringComparison.Ordinal))
                return new Vector3(-sign.transform.position.x, 0, -sign.transform.position.z).normalized;

            if (sign.placementId == "alps-road-closed-north")
                return Vector3.back;
            if (sign.placementId == "alps-road-closed-south")
                return Vector3.forward;
            if (sign.placementId == "alps-road-closed-east")
                return Vector3.left;
            if (sign.placementId == "alps-road-closed-west")
                return Vector3.right;

            return -Face(sign.gameObject).transform.forward;
        }

        static void CorrectFacing()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "stop Play first");
            var scene = SceneManager.GetActiveScene();
            Require(scene.path == ScenePath, "open AlpsTerrainTest only");
            Require(HashFile(SourcePath) == SourceHash, "preserved sign source hash mismatch");

            Directory.CreateDirectory(EvidenceFolder);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var memoryBackup = EvidenceFolder + "/Facing-in-memory-before-" + stamp + ".unity";
            var diskBackup = EvidenceFolder + "/Facing-disk-before-" + stamp + ".unity";
            Require(EditorSceneManager.SaveScene(scene, memoryBackup, true), "could not preserve current in-memory scene");
            File.Copy(ScenePath, diskBackup);

            try
            {
                var town = scene.GetRootGameObjects().Single(root => root.name == TownName).transform;
                var furniture = town.Find(FurnitureName);
                Require(furniture && furniture.Find(MarkerName), "the authored sign layout is missing");
                Require(!furniture.Find(FacingMarkerName), "front orientation was already corrected");
                var signs = All<CatalogPlacement>(scene).Where(IsSign).ToArray();
                Require(signs.Length == 30, "expected the installed 30-sign layout");
                var mapSnapshots = signs.Where(sign => sign.placementId.StartsWith("alps-map-", StringComparison.Ordinal))
                    .Select(Snapshot).ToArray();
                Require(mapSnapshots.Length == 10, "expected ten retained map signs");
                var protectedPlacements = NonSignPlacements(scene);

                foreach (var sign in signs)
                {
                    AimFace(sign.gameObject, IntendedFront(sign));
                    PrefabUtility.RecordPrefabInstancePropertyModifications(sign.transform);
                }
                new GameObject(FacingMarkerName).transform.SetParent(furniture, false);
                BindCaptions(scene);
                Physics.SyncTransforms();
                Validate(scene, protectedPlacements, mapSnapshots);
                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene, ScenePath), "could not save corrected sign faces");
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Validate(scene, protectedPlacements, mapSnapshots);
                CaptureEvidence(scene, scene.GetRootGameObjects()
                    .Single(root => root.name == "Revised attached terrain | 1:1 metres")
                    .GetComponentInChildren<MeshCollider>());
                scene = SceneManager.GetActiveScene();
                Validate(scene, protectedPlacements, mapSnapshots);

                var report = new StringBuilder();
                report.AppendLine("PASS: AlpsTerrainTest sign faces saved, reopened, validated, and rendered in Unity " + Application.unityVersion + ".");
                report.AppendLine("The visible fronts face the sign-reading direction: boundary hazards face inward and Road Closed signs face approaching traffic. Map-sign reading directions and all placements are retained.");
                report.AppendLine("Evidence refreshed: TopPlan.png, IsometricOverview.png, CenterDirectory.png, TimberTwinArrows.png, BoundaryHazard.png, NorthRoadClosed.png.");
                report.AppendLine("No source/catalogue, production-scene, Play, save-data, or Android/device changes.");
                File.WriteAllText(EvidenceFolder + "/Validation.txt", report.ToString());
                Debug.Log(report.ToString());
            }
            catch
            {
                if (scene.IsValid() && scene.isDirty)
                    EditorSceneManager.SaveScene(scene, EvidenceFolder + "/Facing-failed-unsaved-" + stamp + ".unity", true);
                File.Copy(diskBackup, ScenePath, true);
                AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                throw;
            }
        }

        public static void Apply()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "stop Play first");
            var scene = SceneManager.GetActiveScene();
            Require(scene.path == ScenePath, "open AlpsTerrainTest only");
            Require(HashFile(SourcePath) == SourceHash, "preserved sign source hash mismatch");

            Directory.CreateDirectory(EvidenceFolder);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var memoryBackup = EvidenceFolder + "/InMemory-before-" + stamp + ".unity";
            var diskBackup = EvidenceFolder + "/Disk-before-" + stamp + ".unity";
            Require(EditorSceneManager.SaveScene(scene, memoryBackup, true), "could not preserve current in-memory scene");
            File.Copy(ScenePath, diskBackup);

            try
            {
                var town = scene.GetRootGameObjects().Single(root => root.name == TownName).transform;
                var furniture = town.Find(FurnitureName);
                Require(furniture, "town sign and street-furniture group missing");
                Require(!furniture.Find(MarkerName), "sign variety has already been installed");

                var originalSigns = furniture.GetComponentsInChildren<CatalogPlacement>(true)
                    .Where(IsSign).ToArray();
                Require(originalSigns.Length == 10 && originalSigns.All(sign => sign.assetId == "alpine-sign-01"),
                    "expected the original 10 route-directory signs; refusing to overwrite a revised layout");
                var snapshots = originalSigns.Select(Snapshot).ToArray();
                Require(new HashSet<string>(ExistingVariants.Keys).SetEquals(snapshots.Select(snapshot => snapshot.name)),
                    "original map sign names differ from the expected layout");

                var protectedPlacements = NonSignPlacements(scene);
                var terrainRoot = scene.GetRootGameObjects().Single(root => root.name == "Revised attached terrain | 1:1 metres");
                var terrain = terrainRoot.GetComponentInChildren<MeshCollider>();
                Require(terrain, "physical terrain collider missing");
                var roads = RoadColliders(town);
                var blockers = Blockers(scene, terrain, roads);

                var directoryLabels = new[] { "UPPER VILLAGE", "TOWN SQUARE", "SNOW DEPOT", "SOUTH ENTRY" };
                foreach (var snapshot in snapshots)
                {
                    if (snapshot.name == "S5 | TOWN LANDMARKS")
                    {
                        Replace(snapshot, Entry("alpine-sign-01"), directoryLabels, terrain, blockers, roads);
                        continue;
                    }

                    var assetId = ExistingVariants[snapshot.name];
                    var captions = snapshot.name == "S9 | UPPER VILLAGE" ? new[] { "UPPER VILLAGE" } :
                        snapshot.name == "S10 | UPPER VILLAGE" ? new[] { "UPPER VILLAGE" } :
                        snapshot.name == "S3 | SNOW DEPOT" ? new[] { "SNOW DEPOT" } :
                        snapshot.name == "S4 | SNOW DISPOSAL" ? new[] { "SNOW DISPOSAL" } :
                        snapshot.name == "S2 | FUEL + REPAIR" ? new[] { "FUEL + REPAIR" } :
                        snapshot.name == "S1 | SOUTH ENTRY" ? new[] { "SOUTH ENTRY" } : null;

                    Replace(snapshot, Entry(assetId), captions, terrain, blockers, roads);
                }

                var hazards = new GameObject("Boundary hazard boards | 16").transform;
                hazards.SetParent(furniture, false);
                var centralDirectory = All<CatalogPlacement>(scene).Single(sign => sign.placementId == "alps-map-s5");
                blockers.Add(centralDirectory.GetComponentInChildren<Collider>());
                var roadClosures = new GameObject("Road termini closed boards | 4").transform;
                roadClosures.SetParent(furniture, false);
                var hazard = Entry("alpine-sign-04");
                for (var index = 0; index < HazardBoundary.Length; index++)
                {
                    var point = HazardBoundary[index];
                    var position = new Vector3(point.x, 0, point.y);
                    Spawn(hazard, hazards, "Boundary hazard " + (index + 1).ToString("D2"),
                        "alps-boundary-hazard-" + (index + 1).ToString("D2"), position,
                        new Vector3(-point.x, 0, -point.y), terrain, blockers, roads, 6);
                }

                var closed = Entry("alpine-sign-05");
                var termini = new[]
                {
                    ("north", new Vector3(8.25f, 0, 190), Vector3.back),
                    ("south", new Vector3(8.25f, 0, -190), Vector3.forward),
                    ("east", new Vector3(190, 0, 28.25f), Vector3.left),
                    ("west", new Vector3(-190, 0, 28.25f), Vector3.right)
                };
                foreach (var terminus in termini)
                {
                    Spawn(closed, roadClosures, "Road closed | " + terminus.Item1,
                        "alps-road-closed-" + terminus.Item1, terminus.Item2, terminus.Item3,
                        terrain, blockers, roads, 4);
                }
                new GameObject(MarkerName).transform.SetParent(furniture, false);

                BindCaptions(scene);
                Physics.SyncTransforms();
                Validate(scene, protectedPlacements, snapshots);
                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene, ScenePath), "could not save sign layout");
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Validate(scene, protectedPlacements, snapshots);
                CaptureEvidence(scene, scene.GetRootGameObjects()
                    .Single(root => root.name == "Revised attached terrain | 1:1 metres")
                    .GetComponentInChildren<MeshCollider>());
                scene = SceneManager.GetActiveScene();
                Validate(scene, protectedPlacements, snapshots);

                var finalSigns = All<CatalogPlacement>(scene).Where(IsSign).ToArray();
                var report = new StringBuilder();
                report.AppendLine("PASS: AlpsTerrainTest sign variety saved, reopened, validated, and rendered in Unity " + Application.unityVersion + ".");
                report.AppendLine("Preserved source SHA-256 verified: " + SourceHash + ".");
                var maximumMapShift = snapshots.Max(snapshot =>
                {
                    var sign = finalSigns.Single(candidate => candidate.placementId == snapshot.placementId);
                    return Vector2.Distance(new Vector2(sign.transform.position.x, sign.transform.position.z),
                        new Vector2(snapshot.position.x, snapshot.position.z));
                });
                report.AppendLine("30 connected signs: 10 existing map placements (IDs and reading directions retained; maximum collision-avoidance shift " +
                                  maximumMapShift.ToString("F2") + "m), 16 inward-facing boundary hazards, and 4 Road Closed signs at the road termini.");
                report.AppendLine("The central four-board directory reads UPPER VILLAGE / TOWN SQUARE / SNOW DEPOT / SOUTH ENTRY. Existing map signs now use single, twin-arrow, noticeboard, hanging and small timber variants.");
                report.AppendLine("Every sign is seated on the physical terrain, clear of asphalt and other static colliders. All non-sign catalogue placements retained their asset IDs and world transforms.");
                report.AppendLine("Prepared source models, catalogue entries, original Pinefall scene and production scenes were not edited. No town rebuild, Play run, save access, or Android/device test.");
                report.AppendLine("Evidence: TopPlan.png, IsometricOverview.png, CenterDirectory.png, TimberTwinArrows.png, BoundaryHazard.png, NorthRoadClosed.png.");
                report.AppendLine("Placement inventory:");
                foreach (var sign in finalSigns.OrderBy(sign => sign.placementId, StringComparer.Ordinal))
                    report.AppendLine(sign.placementId + " | " + sign.assetId + " | " + sign.transform.position.ToString("F2"));
                File.WriteAllText(EvidenceFolder + "/Validation.txt", report.ToString());
                Debug.Log(report.ToString());
            }
            catch
            {
                if (scene.IsValid() && scene.isDirty)
                    EditorSceneManager.SaveScene(scene, EvidenceFolder + "/Failed-unsaved-" + stamp + ".unity", true);
                File.Copy(diskBackup, ScenePath, true);
                AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                throw;
            }
        }
    }
}
