using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Snowglow {
 // Cosmetic marks only. The snow grid remains the sole owner of snow volume.
 [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
 public sealed class SnowTyreTracks : MonoBehaviour {
  public SnowField snow;
  public MachineController[] machines;
  public float[] wheelWidths;
  [Range(128,4096)] public int capacity=2048;
  public float spacing=.24f,lifetime=100;
  struct Stamp {public Vector3 point,forward;public float width,depth,born;public int cell;public bool alive;}
  Stamp[] stamps;Vector3[] previous;bool[] connected;float[] distance;
  SnowVolumeGrid observed;Mesh mesh;int cursor;float clock,refresh;
  readonly List<Vector3> vertices=new List<Vector3>();
  readonly List<Color> colors=new List<Color>();
  readonly List<int> triangles=new List<int>();
  public int VisibleStamps {get;private set;}
  void Ensure(){
   if(stamps!=null)return;
   stamps=new Stamp[Mathf.Clamp(capacity,128,4096)];
   int count=machines==null?8:Mathf.Max(8,machines.Length*4);
   previous=new Vector3[count];connected=new bool[count];distance=new float[count];
   mesh=new Mesh{name="Transient snow tyre impressions"};mesh.MarkDynamic();
   GetComponent<MeshFilter>().sharedMesh=mesh;
  }
  public void Clear(){
   Ensure();Array.Clear(stamps,0,stamps.Length);Array.Clear(connected,0,connected.Length);
   Array.Clear(distance,0,distance.Length);cursor=0;VisibleStamps=0;mesh.Clear();
   observed=snow?snow.Grid:null;
  }
  public void BreakContact(int wheel){Ensure();if(wheel>=0&&wheel<connected.Length){connected[wheel]=false;distance[wheel]=0;}}
  bool Surface(Vector3 point,out int cell,out float depth){
   cell=snow.Grid.Index(point.x,point.z);depth=cell<0?0:snow.Grid.depth[cell];
   return cell>=0&&!snow.Grid.blocked[cell]&&depth>=.025f;
  }
  void Put(Vector3 point,Vector3 forward,float width){
   if(!Surface(point,out int cell,out float depth))return;
   stamps[cursor]=new Stamp{point=point,forward=forward,width=width,depth=depth,cell=cell,born=clock,alive=true};
   cursor=(cursor+1)%stamps.Length;
  }
  // Shared by real WheelCollider contacts and the Editor acceptance fixtures.
  public void RecordContact(int wheel,Vector3 point,Vector3 forward,float width){
   Ensure();if(wheel<0||wheel>=connected.Length||!snow)return;
   if(observed!=snow.Grid)Clear();
   if(!float.IsFinite(point.x)||!float.IsFinite(point.z)||!Surface(point,out _,out _)){BreakContact(wheel);return;}
   forward.y=0;if(forward.sqrMagnitude<.01f){BreakContact(wheel);return;}forward.Normalize();
   if(!connected[wheel]){previous[wheel]=point;connected[wheel]=true;distance[wheel]=0;return;}
   var delta=point-previous[wheel];delta.y=0;float length=delta.magnitude;
   // Recovery, load/restore or a large discontinuity must not draw a bridge.
   if(length>3){previous[wheel]=point;distance[wheel]=0;return;}
   if(length<.001f)return;
   float step=Mathf.Max(.12f,spacing),next=step-distance[wheel];
   for(float travelled=next;travelled<=length;travelled+=step){
    var sample=Vector3.Lerp(previous[wheel],point,travelled/length);
    // Sample depth again at every stamp: no strip across bare/blocked cells.
    Put(sample,forward,Mathf.Clamp(width,.18f,.85f));
   }
   distance[wheel]=(distance[wheel]+length)%step;previous[wheel]=point;
  }
  void FixedUpdate(){
   Ensure();if(!snow)return;if(observed!=snow.Grid)Clear();
   var game=PrototypeSession.Instance;
   if(game==null||!game.IsRunning){Array.Clear(connected,0,connected.Length);return;}
   for(int m=0;m<machines.Length;m++)for(int w=0;w<machines[m].wheels.Length;w++){
    int slot=m*4+w;var machine=machines[m];var wheel=machine.wheels[w];
    if(!machine.ActiveMachine||!wheel.GetGroundHit(out WheelHit hit)){BreakContact(slot);continue;}
    // The hit may be the ground below thin snow. Reject contact above the top
    // (e.g. another object) instead of painting a mark below an airborne tyre.
    float depth=snow.Depth(hit.point);
    if(hit.point.y>SnowField.Ground+depth+.12f||hit.point.y<SnowField.Ground-.5f){BreakContact(slot);continue;}
    var forward=Quaternion.AngleAxis(wheel.steerAngle,machine.transform.up)*machine.transform.forward;
    RecordContact(slot,hit.point,forward,wheelWidths!=null&&slot<wheelWidths.Length?wheelWidths[slot]:.4f);
   }
  }
  void Update(){
   if(!Application.isPlaying||!snow)return;
   var game=PrototypeSession.Instance;if(game==null||!game.IsRunning)return;
   if(observed!=snow.Grid)Clear();clock+=Time.deltaTime;refresh-=Time.deltaTime;
   if(refresh<=0){refresh=.1f;RefreshMesh();}
  }
  void Quad(Vector3 center,Vector3 right,Vector3 forward,float halfWidth,float halfLength,Color color){
   int first=vertices.Count;
   Vertex(center-right*halfWidth-forward*halfLength,color);
   Vertex(center-right*halfWidth+forward*halfLength,color);
   Vertex(center+right*halfWidth+forward*halfLength,color);
   Vertex(center+right*halfWidth-forward*halfLength,color);
   triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);triangles.Add(first);triangles.Add(first+2);triangles.Add(first+3);
  }
  void Vertex(Vector3 p,Color color){
   int cell=snow.Grid.Index(p.x,p.z);float depth=cell<0?0:snow.Grid.depth[cell];
   vertices.Add(transform.InverseTransformPoint(new Vector3(p.x,SnowField.Ground+depth+.012f,p.z)));colors.Add(color);
  }
  bool Supported(Vector3 p,float depth)=>Surface(p,out _,out float other)&&Mathf.Abs(other-depth)<=.06f;
  public void RefreshMesh(){
   Ensure();vertices.Clear();colors.Clear();triangles.Clear();VisibleStamps=0;
   for(int i=0;i<stamps.Length;i++){
    var stamp=stamps[i];if(!stamp.alive)continue;
    // Removing/dumping snow erases impressions instead of leaving floating ink.
    if(clock-stamp.born>lifetime||!Surface(stamp.point,out _,out float depth)||Mathf.Abs(depth-stamp.depth)>.06f){stamps[i].alive=false;continue;}
    var right=Vector3.Cross(Vector3.up,stamp.forward);
    // Skip the whole print at a cell edge rather than bridge a cleared patch.
    if(!Supported(stamp.point-right*stamp.width*.5f,depth)||!Supported(stamp.point+right*stamp.width*.5f,depth)||
       !Supported(stamp.point-stamp.forward*.12f,depth)||!Supported(stamp.point+stamp.forward*.12f,depth))continue;
    float fade=1-Mathf.Clamp01((clock-stamp.born)/Mathf.Max(1,lifetime));
    var tint=new Color(.32f,.57f,.64f,.32f*fade);
    // Two angled tread bars give a readable herringbone tyre impression.
    for(int side=-1;side<=1;side+=2){
     var across=(right+stamp.forward*(side*.5f)).normalized;
     var along=Vector3.Cross(across,Vector3.up);
     Quad(stamp.point+right*(side*stamp.width*.23f),across,along,stamp.width*.27f,.035f,tint);
    }
    VisibleStamps++;
   }
   mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
  }
  public void AdvanceForChecks(float seconds){clock+=Mathf.Max(0,seconds);RefreshMesh();}
  void OnDestroy(){if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
 }
}
