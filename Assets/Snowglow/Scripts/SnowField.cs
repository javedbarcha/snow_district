using UnityEngine;
using System.Collections.Generic;
namespace Snowglow {
 public sealed class SnowField : MonoBehaviour {
  public PrototypePalette palette;public SnowSeed seed;[System.NonSerialized]public SnowVolumeGrid Grid=new SnowVolumeGrid();
  public const float Ground=5.13f;
  public const int Chunk=32,Across=SnowVolumeGrid.Size/Chunk;Mesh cube;
  readonly Dictionary<int,MeshFilter> chunks=new Dictionary<int,MeshFilter>();
  readonly HashSet<int> dirty=new HashSet<int>();readonly Queue<int> queue=new Queue<int>();
  readonly List<CombineInstance> combine=new List<CombineInstance>(256);
  readonly List<int> indices=new List<int>();
  float disposalTimer;
  readonly Dictionary<int,SnowSupportSurface> supports=new Dictionary<int,SnowSupportSurface>();
  readonly Queue<int> supportQueue=new Queue<int>();readonly HashSet<int> supportQueued=new HashSet<int>();
#if UNITY_EDITOR
  [System.NonSerialized]public bool DiagnosticFreezeCollision;
#endif
  public float TargetCleared {get;private set;}public float TargetArea {get;private set;}
  public int ContractKind {get;private set;}

