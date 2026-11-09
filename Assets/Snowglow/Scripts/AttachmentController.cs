using UnityEngine;
namespace Snowglow {
 public sealed class AttachmentController : MonoBehaviour {
  public Transform mount,boom,bucket,mouth,loadVisual;public ParticleSystem spray;
  public bool Auto=true,Working=true;public float Angle,Lift,Tilt,Load;
  public bool useAuthoredRig;public float bladeRaiseDistance=.7f;
  [Range(0,1)] public float dumpMinimumLift=.55f;
  Vector3 mountRest;Quaternion mountRotation,boomRotation,bucketRotation;
  Transform rearMount,rearLugs;Vector3 rearMountRest,lugRest,lugScale;
  MachineController machine;Vector3 last;bool haveLast,dumping,lowerAfterDump;float lastVolume;
  public void Initialize(MachineController owner){machine=owner;Auto=true;mountRest=mount.localPosition;mountRotation=mount.localRotation;if(boom)boomRotation=boom.localRotation;if(bucket){bucketRotation=bucket.localRotation;rearMount=bucket.Find("Bucket rear mount | outside wall");rearLugs=bucket.Find("Rear mounting lugs");if(rearMount)rearMountRest=rearMount.localPosition;if(rearLugs){lugRest=rearLugs.localPosition;lugScale=rearLugs.localScale;}}ResetSweep();}
  public void ResetSweep(){haveLast=false;}
  public void ResetLowered(){Auto=true;Working=true;Lift=Tilt=0;dumping=lowerAfterDump=false;lastVolume=0;ResetSweep();}
  public void ResetRaised(){Auto=true;Working=false;Angle=0;Lift=machine.definition.loader?.6f:1;Tilt=machine.definition.loader?-.6f:0;dumping=false;lowerAfterDump=false;lastVolume=0;ResetSweep();}
  public void ToggleAuto(){Auto=true;}
  public void CycleAngle(){Angle=Angle<-.1f?0:Angle<.1f?1:-1;}
  public void ToggleWork(){
   if(machine.definition.loader&&!Working&&Load>0){BeginDump(true);return;}
   dumping=false;
   if(machine.definition.loader){Working=!Working;Lift=Working?0:.6f;Tilt=Working?0:-.6f;}
   else {Working=!Working;Lift=Working?0:1;}
  }
  public void RequestDump(){if(machine.definition.loader)BeginDump(false);}
  void BeginDump(bool lower){lowerAfterDump=lower;Working=false;if(Load<=0){FinishDump();return;}dumping=true;Lift=Mathf.Max(dumpMinimumLift,Lift);Tilt=1;ResetSweep();}
  void FinishDump(){dumping=false;Working=lowerAfterDump;Lift=Working?0:Mathf.Max(.6f,Lift);Tilt=Working?0:-.6f;ResetSweep();}
  public float Simulate(float dt,float forwardSpeed){
   var game=PrototypeSession.Instance;
   if(dumping&&Working)dumping=false; // Repair legacy conflicting state.
   if(dumping&&Load<=0)FinishDump();
   Auto=true; // Work/carry, full-bucket carry and dumping own the rig pose.
   UpdateAutoCarry();
   float force=0;Vector3 center=mouth.position;
   bool unloading=dumping;
   if(dumping&&Load>0){
    float accepted=game.Snow.Dump(center+machine.transform.forward*1.3f,Mathf.Min(Load,dt*.9f));Load=Mathf.Max(0,Load-accepted);
    if(accepted>0)spray.Emit(3);if(Load<=0)FinishDump();
   }
   bool low=machine.definition.loader?Lift<.18f&&Tilt<.35f:Working;
   if(!unloading&&!dumping&&low&&Working&&forwardSpeed>.08f&&center.y<SnowField.Ground+PrototypeSession.Instance.Snow.Depth(center)+.7f){
    int samples=haveLast?Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(center,last)/.45f),1,8):1;
    float budget=dt*(machine.definition.loader?1.4f:2.2f)*(1+machine.Engine*.18f);float moved=0,fresh=0;
    if(machine.definition.loader)budget=Mathf.Min(budget,Mathf.Max(0,machine.equipment.capacity-Load));
    Vector3 destination=center+machine.transform.forward*2.2f+machine.transform.right*(Angle==0?0:Mathf.Sign(Angle)*(machine.equipment.width*.5f+1.6f));
    for(int s=1;s<=samples;s++){
     Vector3 p=haveLast?Vector3.Lerp(last,center,(float)s/samples):center;
     float v=game.Snow.Work(p,machine.definition.loader?machine.transform.forward:mount.forward,machine.equipment.width,budget/samples,destination,machine.definition.loader,out float c);moved+=v;fresh+=c;
    }
    Load+=machine.definition.loader?moved:0;lastVolume=moved;
    float encountered=game.Snow.Depth(center);force=moved/dt*1700+encountered*machine.equipment.width*1200;
    force=Mathf.Min(force,machine.definition.mass*4);
    if(moved>0){spray.Emit(Mathf.Clamp(Mathf.CeilToInt(moved*80),1,10));game.RecordWork(fresh);}
    UpdateAutoCarry();
   }
   last=center;haveLast=true;return force;
  }
  void UpdateAutoCarry(){
   // Also recover a stationary/restored full bucket. Never interrupt dumping.
   if(Auto&&machine.definition.loader&&!dumping&&Load>=machine.equipment.capacity*.92f){Working=false;Lift=Mathf.Max(Lift,.6f);Tilt=-.6f;}
  }
  void Update(){
   if(machine==null)return;
   if(machine.definition.loader){float dumpPose=Mathf.Clamp01(Tilt);
    boom.localRotation=(useAuthoredRig?boomRotation:Quaternion.identity)*Quaternion.Euler(-Lift*(30+8*dumpPose),0,0);bucket.localRotation=(useAuthoredRig?bucketRotation:Quaternion.identity)*Quaternion.Euler(Tilt*65+Lift*(11.25f+3*dumpPose),0,0);
    if(rearMount)rearMount.localPosition=rearMountRest+Vector3.up*(.18f*dumpPose);
    if(rearLugs){rearLugs.localPosition=lugRest+Vector3.up*(.09f*dumpPose);rearLugs.localScale=new Vector3(lugScale.x,lugScale.y*(1+.75f*dumpPose),lugScale.z);}
    if(loadVisual!=null){loadVisual.gameObject.SetActive(Load>.001f);loadVisual.localScale=new Vector3(machine.equipment.width*.82f,Mathf.Max(.03f,Load/machine.equipment.capacity*.65f),1);}}
   else if(useAuthoredRig){mount.localPosition=mountRest+Vector3.up*(Auto?(Working?0:bladeRaiseDistance):Lift*bladeRaiseDistance);mount.localRotation=mountRotation*Quaternion.Euler(0,Angle*28,0);}
   else {mount.localPosition=new Vector3(0,Auto?(Working?-.65f:.05f):Mathf.Lerp(-.65f,.4f,Lift),2.55f);mount.localRotation=Quaternion.Euler(0,Angle*28,0);}
  }
 }
}
