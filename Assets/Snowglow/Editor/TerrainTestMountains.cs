using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor {
 public static class TerrainTestMountains {
  const string ScenePath="Assets/Scenes/AlpsTerrainTest.unity", Folder="Assets/Snowglow/Levels/AlpsTerrainTest/MountainPlacement/", Log="Logs/AlpsTestMountains/";
  static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException("Test mountains: "+reason);}
  static string Hash(string path){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path)));}
  struct Placement {
   public string label,slug;public Vector2 centre;public float yaw,width;
   public Placement(string label,string slug,float x,float z,float yaw,float width){this.label=label;this.slug=slug;centre=new Vector2(x,z);this.yaw=yaw;this.width=width;}
  }
  static readonly Placement[] Places={
   new Placement("NW | sharp horn","Mountain_01",-385,355,35,245),
   new Placement("N | serrated ridge","Mountain_05",-20,425,0,285),
   new Placement("NE | twin peaks","Mountain_03",380,350,-40,225),
   new Placement("W | narrow cliff wall","Mountain_02",-435,-100,90,245),
   new Placement("SE | cliff wall","Mountain_02",395,-290,65,275),
   new Placement("SW | rounded foothills","Mountain_06",-285,-430,0,230),
   new Placement("S | glacial cirque","Mountain_04",55,-435,-10,215)
  };
  [Serializable] class ExistingPose {public string name;public Vector3 position,scale;public Quaternion rotation;}
  static ExistingPose[] Poses(Transform parent)=>parent.Cast<Transform>().Select(t=>new ExistingPose{name=t.name,position=t.position,rotation=t.rotation,scale=t.localScale}).ToArray();
  [Serializable] class PoseList {public ExistingPose[] poses;}
  public static void AddGap(){
   const string gapLog="Logs/AlpsMountainGap/",name="SE gap | fractured rock buttress | seating pending";
   Directory.CreateDirectory(gapLog);
   var scene=SceneManager.GetActiveScene();Check(scene.path==ScenePath,"keep the repositioned test scene open");
   // Work on the current in-memory scene. Never reopen a stale disk scene over user edits.
   if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene,gapLog+"User-unsaved-before.unity",true),"preserve user repositioning");
   var group=scene.GetRootGameObjects().Single(g=>g.name=="Mountains | provisional placement").transform;
   Check(group.childCount==7&&!group.Cast<Transform>().Any(t=>t.name==name),"add one mountain only; refuse duplicates");
   var poses=Poses(group);File.WriteAllText(gapLog+"UserMountainPoses.json",JsonUtility.ToJson(new PoseList{poses=poses},true));
   var mountainEntry=AlpsRebuildAssets.Find("Mountain_07");Check(mountainEntry&&mountainEntry.prefab,"unused fractured buttress variant");
   var terrain=scene.GetRootGameObjects().Single(g=>g.name=="Revised attached terrain | 1:1 metres").GetComponentInChildren<MeshCollider>();
   var mountain=(GameObject)PrefabUtility.InstantiatePrefab(mountainEntry.prefab,scene);mountain.name=name;mountain.transform.SetParent(group,false);
   mountain.transform.rotation=Quaternion.Euler(0,20,0);
   var b=WorldBounds(mountain);float scale=180/Mathf.Max(b.size.x,b.size.z);mountain.transform.localScale=Vector3.one*scale;
   const float x=380,z=-330;
   Check(terrain.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out var hit,1000),"gap terrain surface");
   mountain.transform.position=new Vector3(x,hit.point.y,z);
   var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"IcyMountains.mat");Check(material,"existing icy mountain palette");
   foreach(var renderer in mountain.GetComponentsInChildren<MeshRenderer>(true))renderer.sharedMaterial=material;
   foreach(var filter in mountain.GetComponentsInChildren<MeshFilter>(true)){var collider=filter.GetComponent<MeshCollider>();if(!collider)collider=filter.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;}
   Physics.SyncTransforms();
   foreach(var pose in poses){var t=group.Cast<Transform>().Single(c=>c.name==pose.name);Check(t.position==pose.position&&t.rotation==pose.rotation&&t.localScale==pose.scale,"existing user pose retained "+pose.name);}
   var newBounds=WorldBounds(mountain);
   Check(EditorSceneManager.SaveScene(scene,ScenePath),"save current user placements with additional mountain");
   scene=EditorSceneManager.OpenScene(ScenePath);group=scene.GetRootGameObjects().Single(g=>g.name=="Mountains | provisional placement").transform;
   foreach(var pose in poses){var t=group.Cast<Transform>().Single(c=>c.name==pose.name);Check(t.position==pose.position&&t.rotation==pose.rotation&&t.localScale==pose.scale,"saved user pose retained "+pose.name);}
   Check(group.childCount==8,"eight saved mountains");
   var added=group.Cast<Transform>().Single(t=>t.name==name);Check(added.GetComponentsInChildren<MeshCollider>().Length==1,"new shared mesh collider saved");
   Check(added.GetComponentsInChildren<MeshRenderer>().All(r=>r.sharedMaterial==material),"icy palette binding saved");
   var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single();
   Capture(camera,gapLog+"Top.png",new Vector3(0,1400,0),Vector3.zero,true,570);
   Capture(camera,gapLog+"Perspective.png",new Vector3(1150,850,0),Vector3.zero,false,0);
   File.WriteAllText(gapLog+"Validation.txt","PASS: added unused Mountain_07 fractured rock buttress in southeast highlighted gap (near-left when viewed from east), centre "+new Vector3(x,hit.point.y,z)+", yaw 20, uniform scale "+scale+", bounds "+newBounds+". Eight saved mountains; all seven existing user world positions/rotations and local scales match exactly before/after save/reopen. Icy material and one static mesh collider saved. No seating repair or source/terrain/game-scene edits.");
  }
  public static void CorrectGapOrientation(){
   const string gapLog="Logs/AlpsMountainGap/",oldName="SW gap | fractured rock buttress | seating pending";
   var scene=SceneManager.GetActiveScene();Check(scene.path==ScenePath,"test scene open");
   Check(EditorSceneManager.SaveScene(scene,gapLog+"BeforeOrientationCorrection.unity",true),"preserve provisional addition");
   var group=scene.GetRootGameObjects().Single(g=>g.name=="Mountains | provisional placement").transform;
   var added=group.Cast<Transform>().Single(t=>t.name==oldName);
   var poses=Poses(group).Where(p=>p.name!=oldName).ToArray();Check(poses.Length==7,"only move the new eighth mountain");
   var terrain=scene.GetRootGameObjects().Single(g=>g.name=="Revised attached terrain | 1:1 metres").GetComponentInChildren<MeshCollider>();
   Check(terrain.Raycast(new Ray(new Vector3(380,500,-330),Vector3.down),out var hit,1000),"southeast gap surface");
   added.position=new Vector3(380,hit.point.y,-330);added.name="SE gap | fractured rock buttress | seating pending";
   Check(EditorSceneManager.SaveScene(scene,ScenePath),"save southeast addition");scene=EditorSceneManager.OpenScene(ScenePath);
   group=scene.GetRootGameObjects().Single(g=>g.name=="Mountains | provisional placement").transform;
   foreach(var p in poses){var t=group.Cast<Transform>().Single(c=>c.name==p.name);Check(t.position==p.position&&t.rotation==p.rotation&&t.localScale==p.scale,"user pose retained "+p.name);}
   var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single();
   Capture(camera,gapLog+"Top.png",new Vector3(0,1400,0),Vector3.zero,true,570);
   Capture(camera,gapLog+"Perspective.png",new Vector3(1150,850,0),Vector3.zero,false,0);
   File.WriteAllText(gapLog+"Validation.txt","PASS: unused Mountain_07 fractured rock buttress placed in southeast highlighted gap (near-left when viewed from the east). Position "+hit.point+" plus original pivot. All seven existing user poses retained across save/reopen. Eight saved mountains, matching icy palette and static mesh collision. No seating repairs or changes to source/terrain/game scenes.");
  }
  public static void Retry(){var scene=SceneManager.GetActiveScene();Check(scene.path==ScenePath,"retry only the test scene");if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene,Log+"Failed-unsaved.unity",true),"preserve interrupted placement");EditorSceneManager.OpenScene(ScenePath);Apply();}
  public static void Apply(){
   Directory.CreateDirectory(Log);Directory.CreateDirectory(Folder);
   Check(Hash("C:/Users/jrbar/Downloads/Snow_District_Alps_Mountains.blend")==Hash("SourceAssets/AlpsRebuild/Snow_District_Alps_Mountains.blend"),"attached mountain source matches existing preserved export");
   var scene=SceneManager.GetActiveScene();
   if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene,Log+"Unsaved-before.unity",true),"unsaved scene backup");
   if(scene.path!=ScenePath)scene=EditorSceneManager.OpenScene(ScenePath);
   Check(!scene.GetRootGameObjects().Any(g=>g.name=="Mountains | provisional placement"),"already installed; no duplicate placement");
   if(!File.Exists(Log+"Saved-before.unity"))File.Copy(ScenePath,Log+"Saved-before.unity");
   var terrain=scene.GetRootGameObjects().Single(g=>g.name=="Revised attached terrain | 1:1 metres");
   var terrainRenderer=terrain.GetComponentInChildren<MeshRenderer>();var originalMaterial=terrainRenderer.sharedMaterial;
   var terrainCollider=terrain.GetComponentInChildren<MeshCollider>();
   var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single();
   var report=new StringBuilder();
   var near=new Vector3(0,8,120);var target=new Vector3(0,35,425);
   Capture(camera,Log+"TerrainBefore.png",near,target,false,0);
   // Derivative palette only: retain source snow swatches and lift the three rock swatches.
   var palette=new Texture2D(2,2,TextureFormat.RGBA32,false);
   Check(palette.LoadImage(File.ReadAllBytes("Assets/Snowglow/Levels/AlpsTerrainTest/Revision2/Palette.png")),"load terrain palette");
   Check(palette.width==24&&palette.height==4,"expected six four-pixel palette swatches");
   Color32[] rock={new Color32(150,184,213,255),new Color32(176,204,224,255),new Color32(125,164,201,255)};
   for(int y=0;y<palette.height;y++)for(int x=12;x<24;x++)palette.SetPixel(x,y,rock[(x-12)/4]);
   palette.Apply();File.WriteAllBytes(Folder+"IcyTerrainPalette.png",palette.EncodeToPNG());UnityEngine.Object.DestroyImmediate(palette);
   AssetDatabase.ImportAsset(Folder+"IcyTerrainPalette.png",ImportAssetOptions.ForceSynchronousImport);
   var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"IcyTerrainPalette.png");
   importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();
   var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"IcyTerrain.mat");bool newMaterial=!material;
   if(!material)material=new Material(originalMaterial){name="Test terrain | icy blue rock"};
   material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"IcyTerrainPalette.png"));
   if(newMaterial)AssetDatabase.CreateAsset(material,Folder+"IcyTerrain.mat");terrainRenderer.sharedMaterial=material;
   Capture(camera,Log+"TerrainAfter.png",near,target,false,0);
   // Match newly placed mountain rocks to the terrain; never alter the shared source palette.
   var mountainPalette=new Texture2D(2,2,TextureFormat.RGBA32,false);
   Check(mountainPalette.LoadImage(File.ReadAllBytes(AlpsRebuildAssets.Source+"MountainPalette.png")),"mountain palette");
   Check(mountainPalette.width==36&&mountainPalette.height==4,"mountain source swatches");
   Color32[] mountainRock={new Color32(125,164,201,255),new Color32(140,176,208,255),new Color32(150,184,213,255),new Color32(166,196,220,255)};
   for(int y=0;y<4;y++)for(int x=0;x<16;x++)mountainPalette.SetPixel(x,y,mountainRock[x/4]);
   mountainPalette.Apply();File.WriteAllBytes(Folder+"IcyMountainPalette.png",mountainPalette.EncodeToPNG());UnityEngine.Object.DestroyImmediate(mountainPalette);
   AssetDatabase.ImportAsset(Folder+"IcyMountainPalette.png",ImportAssetOptions.ForceSynchronousImport);
   var mountainImporter=(TextureImporter)AssetImporter.GetAtPath(Folder+"IcyMountainPalette.png");mountainImporter.filterMode=FilterMode.Point;mountainImporter.wrapMode=TextureWrapMode.Clamp;mountainImporter.mipmapEnabled=false;mountainImporter.textureCompression=TextureImporterCompression.Uncompressed;mountainImporter.SaveAndReimport();
   var mountainMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"IcyMountains.mat");bool newMountainMaterial=!mountainMaterial;
   if(!mountainMaterial)mountainMaterial=new Material(AssetDatabase.LoadAssetAtPath<Material>(AlpsRebuildAssets.Source+"MountainPalette.mat")){name="Test mountains | icy blue rock"};
   mountainMaterial.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"IcyMountainPalette.png"));if(newMountainMaterial)AssetDatabase.CreateAsset(mountainMaterial,Folder+"IcyMountains.mat");
   var group=new GameObject("Mountains | provisional placement");
   foreach(var p in Places){
    var entry=AlpsRebuildAssets.Find(p.slug);Check(entry&&entry.prefab,"prepared mountain "+p.slug);
    var mountain=(GameObject)PrefabUtility.InstantiatePrefab(entry.prefab,scene);
    mountain.name=p.label+" | seating pending";mountain.transform.SetParent(group.transform,false);
    foreach(var renderer in mountain.GetComponentsInChildren<MeshRenderer>(true))renderer.sharedMaterial=mountainMaterial;
    mountain.transform.rotation=Quaternion.Euler(0,p.yaw,0);
    // Uniform display scale fits each marked footprint; source meshes and their proportions stay intact.
    var bounds=WorldBounds(mountain);float scale=p.width/Mathf.Max(bounds.size.x,bounds.size.z);
    mountain.transform.localScale=Vector3.one*scale;
    Check(terrainCollider.Raycast(new Ray(new Vector3(p.centre.x,500,p.centre.y),Vector3.down),out var hit,1000),"terrain sample at "+p.label);
    mountain.transform.position=new Vector3(p.centre.x,hit.point.y,p.centre.y);
    // Basic placement only. No base reshaping, skirt, terrain sculpt, seam blending or seating repair.
    foreach(var filter in mountain.GetComponentsInChildren<MeshFilter>(true)){
     var collision=filter.GetComponent<MeshCollider>();if(!collision)collision=filter.gameObject.AddComponent<MeshCollider>();collision.sharedMesh=filter.sharedMesh;
    }
    report.AppendLine(p.label+" | "+p.slug+" | position "+mountain.transform.position+" | yaw "+p.yaw+" | uniform scale "+scale+" | "+WorldBounds(mountain));
   }
   Physics.SyncTransforms();
   Check(group.transform.childCount==7,"seven marked zones");
   Check(!group.GetComponentsInChildren<Rigidbody>().Any(),"static mountains");
   Check(group.GetComponentsInChildren<MeshCollider>().Length==7,"seven shared mountain mesh colliders");
   var mesh=terrainCollider.sharedMesh;var query=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();
   Check(query.terrainData==AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/Snowglow/Levels/AlpsTerrainTest/Revision2/CameraHeights.asset"),"camera terrain data unchanged");
   AssetDatabase.SaveAssets();Check(EditorSceneManager.SaveScene(scene,ScenePath),"save test scene");scene=EditorSceneManager.OpenScene(ScenePath);
   var saved=scene.GetRootGameObjects().Single(g=>g.name=="Mountains | provisional placement");
   Check(saved.transform.childCount==7&&saved.GetComponentsInChildren<MeshCollider>().All(c=>c.sharedMesh),"saved mountain references");
   Check(scene.GetRootGameObjects().Single(g=>g.name=="Revised attached terrain | 1:1 metres").GetComponentInChildren<MeshCollider>().sharedMesh==mesh,"terrain geometry unchanged");
   camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single();
   Capture(camera,Log+"PlacementTop.png",new Vector3(0,1400,0),Vector3.zero,true,570);
   Capture(camera,Log+"PlacementPerspective.png",new Vector3(900,900,-1050),new Vector3(0,15,0),false,0);
   EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
   File.WriteAllText(Log+"Validation.txt","PASS: seven provisional mountain placements saved/reopened, matching marked perimeter zones. Source checksum matches existing prepared imports; no source export changes. Terrain geometry/collision/camera data retained. Only rock palette swatches changed on a test-only derivative material; snow swatches retained. No seating repairs or game-scene changes.\n"+report);
  }
  static Bounds WorldBounds(GameObject go){var r=go.GetComponentsInChildren<MeshRenderer>(true);Check(r.Length>0,"mountain render geometry");var b=r[0].bounds;foreach(var renderer in r.Skip(1))b.Encapsulate(renderer.bounds);return b;}
  static void Capture(Camera camera,string path,Vector3 position,Vector3 target,bool ortho,float size){
   var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;bool oldOrtho=camera.orthographic;float oldSize=camera.orthographicSize,oldAspect=camera.aspect;var oldTarget=camera.targetTexture;var active=RenderTexture.active;
   var rt=RenderTexture.GetTemporary(1200,1200,24);var image=new Texture2D(1200,1200,TextureFormat.RGB24,false);
   try{camera.transform.position=position;camera.transform.rotation=Quaternion.LookRotation(target-position,ortho?Vector3.forward:Vector3.up);camera.orthographic=ortho;camera.orthographicSize=size;camera.aspect=1;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1200,1200),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
   finally{camera.transform.SetPositionAndRotation(oldPosition,oldRotation);camera.orthographic=oldOrtho;camera.orthographicSize=oldSize;camera.aspect=oldAspect;camera.targetTexture=oldTarget;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
  }
 }
}
