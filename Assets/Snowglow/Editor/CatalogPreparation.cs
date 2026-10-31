using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Snowglow.Editor {
 public static class CatalogPreparation {
  public const string Root="Assets/Snowglow/Catalog",LibraryPath=Root+"/SnowglowCatalog.asset";
  public static void EnsureFolders(){Directory.CreateDirectory(Root+"/Entries");Directory.CreateDirectory(Root+"/Prefabs");Directory.CreateDirectory(Root+"/Recipes");Directory.CreateDirectory(Root+"/Sources");AssetDatabase.Refresh();}
  public static AssetCatalog Library(){var c=AssetDatabase.LoadAssetAtPath<AssetCatalog>(LibraryPath);if(c)return c;EnsureFolders();c=ScriptableObject.CreateInstance<AssetCatalog>();AssetDatabase.CreateAsset(c,LibraryPath);return c;}
  public static CatalogAsset Prepare(GameObject source,CatalogKind kind,string title,float scale,Vector3 rotation,AssetSource sourceType,string notes,CatalogAsset draft=null){
   if(kind==CatalogKind.UI)throw new InvalidOperationException("Use the Photoshop UI intake for sprite-based Canvas assets.");
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Asset preparation is Editor-only. Stop Play first.");
   if(!source||!CatalogValidation.Finite(scale)||scale<=0||!CatalogValidation.Finite(rotation))throw new ArgumentException("Provide a model and a positive finite uniform scale.");
   EnsureFolders();if(draft&&(draft.status!=PreparationStatus.Draft||draft.prefab))throw new InvalidOperationException("Only an unprepared Draft entry can be completed in place.");var a=draft?draft:ScriptableObject.CreateInstance<CatalogAsset>();if(!draft)a.id="asset-"+Guid.NewGuid().ToString("N");a.name=a.displayName=string.IsNullOrWhiteSpace(title)?source.name:title;a.kind=kind;a.status=PreparationStatus.NeedsReview;
   a.sourceType=sourceType;a.sourcePath=AssetDatabase.GetAssetPath(source);a.sourceGuid=AssetDatabase.AssetPathToGUID(a.sourcePath);a.provenance=notes;
   a.assumptions="Metres, Y-up, Z-forward. Scale/orientation and generated collision/rig are proposals; review before publishing. Original source is unchanged.";
   a.blocksSnow=kind>=CatalogKind.BuildingAssembly;
   var root=new GameObject(a.displayName);root.AddComponent<CatalogPlacement>().assetId=a.id;try{
    var visual=new GameObject("VisualRoot").transform;visual.SetParent(root.transform,false);
    var model=UnityEngine.Object.Instantiate(source,visual);model.name=source.name;model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.Euler(rotation);model.transform.localScale=source.transform.localScale*scale;
    foreach(var c in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
    foreach(var rb in model.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(rb);
    if(model.GetComponentsInChildren<MonoBehaviour>(true).Any())throw new InvalidOperationException("Raw intake expects a visual model without gameplay scripts. Register an existing game prefab through the catalog instead.");
    ConvertMaterials(root,a);
    var b=CatalogValidation.BoundsOf(root);if(b.size.sqrMagnitude<.0001f)throw new InvalidOperationException("No renderable geometry found.");
    visual.localPosition-=kind==CatalogKind.WheelSet?b.center:new Vector3(b.center.x,b.min.y,b.center.z);
    b=CatalogValidation.BoundsOf(root);
    if(kind==CatalogKind.Vehicle){CreateVehicleRig(root,visual,b,a);}
    else if(a.blocksSnow){var proxy=new GameObject("CollisionProxy | review footprint");proxy.transform.SetParent(root.transform,false);var box=proxy.AddComponent<BoxCollider>();box.center=b.center;box.size=b.size;}
    if(kind==CatalogKind.Attachment){
     a.interfaceId="UNCONFIGURED";a.attachment=ScriptableObject.CreateInstance<AttachmentDefinition>();a.attachment.id=a.id+"-equipment";a.attachment.displayName=a.displayName;a.attachment.mountId=a.interfaceId;a.attachment.width=b.size.x;
     AssetDatabase.CreateAsset(a.attachment,Root+"/Entries/"+a.id+"-equipment.asset");
     Anchor(root.transform,"connection",AnchorRole.Connection,Vector3.zero);Anchor(root.transform,"working-edge",AnchorRole.WorkingEdge,new Vector3(0,b.min.y,b.max.z));
    }else if(kind==CatalogKind.WheelSet){a.interfaceId="wheel-standard";a.wheelRadius=Mathf.Max(b.size.y,b.size.z)*.5f;a.wheelWidth=b.size.x;Anchor(root.transform,"connection",AnchorRole.Connection,Vector3.zero);}
    else if(kind==CatalogKind.Accessory){a.interfaceId="accessory-small";Anchor(root.transform,"connection",AnchorRole.Connection,Vector3.zero);}
    else if(kind==CatalogKind.RoadAssembly){Anchor(root.transform,"road-a",AnchorRole.RoadConnection,new Vector3(0,b.max.y,b.min.z)).interfaceId="road-"+b.size.x.ToString("F1",System.Globalization.CultureInfo.InvariantCulture);var end=Anchor(root.transform,"road-b",AnchorRole.RoadConnection,new Vector3(0,b.max.y,b.max.z));end.interfaceId=root.GetComponentsInChildren<AssetAnchor>()[0].interfaceId;end.transform.localRotation=Quaternion.Euler(0,180,0);}
    a.prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+a.id+".prefab");
    RecordMetrics(a);if(!AssetDatabase.Contains(a))AssetDatabase.CreateAsset(a,Root+"/Entries/"+a.id+".asset");var library=Library();if(!library.entries.Contains(a))library.entries.Add(a);EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();return a;
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  public static AssetAnchor Anchor(Transform parent,string id,AnchorRole role,Vector3 local){var go=new GameObject("Anchor | "+id);go.transform.SetParent(parent,false);go.transform.localPosition=local;var a=go.AddComponent<AssetAnchor>();a.id=id;a.role=role;return a;}
  static void ConvertMaterials(GameObject root,CatalogAsset asset){
   var copies=new System.Collections.Generic.Dictionary<Material,Material>();
   foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){
    var materials=renderer.sharedMaterials;
    for(int n=0;n<materials.Length;n++){
     var original=materials[n];if(!original||!original.shader||!(original.shader.name=="Standard"||original.shader.name.StartsWith("Legacy Shaders/",StringComparison.Ordinal)))continue;
     if(!copies.TryGetValue(original,out var converted)){
      var shader=Shader.Find("Universal Render Pipeline/Simple Lit");if(!shader)throw new InvalidOperationException("URP Simple Lit is unavailable.");
      converted=new Material(shader){name=original.name+" | prepared URP",enableInstancing=true};
      Color color=original.HasProperty("_Color")?original.GetColor("_Color"):Color.white;converted.SetColor("_BaseColor",color);if(original.mainTexture)converted.SetTexture("_BaseMap",original.mainTexture);
      if(original.HasProperty("_BumpMap")&&original.GetTexture("_BumpMap")){converted.SetTexture("_BumpMap",original.GetTexture("_BumpMap"));converted.EnableKeyword("_NORMALMAP");}
      if(color.a<.99f||original.renderQueue>=3000){converted.SetFloat("_Surface",1);converted.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);converted.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);converted.SetFloat("_ZWrite",0);converted.SetOverrideTag("RenderType","Transparent");converted.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");converted.renderQueue=3000;}
      AssetDatabase.CreateAsset(converted,AssetDatabase.GenerateUniqueAssetPath(Root+"/Entries/"+asset.id+"-material.asset"));copies.Add(original,converted);
     }materials[n]=converted;
    }renderer.sharedMaterials=materials;
   }
   if(copies.Count>0)asset.assumptions+=" Standard/legacy materials were copied to URP Simple Lit; inspect texture, transparency and shading fidelity.";
  }
  static AssetSocket Socket(Transform root,string id,string mount,CatalogKind accepts,Vector3 at){var go=new GameObject("Socket | "+id);go.transform.SetParent(root,false);go.transform.localPosition=at;var s=go.AddComponent<AssetSocket>();s.id=id;s.interfaceId=mount;s.accepts=accepts;return s;}
  static void CreateVehicleRig(GameObject root,Transform visual,Bounds b,CatalogAsset a){
   root.layer=8;root.AddComponent<Rigidbody>().mass=2800;var rig=root.AddComponent<PreparedVehicleRig>();rig.visualRoot=visual;
   var def=ScriptableObject.CreateInstance<VehicleDefinition>();def.id=a.id;def.displayName=a.displayName;def.track=Mathf.Max(.8f,b.size.x*.85f);def.wheelbase=Mathf.Max(1,b.size.z*.6f);def.wheelRadius=Mathf.Max(.2f,b.size.y*.18f);def.mountId=a.id+"-front";a.vehicle=def;AssetDatabase.CreateAsset(def,Root+"/Entries/"+a.id+"-vehicle.asset");
   var proxy=new GameObject("Chassis proxy | adjust around body");proxy.layer=8;proxy.transform.SetParent(root.transform,false);var box=proxy.AddComponent<BoxCollider>();box.center=b.center;box.size=new Vector3(b.size.x*.8f,Mathf.Max(.2f,b.size.y*.55f),b.size.z*.8f);
   string[] names={"wheel_fl","wheel_fr","wheel_rl","wheel_rr"};
   for(int i=0;i<4;i++){
    var candidate=visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.Replace(" ","_").Equals(names[i],StringComparison.OrdinalIgnoreCase));
    Vector3 at=candidate?root.transform.InverseTransformPoint(candidate.position):new Vector3((i%2==0?-1:1)*def.track*.5f,def.wheelRadius,i<2?def.wheelbase*.5f:-def.wheelbase*.5f);
    var wheel=new GameObject("Physics | "+names[i]);wheel.layer=8;wheel.transform.SetParent(root.transform,false);wheel.transform.localPosition=at;var wc=wheel.AddComponent<WheelCollider>();wc.radius=def.wheelRadius;wc.suspensionDistance=.32f;wc.mass=45;var spring=wc.suspensionSpring;spring.spring=def.mass*9.81f/4/.16f;spring.damper=3500;wc.suspensionSpring=spring;rig.wheels[i]=wc;
    var pivot=new GameObject("Visual | "+names[i]).transform;pivot.SetParent(root.transform,false);pivot.localPosition=at;rig.wheelVisuals[i]=pivot;if(candidate)candidate.SetParent(pivot,true);
   }
   rig.maximumWheelRadius=def.wheelRadius*1.2f;rig.maximumWheelWidth=b.size.x*.3f;
   Socket(root.transform,"front",def.mountId,CatalogKind.Attachment,new Vector3(0,def.wheelRadius,b.max.z));
   Socket(root.transform,"wheels","wheel-standard",CatalogKind.WheelSet,Vector3.zero).maximumWidth=2;
   Socket(root.transform,"roof","accessory-small",CatalogKind.Accessory,new Vector3(0,b.max.y,0));
   a.assumptions+=" Wheel centres/radius, mass and suspension are provisional. Separate wheel_fl/fr/rl/rr visuals or bind them manually. Loader boom must be assigned manually.";
  }
  public static void RecordMetrics(CatalogAsset a){
   a.localBounds=CatalogValidation.BoundsOf(a.prefab);a.triangles=a.renderers=a.materialSlots=0;
   foreach(var r in a.prefab.GetComponentsInChildren<Renderer>(true)){a.renderers++;a.materialSlots+=r.sharedMaterials.Length;Mesh mesh=null;if(r is SkinnedMeshRenderer skin)mesh=skin.sharedMesh;else {var filter=r.GetComponent<MeshFilter>();if(filter)mesh=filter.sharedMesh;}if(mesh)for(int n=0;n<mesh.subMeshCount;n++)a.triangles+=(int)mesh.GetIndexCount(n)/3;}
   EditorUtility.SetDirty(a);
  }
  public static CatalogAsset Draft(string title,CatalogKind kind,AssetSource source,UnityEngine.Object document,string notes){EnsureFolders();var a=ScriptableObject.CreateInstance<CatalogAsset>();a.id="asset-"+Guid.NewGuid().ToString("N");a.displayName=string.IsNullOrWhiteSpace(title)?"New asset draft":title;a.kind=kind;a.sourceType=source;a.creationSourcePath=AssetDatabase.GetAssetPath(document);a.provenance=notes;AssetDatabase.CreateAsset(a,Root+"/Entries/"+a.id+".asset");var library=Library();library.entries.Add(a);EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();return a;}
  public static void Publish(CatalogAsset a){RecordMetrics(a);var report=CatalogValidation.Check(a);if(Library().entries.Any(other=>other&&other!=a&&other.id==a.id))report.errors.Add("Catalog ID is already in use.");if(!a.visualReviewed)report.errors.Add("Confirm visual review in the entry inspector.");if(!report.Ok)throw new InvalidOperationException(report.ToString());a.status=PreparationStatus.Ready;a.reviewedPrefabHash=CatalogValidation.Fingerprint(a);EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();}
  public static void ApproveFitting(FittingProfile f){var report=CatalogValidation.CheckFit(f,false);if(f){report.errors.AddRange(CatalogValidation.Check(f.vehicle,true).errors);report.errors.AddRange(CatalogValidation.Check(f.part,true).errors);}if(!report.Ok)throw new InvalidOperationException(report.ToString());f.fitReviewed=true;f.reviewedVehicleHash=f.vehicle.reviewedPrefabHash;f.reviewedPartHash=f.part.reviewedPrefabHash;f.reviewedFitHash=CatalogValidation.FitFingerprint(f);EditorUtility.SetDirty(f);AssetDatabase.SaveAssets();}
 }
}
