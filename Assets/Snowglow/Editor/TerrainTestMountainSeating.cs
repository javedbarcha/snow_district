using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor {
 public static class TerrainTestMountainSeating {
  const string ScenePath="Assets/Scenes/AlpsTerrainTest.unity",Log="Logs/AlpsMountainSeating/",Folder="Assets/Snowglow/Levels/AlpsTerrainTest/MountainSeating/";
  [Serializable] class Pose {public string name,mesh;public Vector3 before,after,scale;public Quaternion rotation;public int samples,outside;public float minBurial,visibleHeight;}
  [Serializable] class Report {public Pose[] mountains;}
  static void Check(bool b,string message){if(!b)throw new InvalidOperationException("Mountain Y seating: "+message);}
  static Vector3[] Vertices(Mesh mesh){using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))using(var v=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp)){data[0].GetVertices(v);return v.ToArray();}}
  static float Ground(MeshCollider terrain,Vector3 point,out bool outside){var b=terrain.bounds;outside=point.x<b.min.x||point.x>b.max.x||point.z<b.min.z||point.z>b.max.z;point.x=Mathf.Clamp(point.x,b.min.x+.001f,b.max.x-.001f);point.z=Mathf.Clamp(point.z,b.min.z+.001f,b.max.z-.001f);point.y=b.max.y+10;Check(terrain.Raycast(new Ray(point,Vector3.down),out var hit,1000),"terrain sample "+point);return hit.point.y;}
  static Vector3[] Rim(Transform mountain){
   var f=mountain.GetComponentsInChildren<MeshFilter>().Single();var v=Vertices(f.sharedMesh).Select(p=>mountain.InverseTransformPoint(f.transform.TransformPoint(p))).ToArray();float bottom=v.Min(p=>p.y);
   // Source LOD1 has an 8-source-metre lower wall. Bury its upper ring by Y translation only.
   var ring=v.Where(p=>Mathf.Abs(p.y-bottom-8)<.15f).Distinct().OrderBy(p=>Mathf.Atan2(p.z,p.x)).ToArray();Check(ring.Length>=24,"source slope/base transition "+mountain.name);
   var samples=new List<Vector3>();for(int i=0;i<ring.Length;i++){var a=mountain.TransformPoint(ring[i]);var b=mountain.TransformPoint(ring[(i+1)%ring.Length]);int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/2));for(int j=0;j<steps;j++)samples.Add(Vector3.Lerp(a,b,j/(float)steps));}return samples.ToArray();
  }
  public static void Apply(){
   Directory.CreateDirectory(Log);Directory.CreateDirectory(Folder);var scene=SceneManager.GetActiveScene();Check(scene.path==ScenePath,"operate on current test scene with user placements");
   if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene,Log+"User-unsaved-before.unity",true),"backup unsaved user scene");
   Check(EditorSceneManager.SaveScene(scene,Log+"InMemory-before.unity",true),"backup complete current scene");
   var group=scene.GetRootGameObjects().Single(g=>g.name=="Mountains | provisional placement");Check(group.transform.childCount==8,"eight current mountains");
   Check(!File.Exists(Folder+"IcyMountains.mat"),"already applied; do not repeat implicitly");
   var terrain=scene.GetRootGameObjects().Single(g=>g.name=="Revised attached terrain | 1:1 metres").GetComponentInChildren<MeshCollider>();
   var palette=new Texture2D(2,2,TextureFormat.RGBA32,false);Check(palette.LoadImage(File.ReadAllBytes("Assets/Snowglow/Levels/AlpsTerrainTest/MountainPlacement/IcyMountainPalette.png")),"load palette");Check(palette.width==36&&palette.height==4,"nine source swatches");
   // The last swatch is the remaining dark green/charcoal rock visible on rounded foothills.
   for(int y=0;y<4;y++)for(int x=32;x<36;x++)palette.SetPixel(x,y,new Color32(157,192,218,255));
   palette.Apply();File.WriteAllBytes(Folder+"IcyMountainPalette.png",palette.EncodeToPNG());UnityEngine.Object.DestroyImmediate(palette);
   AssetDatabase.ImportAsset(Folder+"IcyMountainPalette.png",ImportAssetOptions.ForceSynchronousImport);var ti=(TextureImporter)AssetImporter.GetAtPath(Folder+"IcyMountainPalette.png");ti.filterMode=FilterMode.Point;ti.wrapMode=TextureWrapMode.Clamp;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.SaveAndReimport();
   var material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Snowglow/Levels/AlpsTerrainTest/MountainPlacement/IcyMountains.mat")){name="Mountains | icy rocks, no dark moss"};material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"IcyMountainPalette.png"));AssetDatabase.CreateAsset(material,Folder+"IcyMountains.mat");
   var report=new List<Pose>();
   foreach(Transform mountain in group.transform){
    var filter=mountain.GetComponentsInChildren<MeshFilter>().Single();var pose=new Pose{name=mountain.name,before=mountain.position,rotation=mountain.rotation,scale=mountain.localScale,mesh=AssetDatabase.GetAssetPath(filter.sharedMesh)};
    var rim=Rim(mountain);float dy=rim.Min(p=>Ground(terrain,p,out var ignored)-p.y-.5f);mountain.position+=Vector3.up*dy;
    var moved=Rim(mountain);pose.samples=moved.Length;pose.minBurial=float.MaxValue;
    foreach(var p in moved){float ground=Ground(terrain,p,out var outside);if(outside)pose.outside++;pose.minBurial=Mathf.Min(pose.minBurial,ground-p.y);Check(ground-p.y>.49f,"base ring buried "+mountain.name);}
    var world=Vertices(filter.sharedMesh).Select(p=>filter.transform.TransformPoint(p));pose.visibleHeight=world.Max(p=>p.y-Ground(terrain,p,out var ignored));Check(pose.visibleHeight>3,"mountain remains visible "+mountain.name);
    pose.after=mountain.position;Check(pose.after.x==pose.before.x&&pose.after.z==pose.before.z&&mountain.rotation==pose.rotation&&mountain.localScale==pose.scale,"Y translation only");
    foreach(var renderer in mountain.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterial=material;
    Check(mountain.GetComponentsInChildren<MeshCollider>().Single().sharedMesh==filter.sharedMesh,"matching collision preserved");report.Add(pose);
   }
   Physics.SyncTransforms();File.WriteAllText(Log+"Seating.json",JsonUtility.ToJson(new Report{mountains=report.ToArray()},true));AssetDatabase.SaveAssets();Check(EditorSceneManager.SaveScene(scene,ScenePath),"save seated scene");scene=EditorSceneManager.OpenScene(ScenePath);group=scene.GetRootGameObjects().Single(g=>g.name=="Mountains | provisional placement");
   foreach(var pose in report){var mountain=group.transform.Cast<Transform>().Single(t=>t.name==pose.name);Check(mountain.position==pose.after&&mountain.rotation==pose.rotation&&mountain.localScale==pose.scale,"saved pose "+pose.name);Check(AssetDatabase.GetAssetPath(mountain.GetComponentInChildren<MeshFilter>().sharedMesh)==pose.mesh,"saved original mesh");}
   Capture(scene,Log+"Overview.png",new Vector3(1150,850,0),Vector3.zero);
   int index=0;foreach(Transform mountain in group.transform){var f=mountain.GetComponentInChildren<MeshRenderer>();var centre=f.bounds.center;var direction=new Vector3(1,.45f,-1).normalized;float distance=Mathf.Max(f.bounds.size.x,f.bounds.size.z)*1.25f;Capture(scene,Log+"Mountain"+(++index).ToString("D2")+".png",centre+direction*distance,centre);}
   File.WriteAllText(Log+"Validation.txt","PASS: eight mountains seated with world Y translation only. Every source slope/base ring sampled at <=2m intervals and buried >=0.49m (target0.5m); visible peaks >3m. Source geometry, XZ, rotation, scale and terrain unchanged. Last dark green rock swatch replaced by icy blue on a new derivative material, with snow/other colors retained. Saved/reopened references/poses/collision pass. Samples beyond terrain use nearest edge height; counts and height changes in Seating.json.\n"+string.Join("\n",report.Select(p=>p.name+" | dy="+(p.after.y-p.before.y)+" | peak above terrain="+p.visibleHeight+" | outside samples="+p.outside)));
  }
  static void Capture(Scene scene,string path,Vector3 position,Vector3 target){var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single();var pos=camera.transform.position;var rot=camera.transform.rotation;bool ortho=camera.orthographic;float aspect=camera.aspect;var previous=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1200,900,24);var image=new Texture2D(1200,900,TextureFormat.RGB24,false);try{camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));camera.orthographic=false;camera.aspect=4f/3;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1200,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}finally{camera.transform.SetPositionAndRotation(pos,rot);camera.orthographic=ortho;camera.aspect=aspect;camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}}
 }
}
