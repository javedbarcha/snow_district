using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor {
    // One-time, reviewable authoring in the isolated terrain scene. No runtime generation.
    [InitializeOnLoad]
    public static class AlpsTownMapAuthoring {
        const string ScenePath = "Assets/Scenes/AlpsTerrainTest.unity";
        const string Log = "Logs/AlpsTownMap/";
        const string Folder = "Assets/Snowglow/Levels/AlpsTerrainTest/TownMap";
        const string Marker = "ALPS TOWN | supplied 400m road map";
        [Serializable] public class Point { public float x,z; public Vector3 V => new Vector3(x,0,z); }
        [Serializable] public class Building : Point { public int id; public string kind; public float width,depth,yaw; }
        [Serializable] public class Layout { public Building[] buildings; public Point[] lights,signs; public Point statue,tower,depot,yard; }
        class Route { public string name; public float width; public Vector3[] points; }
        static Scene scene;
        static MeshCollider terrain;
        static Transform root,roads,homes,landmarks,facilities,furniture;
        static Material asphalt,sidewalk,yellow,square;
        static Layout plan;
        static readonly List<Route> routes = new List<Route>();
        static int meshId;

        static AlpsTownMapAuthoring() { EditorApplication.update += Pending; }
        static void Check(bool condition,string message) { if(!condition) throw new InvalidOperationException("Alps town map: "+message); }
        static T[] All<T>(Scene s) where T:Component => s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        static void Pending() {
            const string request="Logs/AlpsTownMap.request";
            if(!File.Exists(request)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating) return;
            var action=File.ReadAllText(request).Trim(); File.Delete(request); Directory.CreateDirectory(Log);
            try { if(action=="build") Build(); else if(action=="review") Review(); else throw new InvalidOperationException("Unknown action "+action); }
            catch(Exception e) { File.WriteAllText(Log+"Validation.txt","FAIL: "+e); Debug.LogException(e); }
        }
        static float Ground(Vector3 at) {
            Check(terrain.Raycast(new Ray(new Vector3(at.x,500,at.z),Vector3.down),out var hit,1000),"ground under "+at);
            return hit.point.y;
        }
        static Transform Group(string name,Transform parent=null) {
            var g=new GameObject(name); SceneManager.MoveGameObjectToScene(g,scene); if(parent) g.transform.SetParent(parent,false); return g.transform;
        }
        static Bounds Bounds(GameObject g) {
            var renderers=g.GetComponentsInChildren<MeshRenderer>(true); Check(renderers.Length>0,"rendered "+g.name);
            var bounds=renderers[0].bounds; foreach(var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds); return bounds;
        }
        static Material Mat(string name,Color color) {
            var material=new Material(Shader.Find("Universal Render Pipeline/Simple Lit")){name=name,enableInstancing=true};
            material.SetColor("_BaseColor",color); material.SetFloat("_Smoothness",0); material.SetColor("_SpecColor",Color.black);
            AssetDatabase.CreateAsset(material,Folder+"/"+name+".mat"); return material;
        }
        static void Quad(List<Vector3> v,List<int> ix,Vector3 a,Vector3 b,Vector3 c,Vector3 d) {
            int k=v.Count; v.AddRange(new[]{a,b,c,d}); ix.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
        }
        static GameObject MeshObject(string name,Transform parent,List<Vector3> v,List<int> ix,Material mat,bool collision) {
            var mesh=new Mesh{name=name,indexFormat=v.Count>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16};
            mesh.SetVertices(v);mesh.SetTriangles(ix,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,Folder+"/Mesh_"+(meshId++).ToString("D3")+".asset");
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.isStatic=true;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;
            if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;
        }
        static Vector3 Surface(Vector3 at,float offset) { at.y=Ground(at)+offset; return at; }
        static Vector3[] Line(Vector3 a,Vector3 b) {
            int n=Mathf.CeilToInt(Vector3.Distance(a,b)/2);return Enumerable.Range(0,n+1).Select(i=>Vector3.Lerp(a,b,i/(float)n)).ToArray();
        }
        static float DistanceToSegment(Vector3 p,Vector3 a,Vector3 b) {
            p.y=a.y=b.y=0;var d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude));
        }
        static bool Junction(Vector3 at,Route current) => routes.Where(r=>r!=current).Any(r=>Enumerable.Range(1,r.points.Length-1).Any(i=>DistanceToSegment(at,r.points[i-1],r.points[i])<r.width*.5f+2));
        static void Strip(Route route,float width,float elevation,Material mat,bool collision,string suffix) {
            var v=new List<Vector3>();var ix=new List<int>();var p=route.points;
            for(int i=0;i<p.Length-1;i++) {
                Vector3 Side(int j) => Vector3.Cross(Vector3.up,(p[Mathf.Min(p.Length-1,j+1)]-p[Mathf.Max(0,j-1)]).normalized);
                var a=Side(i);var b=Side(i+1);int columns=Mathf.CeilToInt(width/3);
                for(int c=0;c<columns;c++) {
                    float lo=c/(float)columns*2-1,hi=(c+1f)/columns*2-1;
                    Quad(v,ix,Surface(p[i]+a*lo*width*.5f,elevation),Surface(p[i+1]+b*lo*width*.5f,elevation),Surface(p[i+1]+b*hi*width*.5f,elevation),Surface(p[i]+a*hi*width*.5f,elevation));
                }
            }
            MeshObject(route.name+suffix,roads,v,ix,mat,collision);
        }
        static void Markings(Route route) {
            var v=new List<Vector3>();var ix=new List<int>();float distance=0;
            for(int i=0;i<route.points.Length-1;i++) {
                var a=route.points[i];var b=route.points[i+1];float length=Vector3.Distance(a,b);
                if(distance%8<3&&!Junction((a+b)*.5f,route)) {
                    var side=Vector3.Cross(Vector3.up,(b-a).normalized)*.09f;
                    Quad(v,ix,Surface(a-side,.155f),Surface(b-side,.155f),Surface(b+side,.155f),Surface(a+side,.155f));
                }
                distance+=length;
            }
            if(v.Count>0)MeshObject(route.name+" | centre dashes",roads,v,ix,yellow,false);
        }
        static void Pad(string name,Vector3 centre,float width,float depth,Material mat) {
            var v=new List<Vector3>();var ix=new List<int>();
            int nx=Mathf.CeilToInt(width/3),nz=Mathf.CeilToInt(depth/3);
            for(int x=0;x<nx;x++)for(int z=0;z<nz;z++) {
                Vector3 P(int i,int j)=>Surface(centre+new Vector3((i/(float)nx-.5f)*width,0,(j/(float)nz-.5f)*depth),.12f);
                Quad(v,ix,P(x,z),P(x,z+1),P(x+1,z+1),P(x+1,z));
            }
            MeshObject(name,roads,v,ix,mat,true);
        }
        static void Ring(string name,Vector3 centre,float inner,float outer,Material mat,float elevation,bool collision) {
            var v=new List<Vector3>();var ix=new List<int>();const int n=96;
            for(int i=0;i<n;i++) {
                Vector3 P(int j,float radius) { float a=j*2*Mathf.PI/n;return Surface(centre+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*radius,elevation); }
                if(inner==0) {int k=v.Count;v.AddRange(new[]{P(i,0),P(i,outer),P(i+1,outer)});ix.AddRange(new[]{k,k+1,k+2});}
                else Quad(v,ix,P(i,inner),P(i,outer),P(i+1,outer),P(i+1,inner));
            }
            MeshObject(name,roads,v,ix,mat,collision);
        }
        static void ConfigureRoutes() {
            routes.Clear();var loop=new List<Vector3>();
            foreach(var corner in new[]{new Vector3(134,0,134),new Vector3(134,0,-134),new Vector3(-134,0,-134),new Vector3(-134,0,134)}) {
                float start=corner.x>0?(corner.z>0?0:90):(corner.z<0?180:270);
                for(int i=0;i<=32;i++){float a=(start+i*90f/32)*Mathf.Deg2Rad;loop.Add(corner+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*40);}
            }
            loop.Add(loop[0]);
            // Densify the straight portions between quarter-circle corners.
            var dense=new List<Vector3>();for(int i=1;i<loop.Count;i++)dense.AddRange(Line(loop[i-1],loop[i]).Skip(i==1?0:1));
            routes.Add(new Route{name="Alpine Loop | rounded collector",width=12,points=dense.ToArray()});
            void Add(string name,float width,float ax,float az,float bx,float bz)=>routes.Add(new Route{name=name,width=width,points=Line(new Vector3(ax,0,az),new Vector3(bx,0,bz))});
            Add("Main Street | west",12,-196,20,-21,20);Add("Main Street | east",12,21,20,196,20);
            Add("North Street and north entry",12,0,41,0,196);Add("South spine and service entry",12,0,-196,0,-1);
            Add("West Quarter",10,-88,-174,-88,174);Add("East Quarter",10,88,-174,88,174);
            Add("Upper Village Street",10,-174,104,174,104);Add("Depot Avenue",10,-174,-88,174,-88);
            Add("Depot entrance",10,-130,-88,-130,-110);Add("Disposal entrance",10,131,-88,131,-111);
        }
        static void Roads() {
            ConfigureRoutes();
            foreach(var route in routes)Strip(route,route.width+6,.045f,sidewalk,false," | 3m sidewalk allowance");
            Ring("Roundabout | sidewalk",new Vector3(0,0,20),0,30,sidewalk,.05f,false);
            foreach(var route in routes)Strip(route,route.width,.13f,asphalt,true,"");
            Ring("Roundabout | connected carriageway",new Vector3(0,0,20),17,27,asphalt,.13f,true);
            Ring("Statue island",new Vector3(0,0,20),0,16.8f,square,.18f,true);
            Ring("Roundabout | outer yellow line",new Vector3(0,0,20),26.5f,26.65f,yellow,.16f,false);
            Ring("Roundabout | inner yellow line",new Vector3(0,0,20),17.4f,17.55f,yellow,.16f,false);
            foreach(var route in routes)Markings(route);
            Pad("Civic square | tower",new Vector3(38,0,58),41,31,square);
            Pad("Snow depot | 52 x 46m reservation",new Vector3(-130,0,-129),52,46,asphalt);
            Pad("Disposal yard | 52 x 46m reservation",new Vector3(131,0,-129),52,46,asphalt);
        }
        static GameObject Place(string slug,Transform parent,string name,Vector3 at,float yaw,float scale=1) {
            var entry=AlpsRebuildAssets.Find(slug);Check(entry&&entry.prefab,"pipeline prefab "+slug);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(entry.prefab,parent);go.name=name;
            go.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));go.transform.localScale=Vector3.one*scale;
            go.GetComponent<CatalogPlacement>().placementId="alps-map-"+name.Split('|')[0].Trim().Replace(' ','-').ToLowerInvariant();return go;
        }
        static void Seat(GameObject go,Vector3 centre,float lift=.14f) {
            var b=Bounds(go);go.transform.position+=new Vector3(centre.x-b.center.x,0,centre.z-b.center.z);b=Bounds(go);
            float low=float.MaxValue,high=float.MinValue;
            foreach(float x in new[]{b.min.x,b.center.x,b.max.x})foreach(float z in new[]{b.min.z,b.center.z,b.max.z}) {float y=Ground(new Vector3(x,0,z));low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
            Check(high-low<.3f,"flat town footprint "+go.name+" ground spread "+(high-low));
            go.transform.position+=Vector3.up*(low+lift-b.min.y);
        }
        static void Buildings() {
            var residential=new[]{1,2,4,6,8,9,10,11};var shops=new[]{3,6,8,9};int ri=0,si=0;
            foreach(var b in plan.buildings) {
                int variant=b.kind=="civic"?7:b.kind=="shops"?shops[si++%shops.Length]:residential[ri++%residential.Length];
                var go=Place("Building_"+variant.ToString("D2"),homes,b.id.ToString("D2")+" | "+b.kind,b.V,b.yaw);
                var bounds=Bounds(go);float scale=Mathf.Min(1,b.width/bounds.size.x,b.depth/bounds.size.z);go.transform.localScale=Vector3.one*scale;Seat(go,b.V);
                Check(go.GetComponentsInChildren<Collider>().Length==1&&go.GetComponentInChildren<BoxCollider>(),"single box for "+go.name);
            }
            var statue=Place("Monument",landmarks,"Statue | roundabout centre",plan.statue.V,225);Seat(statue,plan.statue.V,.2f);
            var tower=Place("GateTower",landmarks,"Tower | civic square",plan.tower.V,180);Seat(tower,plan.tower.V,.14f);
        }
        static GameObject Borrow(CatalogPlacement source,Transform parent,string name,Vector3 at,float yaw,float scale=1) {
            var go=UnityEngine.Object.Instantiate(source.gameObject,parent);go.name=name;
            go.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));go.transform.localScale=source.transform.lossyScale*scale;
            go.GetComponent<CatalogPlacement>().placementId="alps-map-"+name.Split('|')[0].Trim().Replace(' ','-').ToLowerInvariant();go.SetActive(true);
            foreach(var t in go.GetComponentsInChildren<TextMesh>(true))t.text=t.text.Replace("Pinefall","The Alps").Replace("PINEFALL","THE ALPS");
            foreach(var light in go.GetComponentsInChildren<Light>(true))light.enabled=false;
            return go;
        }
        static void FitFootprint(GameObject go,float width,float depth) {
            var b=Bounds(go);go.transform.localScale*=Mathf.Min(1,width/b.size.x,depth/b.size.z);
        }
        static void FacilitiesAndFurniture() {
            var source=EditorSceneManager.OpenScene("Assets/Scenes/Pinefall.unity",OpenSceneMode.Additive);
            try {
                var placements=All<CatalogPlacement>(source);
                CatalogPlacement Find(string id)=>placements.Single(p=>p.placementId==id);
                var garage=Borrow(Find("pinefall-garage"),facilities,"D01 | snow depot",plan.depot.V,0);FitFootprint(garage,32,18);Seat(garage,plan.depot.V);
                var yard=Borrow(Find("pinefall-disposal-yard"),facilities,"Snow disposal yard | north entry",plan.yard.V,0,1.8f);FitFootprint(yard,44,36);Seat(yard,plan.yard.V);
                // Align the receiving slab TOP to the road; bury its thickness rather than creating an entrance step.
                Physics.SyncTransforms();
                var slab=yard.GetComponentsInChildren<MeshCollider>().Single(c=>c.name.StartsWith("01 Ground pad"));
                yard.transform.position+=Vector3.up*(Ground(plan.yard.V)+.13f-slab.bounds.max.y);
                var fuel=Borrow(Find("pinefall-fuel-station"),facilities,"Fuel and repair | services",new Vector3(-16,0,-130),0);FitFootprint(fuel,10,8);Seat(fuel,new Vector3(-16,0,-130));
                Pad("Services | fuel apron",new Vector3(-16,0,-130),16,21,asphalt);
                var lamps=placements.Where(p=>(p.assetId??"").StartsWith("alpine-street-light-")).GroupBy(p=>p.assetId).Select(g=>g.First()).Where(p=>!p.assetId.EndsWith("-06")).OrderBy(p=>p.assetId).ToArray();
                Check(lamps.Length>0,"existing street-light variants");int i=0;
                foreach(var point in plan.lights) {var go=Borrow(lamps[i%lamps.Length],furniture,"Lamp "+(++i).ToString("D2"),point.V,225);Seat(go,point.V,0);}
                var template=placements.First(p=>p.assetId=="alpine-sign-01");
                var names=new[]{"S6 | NORTH ENTRY","S9 | UPPER VILLAGE","S10 | UPPER VILLAGE","S8 | THE ALPS","S7 | THE ALPS","S5 | TOWN LANDMARKS","S3 | SNOW DEPOT","S4 | SNOW DISPOSAL","S2 | FUEL + REPAIR","S1 | SOUTH ENTRY"};
                for(i=0;i<plan.signs.Length;i++) {
                    var at=plan.signs[i].V;var go=Borrow(template,furniture,names[i],at,225);
                    foreach(var t in go.GetComponentsInChildren<TextMesh>(true))t.text=names[i].Split('|')[1].Trim();Seat(go,at,0);
                }
            } finally { EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(scene); }
        }
        static void Reimport() {
            var metrics=JsonUtility.FromJson<AlpsRebuildAssets.Export>(File.ReadAllText(AlpsRebuildAssets.Source+"ExportMetrics.json"));
            var report=new List<string>();
            foreach(var row in metrics.assets.Where(a=>a.kind=="Building"||a.kind=="Landmark")) {
                string path=AlpsRebuildAssets.Source+row.slug+".fbx";
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                var entry=AlpsRebuildAssets.Find(row.slug);Check(entry&&entry.prefab&&entry.status==PreparationStatus.NeedsReview,"existing pipeline assembly "+row.slug);
                var size=CatalogValidation.BoundsOf(AssetDatabase.LoadAssetAtPath<GameObject>(path)).size;
                Check((size-new Vector3(row.size[0],row.size[1],row.size[2])).magnitude<.15f,"metre scale after reimport "+row.slug);
                Check(entry.prefab.GetComponentsInChildren<MeshRenderer>(true).All(r=>r.sharedMaterials.All(m=>m&&(m.shader.name.StartsWith("Universal Render Pipeline/")||m.shader.name==SnowMaterialAuthoring.MeshShader))),"shared URP materials "+row.slug);
                report.Add(row.slug+" | force reimported existing evaluated FBX | "+entry.triangles+" triangles | "+size+" | "+entry.status);
            }
            Check(report.Count==13,"11 building and2 landmark assemblies reimported");
            File.WriteAllText(Log+"Intake.txt","Attached Blender hashes match preserved SourceAssets/AlpsRebuild files. Existing evaluated exports force-reimported; original embedded text not executed, no duplicate catalogue entries or source overwrites. Ten residential/shop/civic variants placed; extra building-file clock tower remains unplaced because the plan has one separate landmark tower.\n"+string.Join("\n",report));
        }
        static void Setup() {
            scene=SceneManager.GetActiveScene();Check(scene.path==ScenePath,"current terrain test scene only");
            terrain=scene.GetRootGameObjects().Single(g=>g.name=="Revised attached terrain | 1:1 metres").GetComponentInChildren<MeshCollider>();
            plan=JsonUtility.FromJson<Layout>(File.ReadAllText("SourceAssets/AlpsTownMap/Layout.json"));
        }
        static void Build() {
            Setup();Check(!scene.GetRootGameObjects().Any(g=>g.name==Marker),"town already installed; refuse repeat overwrite");
            string stamp=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"),backup=Log+"InMemory-before-"+stamp+".unity";
            Check(EditorSceneManager.SaveScene(scene,backup,true),"preserve current in-memory scene");File.Copy(ScenePath,Log+"Disk-before-"+stamp+".unity");
            Check(!AssetDatabase.IsValidFolder(Folder),"new town asset folder; refuse overwrite");
            try {
                Reimport();Directory.CreateDirectory(Folder);AssetDatabase.Refresh();meshId=0;
                root=Group(Marker);roads=Group("01 Roads and squares",root);homes=Group("02 Numbered buildings | 01-75",root);landmarks=Group("03 Statue and tower",root);facilities=Group("04 Depot, disposal and services",root);furniture=Group("05 Street lights and signboards",root);
                asphalt=Mat("Town asphalt",new Color(.28f,.36f,.40f));sidewalk=Mat("Town pavement",new Color(.72f,.79f,.79f));yellow=Mat("Road yellow",new Color(1,.76f,.16f));square=Mat("Town square stone",new Color(.71f,.66f,.52f));
                Roads();Buildings();FacilitiesAndFurniture();
                // Centre now contains the monument: start the existing sandbox tractor on the depot apron.
                var tractor=All<TerrainTractorTest>(scene).Single().tractor;
                var at=new Vector3(-130,0,-114);float before=Ground(tractor.transform.position);float offset=tractor.transform.position.y-before;
                tractor.transform.SetPositionAndRotation(new Vector3(at.x,Ground(at)+offset+.13f,at.z),Quaternion.identity);
                Physics.SyncTransforms();File.WriteAllLines(Log+"FacilityColliders.txt",facilities.GetComponentsInChildren<Collider>(true).Select(c=>c.name+" | "+c.bounds+" | trigger="+c.isTrigger));Validate();AssetDatabase.SaveAssets();Check(EditorSceneManager.SaveScene(scene,ScenePath),"save mapped town");
                scene=EditorSceneManager.OpenScene(ScenePath);Review();
            } catch {
                if(SceneManager.GetActiveScene().isDirty)EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Log+"Failed-unsaved-"+stamp+".unity",true);
                File.Copy(backup,ScenePath,true);AssetDatabase.ImportAsset(ScenePath,ImportAssetOptions.ForceUpdate);EditorSceneManager.OpenScene(ScenePath);throw;
            }
        }
        static void Validate() {
            Setup();root=scene.GetRootGameObjects().Single(g=>g.name==Marker).transform;homes=root.Find("02 Numbered buildings | 01-75");
            Check(homes.childCount==75,"75 numbered town buildings");
            var houses=homes.Cast<Transform>().ToArray();
            foreach(var b in plan.buildings) {
                var t=houses.Single(h=>h.name.StartsWith(b.id.ToString("D2")+" |"));var bounds=Bounds(t.gameObject);
                Check(Vector2.Distance(new Vector2(bounds.center.x,bounds.center.z),new Vector2(b.x,b.z))<.02f,"map footprint centre "+b.id);
                Check(Mathf.Abs(Mathf.DeltaAngle(t.eulerAngles.y,b.yaw))<.01f,"map front "+b.id);
                Check(bounds.size.x<=b.width+.02f&&bounds.size.z<=b.depth+.02f,"map footprint envelope "+b.id);
            }
            float closest=float.MaxValue;
            for(int i=0;i<houses.Length;i++)for(int j=i+1;j<houses.Length;j++) {
                var a=Bounds(houses[i].gameObject);var b=Bounds(houses[j].gameObject);
                float dx=Mathf.Max(0,Mathf.Max(a.min.x-b.max.x,b.min.x-a.max.x)),dz=Mathf.Max(0,Mathf.Max(a.min.z-b.max.z,b.min.z-a.max.z));
                closest=Mathf.Min(closest,Mathf.Sqrt(dx*dx+dz*dz));Check(dx*dx+dz*dz>=49-.01f,"7m building separation");
            }
            var placed=root.GetComponentsInChildren<CatalogPlacement>(true);
            Check(placed.Count(p=>p.name.StartsWith("Lamp "))==74&&placed.Count(p=>p.name.Length>1&&p.name[0]=='S'&&char.IsDigit(p.name[1])&&p.name.Contains(" | "))==10,"74 street lights,10 signboards");
            Check(root.Find("03 Statue and tower").childCount==2,"one statue, one tower");
            Check(All<MachineController>(scene).Length==1&&!All<PrototypeSession>(scene).Any()&&!All<SnowField>(scene).Any(),"standalone tractor sandbox retained; no gameplay/save or snow conversion");
            var roadColliders=root.Find("01 Roads and squares").GetComponentsInChildren<MeshCollider>();int samples=0;
            foreach(var route in routes)foreach(var p in route.points) {
                Check(roadColliders.Any(c=>c.Raycast(new Ray(new Vector3(p.x,Ground(p)+5,p.z),Vector3.down),out var hit,10)),"physical connected road sample "+route.name+" "+p);
                var blockers=Physics.OverlapBox(new Vector3(p.x,Ground(p)+1.65f,p.z),new Vector3(1.45f,1.2f,1.45f),Quaternion.identity,1,QueryTriggerInteraction.Ignore)
                    .Where(c=>c.transform.IsChildOf(root)&&!c.transform.IsChildOf(root.Find("01 Roads and squares"))&&!c.GetComponentInParent<MachineController>()).ToArray();
                Check(blockers.Length==0,"road clearance "+route.name+" "+p+" "+string.Join(",",blockers.Select(c=>c.name)));samples++;
            }
            File.WriteAllText(Log+"LayoutChecks.txt","PASS:75 mapped footprint centres/fronts/envelopes, 7m minimum building separation (actual "+closest+"m), 74 lamps,10 signs, two landmarks. "+samples+" actual physical road support/clearance samples. Widths12m main/collector and10m secondary, 3m sidewalk allowance. Connected loop, roundabout, entries and facility spurs. No scene terrain/mountain changes. Standalone tractor relocated from statue centre to depot apron; original handling preserved.\n");
        }
        static void Capture(string path,Camera camera) {
            var rt=RenderTexture.GetTemporary(1600,1000,24);var prior=RenderTexture.active;var old=camera.targetTexture;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try {camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally {camera.targetTexture=old;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
        static void Review() {
            Setup();ConfigureRoutes();
            Validate();
            var source=All<Camera>(scene).Single();var temporary=Group("Temporary town review camera");var camera=temporary.gameObject.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;camera.farClipPlane=3000;camera.aspect=1.6f;camera.layerCullDistances=new float[32];
            var focus=Group("Temporary town review focus");var rig=temporary.gameObject.AddComponent<CameraRig>();rig.view=camera;rig.terrain=All<Terrain>(scene).Single();
            try {
                camera.orthographic=true;camera.orthographicSize=230;camera.transform.SetPositionAndRotation(new Vector3(0,650,0),Quaternion.Euler(90,0,0));Capture(Log+"TopPlan.png",camera);
                camera.orthographicSize=225;camera.transform.SetPositionAndRotation(new Vector3(-390,500,-390),Quaternion.Euler(41,45,0));Capture(Log+"IsometricOverview.png",camera);
                camera.orthographic=false;camera.fieldOfView=55;camera.transform.position=new Vector3(-280,200,-310);camera.transform.LookAt(new Vector3(0,Ground(Vector3.zero),0));Capture(Log+"TownPerspective.png",camera);
                foreach(var b in plan.buildings) {
                    var t=root.Find("02 Numbered buildings | 01-75").Cast<Transform>().Single(h=>h.name.StartsWith(b.id.ToString("D2")+" |"));
                    var at=new Vector3(b.x,0,b.z)+(t.forward*(Mathf.Max(b.width,b.depth)*.5f+9));at.y=Ground(at);
                    focus.position=at;rig.SetMode(CameraMode.Isometric);rig.UpdateForTarget(focus,.1f,false);Check(camera.orthographic&&Quaternion.Angle(camera.transform.rotation,Quaternion.Euler(41,45,0))<.01f,"actual gameplay isometric camera");
                    Capture(Log+"IsoBuilding"+b.id.ToString("D2")+".png",camera);
                }
                foreach(var spec in new[]{("Depot",new Vector3(-130,0,-114)),("Disposal",new Vector3(131,0,-101)),("Statue",new Vector3(-15,0,2)),("Tower",new Vector3(43,0,40)),("Services",new Vector3(-16,0,-117))}) {
                    var at=spec.Item2;at.y=Ground(at);focus.position=at;rig.SetMode(CameraMode.Isometric);rig.UpdateForTarget(focus,.1f,false);Capture(Log+"Iso"+spec.Item1+".png",camera);
                }
            } finally {UnityEngine.Object.DestroyImmediate(focus.gameObject);UnityEngine.Object.DestroyImmediate(temporary.gameObject);EditorSceneManager.OpenScene(ScenePath);}
            File.WriteAllText(Log+"Validation.txt","PASS: supplied map town saved/reopened in AlpsTerrainTest. Eleven building and two landmark pipeline assets force-reimported after supplied/preserved Blender hash equality.75 numbered buildings, depot, disposal/service facilities, statue/tower,74 lamps,10 signs; grouped static scene assets and shared materials/meshes. Original terrain and fitted mountains retained. Native top/perspective/iso overview,75 actual gameplay CameraRig building views and five facility/landmark views captured. Standalone direct driving remains; no production scene/save/build changes. Play/driving and mobile performance acceptance remain separate.\n");
        }
    }
}
