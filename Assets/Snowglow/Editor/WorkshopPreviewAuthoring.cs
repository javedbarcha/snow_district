using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Snowglow.Editor {
 public static class WorkshopPreviewAuthoring {
  const int Layer=12;
  public static WorkshopPreview Build(Transform parent,MachineController[] fleet){
   var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var layer=tags.FindProperty("layers").GetArrayElementAtIndex(Layer);if(layer.stringValue!=""&&layer.stringValue!="WorkshopPreview")throw new System.InvalidOperationException("Layer 12 is already used");layer.stringValue="WorkshopPreview";tags.ApplyModifiedPropertiesWithoutUndo();
   var root=new GameObject("Workshop | authored visual display");root.transform.position=new Vector3(10000,100,10000);
   var view=root.AddComponent<WorkshopPreview>();view.vehicles=new GameObject[fleet.Length];view.parts=new GameObject[fleet.Length*2];
   var wall=Material("WorkshopWall",new Color(.32f,.39f,.44f));var steel=Material("WorkshopSteel",new Color(.10f,.16f,.20f));var yellow=Material("WorkshopSafety",new Color(1,.67f,.05f));
   Block(root.transform,"Workshop floor",new Vector3(0,-.16f,0),new Vector3(15,.25f,13),wall);
   Block(root.transform,"Workshop rear wall",new Vector3(0,2.5f,-4.8f),new Vector3(15,5.3f,.25f),wall);
   for(int i=-1;i<=1;i++){Block(root.transform,"Workshop column",new Vector3(i*4.3f,2.4f,-4.4f),new Vector3(.23f,5,.3f),steel);Block(root.transform,"Yellow safety marker",new Vector3(i*4.3f,.6f,-4.2f),new Vector3(.3f,1.3f,.35f),yellow);}
   Block(root.transform,"Workshop cross beam",new Vector3(0,4.4f,-4.1f),new Vector3(15,.3f,.4f),steel);
   for(int i=0;i<fleet.Length;i++){
    view.vehicles[i]=Copy(fleet[i].transform,root.transform,"Vehicle "+i);
    var wheel=fleet[i].wheelVisuals.FirstOrDefault(t=>t&&t.GetComponentsInChildren<MeshFilter>(true).Length>0);
    view.parts[i*2]=Copy(wheel?wheel:fleet[i].transform,root.transform,"Traction tyre "+i,3.5f);
    view.parts[i*2+1]=EnginePart(root.transform,i,steel,yellow);
   }
   string path=ProductionMenuAuthoring.ArtFolder+"/WorkshopPreview.renderTexture";
   var target=AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
   if(!target){target=new RenderTexture(768,288,24){name="Workshop 3D preview",antiAliasing=1};AssetDatabase.CreateAsset(target,path);}
   if(target.width!=768||target.height!=288){target.Release();target.width=768;target.height=288;EditorUtility.SetDirty(target);}
   var cameraObject=new GameObject("Workshop display camera");cameraObject.transform.SetParent(root.transform,false);cameraObject.layer=Layer;
   var camera=cameraObject.AddComponent<Camera>();view.previewCamera=camera;camera.targetTexture=target;camera.cullingMask=1<<Layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.25f,.30f);camera.orthographic=true;camera.orthographicSize=2.6f;camera.nearClipPlane=.1f;camera.farClipPlane=40;camera.allowHDR=false;camera.allowMSAA=false;
   camera.transform.localPosition=new Vector3(7,4.5f,8);camera.transform.LookAt(root.transform.position+Vector3.up*1.8f);cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderShadows=false;
   camera.enabled=false;root.SetActive(false);return view;
  }
  static Material Material(string name,Color color){string path=ProductionMenuAuthoring.ArtFolder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.1f);AssetDatabase.CreateAsset(m,path);}return m;}
  // Generic low-poly upgrade illustration, not a mechanical replacement for the vehicle's source model.
  static GameObject EnginePart(Transform parent,int index,Material steel,Material yellow){var root=new GameObject("Engine upgrade display "+index);root.transform.SetParent(parent,false);Block(root.transform,"Engine block",new Vector3(0,.9f,0),new Vector3(2.2f,1.2f,1.3f),steel);Block(root.transform,"Oil sump",new Vector3(0,.25f,0),new Vector3(1.8f,.4f,1.1f),steel);for(int i=0;i<3;i++){Block(root.transform,"Cylinder head",new Vector3(-.7f+i*.7f,1.65f,0),new Vector3(.6f,.45f,1.3f),yellow);Block(root.transform,"Exhaust runner",new Vector3(-.7f+i*.7f,1.1f,.85f),new Vector3(.18f,.7f,.35f),steel);}Block(root.transform,"Exhaust manifold",new Vector3(0,.8f,1.05f),new Vector3(2.1f,.25f,.25f),steel);Block(root.transform,"Flywheel casing",new Vector3(1.3f,.85f,0),new Vector3(.35f,1.25f,1.45f),yellow);root.transform.localScale=Vector3.one*1.8f;root.SetActive(false);return root;}
  static void Block(Transform parent,string name,Vector3 position,Vector3 size,Material material){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=Layer;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
  static GameObject Copy(Transform source,Transform parent,string name,float fit=5.5f){
   var root=new GameObject(name);root.transform.SetParent(parent,false);
   foreach(var filter in source.GetComponentsInChildren<MeshFilter>(true)){
    var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!filter.sharedMesh||!renderer.enabled||!filter.gameObject.activeInHierarchy)continue;
    var item=new GameObject(filter.name,typeof(MeshFilter),typeof(MeshRenderer));item.layer=Layer;item.transform.SetParent(root.transform,false);
    var matrix=source.worldToLocalMatrix*filter.transform.localToWorldMatrix;item.transform.localPosition=matrix.GetColumn(3);item.transform.localRotation=matrix.rotation;item.transform.localScale=matrix.lossyScale;
    item.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;var copy=item.GetComponent<MeshRenderer>();copy.sharedMaterials=renderer.sharedMaterials;copy.shadowCastingMode=ShadowCastingMode.Off;copy.receiveShadows=false;
   }
   var renderers=root.GetComponentsInChildren<Renderer>();if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);float scale=fit/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);root.transform.localScale=Vector3.one*scale;root.transform.position+=parent.position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);root.transform.localPosition*=scale;}
   root.SetActive(false);return root;
  }
 }
}
