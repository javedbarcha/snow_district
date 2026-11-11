#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Snowglow.Editor
{
    [DefaultExecutionOrder(10000)]
    public sealed class CollisionAuditProbe : MonoBehaviour
    {
        public Action<string> Capture;
        public Action BeforePhysics;
        public const string Header = "trial,vehicle,case,isolation,targetSpeed,pileHeight,pileWidth,maxSpeed,maxRise,maxTilt,maxAngular,maxUp,totalAirborne,longestAirborne,peakAntiRoll,minTravel,maxTravel,maxCOMStep,maxCOMShift,maxMassStep,maxSnowResistance,maxContactImpulseY,contacts,targetContacts,snowWheelSamples,edgeWheelSamples,minGrounded,maxTravelled,loadStart,loadEnd,initialInertia,finalInertia";
        MachineController machine;
        Collider target;
        StreamWriter trace, contactsFile;
        string folder, scenarioName, isolation;
        int trial, contacts, targetContacts, snowHits, edgeHits, minGrounded = 4;
        float targetSpeed, height, width, duration, maxSpeed, maxRise, maxTilt, maxSpin, maxUp, airborne, airRun, maxAirRun, maxCOMStep, maxCOMShift, maxMassStep, maxSnow, maxImpulse, maxTravelled, startLoad;
        Vector3 start, com, startCOM, inertia;
        float mass;
        bool first = true, captured;
        static string F(float x) => x.ToString("G7", CultureInfo.InvariantCulture);
        static string Q(string x) => "\"" + x.Replace("\"", "\"\"") + "\"";
        static string PathOf(Transform item) => item.parent ? PathOf(item.parent) + "/" + item.name : item.name;
        public void Begin(MachineController owner, string destination, int index, string scenario, string comparison, float speed, float h, float w, Collider obstacle, Vector3 position)
        {
            machine = owner; folder = destination; trial = index; scenarioName = scenario; isolation = comparison; targetSpeed = speed; height = h; width = w; target = obstacle; start = position; startLoad = owner.Tool.Load;
            trace = new StreamWriter(folder + "/Trace-" + index.ToString("D2") + ".csv");
            trace.WriteLine("time,speed,y,tilt,up,spin,grounded,comX,comY,comZ,mass,load,snowResistance,antiRollPeak,travelMin,travelMax,wheelContacts");
            contactsFile = new StreamWriter(folder + "/Contacts-" + index.ToString("D2") + ".csv");
            contactsFile.WriteLine("time,path,layer,speed,impulseY,normalMinY,normalMaxY,phase");
        }
        void FixedUpdate()
        {
            if (!machine) return;
            BeforePhysics?.Invoke();
            duration += Time.fixedDeltaTime;
            if (duration < 1.5f) return;
            var body = machine.Body;
            if (first) { com = startCOM = body.centerOfMass; mass = body.mass; inertia = body.inertiaTensor; first = false; }
            float tilt = Vector3.Angle(transform.up, Vector3.up);
            maxSpeed = Mathf.Max(maxSpeed, body.linearVelocity.magnitude); maxRise = Mathf.Max(maxRise, body.position.y - start.y);
            maxTilt = Mathf.Max(maxTilt, tilt); maxSpin = Mathf.Max(maxSpin, body.angularVelocity.magnitude); maxUp = Mathf.Max(maxUp, body.linearVelocity.y);
            maxCOMStep = Mathf.Max(maxCOMStep, Vector3.Distance(com, body.centerOfMass)); maxCOMShift = Mathf.Max(maxCOMShift, Vector3.Distance(startCOM, body.centerOfMass));
            maxMassStep = Mathf.Max(maxMassStep, Mathf.Abs(body.mass - mass)); com = body.centerOfMass; mass = body.mass;
            maxSnow = Mathf.Max(maxSnow, machine.LastSnowForce); maxTravelled = Mathf.Max(maxTravelled, Vector3.ProjectOnPlane(body.position - start, Vector3.up).magnitude);
            int grounded = 0; var wheelContacts = new List<string>();
            foreach (var wheel in machine.wheels)
                if (wheel.GetGroundHit(out var hit))
                {
                    grounded++;
                    if (hit.collider.gameObject.layer == 9) snowHits++;
                    if (target && hit.collider == target) edgeHits++;
                    float travel = (-wheel.transform.InverseTransformPoint(hit.point).y - wheel.radius) / wheel.suspensionDistance;
                    wheelContacts.Add(wheel.name + "=" + PathOf(hit.collider.transform) + ";layer=" + hit.collider.gameObject.layer + ";y=" + F(hit.point.y) + ";normalY=" + F(hit.normal.y) + ";travel=" + F(travel) + ";force=" + F(hit.force));
                }
            minGrounded = Mathf.Min(minGrounded, grounded);
            if (grounded == 0) { airborne += Time.fixedDeltaTime; airRun += Time.fixedDeltaTime; maxAirRun = Mathf.Max(maxAirRun, airRun); } else airRun = 0;
            trace.WriteLine(string.Join(",", F(duration), F(body.linearVelocity.magnitude), F(body.position.y), F(tilt), F(body.linearVelocity.y), F(body.angularVelocity.magnitude), grounded,
                F(com.x), F(com.y), F(com.z), F(body.mass), F(machine.Tool.Load), F(machine.LastSnowForce), F(machine.DiagnosticPeakAntiRoll), F(machine.DiagnosticMinTravel), F(machine.DiagnosticMaxTravel), Q(string.Join(" | ", wheelContacts))));
            if (!captured && (maxRise > .6f || tilt > 35)) { Capture?.Invoke(folder + "/Trial-" + trial + "-abnormal.png"); captured = true; }
        }
        void Contact(Collision collision, string phase)
        {
            if (!machine || duration < 1.5f) return;
            if (phase == "enter") { contacts++; if (collision.collider == target) targetContacts++; }
            maxImpulse = Mathf.Max(maxImpulse, Mathf.Abs(collision.impulse.y));
            var points = collision.contacts;
            contactsFile.WriteLine(string.Join(",", F(duration), Q(PathOf(collision.collider.transform)), collision.collider.gameObject.layer, F(collision.relativeVelocity.magnitude), F(collision.impulse.y),
                points.Length > 0 ? F(points.Min(p => p.normal.y)) : "n/a", points.Length > 0 ? F(points.Max(p => p.normal.y)) : "n/a", phase));
        }
        void OnCollisionEnter(Collision collision) => Contact(collision, "enter");
        void OnCollisionStay(Collision collision) => Contact(collision, "stay");
        public string Finish()
        {
            trace?.Dispose(); contactsFile?.Dispose();
            return string.Join(",", trial, Q(machine.definition.displayName), Q(scenarioName), Q(isolation), F(targetSpeed), F(height), F(width), F(maxSpeed), F(maxRise), F(maxTilt), F(maxSpin), F(maxUp), F(airborne), F(maxAirRun),
                F(machine.DiagnosticPeakAntiRoll), F(machine.DiagnosticMinTravel), F(machine.DiagnosticMaxTravel), F(maxCOMStep), F(maxCOMShift), F(maxMassStep), F(maxSnow), F(maxImpulse), contacts, targetContacts, snowHits, edgeHits, minGrounded, F(maxTravelled), F(startLoad), F(machine.Tool.Load), Q(inertia.ToString("F3")), Q(machine.Body.inertiaTensor.ToString("F3")));
        }
        void OnDestroy() { trace?.Dispose(); contactsFile?.Dispose(); }
    }
}
#endif
