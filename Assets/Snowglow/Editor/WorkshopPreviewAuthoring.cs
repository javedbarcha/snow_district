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
   Interior(root.transform,wall,steel,yellow);
   for(int i=0;i<fleet.Length;i++){
    view.vehicles[i]=Copy(fleet[i].transform,root.transform,"Vehicle "+i);
    var wheel=fleet[i].wheelVisuals.FirstOrDefault(t=>t&&t.GetComponentsInChildren<MeshFilter>(true).Length>0);
    view.parts[i*2]=Copy(wheel?wheel:fleet[i].transform,root.transform,"Traction tyre "+i,3.5f);
    view.parts[i*2+1]=EnginePart(root.transform,i,steel,yellow);
    view.vehicles[i].transform.localPosition+=Vector3.up*.32f;view.parts[i*2].transform.localPosition+=Vector3.up*.32f;view.parts[i*2+1].transform.localPosition+=Vector3.up*.32f;
   }
   string path=ProductionMenuAuthoring.ArtFolder+"/WorkshopPreview.renderTexture";
   var target=AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
   if(!target){target=new RenderTexture(1280,642,24){name="Workshop 3D preview",antiAliasing=1};AssetDatabase.CreateAsset(target,path);}
   if(target.width!=1280||target.height!=642){target.Release();target.width=1280;target.height=642;EditorUtility.SetDirty(target);}
   var cameraObject=new GameObject("Workshop display camera");cameraObject.transform.SetParent(root.transform,false);cameraObject.layer=Layer;
   var camera=cameraObject.AddComponent<Camera>();view.previewCamera=camera;camera.targetTexture=target;camera.cullingMask=1<<Layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.25f,.30f);camera.orthographic=false;camera.fieldOfView=44;camera.nearClipPlane=.1f;camera.farClipPlane=65;camera.allowHDR=false;camera.allowMSAA=false;
   view.ApplyOrbit();cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderShadows=false;
   camera.enabled=false;root.SetActive(false);return view;
  }
  static void Interior(Transform p,Material wall,Material steel,Material yellow){
   var pale=Material("WorkshopPale",new Color(.72f,.79f,.82f));var red=Material("WorkshopRed",new Color(.65f,.09f,.055f));var rubber=Material("WorkshopRubber",new Color(.035f,.045f,.055f));var wood=Material("WorkshopWood",new Color(.43f,.27f,.13f));
   var glow=Material("WorkshopLight",new Color(.86f,.96f,1));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(.7f,.85f,1)*2);EditorUtility.SetDirty(glow);
   Block(p,"Concrete floor",new Vector3(0,-.2f,0),new Vector3(42,.4f,42),wall);
   for(int side=0;side<4;side++){
    var bay=new GameObject("Workshop bay "+side);bay.transform.SetParent(p,false);bay.transform.localRotation=Quaternion.Euler(0,side*90,0);bay.transform.localPosition=bay.transform.localRotation*new Vector3(0,0,-6);var b=bay.transform;
    Block(b,"Insulated wall",new Vector3(0,6,-14),new Vector3(42,12,.3f),wall);
    for(int x=-12;x<=12;x+=4){Block(b,"Steel upright",new Vector3(x,6,-13.7f),new Vector3(.22f,12,.35f),steel);Block(b,"Safety sleeve",new Vector3(x,.65f,-13.4f),new Vector3(.3f,1.3f,.3f),yellow);}
    Block(b,"Upper girder",new Vector3(0,11.5f,-13.4f),new Vector3(42,.35f,.45f),steel);
    Block(b,"Roller door surround",new Vector3(0,2.65f,-13.65f),new Vector3(7,5.3f,.2f),steel);
    for(int j=0;j<13;j++)Block(b,"Door slat",new Vector3(0,.25f+j*.39f,-13.45f),new Vector3(6.5f,.36f,.12f),pale);
    Block(b,"Door glazing",new Vector3(0,3.6f,-13.3f),new Vector3(5.8f,.6f,.06f),steel);
    Block(b,"Bench steel frame",new Vector3(-8,.8f,-12),new Vector3(5,1.6f,1.25f),steel);Block(b,"Workbench timber",new Vector3(-8,1.65f,-12),new Vector3(5.2f,.18f,1.5f),wood);
    Block(b,"Tool pegboard",new Vector3(-8,3,-13.2f),new Vector3(5,2,.12f),steel);
    for(int j=0;j<8;j++){float x=-10+j*.55f;Block(b,"Hanging spanner",new Vector3(x,3,-13),new Vector3(.08f,.6f,.08f),pale);Block(b,"Spanner jaw",new Vector3(x,3.3f,-13),new Vector3(.22f,.1f,.09f),pale);}
    Block(b,"Bench vice",new Vector3(-9,1.9f,-11.9f),new Vector3(.7f,.4f,.5f),red);
    Block(b,"Rolling tool chest",new Vector3(7,.9f,-12),new Vector3(2.2f,1.6f,1.2f),red);
    for(int j=0;j<5;j++)Block(b,"Drawer handle",new Vector3(7,.35f+j*.28f,-11.36f),new Vector3(1.6f,.06f,.08f),pale);
    for(int x=9;x<=12;x+=3){Block(b,"Storage shelf upright",new Vector3(x,2.5f,-12),new Vector3(.12f,5,1.5f),steel);}
    for(int j=0;j<4;j++){Block(b,"Storage shelf",new Vector3(10.5f,.3f+j*1.25f,-12),new Vector3(3.2f,.12f,1.6f),steel);for(int k=0;k<2;k++)Block(b,"Parts crate",new Vector3(9.8f+k*1.4f,.7f+j*1.25f,-12),new Vector3(1.05f,.65f,1.05f),wood);}
    for(int j=0;j<3;j++)Cylinder(b,"Spare tyre stack",new Vector3(-5,.28f+j*.5f,-10.5f),new Vector3(1.2f,.24f,1.2f),rubber);
    Cylinder(b,"Oil drum",new Vector3(5,.65f,-11),new Vector3(.9f,.65f,.9f),red);
    Cylinder(b,"Air compressor tank",new Vector3(4,1,-12),new Vector3(.65f,1,.65f),pale);
    Block(b,"Compressor motor",new Vector3(4,2.2f,-12),new Vector3(.65f,.4f,.6f),steel);
    Cylinder(b,"Fire extinguisher",new Vector3(3.8f,1.3f,-13.25f),new Vector3(.22f,.4f,.22f),red);
    Block(b,"Emergency sign",new Vector3(3.8f,2.2f,-13.2f),new Vector3(.5f,.5f,.05f),yellow);
    Block(b,"Ceiling fixture",new Vector3(0,11.8f,-6),new Vector3(5,.2f,.7f),steel);Block(b,"Light diffuser",new Vector3(0,11.65f,-6),new Vector3(4.7f,.05f,.5f),glow);
    for(int j=0;j<8;j++)Block(b,"Bay floor marking",new Vector3(-4.7f,.015f,-4+j*1.2f),new Vector3(.15f,.02f,.7f),yellow);
   }
   Block(p,"Ceiling",new Vector3(0,12.2f,0),new Vector3(42,.3f,42),steel);
   Cylinder(p,"Circular machinery stage",new Vector3(0,.14f,0),new Vector3(16,.14f,16),steel);
   for(int i=0;i<48;i++){float angle=i*Mathf.PI*2/48;var marker=new GameObject("Stage safety edge");marker.transform.SetParent(p,false);marker.transform.localRotation=Quaternion.identity;Block(marker.transform,"Yellow edge",new Vector3(Mathf.Sin(angle)*7.9f,.285f,Mathf.Cos(angle)*7.9f),new Vector3(.26f,.03f,.11f),yellow);}
   for(int i=0;i<3;i++){var go=new GameObject("Workshop soft light "+i);go.layer=Layer;go.transform.SetParent(p,false);go.transform.localPosition=new Vector3(i==0?0:i==1?-9:9,9,i==0?3:-6);var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=i==2?new Color(1,.85f,.65f):new Color(.8f,.9f,1);light.intensity=2.8f;light.range=32;light.shadows=LightShadows.None;light.cullingMask=1<<Layer;}
   // Batch the decorative room by material. Vehicles/parts remain separately switchable.
   var filters=p.GetComponentsInChildren<MeshFilter>();var groups=filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial).ToArray();
   foreach(var group in groups){var mesh=new Mesh{name="Workshop interior "+group.Key.name};mesh.indexFormat=IndexFormat.UInt32;mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=p.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());string path=ProductionMenuAuthoring.ArtFolder+"/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){existing.Clear();existing.indexFormat=IndexFormat.UInt32;existing.vertices=mesh.vertices;existing.normals=mesh.normals;existing.uv=mesh.uv;existing.tangents=mesh.tangents;existing.triangles=mesh.triangles;existing.RecalculateBounds();existing.UploadMeshData(false);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.layer=Layer;go.transform.SetParent(p,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=group.Key;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
   foreach(var f in filters)UnityEngine.Object.DestroyImmediate(f.gameObject);
  }
  static void Cylinder(Transform p,string name,Vector3 position,Vector3 scale,Material m){var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.layer=Layer;go.transform.SetParent(p,false);go.transform.localPosition=position;go.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<MeshRenderer>().sharedMaterial=m;}
  static Material Material(string name,Color color){string path=ProductionMenuAuthoring.ArtFolder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.1f);AssetDatabase.CreateAsset(m,path);}return m;}
  // Generic low-poly upgrade illustration, not a mechanical replacement for the vehicle's source model.
  static GameObject EnginePart(Transform parent,int index,Material steel,Material yellow){var root=new GameObject("Engine upgrade display "+index);root.transform.SetParent(parent,false);Block(root.transform,"Engine block",new Vector3(0,.9f,0),new Vector3(2.2f,1.2f,1.3f),steel);Block(root.transform,"Oil sump",new Vector3(0,.25f,0),new Vector3(1.8f,.4f,1.1f),steel);for(int i=0;i<3;i++){Block(root.transform,"Cylinder head",new Vector3(-.7f+i*.7f,1.65f,0),new Vector3(.6f,.45f,1.3f),yellow);Block(root.transform,"Exhaust runner",new Vector3(-.7f+i*.7f,1.1f,.85f),new Vector3(.18f,.7f,.35f),steel);}Block(root.transform,"Exhaust manifold",new Vector3(0,.8f,1.05f),new Vector3(2.1f,.25f,.25f),steel);Block(root.transform,"Flywheel casing",new Vector3(1.3f,.85f,0),new Vector3(.35f,1.25f,1.45f),yellow);root.transform.localScale=Vector3.one*1.3f;root.SetActive(false);return root;}
  static void Block(Transform parent,string name,Vector3 position,Vector3 size,Material material){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=Layer;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
  static GameObject Copy(Transform source,Transform parent,string name,float fit=0){
   var root=new GameObject(name);root.transform.SetParent(parent,false);
   foreach(var filter in source.GetComponentsInChildren<MeshFilter>(true)){
    var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!filter.sharedMesh||!renderer.enabled||!filter.gameObject.activeInHierarchy)continue;
    var item=new GameObject(filter.name,typeof(MeshFilter),typeof(MeshRenderer));item.layer=Layer;item.transform.SetParent(root.transform,false);
    var frame=fit>0?source.worldToLocalMatrix:Matrix4x4.TRS(source.position,source.rotation,Vector3.one).inverse;var matrix=frame*filter.transform.localToWorldMatrix;item.transform.localPosition=matrix.GetColumn(3);item.transform.localRotation=matrix.rotation;item.transform.localScale=matrix.lossyScale;
    item.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;var copy=item.GetComponent<MeshRenderer>();copy.sharedMaterials=renderer.sharedMaterials;copy.shadowCastingMode=ShadowCastingMode.Off;copy.receiveShadows=false;
   }
   var renderers=root.GetComponentsInChildren<Renderer>();if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);float scale=fit>0?fit/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z):1;root.transform.localScale=Vector3.one*scale;root.transform.position+=parent.position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);root.transform.localPosition*=scale;}
   root.SetActive(false);return root;
  }
 }
}
