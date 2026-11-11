#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
namespace Snowglow.Editor
{
    public sealed class CollisionPlayProbe : MonoBehaviour
    {
        public System.Action<string> Capture;
        MachineController machine;
        Collider target;
        string folder;
        int trial, repeat, contacts, targetContacts, airborne;
        float baseline, maxHeight, maxTilt, maxSpin, maxUp, maxImpact, duration, airborneDuration, maxAirborne;
        bool captured;
        string lastHit = "none";
        public void Begin(MachineController owner, Collider obstacle, string destination, int index, int repetition, float height)
        { machine = owner; target = obstacle; folder = destination; trial = index; repeat = repetition; baseline = height; }
        void FixedUpdate()
        {
            if (!machine) return;
            duration += Time.fixedDeltaTime;
            if (duration < 1.5f) return;
            maxHeight = Mathf.Max(maxHeight, machine.Body.position.y - baseline);
            var tilt = Vector3.Angle(transform.up, Vector3.up);
            maxTilt = Mathf.Max(maxTilt, tilt);
            maxSpin = Mathf.Max(maxSpin, machine.Body.angularVelocity.magnitude);
            maxUp = Mathf.Max(maxUp, machine.Body.linearVelocity.y);
            bool flying = machine.wheels.All(w => !w.isGrounded);
            if (flying) { airborne++; airborneDuration += Time.fixedDeltaTime; maxAirborne = Mathf.Max(maxAirborne, airborneDuration); }
            else airborneDuration = 0;
            if (!captured && (tilt > 25 || machine.Body.position.y - baseline > .4f))
            { Capture?.Invoke(folder + "/Trial-" + trial + "-abnormal.png"); captured = true; }
        }
        void OnCollisionEnter(Collision collision)
        {
            contacts++; if (collision.collider == target) targetContacts++;
            if (collision.relativeVelocity.magnitude > maxImpact)
            { maxImpact = collision.relativeVelocity.magnitude; lastHit = collision.collider.name; }
        }
        public string Report() => $"Trial {trial}: {machine.definition.displayName}; target={target.name} at {target.bounds.center}; repeat={repeat}; tool={(repeat == 2 ? "raised" : "down")}; contacts={contacts}; intendedTargetContacts={targetContacts}; strongestHit={lastHit}; impact={maxImpact:F2}m/s; rise={maxHeight:F2}m; tilt={maxTilt:F1}deg; angularSpeed={maxSpin:F2}rad/s; upwardSpeed={maxUp:F2}m/s; airborne={airborne * Time.fixedDeltaTime:F2}s; longestAirborne={maxAirborne:F2}s";
    }
}

#endif
