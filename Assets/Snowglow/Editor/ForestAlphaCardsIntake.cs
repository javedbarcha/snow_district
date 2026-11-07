using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Snowglow.Editor {
 public static class ForestAlphaCardsIntake {
  const string Source="Assets/Snowglow/Catalog/Sources/ForestAlphaCards/",Logs="Logs/ForestAlphaCards",Request="Logs/ForestAlphaCards.request",CardName="Distant forest | crossed alpha cards";
  [Serializable]public class Manifest{public Card[] cards;public string sourceSha256;}
  [Serializable]public class Card{public string file,asset,view;public float card_width_metres,card_height_metres;public float[] pivot_uv_bottom_left,source_bounds_metres,atlas_uv_bottom_left,alpha_uv_bounds;}
  static int checks;
  static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
  static bool Tree(CatalogPlacement p)=>(p.assetId??"").StartsWith("alpine-tree-");
  static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Forest cards: "+message);checks++;}
  [InitializeOnLoadMethod]static void Register(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=Pending;File.Delete(Request);try{Install();}catch(Exception e){Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  static string Collision(CatalogPlacement p){var b=new StringBuilder(p.transform.localToWorldMatrix.ToString("R"));foreach(var c in p.GetComponentsInChildren<Collider>(true)){b.Append(c.GetType().Name+c.transform.localToWorldMatrix.ToString("R")+c.enabled+c.isTrigger);if(c is BoxCollider box)b.Append(box.center.ToString("R")+box.size.ToString("R"));if(c is CapsuleCollider cap)b.Append(cap.center.ToString("R")+cap.radius.ToString("R")+cap.height.ToString("R")+cap.direction);}return b.ToString();}
  static Bounds GeometryBounds(GameObject root){var card=root.transform.Find(CardName);bool on=card&&card.gameObject.activeSelf;if(card)card.gameObject.SetActive(false);try{
   var renderers=root.GetComponentsInChildren<Renderer>(true).Where(r=>!card||!r.transform.IsChildOf(card)).ToArray();bool first=true;var b=new Bounds();foreach(var r in renderers){var rb=r.localBounds;for(int i=0;i<8;i++){var p=root.transform.InverseTransformPoint(r.transform.TransformPoint(rb.center+Vector3.Scale(rb.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}return b;
  }finally{if(card)card.gameObject.SetActive(on);}}
  static Material PrepareMaterial(){
   var path=Source+"Forest_Cards_Atlas_4096.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=true;importer.alphaTestReferenceValue=.45f;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.isReadable=false;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Compressed;
   var android=importer.GetPlatformTextureSettings("Android");android.overridden=true;android.maxTextureSize=4096;android.format=TextureImporterFormat.ASTC_6x6;android.compressionQuality=50;importer.SetPlatformTextureSettings(android);importer.SaveAndReimport();
   string matPath=Source+"ForestCards.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,matPath);}
   ForestCardColorAuthoring.BindShader(material);material.name="Forest cards | shared alpha-cutout atlas";material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));material.SetColor("_BaseColor",Color.white);material.SetFloat("_Cutoff",.45f);material.SetOverrideTag("RenderType","TransparentCutout");material.renderQueue=2450;material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
  }
  static Mesh PrepareMesh(string index,Card[] cards,Bounds bounds){
   var vertices=new List<Vector3>();var uv=new List<Vector2>();
   foreach(var card in cards.OrderBy(c=>c.view)){
    bool side=card.view=="Side";var crop=card.alpha_uv_bounds;Check(crop!=null&&crop.Length==4&&crop[2]>crop[0]&&crop[3]>crop[1],"alpha bounds "+card.file);float width=side?bounds.size.z:bounds.size.x,height=bounds.size.y;
    foreach(var corner in new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)}){
     float x=(corner.x-.5f)*width,y=corner.y*height+bounds.min.y;
     vertices.Add(new Vector3(bounds.center.x+(side?0:x),y,bounds.center.z+(side?x:0)));
     var rect=card.atlas_uv_bottom_left;uv.Add(new Vector2(Mathf.Lerp(rect[0],rect[2],Mathf.Lerp(crop[0],crop[2],corner.x)),Mathf.Lerp(rect[1],rect[3],Mathf.Lerp(crop[1],crop[3],corner.y))));
    }
   }
   string path=Source+"Card-"+index+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}mesh.Clear();mesh.name="Forest "+index+" | front + side | 4 triangles";mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.triangles=new[]{0,1,2,0,2,3,4,5,6,4,6,7};mesh.RecalculateNormals();mesh.RecalculateBounds();Check((mesh.bounds.size-bounds.size).magnitude<.002f,"card dimensions match geometry "+index);EditorUtility.SetDirty(mesh);return mesh;
  }
  static void ConfigureRenderer(MeshRenderer r,Material material){r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;r.enabled=false;}
  static void PrepareCardEntry(string index,string title,Mesh mesh,Material material){
   string id="forest-card-"+index;var entry=CatalogPreparation.Library().Find(id);
   if(!entry){var raw=new GameObject(title+" | source cross card");try{raw.AddComponent<MeshFilter>().sharedMesh=mesh;ConfigureRenderer(raw.AddComponent<MeshRenderer>(),material);var draft=CatalogPreparation.Draft(title+" distant card",CatalogKind.Landscape,AssetSource.ReferenceImage,null,"User supplied Alpine_Forest_Alpha_Cards.zip; original images and metadata preserved in SourceAssets/ForestAlphaCards. No bundled instructions or scripts executed.");draft.id=id;draft.creationSourcePath="SourceAssets/ForestAlphaCards/Alpine_Forest_Alpha_Cards.zip";entry=CatalogPreparation.Prepare(raw,CatalogKind.Landscape,draft.displayName,1,Vector3.zero,AssetSource.ReferenceImage,draft.provenance,draft,visualOnly:true);}finally{UnityEngine.Object.DestroyImmediate(raw);}}
   var root=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(entry.prefab));try{root.transform.Find("VisualRoot").localPosition=-new Vector3(mesh.bounds.center.x,mesh.bounds.min.y,mesh.bounds.center.z);var filter=root.GetComponentInChildren<MeshFilter>(true);filter.sharedMesh=mesh;ConfigureRenderer(filter.GetComponent<MeshRenderer>(),material);filter.GetComponent<MeshRenderer>().enabled=true;PrefabUtility.SaveAsPrefabAsset(root,AssetDatabase.GetAssetPath(entry.prefab));}finally{PrefabUtility.UnloadPrefabContents(root);}
   entry.sourcePath=Source+"Forest_Cards_Atlas_4096.png";entry.sourceGuid=AssetDatabase.AssetPathToGUID(entry.sourcePath);entry.blocksSnow=false;entry.minimumScale=.75f;entry.maximumScale=1.6f;entry.tags=new[]{"forest-card","alpha-cutout","crossed-card","matches-alpine-tree-"+index};entry.assumptions="Two crossed atlas quads, 4 triangles, double-sided cutout; calibrated to existing LOD3 height/root. No colliders or shadows. Best at distance; two supplied views cannot reproduce all camera elevations.";entry.status=PreparationStatus.NeedsReview;entry.visualReviewed=false;CatalogPreparation.RecordMetrics(entry);EditorUtility.SetDirty(entry);Check(CatalogValidation.Check(entry).Ok&&entry.triangles==4,"card catalog entry "+id);
  }
  static void ConfigureTree(GameObject root,Mesh mesh,Material material){
   var child=root.transform.Find(CardName);if(!child){var go=new GameObject(CardName);go.transform.SetParent(root.transform,false);child=go.transform;go.AddComponent<MeshFilter>();go.AddComponent<MeshRenderer>();}
   child.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=child.GetComponent<MeshRenderer>();ConfigureRenderer(renderer,material);
   var cull=root.GetComponent<DistanceVisibility>();Check(cull,"missing original visibility component");cull.nearParts=root.GetComponentsInChildren<Renderer>(true).Where(r=>!r.transform.IsChildOf(child)).ToArray();cull.distantParts=new Renderer[]{renderer};cull.distance=200;cull.distantDistance=2000;cull.switchBuffer=12;foreach(var t in child.GetComponentsInChildren<Transform>(true))t.gameObject.layer=11;
   foreach(var near in cull.nearParts)near.enabled=true;
  }
  static void Tests(DistanceVisibility source){
   GameObject fixture=null;try{fixture=UnityEngine.Object.Instantiate(source.gameObject);fixture.hideFlags=HideFlags.HideAndDontSave;var v=fixture.GetComponent<DistanceVisibility>();var p=fixture.transform.position;
    v.Evaluate(p+Vector3.right*100);Check(v.nearParts.All(r=>r.enabled)&&v.distantParts.All(r=>!r.enabled),"near geometry");
    v.Evaluate(p+Vector3.right*205);Check(v.ShowingGeometry,"outward hysteresis");v.Evaluate(p+Vector3.right*215);Check(v.nearParts.All(r=>!r.enabled)&&v.distantParts.All(r=>r.enabled),"far card");
    v.Evaluate(p+Vector3.right*195);Check(!v.ShowingGeometry,"inward hysteresis");v.Evaluate(p+Vector3.right*185);Check(v.ShowingGeometry,"return to geometry");
    v.Evaluate(p+Vector3.right*2001);Check(v.nearParts.Concat(v.distantParts).All(r=>!r.enabled),"far culling");
    for(int d=0;d<=2050;d+=3){v.Evaluate(p+Vector3.right*d);Check(!(v.nearParts.Any(r=>r.enabled)&&v.distantParts.Any(r=>r.enabled)),"overlapping representations");}
    Check(fixture.GetComponentsInChildren<Collider>(true).All(c=>c.enabled),"collider disabled by switching");
   }finally{if(fixture)UnityEngine.Object.DestroyImmediate(fixture);}
  }
  [MenuItem("Snowglow/Asset intake/Import distant forest alpha cards")]
  public static void Install(){
   checks=0;var scene=SceneManager.GetActiveScene();Check(scene.path==SceneValidation.ScenePath&&!EditorApplication.isPlayingOrWillChangePlaymode,"open Pinefall with Play stopped");Directory.CreateDirectory(Logs);Directory.CreateDirectory(Logs+"/OriginalPrefabs");if(!File.Exists(Logs+"/Pinefall-before.unity"))EditorSceneManager.SaveScene(scene,Logs+"/Pinefall-before.unity",true);if(scene.isDirty)EditorSceneManager.SaveScene(scene,Logs+"/Pinefall-unsaved-before.unity",true);
   var placements=All<CatalogPlacement>(scene).Where(p=>!string.IsNullOrEmpty(p.placementId)).ToDictionary(p=>p.placementId,Collision);var forest=All<CatalogPlacement>(scene).Where(Tree).ToArray();Check(forest.Length==438||forest.Length==638||forest.Length==500,"expected authored forest");
   var protectedPaths=All<MeshFilter>(scene).Where(f=>f.sharedMesh).Select(f=>AssetDatabase.GetAssetPath(f.sharedMesh)).Concat(All<Terrain>(scene).Select(t=>AssetDatabase.GetAssetPath(t.terrainData))).Concat(new[]{AssetDatabase.GetAssetPath(All<PrototypeSession>(scene).Single().Snow.seed)}).Where(p=>p.StartsWith("Assets/")&&!p.StartsWith(Source)).Distinct().ToDictionary(p=>p,p=>AssetDatabase.GetAssetDependencyHash(p));
   ForestSeatingAuthoring.ForestCamera(scene);var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(Source+"CardsManifest.json"));Check(data.cards.Length==52,"expected 52 views");var material=PrepareMaterial();
   for(int i=1;i<=26;i++){
    string index=i.ToString("D2");var entry=CatalogPreparation.Library().Find("alpine-tree-"+index);string path=AssetDatabase.GetAssetPath(entry.prefab);string backup=Logs+"/OriginalPrefabs/"+index+".prefab";if(!File.Exists(backup))File.Copy(path,backup);
    var root=PrefabUtility.LoadPrefabContents(path);try{var cards=data.cards.Where(c=>c.asset.StartsWith(index+"_")).ToArray();Check(cards.Length==2,"variant mapping "+index);var mesh=PrepareMesh(index,cards,GeometryBounds(root));PrepareCardEntry(index,entry.displayName,mesh,material);ConfigureTree(root,mesh,material);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
    entry.tags=entry.tags.Concat(new[]{"distant-alpha-card"}).Distinct().ToArray();entry.assumptions="Existing LOD3 geometry nearby; matching atlas cross-card at camera distance beyond 200m with 12m hysteresis; all visuals culled beyond 2000m. Original solid collider stays enabled. Runtime toggles saved renderers only; no spawning/mesh/material creation. Higher-detail source images can differ slightly in silhouette.";entry.status=PreparationStatus.NeedsReview;entry.visualReviewed=false;entry.reviewedPrefabHash="";CatalogPreparation.RecordMetrics(entry);EditorUtility.SetDirty(entry);
   }
   forest=All<CatalogPlacement>(scene).Where(Tree).ToArray();foreach(var tree in forest){var cull=tree.GetComponent<DistanceVisibility>();Check(cull.distantParts.Length==1&&cull.distantParts[0]&&cull.nearParts.Length>0,"scene prefab bindings "+tree.placementId);Check(cull.distantParts[0].GetComponent<MeshFilter>().sharedMesh.triangles.Length==12,"scene card geometry");Check(cull.distantParts[0].sharedMaterial==material,"shared atlas");Check(cull.distance==200&&cull.distantDistance==2000,"saved distances");}
   foreach(var group in forest.GroupBy(p=>p.assetId))Tests(group.First().GetComponent<DistanceVisibility>());
   foreach(var p in All<CatalogPlacement>(scene).Where(p=>!string.IsNullOrEmpty(p.placementId)))Check(placements[p.placementId]==Collision(p),"pose/collision changed "+p.placementId);foreach(var pair in protectedPaths)Check(pair.Value==AssetDatabase.GetAssetDependencyHash(pair.Key),"original mesh/Terrain/snow changed");
   AssetDatabase.SaveAssets();SceneValidation.Validate(scene);MechanicsChecks.Run();EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"save failed");scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);SceneValidation.Validate(scene);
   foreach(var p in All<CatalogPlacement>(scene).Where(Tree))Check(p.GetComponent<DistanceVisibility>().distantParts.Length==1,"reopened bindings");
   Preview(scene);EditorSceneManager.OpenScene(SceneValidation.ScenePath);
   File.WriteAllText(Logs+"/Validation.txt","PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+"; "+checks+" checks. 26 Needs Review standalone catalog cards, 26 existing tree prefabs augmented, "+forest.Length+" forest placements with saved near/far bindings. Shared 4096 atlas, Android ASTC6x6, alpha coverage mipmaps, double-sided opaque cutout, no card shadows/colliders. Camera-distance 200m switching, 12m hysteresis, 2000m far cutoff. All-far inventory "+(forest.Length*4)+" triangles/"+forest.Length+" renderers; original near geometry retained. Per-variant outward/inward/hysteresis/far-cull checks passed without simultaneous near/far visuals; collision stays enabled. Saved/reopened, scene and 19 snow checks passed. Existing placement/collision snapshots and source mesh/Terrain/snow hashes unchanged. No runtime factories, district rebake or player-save writes. Native near/card previews generated. Live driving, Android packaging and phone performance untested.");
  }
  static void Preview(Scene scene){bool previousAsync=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;try{foreach(int index in new[]{1,9,14,21,25,26}){
   var asset=CatalogPreparation.Library().Find("alpine-tree-"+index.ToString("D2"));foreach(bool far in new[]{false,true}){var p=new PreviewRenderUtility();try{var copy=UnityEngine.Object.Instantiate(asset.prefab);p.AddSingleGO(copy);var c=copy.GetComponent<DistanceVisibility>();c.Evaluate(copy.transform.position+Vector3.right*(far?300:50));var bounds=GeometryBounds(copy);p.camera.orthographic=true;p.camera.orthographicSize=Mathf.Max(bounds.extents.y,bounds.extents.x)*1.5f;p.camera.nearClipPlane=.1f;p.camera.farClipPlane=100;p.camera.transform.position=bounds.center+new Vector3(1,.3f,1).normalized*40;p.camera.transform.LookAt(bounds.center);p.camera.backgroundColor=new Color(.5f,.64f,.7f);p.ambientColor=Color.white;p.lights[0].intensity=1;p.lights[0].transform.rotation=Quaternion.Euler(40,30,0);p.BeginStaticPreview(new Rect(0,0,512,512));p.Render(true);var image=p.EndStaticPreview();File.WriteAllBytes(Logs+"/Variant-"+index+(far?"-card":"-mesh")+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}finally{p.Cleanup();}}
  }}finally{ShaderUtil.allowAsyncCompilation=previousAsync;}
  }
 }
}
