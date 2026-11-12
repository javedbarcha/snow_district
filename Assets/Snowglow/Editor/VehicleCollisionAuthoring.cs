using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Snowglow.Editor
{
    [InitializeOnLoad]
    public static class VehicleCollisionAuthoring
    {
        const string Folder = "Logs/VehicleCollision";
        const string Group = "Attachment collision | authored";
        static VehicleCollisionAuthoring() { EditorApplication.update += Pending; }
        static void Pending()
        {
            if (!File.Exists(Folder + ".request") || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(Folder + ".request");
            try { Install(); } catch (Exception e) { Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/Validation.txt", "FAIL: " + e); Debug.LogException(e); }
        }
        static Vector3[] Points(MeshFilter mesh, Transform frame) => mesh.sharedMesh.vertices.Select(v => frame.InverseTransformPoint(mesh.transform.TransformPoint(v))).ToArray();
        static Bounds Bounds(Vector3[] points)
        {
            var b = new Bounds(points[0], Vector3.zero); foreach (var p in points) b.Encapsulate(p); return b;
        }
        static void Box(Transform parent, string name, Vector3 centre, Vector3 size)
        {
            var child = parent.Find(name);
            if (!child) { child = new GameObject(name).transform; child.SetParent(parent, false); }
            child.gameObject.layer = 8;
            var box = child.GetComponent<BoxCollider>(); if (!box) box = child.gameObject.AddComponent<BoxCollider>();
            box.center = centre; box.size = size; box.isTrigger = false;
        }
        public static void Apply(GameObject root)
        {
            var machine = root.GetComponent<MachineController>();
            if (!machine) throw new InvalidOperationException("Prepared machinery required.");
            var body = root.GetComponentsInChildren<MeshFilter>(true).Single(m => m.name == "body");
            var chassis = root.GetComponentsInChildren<BoxCollider>(true).Single(c => c.name.StartsWith("Chassis proxy", StringComparison.Ordinal));
            var points = Points(body, chassis.transform);
            // Highest broad horizontal body level is the cabin roof, excluding narrow
            // exhaust/beacon geometry. Keep chassis footprint and bottom unchanged.
            var roof = points.GroupBy(p => Mathf.RoundToInt(p.y / .02f)).Select(g => Bounds(g.ToArray()))
                .Where(b => b.size.x > .8f && b.size.z > .6f).OrderByDescending(b => b.max.y).First();
            float bottom = chassis.center.y - chassis.size.y * .5f;
            var size = chassis.size; size.y = roof.max.y - bottom;
            var centre = chassis.center; centre.y = (roof.max.y + bottom) * .5f;
            chassis.center = centre; chassis.size = size;
            Transform frame = machine.definition.loader ? machine.Tool.bucket : machine.Tool.mount;
            var group = frame.Find(Group);
            if (!group) { group = new GameObject(Group).transform; group.SetParent(frame, false); }
            group.gameObject.layer = 8;
            string meshName = machine.definition.loader ? "LoaderBucket" : "PlowAttachment";
            var mesh = frame.GetComponentsInChildren<MeshFilter>(true).Single(m => m.name == meshName);
            var vertices = Points(mesh, group);
            var b = Bounds(vertices);
            if (!machine.definition.loader)
            {
                // Front blade face only: do not turn the rear lift frame into a solid box.
                b = Bounds(vertices.Where(p => p.z >= b.max.z - .65f).ToArray());
                Box(group, "Plow face", b.center, new Vector3(b.size.x, b.size.y, Mathf.Max(.12f, b.size.z)));
            }
            else
            {
                // Open-top/open-front bucket: floor, back and two cheeks, no solid
                // aggregate box sealing the collection cavity.
                const float thickness = .10f;
                Box(group, "Bucket floor", new Vector3(b.center.x, b.min.y + thickness * .5f, b.center.z), new Vector3(b.size.x, thickness, b.size.z));
                Box(group, "Bucket back", new Vector3(b.center.x, b.center.y, b.min.z + thickness * .5f), new Vector3(b.size.x, b.size.y, thickness));
                foreach (int side in new[] { -1, 1 })
                    Box(group, side < 0 ? "Bucket left cheek" : "Bucket right cheek", new Vector3(b.center.x + side * (b.extents.x - thickness * .5f), b.center.y, b.center.z), new Vector3(thickness, b.size.y, b.size.z));
            }
        }
        static void Validate(MachineController machine, StringBuilder report)
        {
            var boxes = machine.GetComponentsInChildren<BoxCollider>(true);
            int expected = machine.definition.loader ? 5 : 2;
            if (boxes.Length != expected || boxes.Any(b => b.isTrigger || !b.enabled || b.attachedRigidbody != machine.GetComponent<Rigidbody>())) throw new InvalidOperationException("Unexpected compound collider setup: " + machine.name);
            if (machine.GetComponentsInChildren<WheelCollider>(true).Length != 4) throw new InvalidOperationException("Wheel rig changed.");
            foreach (var box in boxes) report.AppendLine(machine.name + " / " + box.name + " centre=" + box.center.ToString("F4") + " size=" + box.size.ToString("F4"));
        }
        [MenuItem("Snowglow/Vehicles/Extend cabins and add attachment collision")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != SceneValidation.ScenePath || scene.isDirty) throw new InvalidOperationException("Open clean saved Pinefall; manual edits will not be overwritten.");
            Directory.CreateDirectory(Folder);
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(scene.path, Folder + "/Pinefall-before-" + stamp + ".unity");
            var session = UnityEngine.Object.FindFirstObjectByType<PrototypeSession>();
            string snow = JsonUtility.ToJson(session.Snow.seed.grid);
            var poses = session.Fleet.Select(m => m.transform.localToWorldMatrix).ToArray();
            var report = new StringBuilder("Unity " + Application.unityVersion + " / " + EditorUserBuildSettings.activeBuildTarget + "\n");
            foreach (string path in new[] { StarterPlowAuthoring.PrefabPath, LoaderModelIntake.PrefabPath })
            {
                File.Copy(path, Folder + "/" + Path.GetFileNameWithoutExtension(path) + "-before-" + stamp + ".prefab");
                var root = PrefabUtility.LoadPrefabContents(path);
                try { Apply(root); Validate(root.GetComponent<MachineController>(), report); PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (var machine in session.Fleet) { Apply(machine.gameObject); Validate(machine, report); }
            for (int i = 0; i < poses.Length; i++) if (poses[i] != session.Fleet[i].transform.localToWorldMatrix) throw new InvalidOperationException("Fleet pose changed.");
            if (snow != JsonUtility.ToJson(session.Snow.seed.grid)) throw new InvalidOperationException("Snow seed changed.");
            SceneValidation.Validate(scene);
            EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
            AssetDatabase.SaveAssets(); scene = EditorSceneManager.OpenScene(SceneValidation.ScenePath);
            session = UnityEngine.Object.FindFirstObjectByType<PrototypeSession>();
            foreach (var machine in session.Fleet) Validate(machine, report);
            SceneValidation.Validate(scene); MechanicsChecks.Run();
            report.AppendLine("PASS: saved/reopened two fleet prefabs and Pinefall; compound colliders/wheels, fleet poses, snow seed, scene validation and 19 snow checks. No Play entry. New impact/plowing behaviour and inertia not Play-tested; prior 77-case acceptance predates these collider changes.");
            File.WriteAllText(Folder + "/Validation.txt", report.ToString());
        }
    }
}
