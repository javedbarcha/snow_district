using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Snowglow.Editor {
 public static class MachineryPartModelChecks {
  public const string Folder="Logs/MachineryPartModels";
  static int checks;
  static void Check(bool value,string reason){checks++;if(!value)throw new InvalidOperationException("Part models: "+reason);}
  public static string FactorySignature(WorkshopPreview view){return string.Join("\n",Enumerable.Range(0,view.vehicles.Length).SelectMany(vehicle=>view.parts[vehicle*7+5].GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name!="Rear mounting lugs").Select(f=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(f.sharedMesh))+"/"+f.sharedMesh.GetInstanceID()+"/"+(view.parts[vehicle*7+5].transform.worldToLocalMatrix*f.transform.localToWorldMatrix).ToString("F4"))));}
  public static void Run(WorkshopPreview view){
   checks=0;Directory.CreateDirectory(Folder);var signatures=new System.Collections.Generic.HashSet<string>();var metrics=new System.Collections.Generic.List<string>{"variant,vertices,triangles,material_slots"};
   foreach(bool loader in new[]{false,true})foreach(int index in new[]{0,1,2,3,4,6}){
    string key=MachineryPartModels.Key(loader,index);var prefab=MachineryPartModels.Get(loader,index);Check(prefab&&AssetDatabase.Contains(prefab),"saved prefab "+key);
    var filters=prefab.GetComponentsInChildren<MeshFilter>(true);var filter=prefab.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;Check(mesh&&AssetDatabase.Contains(mesh),"persistent mesh "+key);
    Check(prefab.GetComponentsInChildren<Collider>(true).Length==0&&prefab.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"visual-only model "+key);
    Check(mesh.vertices.All(v=>CatalogValidation.Finite(v))&&mesh.normals.All(v=>CatalogValidation.Finite(v)),"finite geometry "+key);
    var vertices=mesh.vertices;var triangles=mesh.triangles;int count=filters.Sum(f=>f.sharedMesh.triangles.Length/3);int rootCount=triangles.Length/3;
    Check(count>100&&count<8000,"low-poly triangle budget "+key+" = "+count);
    Check(Enumerable.Range(0,rootCount).All(t=>Vector3.Cross(vertices[triangles[t*3+1]]-vertices[triangles[t*3]],vertices[triangles[t*3+2]]-vertices[triangles[t*3]]).sqrMagnitude>1e-12f),"no degenerate faces "+key);
    Check(prefab.GetComponent<MeshRenderer>().sharedMaterials.All(m=>m&&m.shader&&m.shader.name=="Universal Render Pipeline/Lit"),"saved lit materials "+key);
    string signature=Hash128.Compute(string.Join(";",vertices.Select(v=>v.ToString("F5")))+string.Join(",",triangles)).ToString();Check(signatures.Add(signature),"every variant has distinct geometry "+key);
    var entry=CatalogPreparation.Library().Find("machinery-shop-"+key);Check(entry&&entry.prefab==prefab&&entry.status==PreparationStatus.NeedsReview&&entry.triangles==count,"catalogue provenance and metrics "+key);
    if(index==0||index==2||index==3){
     float center=0;var rim=vertices.Where(v=>{float radius=new Vector2(v.y-center,v.z).magnitude;return radius>.25f&&radius<.61f;}).ToArray();
     Check(rim.Any(v=>v.x<-.12f)&&rim.Any(v=>v.x>.12f),"both tyre faces contain rim geometry "+key);
     foreach(int side in new[]{-1,1}){var face=rim.Where(v=>v.x*side>0).Select(v=>v.x).ToArray();Check(face.Max()-face.Min()>.06f,"dished rim has depth on each side "+key);}
    }
    metrics.Add(key+","+filters.Sum(f=>f.sharedMesh.vertexCount)+","+count+","+prefab.GetComponentsInChildren<MeshRenderer>(true).Sum(r=>r.sharedMaterials.Length));
   }
   var lugs=view.parts[12].GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name=="Rear mounting lugs");Check(AssetDatabase.GetAssetPath(lugs.sharedMesh).EndsWith("/LoaderFactoryMountLugs.asset")&&lugs.sharedMesh.triangles.Length/3>24,"factory bucket uses shaped lugs and pins");
   var hydraulics=MachineryPartModels.Get(false,6).transform.Find("Original tractor lift hydraulics");Check(hydraulics&&hydraulics.GetComponentsInChildren<MeshFilter>(true).All(f=>AssetDatabase.GetAssetPath(f.sharedMesh).EndsWith("TractorDefaultHydraulics.fbx")),"new plow reuses original exported hydraulic geometry");
   File.WriteAllLines(Folder+"/Geometry.csv",metrics);CaptureModels();
   File.WriteAllText(Folder+"/Checks.txt","PASS: "+checks+" checks on 12 distinct saved low-poly models: valid nondegenerate mesh geometry, both tyre faces with recessed rim depth, <8,000 triangles each, persistent URP materials, no controllers/colliders, catalogue provenance and distinct geometry. Native front/rear captures generated; visual inspection required. Factory blade signatures checked separately by installer. No gameplay fitting or performance change.");
  }
  static void CaptureModels(){
   const int width=512,height=400;var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Disposable part-model capture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);root.transform.position=new Vector3(-10000,200,-10000);
   var cameraObject=new GameObject("Part camera");cameraObject.transform.SetParent(root.transform,false);var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.14f,.20f,.25f);camera.cullingMask=1<<12;camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=30;camera.allowHDR=false;cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderShadows=false;
   for(int i=0;i<3;i++){var go=new GameObject("Softbox");go.transform.SetParent(root.transform,false);go.transform.localPosition=new Vector3(i==0?-3:3,5,i==2?4:-4);var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=3;light.range=20;light.cullingMask=1<<12;light.shadows=LightShadows.None;}
   UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,UnityEngine.SceneManagement.SceneManager.GetActiveScene());
   var target=new RenderTexture(width,height,24);var previous=RenderTexture.active;var image=new Texture2D(width,height,TextureFormat.RGB24,false);var sheet=new Texture2D(width*6,height*2,TextureFormat.RGB24,false);camera.targetTexture=target;
   try{
    int tile=0;foreach(bool loader in new[]{false,true})foreach(int index in new[]{0,2,3,1,4,6}){
     var model=UnityEngine.Object.Instantiate(MachineryPartModels.Get(loader,index),root.transform);try{
      var renderers=model.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);camera.orthographicSize=Mathf.Max(bounds.extents.y,Mathf.Max(bounds.extents.x,bounds.extents.z))*.95f+.45f;
      camera.transform.position=bounds.center+new Vector3(5,3,6);camera.transform.LookAt(bounds.center);camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();var pixels=image.GetPixels32();var background=pixels[0];Check(pixels.Count(p=>Math.Abs(p.r-background.r)+Math.Abs(p.g-background.g)+Math.Abs(p.b-background.b)>25)>1000,"nonempty front render");
      string name=MachineryPartModels.Key(loader,index);File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());sheet.SetPixels((tile%6)*width,(1-tile/6)*height,width,height,image.GetPixels());
      camera.transform.position=bounds.center+new Vector3(-5,2.8f,-6);camera.transform.LookAt(bounds.center);camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();pixels=image.GetPixels32();background=pixels[0];Check(pixels.Count(p=>Math.Abs(p.r-background.r)+Math.Abs(p.g-background.g)+Math.Abs(p.b-background.b)>25)>1000,"nonempty rear render");File.WriteAllBytes(Folder+"/"+name+"-rear.png",image.EncodeToPNG());tile++;
     }finally{UnityEngine.Object.DestroyImmediate(model);}
    }
    sheet.Apply();File.WriteAllBytes(Folder+"/AllVariants.png",sheet.EncodeToPNG());
   }finally{camera.targetTexture=null;RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(sheet);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);EditorSceneManager.ClosePreviewScene(scene);}
  }
 }
}
