using UnityEngine;
namespace Snowglow {
public enum AttachmentKind { Blade, Bucket, Blower }
[CreateAssetMenu(menuName="Snowglow/Attachment")]
public sealed class AttachmentDefinition : ScriptableObject {
 public string id,displayName,mountId; public AttachmentKind kind;
 public float mass=350,width=2.6f,capacity=1.6f;public bool requiresHydraulics=true;
 public string Validate(VehicleDefinition v) {
  if(v==null)return "No machine selected.";
  if(mountId!=v.mountId)return "Mount interface does not match.";
  if(kind==AttachmentKind.Blower)return "Snowblower behavior is reserved for a future implementation.";
  if(mass+(kind==AttachmentKind.Bucket?capacity*250:0)>v.maxAttachmentMass)return "Attachment exceeds rated mass.";
  if(width>v.maxWorkingWidth)return "Attachment exceeds clearance rating.";
  if(requiresHydraulics&&!v.hydraulics)return "Hydraulic supply required.";
  if((kind==AttachmentKind.Bucket)!=v.loader)return "Equipment requires a different machine rig.";
  return null;
 }
}}
