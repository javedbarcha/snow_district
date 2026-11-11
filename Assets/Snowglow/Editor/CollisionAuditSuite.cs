using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Snowglow.Editor
{
    public static class CollisionAuditSuite
    {
        const string Folder = "Logs/CollisionAudit";
        static readonly MethodInfo Rebuild = typeof(SnowField).GetMethod("Rebuild", BindingFlags.Instance | BindingFlags.NonPublic);
        sealed class Case
        {
            public int vehicle, shape;
            public string name, isolation = "normal";
            public float speed, height, width, offset, steer;
            public bool foundation, stationary, full, dump, ramp, raised, coast;
        }
        static readonly List<Case> Cases = new List<Case>();
        static PrototypeSession game;
        static Action complete;
        static int index;
        static float started;
        static MachineController machine;
        static CollisionAuditProbe probe;
        static float[] depth, credit;
        static bool[] blocked;
        static bool originalIgnore;
        static Collider foundation;
        static bool dumped;
        static bool production;
        static Collider ramp;
        static StreamWriter summary;
        static Case Current => Cases[index];
        static Dictionary<Collider, bool> bodyStates;
        static Dictionary<Rigidbody, float> separationLimits;
        static readonly HashSet<KeyValuePair<Collider, Collider>> ignoredPairs = new HashSet<KeyValuePair<Collider, Collider>>();
        public static string ObjectPath(Transform item) => item.parent ? ObjectPath(item.parent) + "/" + item.name : item.name;
        static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

        public static void Start(PrototypeSession session, Action done)
        {
            game = session; complete = done; index = -1;
            production = File.Exists(Folder + "/Production.request");
            if (production) File.Delete(Folder + "/Production.request");
            Directory.CreateDirectory(Folder);
            originalIgnore = Physics.GetIgnoreLayerCollision(8, 9);
            bodyStates = game.Fleet.SelectMany(v => v.GetComponentsInChildren<Collider>(true)).Where(c => !(c is WheelCollider)).ToDictionary(c => c, c => c.enabled);
            separationLimits = game.Fleet.ToDictionary(v => v.Body, v => v.Body.maxDepenetrationVelocity);
            depth = (float[])game.Snow.Grid.depth.Clone(); credit = (float[])game.Snow.Grid.credit.Clone(); blocked = (bool[])game.Snow.Grid.blocked.Clone();
            var colliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c => c.enabled && !c.isTrigger).ToArray();
            using (var writer = new StreamWriter(Folder + "/Colliders.csv"))
            {
                writer.WriteLine("path,type,layer,rigidbody,center,size,material,bounce,friction");
                foreach (var collider in colliders)
                {
                    var material = collider.sharedMaterial;
                    writer.WriteLine(string.Join(",", Csv(ObjectPath(collider.transform)), collider.GetType().Name, collider.gameObject.layer,
                        Csv(collider.attachedRigidbody ? collider.attachedRigidbody.name : "static"), Csv(collider.bounds.center.ToString("F3")), Csv(collider.bounds.size.ToString("F3")),
                        Csv(material ? material.name : "default"), material ? material.bounciness.ToString(CultureInfo.InvariantCulture) : "0", material ? material.dynamicFriction.ToString(CultureInfo.InvariantCulture) : "default"));
                }
            }
            foundation = colliders.Where(c => c is BoxCollider && !c.attachedRigidbody && ObjectPath(c.transform).ToLowerInvariant().Contains("foundation"))
                .OrderBy(c => Vector3.Distance(c.bounds.center, new Vector3(-22, 9.39f, 16))).First();
            ramp = colliders.FirstOrDefault(c => c.name == "CollisionWedge" && ObjectPath(c.transform).Contains("Traction"));
            using (var writer = new StreamWriter(Folder + "/Fleet.txt"))
                foreach (var vehicle in game.Fleet)
                {
                    writer.WriteLine(vehicle.name + "; definition COM=" + vehicle.definition.dryCenterOfMass + "; mass=" + vehicle.definition.mass + "; equipment mass=" + vehicle.equipment.mass + "; separationLimit=" + vehicle.Body.maxDepenetrationVelocity + "; solver=" + vehicle.Body.solverIterations + "/" + vehicle.Body.solverVelocityIterations + "; collisionMode=" + vehicle.Body.collisionDetectionMode);
                    foreach (var collider in vehicle.GetComponentsInChildren<Collider>(true))
                        writer.WriteLine("  " + ObjectPath(collider.transform) + " " + collider.GetType().Name + " enabled=" + collider.enabled + " trigger=" + collider.isTrigger + " center=" + collider.bounds.center + " size=" + collider.bounds.size);
                    foreach (var wheel in vehicle.wheels) writer.WriteLine("  wheel " + wheel.name + "; radius=" + wheel.radius + "; suspension=" + wheel.suspensionDistance + "; forceAppPoint=" + wheel.forceAppPointDistance + "; spring=" + wheel.suspensionSpring.spring + "; damper=" + wheel.suspensionSpring.damper);
                }
            Cases.Clear();
            for (int vehicle = 0; vehicle < game.Fleet.Length; vehicle++)
            {
                foreach (float speed in new[] { 2f, 4f, 6.5f })
                {
                    Cases.Add(new Case { vehicle = vehicle, name = "clear-road", shape = 0, speed = speed });
                    Cases.Add(new Case { vehicle = vehicle, name = "shallow-road", shape = 1, height = .30f, width = 8, speed = speed });
                    Cases.Add(new Case { vehicle = vehicle, name = "threshold-pile", shape = 2, height = .55f, width = 2, speed = speed });
                    Cases.Add(new Case { vehicle = vehicle, name = "small-solid-pile", shape = 2, height = .65f, width = 2, speed = speed });
                    Cases.Add(new Case { vehicle = vehicle, name = "medium-pile", shape = 2, height = 1.2f, width = 4, speed = speed });
                    Cases.Add(new Case { vehicle = vehicle, name = "tall-narrow-pile", shape = 2, height = 2.4f, width = 4, speed = speed });
                    Cases.Add(new Case { vehicle = vehicle, name = "broad-pile", shape = 2, height = 1.2f, width = 8, speed = speed });
                    Cases.Add(new Case { vehicle = vehicle, name = "continuous-uneven-road", shape = 3, height = 1.5f, width = 8, speed = speed });
                }
                foreach (string isolation in new[] { "normal", "anti-roll-off", "fixed-com", "snow-collision-off" })
                {
                    Cases.Add(new Case { vehicle = vehicle, name = "foundation-edge", foundation = true, isolation = isolation, speed = 6.5f });
                    Cases.Add(new Case { vehicle = vehicle, name = "continuous-uneven-road-isolation", shape = 3, height = 1.5f, width = 8, isolation = isolation, speed = 6.5f });
                }
                if (game.Fleet[vehicle].definition.loader)
                    foreach (string isolation in new[] { "empty-raised", "full-raised", "full-dump", "fixed-com-dump" })
                        Cases.Add(new Case { vehicle = vehicle, name = "stationary-com-" + isolation, stationary = true, full = isolation != "empty-raised", dump = isolation.Contains("dump"), isolation = isolation == "fixed-com-dump" ? "fixed-com" : "normal" });
            }
            if (File.Exists(Folder + "/Targeted.request"))
            {
                File.Delete(Folder + "/Targeted.request");
                Cases.Clear();
                for (int vehicle = 0; vehicle < game.Fleet.Length; vehicle++)
                    foreach (string isolation in new[] { "normal", "anti-roll-off", "fixed-com", "snow-collision-off", "frozen-snow-collision", "chassis-proxy-off", "soft-separation" })
                    {
                        if (isolation != "frozen-snow-collision") Cases.Add(new Case { vehicle = vehicle, name = "foundation-edge-cleared-approach", foundation = true, isolation = isolation, speed = 6.5f });
                        Cases.Add(new Case { vehicle = vehicle, name = "tall-narrow-pile-targeted", shape = 2, height = 2.4f, width = 4, isolation = isolation, speed = 6.5f });
                        Cases.Add(new Case { vehicle = vehicle, name = "shallow-road-targeted", shape = 1, height = .3f, width = 8, isolation = isolation, speed = 2 });
                    }
            }
            if (File.Exists(Folder + "/ProxyOnly.request"))
            {
                File.Delete(Folder + "/ProxyOnly.request");
                Cases.Clear();
                for (int vehicle = 0; vehicle < game.Fleet.Length; vehicle++)
                    foreach (string isolation in new[] { "normal", "chassis-proxy-off" })
                    {
                        Cases.Add(new Case { vehicle = vehicle, name = "foundation-edge-cleared-approach", foundation = true, isolation = isolation, speed = 6.5f });
                        Cases.Add(new Case { vehicle = vehicle, name = "tall-narrow-pile-targeted", shape = 2, height = 2.4f, width = 4, isolation = isolation, speed = 6.5f });
                        Cases.Add(new Case { vehicle = vehicle, name = "shallow-road-targeted", shape = 1, height = .3f, width = 8, isolation = isolation, speed = 2 });
                    }
            }
            if (production)
            {
                Cases.RemoveAll(c => c.isolation != "normal" || c.foundation || c.name.Contains("isolation"));
                // Retain the 48 size/speed passes and normal stationary loader checks.
                for (int vehicle = 0; vehicle < game.Fleet.Length; vehicle++)
                {
                    foreach (float offset in new[] { -.08f, 0, .08f })
                        foreach (bool raised in new[] { false, true })
                            Cases.Add(new Case { vehicle = vehicle, name = "production-foundation-" + (raised ? "raised" : "down"), foundation = true, raised = raised, offset = offset, speed = 6.5f });
                    foreach (float speed in new[] { 2f, 4f, 6.5f }) Cases.Add(new Case { vehicle = vehicle, name = "production-traction-ramp", ramp = true, raised = true, speed = speed });
                    Cases.Add(new Case { vehicle = vehicle, name = "production-coast", speed = 4, coast = true });
                    Cases.Add(new Case { vehicle = vehicle, name = "production-gentle-turn", speed = 4, steer = .12f });
                }
            }
            summary = new StreamWriter(Folder + "/Results.csv") { AutoFlush = true };
            summary.WriteLine(CollisionAuditProbe.Header);
            File.WriteAllText(Folder + "/Plan.txt", Cases.Count + " actual Play-mode trials. Production acceptance=" + production + ". Targets 2/4/6.5 m/s use ordinary pedals, not imposed velocity. Snow uses real volume/visual rebuilds; production fixtures initialize the smooth support before approaching, then allow live updates. All test data are transient.\n");
            Next();
        }
        static void PrepareSnow(Case test)
        {
            game.Snow.DiagnosticFreezeCollision = false;
            var grid = game.Snow.Grid;
            var chunks = new HashSet<int>();
            Vector3 routeStart = FoundationApproach(out var routeRotation);
            Vector3 forward = routeRotation * Vector3.forward, sideways = routeRotation * Vector3.right;
            for (int i = 0; i < grid.depth.Length; i++)
            {
                float x = grid.X(i), z = grid.Z(i), value = depth[i];
                bool lane = Mathf.Abs(x - .25f) <= 5 && z >= -45 && z <= -12;
                Vector3 relative = new Vector3(x, 0, z) - new Vector3(routeStart.x, 0, routeStart.z);
                bool corridor = test.foundation && Mathf.Abs(Vector3.Dot(relative, sideways)) < 3 && Vector3.Dot(relative, forward) > -3 && Vector3.Dot(relative, forward) < 14;
                if (corridor) value = 0;
                if(test.ramp && Mathf.Abs(x-ramp.bounds.center.x)<6 && z>ramp.bounds.min.z-8 && z<ramp.bounds.max.z+5)value=0;
                if (lane)
                {
                    value = 0;
                    if (!test.foundation && !test.stationary)
                    {
                        if (test.shape == 1 && z >= -31 && z <= -19 && Mathf.Abs(x - .25f) < test.width * .5f) value = test.height;
                        if (test.shape == 2)
                        {
                            float radius = test.width * .5f;
                            float distance = Vector2.Distance(new Vector2(x, z), new Vector2(.25f, -27.75f));
                            value = test.height * Mathf.Max(0, 1 - distance / radius);
                        }
                        if (test.shape == 3 && z >= -31 && z <= -19 && Mathf.Abs(x - .25f) < test.width * .5f)
                        {
                            float wave = .5f + .5f * Mathf.Sin((z + 31) * Mathf.PI * .5f);
                            float cross = .8f + .2f * Mathf.Sin(x * 1.4f + z * .7f);
                            value = .25f + (test.height - .25f) * wave * cross;
                        }
                    }
                }
                if (Mathf.Abs(value - grid.depth[i]) > .000001f) chunks.Add((i / SnowVolumeGrid.Size / SnowField.Chunk) * SnowField.Across + i % SnowVolumeGrid.Size / SnowField.Chunk);
                grid.depth[i] = value; grid.credit[i] = credit[i]; grid.blocked[i] = lane ? false : blocked[i];
            }
            grid.WakePiles();
            foreach (int chunk in chunks) Rebuild.Invoke(game.Snow, new object[] { chunk });
            if (production) game.Snow.RefreshSupportForValidation();
            game.Snow.DiagnosticFreezeCollision = test.isolation == "frozen-snow-collision";
            Physics.SyncTransforms();
        }
        static Vector3 FoundationApproach(out Quaternion rotation)
        {
            var b = foundation.bounds;
            var outward = b.size.x < b.size.z ? Vector3.right : Vector3.forward;
            if (Vector3.Dot(b.center, outward) > 0) outward = -outward;
            rotation = Quaternion.LookRotation(-outward);
            return b.center + outward * (Vector3.Dot(b.extents, new Vector3(Mathf.Abs(outward.x), 0, Mathf.Abs(outward.z))) + 9);
        }
        static void Next()
        {
            if (probe)
            {
                RestoreContacts();
                summary.WriteLine(probe.Finish());
                UnityEngine.Object.DestroyImmediate(probe);
                machine.DiagnosticSkipAntiRoll = machine.DiagnosticFreezeCenterOfMass = false;
                foreach (var pair in bodyStates) if (pair.Key) pair.Key.enabled = pair.Value;
                machine.SetActiveMachine(false); machine.RecoverToDepot();
            }
            index++;
            if (index >= Cases.Count)
            {
                Cleanup();
                File.AppendAllText(Folder + "/Summary.txt", "Completed " + Cases.Count + " expanded Play-mode cases. See Results.csv, Contacts.csv and per-trial fixed-step traces.\n");
                complete(); return;
            }
            var test = Current;
            Physics.IgnoreLayerCollision(8, 9, test.isolation == "snow-collision-off" || originalIgnore);
            PrepareSnow(test);
            game.Profile.active = test.vehicle;
            machine = game.Fleet[test.vehicle];
            machine.Condition = 100; machine.Fuel = machine.definition.fuelCapacity; machine.Tyres = machine.Engine = 0;
            machine.Body.maxDepenetrationVelocity = test.isolation == "soft-separation" ? 2 : separationLimits[machine.Body];
            machine.DiagnosticSkipAntiRoll = test.isolation == "anti-roll-off";
            machine.DiagnosticFreezeCenterOfMass = false;
            machine.DiagnosticPeakAntiRoll = 0; machine.DiagnosticMinTravel = float.PositiveInfinity; machine.DiagnosticMaxTravel = float.NegativeInfinity;
            Vector3 position = new Vector3(.25f, 0, -36); Quaternion rotation = Quaternion.identity;
            if (test.foundation)
            {
                position = FoundationApproach(out rotation);
                position += rotation * Vector3.right * test.offset;
            }
            if (test.ramp) position = new Vector3(ramp.bounds.center.x, 0, ramp.bounds.min.z - 5);
            if (!Physics.Raycast(position + Vector3.up * 40, Vector3.down, out var ground, 100, 1 << 0, QueryTriggerInteraction.Ignore)) throw new InvalidOperationException("Audit approach has no ground.");
            position.y = ground.point.y + .08f;
            machine.SetActiveMachine(true); machine.Body.position = position; machine.Body.rotation = rotation; machine.ResetMotion();
            machine.Tool.Load = test.full ? machine.equipment.capacity : 0;
            if (test.stationary || test.raised) machine.Tool.ResetRaised(); else machine.Tool.ResetLowered();
            game.Input.Clear(); game.Input.Brake = 1;
            Physics.SyncTransforms();
            probe = machine.gameObject.AddComponent<CollisionAuditProbe>();
            if (test.isolation == "chassis-proxy-off") probe.BeforePhysics = IgnoreChassisContacts;
            probe.Begin(machine, Folder, index, test.name, test.isolation, test.speed, test.height, test.width, test.foundation ? foundation : null, position);
            probe.Capture = path => ScreenCapture.CaptureScreenshot(path);
            game.CameraRig.ResetView(); started = Time.time; dumped = false;
            File.WriteAllText(Folder + "/Progress.txt", (index + 1) + "/" + Cases.Count + " " + machine.definition.displayName + " " + test.name + " " + test.isolation + " target=" + test.speed);
        }
        public static void Tick()
        {
            float elapsed = Time.time - started;
            var test = Current;
            if (elapsed > (test.stationary ? 7 : test.shape == 3 ? 14 : 12) || (!test.foundation && !test.ramp && !test.stationary && machine.Body.position.z > -14)) { Next(); return; }
            if (elapsed < 1.5f) { game.Input.Throttle = 0; game.Input.Brake = 1; return; }
            machine.DiagnosticFreezeCenterOfMass = test.isolation == "fixed-com";
            if (test.dump && elapsed > 2.5f && !dumped) { machine.Tool.RequestDump(); dumped = true; }
            float forward = Vector3.Dot(machine.Body.linearVelocity, machine.transform.forward);
            game.Input.Throttle = test.stationary ? 0 : Mathf.Clamp01((test.speed - forward) * .9f + .15f);
            game.Input.Brake = test.stationary ? 1 : Mathf.Clamp01((forward - test.speed - .3f) * .6f);
            game.Input.Steer = test.steer;
            if(test.coast && elapsed>5){game.Input.Throttle=0;game.Input.Brake=0;}
        }
        public static void Cleanup()
        {
            RestoreContacts();
            summary?.Dispose(); summary = null;
            Physics.IgnoreLayerCollision(8, 9, originalIgnore);
            if (game && game.Snow) game.Snow.DiagnosticFreezeCollision = false;
            if (bodyStates != null) foreach (var pair in bodyStates) if (pair.Key) pair.Key.enabled = pair.Value;
            if (separationLimits != null) foreach (var pair in separationLimits) if (pair.Key) pair.Key.maxDepenetrationVelocity = pair.Value;
            if (game) foreach (var vehicle in game.Fleet) { vehicle.DiagnosticFreezeCenterOfMass = vehicle.DiagnosticSkipAntiRoll = false; }
        }
        static void IgnoreChassisContacts()
        {
            foreach (var chassis in machine.GetComponentsInChildren<Collider>().Where(c => !(c is WheelCollider)))
                foreach (var other in Physics.OverlapBox(machine.Body.worldCenterOfMass, new Vector3(6, 6, 6), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    if (other.attachedRigidbody != machine.Body && !(other is WheelCollider) && !Physics.GetIgnoreCollision(chassis, other))
                    {
                        Physics.IgnoreCollision(chassis, other, true);
                        ignoredPairs.Add(new KeyValuePair<Collider, Collider>(chassis, other));
                    }
        }
        static void RestoreContacts()
        {
            foreach (var pair in ignoredPairs) if (pair.Key && pair.Value) Physics.IgnoreCollision(pair.Key, pair.Value, false);
            ignoredPairs.Clear();
        }
    }
}
