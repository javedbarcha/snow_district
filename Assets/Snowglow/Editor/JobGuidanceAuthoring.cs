using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Snowglow.Editor {
 public static class JobGuidanceAuthoring {
  const string AssetsFolder="Assets/Snowglow/UI/JobGuidance",Folder="Logs/JobGuidance",Request="Logs/JobGuidance.request";
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete(Request);
   try{Install();}catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}
  }
  static Material Material(string name,Color color,string shader="Universal Render Pipeline/Unlit"){
   string path=AssetsFolder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!material){material=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(material,path);}
   material.SetColor(shader=="Snowglow/Job Hologram"?"_Tint":"_BaseColor",color);EditorUtility.SetDirty(material);return material;
  }
  static Mesh SaveMesh(Mesh mesh,string name){
   string path=AssetsFolder+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(existing){existing.Clear();existing.vertices=mesh.vertices;existing.uv=mesh.uv;existing.normals=mesh.normals;existing.triangles=mesh.triangles;existing.RecalculateBounds();existing.UploadMeshData(false);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(mesh);return existing;}
   mesh.name=name;AssetDatabase.CreateAsset(mesh,path);return mesh;
  }
  static GameObject Display(string name,Transform parent,Mesh mesh,Material material){
   var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.layer=2;go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
   var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return go;
  }
  static Mesh ArrowMesh(){
   var shape=new[]{new Vector2(-.25f,-1),new Vector2(.25f,-1),new Vector2(.25f,0),new Vector2(.85f,0),new Vector2(0,1.25f),new Vector2(-.85f,0),new Vector2(-.25f,0)};
   var vertices=new Vector3[14];for(int i=0;i<7;i++){vertices[i]=new Vector3(shape[i].x,.12f,shape[i].y);vertices[i+7]=new Vector3(shape[i].x,-.12f,shape[i].y);}
   var triangles=new List<int>{0,2,1,0,6,2,3,5,4,7,8,9,7,9,13,10,11,12};
   for(int i=0;i<7;i++){int j=(i+1)%7;triangles.AddRange(new[]{i,j,i+7,j,j+7,i+7});}
   var mesh=new Mesh{vertices=vertices,triangles=triangles.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();return SaveMesh(mesh,"Direction arrow");
  }
  static Vector3[] Perimeter(Rect bounds){
   var corners=new[]{new Vector3(bounds.xMin,0,bounds.yMin),new Vector3(bounds.xMax,0,bounds.yMin),new Vector3(bounds.xMax,0,bounds.yMax),new Vector3(bounds.xMin,0,bounds.yMax)};
   var points=new List<Vector3>();for(int edge=0;edge<4;edge++){var a=corners[edge];var b=corners[(edge+1)%4];int count=Mathf.CeilToInt(Vector3.Distance(a,b)/1.5f);for(int n=0;n<count;n++)points.Add(Vector3.Lerp(a,b,(float)n/count));}return points.ToArray();
  }
  static Mesh WallMesh(Vector3[] points,int job){
   var vertices=new Vector3[points.Length*4];var uv=new Vector2[vertices.Length];var triangles=new int[points.Length*6];
   for(int i=0;i<points.Length;i++){
    var a=points[i]+Vector3.up*(SnowField.Ground+.05f);var b=points[(i+1)%points.Length]+Vector3.up*(SnowField.Ground+.05f);int v=i*4,t=i*6;
    vertices[v]=a;vertices[v+1]=b;vertices[v+2]=b+Vector3.up*2.2f;vertices[v+3]=a+Vector3.up*2.2f;
    uv[v]=Vector2.zero;uv[v+1]=Vector2.right;uv[v+2]=Vector2.one;uv[v+3]=Vector2.up;
    triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;
   }
   var mesh=new Mesh{vertices=vertices,uv=uv,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();return SaveMesh(mesh,"Venue hologram "+job);
  }
  public static JobGuidance Apply(PrototypeSession game){
   Directory.CreateDirectory(AssetsFolder);AssetDatabase.Refresh();
   var prior=game.gameObject.scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<JobGuidance>(true)).FirstOrDefault();if(prior)UnityEngine.Object.DestroyImmediate(prior.gameObject);
   var old=game.gameObject.scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="07 Contract reference markers");if(old)old.SetActive(false);
   var root=new GameObject("07 Job guidance | visual only");var guidance=root.AddComponent<JobGuidance>();
   var amber=Material("Direction amber",new Color(2.2f,.85f,.025f,1));var dark=Material("Direction outline",new Color(.015f,.055f,.085f,1));
   var cyan=Material("Venue cyan rim",new Color(.01f,1.2f,2.2f,1));var hologram=Material("Venue hologram",new Color(.01f,1.5f,2.5f,.55f),"Snowglow/Job Hologram");
   var arrowRoot=new GameObject("Direction to current job");arrowRoot.transform.SetParent(root.transform,false);guidance.arrow=arrowRoot.transform;arrowRoot.transform.localScale=Vector3.one*1.1f;
   var mesh=ArrowMesh();Display("Amber arrow",arrowRoot.transform,mesh,amber);var outline=Display("Dark arrow outline",arrowRoot.transform,mesh,dark);outline.transform.localScale=new Vector3(1.15f,1,1.12f);outline.transform.localPosition=Vector3.down*.15f;
   guidance.venues=new JobVenueVisual[3];
   for(int job=0;job<3;job++){
    var venue=new GameObject(CareerProgression.JobNames[job]+" | hologram");venue.transform.SetParent(root.transform,false);var points=Perimeter(SnowField.ContractBounds(job));Display("Translucent scanning boundary",venue.transform,WallMesh(points,job),hologram);
    var edge=new GameObject("Snow surface boundary",typeof(LineRenderer));edge.layer=2;edge.transform.SetParent(venue.transform,false);var line=edge.GetComponent<LineRenderer>();line.sharedMaterial=cyan;line.useWorldSpace=true;line.loop=true;line.positionCount=points.Length;line.widthMultiplier=.18f;line.numCornerVertices=2;line.alignment=LineAlignment.View;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
    for(int i=0;i<points.Length;i++){var point=points[i];int index=game.Snow.seed.grid.Index(point.x,point.z);point.y=SnowField.Ground+(index>=0?game.Snow.seed.grid.depth[index]:0)+.09f;line.SetPosition(i,point);}
    guidance.venues[job]=new JobVenueVisual{root=venue,outline=line,points=points};
   }
   guidance.vehicleHeights=game.Fleet.Select(m=>{float top=3;foreach(var renderer in m.GetComponentsInChildren<MeshRenderer>(true))if(renderer.enabled&&renderer.gameObject.activeInHierarchy)top=Mathf.Max(top,renderer.bounds.max.y-m.transform.position.y);return top;}).ToArray();
   guidance.Hide();EditorUtility.SetDirty(guidance);return guidance;
  }
  [MenuItem("Snowglow/UI/Install job direction and venue holograms")]
  public static void Install(){
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=SceneValidation.ScenePath)throw new InvalidOperationException("Open Pinefall with Play stopped");Directory.CreateDirectory(Folder);
   File.Copy(scene.path,Folder+"/Pinefall-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");if(scene.isDirty&&!EditorSceneManager.SaveScene(scene,Folder+"/Pinefall-unsaved-before.unity",true))throw new IOException("Unsaved scene backup failed");
   var game=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<PrototypeSession>(true)).Single();Apply(game);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
   scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);game=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<PrototypeSession>(true)).Single();JobGuidanceChecks.Run(game);ProductionMenuChecks.Run(game.HUD);SceneValidation.Validate(scene);MechanicsChecks.Run();
   EditorSceneManager.OpenScene(SceneValidation.ScenePath);File.WriteAllText(Folder+"/Validation.txt","PASS: Saved/reopened job guidance, native direction/arrival/menu/free-work/vehicle-switch/boundary checks, UI/flow, scene and snow checks. Native venue and arrow captures generated. No Play entry or player-save writes. Device visual/performance acceptance remains separate.");
  }
 }
}
