using UnityEngine;
namespace Snowglow {
[CreateAssetMenu(menuName="Snowglow/Vehicle")]
public sealed class VehicleDefinition : ScriptableObject {
 public string id, displayName, mountId; public bool loader;
 public float mass=2800,wheelRadius=.58f,wheelbase=3,track=1.95f,torque=1900,brakeTorque=7000,maxSpeed=12,steering=32;
 public float fuelCapacity=70,maxAttachmentMass=900,maxWorkingWidth=4; public bool hydraulics=true; public int price=1400;
}}
