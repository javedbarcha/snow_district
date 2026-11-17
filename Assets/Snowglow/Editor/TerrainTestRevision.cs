using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor {
 // Targeted replacement: never modifies source machinery or either town scene.
 public static class TerrainTestRevision {
  const string Folder="Assets/Snowglow/Levels/AlpsTerrainTest/Revision2/";
  const string Log="Logs/AlpsTerrainTestRevision/";
  const string ScenePath="Assets/Scenes/AlpsTerrainTest.unity";
  static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException("Terrain revision: "+why);}
  public static void Apply(){
   Directory.CreateDirectory(Log);
   var scene=SceneManager.GetActiveScene();
   if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene,Log+"Unsaved-before.unity",true),"preserve unsaved scene");
   if(scene.path!=ScenePath)scene=EditorSceneManager.OpenScene(ScenePath);
   if(!File.Exists(Log+"Saved-before.unity"))File.Copy(ScenePath,Log+"Saved-before.unity");
   Check(!scene.GetRootGameObjects().Any(g=>g.name=="Revised attached terrain | 1:1 metres"),"replacement already installed");
   AssetDatabase.Refresh();
   var texturePath=Folder+"Palette.png";
   var ti=(TextureImporter)AssetImporter.GetAtPath(texturePath);
   ti.filterMode=FilterMode.Point;ti.textureCompression=TextureImporterCompression.Uncompressed;
   ti.wrapMode=TextureWrapMode.Clamp;ti.SaveAndReimport();
   var material=new Material(Shader.Find("Universal Render Pipeline/Simple Lit")){name="Revised terrain palette",enableInstancing=true};
   material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
   material.SetColor("_BaseColor",Color.white);material.SetColor("_SpecColor",Color.black);material.SetFloat("_Smoothness",0);
   Check(!File.Exists(Folder+"Palette.mat"),"do not overwrite prepared material");
   AssetDatabase.CreateAsset(material,Folder+"Palette.mat");
   var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"Terrain.fbx");
   importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;
   importer.globalScale=1;importer.isReadable=false;
   importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
   importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"TestTerrainPalette"),material);
   importer.SaveAndReimport();
   var terrain=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Terrain.fbx"),scene);
   terrain.name="Revised attached terrain | 1:1 metres";
   var front=terrain.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Front");
   if((front.position-front.parent.position).z<0)terrain.transform.rotation=Quaternion.Euler(0,180,0);
   var filter=terrain.GetComponentsInChildren<MeshFilter>(true).Single();
   filter.GetComponent<MeshRenderer>().sharedMaterial=material;
   var collision=filter.gameObject.AddComponent<MeshCollider>();collision.sharedMesh=filter.sharedMesh;
   // Remove only the previous visible terrain, retaining all original source assets.
   var old=scene.GetRootGameObjects().Single(g=>g.name=="Original attached terrain | 1:1 metres");
   UnityEngine.Object.DestroyImmediate(old);
   Physics.SyncTransforms();
   var b=collision.bounds;
   Check(Mathf.Abs(b.size.x-1000)<.1f&&Mathf.Abs(b.size.z-1000)<.1f&&Mathf.Abs(b.size.y-105)<.1f,"source metre dimensions "+b);
   float floor=Mathf.Floor(b.min.y)-1, height=Mathf.Ceil(b.max.y)-floor+1;
   var data=new TerrainData{heightmapResolution=257,size=new Vector3(b.size.x,height,b.size.z)};
   var samples=new float[257,257];
   for(int z=0;z<257;z++)for(int x=0;x<257;x++){
    var position=new Vector3(Mathf.Lerp(b.min.x+.001f,b.max.x-.001f,x/256f),b.max.y+10,Mathf.Lerp(b.min.z+.001f,b.max.z-.001f,z/256f));
    Check(collision.Raycast(new Ray(position,Vector3.down),out var hit,height+30),"camera query hole "+x+","+z);
    samples[z,x]=(hit.point.y-floor)/height;
   }
   data.SetHeights(0,0,samples);AssetDatabase.CreateAsset(data,Folder+"CameraHeights.asset");
   var query=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();
   query.terrainData=data;query.transform.position=new Vector3(b.min.x,floor,b.min.z);
   query.GetComponent<TerrainCollider>().terrainData=data;query.GetComponent<TerrainCollider>().enabled=false;
   query.drawHeightmap=false;query.drawTreesAndFoliage=false;
   Check(collision.Raycast(new Ray(new Vector3(0,b.max.y+10,0),Vector3.down),out var centre,height+30),"centre spawn surface");
   Check(Vector3.Angle(centre.normal,Vector3.up)<20,"drivable centre spawn");
   var test=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TerrainTractorTest>(true)).Single();
   var tractor=test.tractor;tractor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   float bottom=tractor.wheels.Min(w=>w.transform.TransformPoint(w.center).y-w.radius-w.suspensionDistance*.5f);
   tractor.transform.position=Vector3.up*(centre.point.y-bottom+.15f);
   test.cameraRig.terrain=query;test.cameraRig.UpdateForTarget(tractor.transform,.1f,false);
   Check(scene.GetRootGameObjects().Length==6,"six minimal scene roots");
   AssetDatabase.SaveAssets();Check(EditorSceneManager.SaveScene(scene,ScenePath),"save test terrain replacement");
   var expectedMesh=filter.sharedMesh;
   scene=EditorSceneManager.OpenScene(ScenePath);
   var saved=scene.GetRootGameObjects().Single(g=>g.name=="Revised attached terrain | 1:1 metres");
   Check(saved.GetComponentsInChildren<MeshCollider>().Single().sharedMesh==expectedMesh,"saved physical mesh");
   EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
   File.WriteAllText(Log+"Validation.txt","PASS: revised terrain saved/reopened at 1:1 metres. Bounds "+b+". 66,049 camera-height collision samples supported. Centre slope "+Vector3.Angle(centre.normal,Vector3.up)+" degrees. One original tractor reseated; six roots; direct test Play retained. Source assets and game scenes untouched.");
   Capture(scene);
  }
  static void Capture(Scene scene){
   var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single();
   var previous=camera.targetTexture;var active=RenderTexture.active;
   var rt=RenderTexture.GetTemporary(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
   try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Log+"TestView.png",texture.EncodeToPNG());}
   finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);}
  }
 }
}
