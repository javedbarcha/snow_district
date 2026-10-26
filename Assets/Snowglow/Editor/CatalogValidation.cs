using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
namespace Snowglow.Editor {
 public sealed class CatalogReport {
  public readonly List<string> errors=new List<string>(),warnings=new List<string>();
  public bool Ok=>errors.Count==0;
  public override string ToString(){return string.Join("\n",errors.Select(e=>"ERROR: "+e).Concat(warnings.Select(w=>"REVIEW: "+w)).DefaultIfEmpty("Structural checks passed. Visual/handling/device review is separate."));}
 }
 public static class CatalogValidation {
  static string VectorKey(Vector3 v)=>v.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+v.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+v.z.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
  public static string FitFingerprint(FittingProfile f)=>Hash128.Compute((f.vehicle?f.vehicle.id:"")+"|"+(f.part?f.part.id:"")+"|"+f.socketId+"|"+VectorKey(f.positionOffset)+"|"+VectorKey(f.rotationOffset)).ToString();
  public static string Fingerprint(CatalogAsset a){
   if(!a||!a.prefab)return "";
   string policy=JsonUtility.ToJson(a.localBounds)+"|"+a.interfaceId+"|"+a.kind+"|"+a.blocksSnow+"|"+a.minimumScale.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+a.maximumScale.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+a.addedMass.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+a.wheelRadius.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+a.wheelWidth.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+a.wheelMass.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+a.torqueMultiplier.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+a.gripMultiplier.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
   return Hash128.Compute(AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(a.prefab))+"|"+(a.vehicle?JsonUtility.ToJson(a.vehicle):"")+"|"+(a.attachment?JsonUtility.ToJson(a.attachment):"")+"|"+policy+"|"+a.mirrorRightWheels).ToString();
  }
  public static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
  public static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
  public static Bounds BoundsOf(GameObject root){
   bool found=false;var result=new Bounds(Vector3.zero,Vector3.zero);
   foreach(var r in root.GetComponentsInChildren<Renderer>(true)){
    if(r is ParticleSystemRenderer)continue;
    var b=r.localBounds;for(int n=0;n<8;n++){
     var p=root.transform.InverseTransformPoint(r.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((n&1)==0?-1:1,(n&2)==0?-1:1,(n&4)==0?-1:1))));
     if(!found){result=new Bounds(p,Vector3.zero);found=true;}else result.Encapsulate(p);
    }
   }return result;
  }
  public static CatalogReport Check(CatalogAsset a,bool requireReady=false){
   var r=new CatalogReport();if(!a){r.errors.Add("Missing catalog entry.");return r;}
   if(string.IsNullOrEmpty(a.id)||!Regex.IsMatch(a.id,"^[a-z0-9][a-z0-9._-]{2,80}$"))r.errors.Add("ID must use 3-81 lowercase letters, digits, dots, underscores or hyphens.");
   if(!a.prefab||!AssetDatabase.GetAssetPath(a.prefab).EndsWith(".prefab",StringComparison.OrdinalIgnoreCase)){r.errors.Add("Assign a prepared .prefab, not the raw model.");return r;}
   if(requireReady&&(a.status!=PreparationStatus.Ready||!a.visualReviewed))r.errors.Add("Entry has not been reviewed and published Ready.");
   if(requireReady&&a.reviewedPrefabHash!=Fingerprint(a))r.errors.Add("Prefab/physics/constraints changed since approval; revalidate and publish again.");
   if(a.prefab.transform.localScale!=Vector3.one)r.errors.Add("Prepared root scale must be (1,1,1); normalize the visual child instead.");
   var bounds=BoundsOf(a.prefab);if(!Finite(bounds.size)||bounds.size.sqrMagnitude<.0001f)r.errors.Add("Renderable geometry has missing/invalid dimensions.");
   if(requireReady&&(!Finite(a.localBounds.center)||!Finite(a.localBounds.size)||(bounds.center-a.localBounds.center).sqrMagnitude>.000001f||(bounds.size-a.localBounds.size).sqrMagnitude>.000001f))r.errors.Add("Stored footprint is invalid or differs from the prepared prefab; remeasure and publish.");
   if(a.minimumScale<=0||a.maximumScale<a.minimumScale||!Finite(a.minimumScale)||!Finite(a.maximumScale))r.errors.Add("Invalid allowed placement scale range.");
   if(a.price<0||a.addedMass<0||a.torqueMultiplier<=0||a.gripMultiplier<=0||!Finite(a.addedMass)||!Finite(a.torqueMultiplier)||!Finite(a.gripMultiplier))r.errors.Add("Price/mass/modifiers are invalid.");
   if(a.prefab.GetComponentsInChildren<Camera>(true).Length>0||a.prefab.GetComponentsInChildren<AudioListener>(true).Length>0)r.errors.Add("Remove imported cameras/audio listeners from catalog building blocks.");
   foreach(var t in a.prefab.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)r.errors.Add("Missing script on "+t.name);
   foreach(var renderer in a.prefab.GetComponentsInChildren<Renderer>(true))foreach(var mat in renderer.sharedMaterials)if(!mat||!mat.shader||mat.shader.name=="Hidden/InternalErrorShader")r.errors.Add("Missing material/shader on "+renderer.name);
   var anchors=a.prefab.GetComponentsInChildren<AssetAnchor>(true);
   if(anchors.Any(x=>string.IsNullOrWhiteSpace(x.id))||anchors.GroupBy(x=>x.id).Any(g=>g.Count()>1))r.errors.Add("Anchor IDs must be nonempty and unique within a prefab.");
   var sockets=a.prefab.GetComponentsInChildren<AssetSocket>(true);
   if(sockets.Any(x=>string.IsNullOrWhiteSpace(x.id))||sockets.GroupBy(x=>x.id).Any(g=>g.Count()>1))r.errors.Add("Socket IDs must be nonempty and unique.");
   foreach(var s in sockets)if(string.IsNullOrWhiteSpace(s.interfaceId)||s.maximumMass<0||s.maximumWidth<=0||!Finite(s.minimumAngles)||!Finite(s.maximumAngles)||s.minimumAngles.x>s.maximumAngles.x||s.minimumAngles.y>s.maximumAngles.y||s.minimumAngles.z>s.maximumAngles.z)r.errors.Add("Invalid socket limits on "+s.id);
   if(a.blocksSnow&&!a.prefab.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled&&!c.isTrigger))r.errors.Add("Snow-blocking asset needs a non-trigger collision proxy.");
   if(a.kind==CatalogKind.Vehicle){
    if(!a.vehicle||a.vehicle.mass<=0||a.vehicle.wheelRadius<=0)r.errors.Add("Valid vehicle physics definition is required.");
    var rig=a.prefab.GetComponent<PreparedVehicleRig>();
    if(!rig||rig.wheels.Length!=4||rig.wheelVisuals.Length!=4||rig.wheels.Any(w=>!w)||rig.wheelVisuals.Any(w=>!w))r.errors.Add("Map four WheelColliders and four wheel visual anchors in PreparedVehicleRig.");
    else if(rig.wheelVisuals.Any(w=>w.GetComponentsInChildren<Renderer>(true).Length==0))r.errors.Add("Each wheel visual anchor needs separate renderable geometry; split fused wheels before publishing.");
    if(!a.prefab.GetComponent<Rigidbody>())r.errors.Add("Vehicle needs a Rigidbody.");
    var m=a.prefab.GetComponent<MachineController>();if(m&&(!m.equipment||!m.Tool||!m.Tool.mount||!m.Tool.mouth||!m.Tool.spray||m.wheels.Length!=4||m.wheelVisuals.Length!=4))r.errors.Add("Assembled vehicle has incomplete gameplay bindings.");
    if(a.vehicle&&a.vehicle.loader&&(!rig||!rig.boom))r.errors.Add("Map the loader boom pivot; automatic inference is not enough for publishing.");
   }
   if(a.kind==CatalogKind.Attachment){
    if(!a.attachment||a.attachment.mass<=0||a.attachment.width<=0)r.errors.Add("Attachment definition with dimensions and mass is required.");
    if(!anchors.Any(x=>x.role==AnchorRole.Connection)||!anchors.Any(x=>x.role==AnchorRole.WorkingEdge))r.errors.Add("Attachment needs connection and working-edge anchors.");
    if(a.attachment&&a.attachment.kind==AttachmentKind.Bucket&&(!anchors.Any(x=>x.role==AnchorRole.LoadVisual)||a.attachment.capacity<=0))r.errors.Add("Bucket needs positive capacity and a preauthored load visual anchor.");
   }
   if(a.kind==CatalogKind.WheelSet&&(a.wheelRadius<=0||a.wheelWidth<=0||a.wheelMass<=0||string.IsNullOrWhiteSpace(a.interfaceId)))r.errors.Add("Wheel radius, width, mass and interface ID are required.");
   if(a.kind==CatalogKind.RoadAssembly&&anchors.Count(x=>x.role==AnchorRole.RoadConnection)<2)r.errors.Add("Road assembly requires at least two connection anchors.");
   if(a.triangles>50000||a.materialSlots>12)r.warnings.Add("High geometry/material count for the mobile reference target; profile on Galaxy A07.");
   if(!a.visualReviewed)r.warnings.Add("Scale, axes, collision proxy and moving-part alignment require visual approval.");
   return r;
  }
  public static CatalogReport CheckFit(FittingProfile f,bool reviewed=true){
   var r=new CatalogReport();if(!f||!f.vehicle||!f.part){r.errors.Add("Select vehicle and part.");return r;}
   r.errors.AddRange(Check(f.vehicle,reviewed).errors);r.errors.AddRange(Check(f.part,reviewed).errors);
   var slot=f.vehicle.prefab?f.vehicle.prefab.GetComponentsInChildren<AssetSocket>(true).FirstOrDefault(s=>s.id==f.socketId):null;
   if(!slot){r.errors.Add("Vehicle has no socket "+f.socketId);return r;}
   var part=f.part;
   if(slot.accepts!=part.kind)r.errors.Add("Socket accepts "+slot.accepts+", not "+part.kind);
   string mount=part.attachment?part.attachment.mountId:part.interfaceId;
   if(string.IsNullOrWhiteSpace(mount)||mount!=slot.interfaceId)r.errors.Add("Mount interfaces differ.");
   float mass=part.attachment?part.attachment.mass+(part.attachment.kind==AttachmentKind.Bucket?part.attachment.capacity*250:0):part.kind==CatalogKind.WheelSet?part.wheelMass*4:part.addedMass;
   float width=part.attachment?part.attachment.width:part.kind==CatalogKind.WheelSet?part.wheelWidth:part.localBounds.size.x;
   if(mass>slot.maximumMass)r.errors.Add("Full operating mass exceeds socket rating.");
   if(width>slot.maximumWidth)r.errors.Add("Part exceeds socket width.");
   if(part.attachment){if(part.attachment.requiresHydraulics&&!slot.hydraulics)r.errors.Add("Hydraulics required.");string e=part.attachment.Validate(f.vehicle.vehicle);if(e!=null)r.errors.Add(e);if(Mathf.Abs(part.localBounds.size.x-part.attachment.width)>Mathf.Max(.25f,part.attachment.width*.15f))r.errors.Add("Visual width and working width differ; correct scale or attachment dimensions.");}
   if(part.kind==CatalogKind.WheelSet){var rig=f.vehicle.prefab.GetComponent<PreparedVehicleRig>();if(rig&&(part.wheelRadius>rig.maximumWheelRadius||part.wheelWidth>rig.maximumWheelWidth))r.errors.Add("Wheel set exceeds authored clearance envelope.");}
   if(!Finite(f.positionOffset)||!Finite(f.rotationOffset))r.errors.Add("Non-finite fitting transform.");
   if(f.rotationOffset.x<slot.minimumAngles.x||f.rotationOffset.y<slot.minimumAngles.y||f.rotationOffset.z<slot.minimumAngles.z||f.rotationOffset.x>slot.maximumAngles.x||f.rotationOffset.y>slot.maximumAngles.y||f.rotationOffset.z>slot.maximumAngles.z)r.errors.Add("Fitting rotation exceeds socket movement envelope.");
   if(reviewed&&!f.fitReviewed)r.errors.Add("Fitting must be reviewed in the assembly preview.");
   if(reviewed&&f.fitReviewed&&(f.reviewedVehicleHash!=f.vehicle.reviewedPrefabHash||f.reviewedPartHash!=f.part.reviewedPrefabHash))r.errors.Add("Vehicle/part changed since fitting approval; review the fitting again.");
   if(reviewed&&f.fitReviewed&&f.reviewedFitHash!=FitFingerprint(f))r.errors.Add("Fitting transform/socket changed since approval; review it again.");
   return r;
  }
 }
}
