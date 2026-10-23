using UnityEngine;
namespace Snowglow {
 public sealed class AttachmentController : MonoBehaviour {
  public Transform mount,boom,bucket,mouth,loadVisual;public ParticleSystem spray;
  public bool Auto=true,Working=true;public float Angle,Lift,Tilt,Load;
  MachineController machine;Vector3 last;bool haveLast,dumping;float lastVolume;
  public void Initialize(MachineController owner){machine=owner;ResetSweep();}
  public void ResetSweep(){haveLast=false;}
  public void ToggleAuto(){Auto=!Auto;}
  public void CycleAngle(){Angle=Angle<-.1f?0:Angle<.1f?1:-1;}
  public void ToggleWork(){
   if(machine.definition.loader){Working=!Working;Lift=Working?0:.6f;Tilt=Working?0:-.6f;}
   else {Working=!Working;Lift=Working?0:1;}
  }
  public void RequestDump(){if(!machine.definition.loader)return;dumping=true;Working=false;Lift=Mathf.Max(.55f,Lift);Tilt=1;}
  public float Simulate(float dt,float forwardSpeed){
   var game=PrototypeSession.Instance;var input=game.Input;
   if(!Auto){Lift=Mathf.Clamp01(Lift+input.Lift*dt*.6f);Tilt=Mathf.Clamp(Tilt+input.Tilt*dt,-1,1);}
   if(!machine.definition.loader&&!Auto)Working=Lift<.3f;
   float force=0;Vector3 center=mouth.position;
   if(dumping&&Load>.0001f){
    float accepted=game.Snow.Dump(center+machine.transform.forward*1.3f,Mathf.Min(Load,dt*.9f));Load-=accepted;
    if(accepted>0)spray.Emit(3);if(Load<.0001f){Load=0;dumping=false;}
   }
   bool low=machine.definition.loader?Lift<.18f&&Tilt<.35f:Working;
   if(low&&Working&&forwardSpeed>.08f&&center.y<SnowField.Ground+PrototypeSession.Instance.Snow.Depth(center)+.7f){
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
    if(Auto&&machine.definition.loader&&Load>=machine.equipment.capacity*.92f){Working=false;Lift=.6f;Tilt=-.6f;}
   }
   last=center;haveLast=true;return force;
  }
  void Update(){
   if(machine==null)return;
   if(machine.definition.loader){boom.localRotation=Quaternion.Euler(-Lift*40,0,0);bucket.localRotation=Quaternion.Euler(Tilt*65+Lift*15,0,0);
    if(loadVisual!=null){loadVisual.gameObject.SetActive(Load>.001f);loadVisual.localScale=new Vector3(machine.equipment.width*.82f,Mathf.Max(.03f,Load/machine.equipment.capacity*.65f),1);}}
   else {mount.localPosition=new Vector3(0,Auto?(Working?-.65f:.05f):Mathf.Lerp(-.65f,.4f,Lift),2.55f);mount.localRotation=Quaternion.Euler(0,Angle*28,0);}
  }
 }
}