  public void Initialize(int kind,SnowVolumeGrid restored=null) {
   ContractKind=kind;
   var source=SnowVolumeGrid.Upgrade(restored??seed.grid);
   if(source==null||seed.grid.depth.Length!=SnowVolumeGrid.Size*SnowVolumeGrid.Size)throw new System.InvalidOperationException("Update the saved snow using Snowglow > Rebuild snow at 50 cm resolution before Play.");
   Grid=new SnowVolumeGrid{depth=(float[])source.depth.Clone(),credit=(float[])source.credit.Clone(),blocked=(bool[])seed.grid.blocked.Clone(),initialVolume=source.initialVolume,disposed=source.disposed,revision=source.revision,authoredLayoutVersion=source.authoredLayoutVersion};
   if(restored!=null)MigrateAuthoredLayout(Grid);
   cube=GetComponent<MeshFilter>().sharedMesh;
   foreach(Transform child in transform) {
    var mf=child.GetComponent<MeshFilter>();if(mf==null)continue;
    int id=int.Parse(child.name.Split('|')[0]);chunks[id]=mf;
    mf.sharedMesh=Instantiate(mf.sharedMesh);var mc=child.GetComponent<MeshCollider>();if(mc.sharedMesh!=null)mc.sharedMesh=Instantiate(mc.sharedMesh);
    if(restored!=null)MarkChunk(id);
   }
   foreach(int id in chunks.Keys)RebuildSupport(id,true);
   Grid.WakePiles();Recount();
  }
#if UNITY_EDITOR
  public void Bake(PrototypePalette p,SnowSeed target) {
   palette=p;seed=target;ContractKind=0;
   var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;DestroyImmediate(temp);
   var source=gameObject.AddComponent<MeshFilter>();source.sharedMesh=cube;
   Seed();Physics.SyncTransforms();
   for(int i=0;i<Grid.depth.Length;i++)Grid.blocked[i]=Physics.CheckBox(new Vector3(Grid.X(i),Ground+.85f,Grid.Z(i)),new Vector3(SnowVolumeGrid.Cell*.45f,.65f,SnowVolumeGrid.Cell*.45f),Quaternion.identity,1<<0,QueryTriggerInteraction.Ignore);
   seed.grid=Grid;
   for(int z=0;z<Across;z++)for(int x=0;x<Across;x++) {
    int id=z*Across+x;var go=new GameObject(id+"| Snow chunk "+x+","+z+" | combined cubes");go.transform.SetParent(transform,false);go.layer=9;
    go.AddComponent<MeshCollider>();var mf=go.AddComponent<MeshFilter>();mf.sharedMesh=new Mesh{name="Snow visuals "+id};go.AddComponent<MeshRenderer>().sharedMaterial=p.snow;chunks[id]=mf;Rebuild(id);
   }
   Recount();
  }
  public void RebuildResolution(SnowSeed target){
   var upgraded=SnowVolumeGrid.Upgrade(seed.grid);if(upgraded==null)throw new System.InvalidOperationException("Unsupported snow seed dimensions.");
   cube=GetComponent<MeshFilter>().sharedMesh;chunks.Clear();dirty.Clear();queue.Clear();supports.Clear();supportQueue.Clear();supportQueued.Clear();
   while(transform.childCount>0)DestroyImmediate(transform.GetChild(0).gameObject);
   Grid=upgraded;target.grid=upgraded;seed=target;
   for(int z=0;z<Across;z++)for(int x=0;x<Across;x++){
    int id=z*Across+x;var go=new GameObject(id+"| Snow chunk "+x+","+z+" | 50 cm columns");go.transform.SetParent(transform,false);go.layer=9;
    go.AddComponent<MeshCollider>();var mf=go.AddComponent<MeshFilter>();mf.sharedMesh=new Mesh{name="Snow visuals "+id};go.AddComponent<MeshRenderer>().sharedMaterial=palette.snow;chunks[id]=mf;Rebuild(id);
   }
   Recount();
  }
#endif
  public void SetContract(int kind){ContractKind=kind;Recount();}
  public void ResetToSeed(){var src=seed.grid;Grid=new SnowVolumeGrid{depth=(float[])src.depth.Clone(),credit=(float[])src.credit.Clone(),blocked=(bool[])src.blocked.Clone(),initialVolume=src.initialVolume,authoredLayoutVersion=src.authoredLayoutVersion};ReplaceGrid(Grid);}
  public void ReplaceGrid(SnowVolumeGrid grid){Grid=grid;Grid.WakePiles();foreach(int id in chunks.Keys)MarkChunk(id);Recount();}
  void Seed() {
   Grid=new SnowVolumeGrid{authoredLayoutVersion=CurrentLayoutVersion};
   for(int i=0;i<Grid.depth.Length;i++) {
    float x=Grid.X(i),z=Grid.Z(i);
    bool roads=(Mathf.Abs(x)<113&&NearLane(z))||(Mathf.Abs(z)<113&&NearLane(x));
    bool parking=x>74&&x<110&&z>7&&z<47;
    bool connection=x>60&&x<95&&Mathf.Abs(z-27)<5;
    bool depot=x>-115&&x<-75&&z>-110&&z<-70;
    if(roads||parking||connection)Grid.depth[i]=.22f+.08f*Mathf.PerlinNoise(x*.1f+20,z*.1f+20);
    if(depot||DepotAccessStartsClear(x,z))Grid.depth[i]=0;
    // Keep curb/fence lots snow-free in the data field. Terrain is decorative blanket snow.
    if(IsTarget(x,z))Grid.credit[i]=Grid.depth[i];
   }
   // Starter loader pile, built from volume data rather than a decorative mesh.
   for(int i=0;i<Grid.depth.Length;i++){float dx=Grid.X(i)-94,dz=Grid.Z(i)-18;float r=Mathf.Sqrt(dx*dx+dz*dz);if(r<3.5f)Grid.depth[i]+=(1-r/3.5f)*1.7f;}
   Grid.initialVolume=Grid.Total();
  }
  // Authored access layout: retain the two main roads but start them snow-free.
  // This only affects initial authoring; snow can still be pushed here in Play.
  public static bool DepotAccessStartsClear(float x,float z)=>
   (x> -113&&x< -60&&Mathf.Abs(z+65)<5)||
   (z> -113&&z< -60&&Mathf.Abs(x+65)<5)||
   (x> -95&&x< -60&&Mathf.Abs(z+90)<5);
  public const int CurrentLayoutVersion=1;
  public static bool RemovedAccessSnow(float x,float z)=>DepotAccessStartsClear(x,z)||(x>70&&x<95&&Mathf.Abs(z+85)<5);
  // Apply the authored access cleanup once to old resumed jobs. This is a layout
  // adjustment, not disposal or rewarded work; later player-deposited snow stays.
  public static float MigrateAuthoredLayout(SnowVolumeGrid grid){
   if(grid.authoredLayoutVersion>=CurrentLayoutVersion)return 0;
   double removed=0;
   for(int i=0;i<grid.depth.Length;i++)if(RemovedAccessSnow(grid.X(i),grid.Z(i))){removed+=grid.depth[i]*SnowVolumeGrid.Area;grid.depth[i]=0;grid.credit[i]=0;}
   grid.initialVolume-=(float)removed;grid.authoredLayoutVersion=CurrentLayoutVersion;
   return (float)removed;
  }
  static bool NearLane(float v){return Mathf.Abs(v)<5||Mathf.Abs(v-65)<5||Mathf.Abs(v+65)<5;}
  // X/Y of this Rect represent world X/Z. Shared by progress and saved venue visuals.
  public static Rect ContractBounds(int kind){
   if(kind==1)return Rect.MinMaxRect(77,9,108,33);
   if(kind==2)return Rect.MinMaxRect(-35,-4.5f,35,4.5f);
   return Rect.MinMaxRect(-59,-69.5f,-29,-60.5f);
  }
  public bool IsTarget(float x,float z){var bounds=ContractBounds(ContractKind);return x>bounds.xMin&&x<bounds.xMax&&z>bounds.yMin&&z<bounds.yMax;}
  public void Recount() {
   TargetArea=TargetCleared=0;
   for(int i=0;i<Grid.depth.Length;i++)if(IsTarget(Grid.X(i),Grid.Z(i))){TargetArea+=SnowVolumeGrid.Area;if(Grid.depth[i]<.07f)TargetCleared+=SnowVolumeGrid.Area;}
  }
  public float Progress=>TargetArea<=0?0:TargetCleared/TargetArea;
  public float Depth(Vector3 p){int i=Grid.Index(p.x,p.z);return i<0?0:Grid.depth[i];}
  public void Mark(int index){
   int x=index%SnowVolumeGrid.Size,z=index/SnowVolumeGrid.Size;
   // Shared border samples must be refreshed on both sides of a chunk seam.
   for(int cz=Mathf.Max(0,z-3)/Chunk;cz<=Mathf.Min(SnowVolumeGrid.Size-1,z+3)/Chunk;cz++)
    for(int cx=Mathf.Max(0,x-3)/Chunk;cx<=Mathf.Min(SnowVolumeGrid.Size-1,x+3)/Chunk;cx++)MarkChunk(cz*Across+cx);
  }
  void MarkChunk(int id){if(dirty.Add(id))queue.Enqueue(id);}
  public float Work(Vector3 center,Vector3 forward,float width,float capacity,Vector3 destination,bool collect,out float credited) {
   credited=0;if(!collect&&!Grid.CanDeposit(destination.x,destination.z))return 0;
   float moved=0;indices.Clear();Vector3 right=Vector3.Cross(Vector3.up,forward);
   int minX=Mathf.Max(0,Mathf.FloorToInt((center.x-SnowVolumeGrid.Origin-width)/SnowVolumeGrid.Cell)),maxX=Mathf.Min(SnowVolumeGrid.Size-1,Mathf.CeilToInt((center.x-SnowVolumeGrid.Origin+width)/SnowVolumeGrid.Cell));
   int minZ=Mathf.Max(0,Mathf.FloorToInt((center.z-SnowVolumeGrid.Origin-width)/SnowVolumeGrid.Cell)),maxZ=Mathf.Min(SnowVolumeGrid.Size-1,Mathf.CeilToInt((center.z-SnowVolumeGrid.Origin+width)/SnowVolumeGrid.Cell));
   for(int z=minZ;z<=maxZ;z++)for(int x=minX;x<=maxX;x++) {
    int i=z*SnowVolumeGrid.Size+x;Vector3 d=new Vector3(Grid.X(i)-center.x,0,Grid.Z(i)-center.z);
    if(Mathf.Abs(Vector3.Dot(d,right))<=width*.5f+SnowVolumeGrid.Cell*.35f&&Mathf.Abs(Vector3.Dot(d,forward))<.8f&&Grid.depth[i]>.001f)indices.Add(i);
   }
   foreach(int i in indices) {
    if(capacity<=.000001f)break;
    float desired=Mathf.Min(Grid.depth[i]*SnowVolumeGrid.Area,capacity);
    // Deposit first: an obstructed destination cannot silently destroy snow.
    float accepted=collect?desired:Grid.Deposit(destination.x,destination.z,desired,Mark);
    float taken=Grid.Take(i,accepted,out float fresh);Mark(i);credited+=fresh;moved+=taken;capacity-=taken;
   }return moved;
  }
  // Recovery unloads before teleporting, preserving the volume ledger even near
  // blocked cells or the playable snow boundary. Prefer the nearest discharge.
  public float DropRecoveryLoad(Vector3 position,float amount)=>DropRecoveryLoad(Grid,position,amount,Mark);
  public static float DropRecoveryLoad(SnowVolumeGrid grid,Vector3 position,float amount,System.Action<int> changed){
   float min=SnowVolumeGrid.Origin+5,max=SnowVolumeGrid.Origin+SnowVolumeGrid.Size*SnowVolumeGrid.Cell-5;
   position.x=Mathf.Clamp(position.x,min,max);position.z=Mathf.Clamp(position.z,min,max);
   float left=amount;left-=grid.Deposit(position.x,position.z,left,changed);
   for(float radius=2;left>.000001f&&radius<32;radius+=2)for(int side=0;side<8&&left>.000001f;side++){
    float angle=side*Mathf.PI*.25f;left-=grid.Deposit(position.x+Mathf.Cos(angle)*radius,position.z+Mathf.Sin(angle)*radius,left,changed);
   }
   return amount-left;
  }
  public float Dump(Vector3 position,float amount) {return Grid.Deposit(position.x,position.z,amount,Mark);}
  void Update() {
   var game=PrototypeSession.Instance;
   if(game!=null&&game.IsRunning)Grid.Settle(Time.deltaTime,512,Mark);
   // Budget mesh work. Physics obstruction uses only substantial snow columns.
   for(int n=0;n<2&&queue.Count>0;n++){int id=queue.Dequeue();dirty.Remove(id);Rebuild(id);}
   if(game!=null&&game.IsRunning)for(int n=0;n<2&&supportQueue.Count>0;n++){int id=supportQueue.Dequeue();supportQueued.Remove(id);RebuildSupport(id,false);}
   if(Time.frameCount%15==0)Recount();
   if(game!=null&&game.IsRunning){disposalTimer+=Time.deltaTime;if(disposalTimer>=1){ProcessDisposal();disposalTimer-=1;}}
  }
  void ProcessDisposal() {
   float budget=1.5f;
   for(int z=(int)(30/SnowVolumeGrid.Cell);z<(int)(57/SnowVolumeGrid.Cell)&&budget>0;z++)for(int x=(int)(207/SnowVolumeGrid.Cell);x<(int)(237/SnowVolumeGrid.Cell)&&budget>0;x++) {
    int i=z*SnowVolumeGrid.Size+x;float excess=Mathf.Max(0,Grid.depth[i]-.45f)*SnowVolumeGrid.Area;float take=Grid.Take(i,Mathf.Min(excess,budget),out float unused);
    if(take<=0)continue;Grid.disposed+=take;budget-=take;Mark(i);
   }
  }
  void Rebuild(int id) {
   int cx=id%Across,cz=id/Across;combine.Clear();
   for(int z=cz*Chunk;z<(cz+1)*Chunk;z++)for(int x=cx*Chunk;x<(cx+1)*Chunk;x++) {
    int i=z*SnowVolumeGrid.Size+x;float d=Grid.depth[i];if(d<.004f)continue;
    var c=new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(new Vector3(Grid.X(i),Ground+d*.5f,Grid.Z(i)),Quaternion.identity,new Vector3(SnowVolumeGrid.Cell*.995f,d,SnowVolumeGrid.Cell*.995f))};combine.Add(c);
   }
   var mf=chunks[id];mf.sharedMesh.Clear();if(combine.Count>0)mf.sharedMesh.CombineMeshes(combine.ToArray(),true,true);
#if UNITY_EDITOR
   if(DiagnosticFreezeCollision)return;
#endif
   RebuildSupport(id,!Application.isPlaying);
  }
  void RebuildSupport(int id,bool immediate){
#if UNITY_EDITOR
   if(DiagnosticFreezeCollision)return;
#endif
   var collider=chunks[id].GetComponent<MeshCollider>();
   if(!supports.TryGetValue(id,out var support)){
    var old=collider.sharedMesh;collider.sharedMesh=null;collider.enabled=false;
    if(old!=null){if(Application.isPlaying)Destroy(old);else if(!IsPersistentMesh(old))DestroyImmediate(old);}
    support=new SnowSupportSurface();supports.Add(id,support);
   }
   if(support.Update(Grid,id%Across,id/Across,collider,immediate)&&supportQueued.Add(id))supportQueue.Enqueue(id);
  }
  static bool IsPersistentMesh(Object value){
#if UNITY_EDITOR
   return UnityEditor.EditorUtility.IsPersistent(value);
#else
   return false;
#endif
  }
#if UNITY_EDITOR
  public void RefreshSupportForValidation(){foreach(int id in chunks.Keys)RebuildSupport(id,true);}
#endif
  void OnDestroy(){if(!Application.isPlaying)return;var meshes=new HashSet<Mesh>();foreach(var mf in chunks.Values)if(mf!=null){if(mf.sharedMesh!=null)meshes.Add(mf.sharedMesh);var c=mf.GetComponent<MeshCollider>();if(c.sharedMesh!=null)meshes.Add(c.sharedMesh);}foreach(var support in supports.Values)if(support.mesh!=null)meshes.Add(support.mesh);foreach(var mesh in meshes)Destroy(mesh);}
 }
}
