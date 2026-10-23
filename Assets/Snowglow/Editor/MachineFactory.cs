using UnityEngine;
namespace Snowglow.Editor {
 public static class MachineFactory {
  public static MachineController Build(VehicleDefinition d,AttachmentDefinition a,PrototypePalette p,Vector3 position){
   var root=PrimitiveKit.Group(d.displayName+" | authored primitive machinery",null,position);root.gameObject.layer=8;
   var body=root.gameObject.AddComponent<Rigidbody>();body.mass=d.mass+a.mass;
   var c=root.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.15f,0);c.size=new Vector3(d.track,.8f,d.wheelbase+1);
   var m=root.gameObject.AddComponent<MachineController>();m.definition=d;m.equipment=a;m.Fuel=d.fuelCapacity;
   var rig=PrimitiveKit.Group("VisualRoot | replace body meshes here",root,Vector3.zero);
   Material paint=d.loader?p.amber:p.orange;
   PrimitiveKit.Cube("Chassis",rig,Vector3.zero,new Vector3(d.track,.48f,d.wheelbase+1),p.dark);
   PrimitiveKit.Cube("Engine housing",rig,new Vector3(0,.55f,d.loader?-1.15f:1.1f),new Vector3(d.track*.9f,1,d.loader?1.5f:1.7f),paint);
   var cab=PrimitiveKit.Group("Cab assembly",rig,new Vector3(0,.9f,d.loader?.1f:-.55f));
   PrimitiveKit.Cube("Cab floor",cab,Vector3.zero,new Vector3(d.track*.85f,.2f,1.65f),paint);
   PrimitiveKit.Cube("Cab glazing",cab,new Vector3(0,.75f,0),new Vector3(d.track*.76f,1.4f,1.5f),p.glass);
   PrimitiveKit.Cube("Roof",cab,new Vector3(0,1.5f,0),new Vector3(d.track*.94f,.18f,1.9f),paint);
   PrimitiveKit.Cube("Roof snow",cab,new Vector3(0,1.62f,0),new Vector3(d.track*.92f,.1f,1.85f),p.snow);
   foreach(float x in new[]{-.7f,.7f})foreach(float z in new[]{-.7f,.7f})PrimitiveKit.Cube("Cab pillar",cab,new Vector3(x,.75f,z),new Vector3(.09f,1.5f,.09f),paint);
   foreach(float side in new[]{-1f,1}){
    PrimitiveKit.Cube("Door",cab,new Vector3(side*d.track*.4f,.15f,0),new Vector3(.08f,.55f,1.3f),paint);
    PrimitiveKit.Cube("Door handle",cab,new Vector3(side*d.track*.44f,.5f,-.15f),new Vector3(.07f,.07f,.28f),p.dark);
    PrimitiveKit.Beam("Mirror arm",cab,new Vector3(side*.75f,1,.65f),new Vector3(side*1.2f,1,.75f),.05f,p.dark);
    PrimitiveKit.Cube("Mirror",cab,new Vector3(side*1.2f,1,.75f),new Vector3(.12f,.35f,.22f),p.dark);
    PrimitiveKit.Cube("Side step",rig,new Vector3(side*(d.track*.5f+.12f),-.2f,-.4f),new Vector3(.35f,.15f,1.4f),p.dark);
    PrimitiveKit.Cube("Headlight",rig,new Vector3(side*.7f,.5f,d.wheelbase*.5f+.56f),new Vector3(.36f,.24f,.09f),p.amber);
   }
   PrimitiveKit.Part("Exhaust cylinder",PrimitiveType.Cylinder,rig,new Vector3(-.7f,1.6f,-1.3f),new Vector3(.13f,1.1f,.13f),p.dark);
   PrimitiveKit.Part("Amber beacon",PrimitiveType.Cylinder,cab,new Vector3(0,1.8f,0),new Vector3(.25f,.16f,.25f),p.amber);
   var physical=PrimitiveKit.Group("PhysicsRig | preserve when replacing visuals",root,Vector3.zero);m.wheels=new WheelCollider[4];m.wheelVisuals=new Transform[4];
   for(int i=0;i<4;i++){
    float side=i%2==0?-1:1;float z=i<2?d.wheelbase*.5f:-d.wheelbase*.5f;
    var wheel=PrimitiveKit.Group("WheelCollider "+i,physical,new Vector3(side*d.track*.5f,-.25f,z));var wc=wheel.gameObject.AddComponent<WheelCollider>();wc.radius=d.wheelRadius;wc.suspensionDistance=.32f;wc.mass=45;
    var spring=wc.suspensionSpring;spring.spring=d.mass*9.81f/4/.16f;spring.damper=3500;spring.targetPosition=.5f;wc.suspensionSpring=spring;wc.forceAppPointDistance=.1f;m.wheels[i]=wc;
    var visual=PrimitiveKit.Group("WheelVisual "+i+" | steering + rotation anchor",rig,wheel.localPosition);m.wheelVisuals[i]=visual;
    var tire=PrimitiveKit.Part("Tyre | cylinder",PrimitiveType.Cylinder,visual,Vector3.zero,new Vector3(d.wheelRadius*2,.22f,d.wheelRadius*2),p.dark);tire.transform.localRotation=Quaternion.Euler(0,0,90);
    var hub=PrimitiveKit.Part("Wheel hub",PrimitiveType.Cylinder,visual,new Vector3(side*.24f,0,0),new Vector3(d.wheelRadius,.04f,d.wheelRadius),paint);hub.transform.localRotation=Quaternion.Euler(0,0,90);
    for(int n=0;n<12;n++){float angle=n*30*Mathf.Deg2Rad;var tread=PrimitiveKit.Cube("Tyre tread",visual,new Vector3(0,Mathf.Cos(angle)*d.wheelRadius,Mathf.Sin(angle)*d.wheelRadius),new Vector3(.48f,.075f,.18f),p.dark);tread.transform.localRotation=Quaternion.Euler(n*30,0,15);}
    PrimitiveKit.Cube("Wheel arch",rig,new Vector3(side*d.track*.5f,.5f,z),new Vector3(.6f,.12f,1.5f),paint);
   }
   var tool=root.gameObject.AddComponent<AttachmentController>();m.Tool=tool;
   var mount=PrimitiveKit.Group("Attachment mount | "+a.mountId,root,new Vector3(0,-.65f,2.55f));tool.mount=mount;
   if(!d.loader){
    var blade=PrimitiveKit.Group("Blade assembly | replace visual only",mount,Vector3.zero);
    PrimitiveKit.Cube("Blade plate",blade,new Vector3(0,.1f,0),new Vector3(a.width,.9f,.22f),p.teal);
    var lip=PrimitiveKit.Cube("Upper curved lip approximation",blade,new Vector3(0,.54f,-.1f),new Vector3(a.width,.23f,.4f),p.teal);lip.transform.localRotation=Quaternion.Euler(-30,0,0);
    PrimitiveKit.Cube("Wear edge",blade,new Vector3(0,-.35f,.06f),new Vector3(a.width,.1f,.3f),p.dark);
    foreach(float x in new[]{-.8f,.8f}){PrimitiveKit.Beam("Push frame",blade,new Vector3(x,0,-1),new Vector3(x,0,-.1f),.12f,p.dark);PrimitiveKit.Beam("Hydraulic piston",blade,new Vector3(x,.3f,-.8f),new Vector3(x*.6f,.2f,-.1f),.08f,p.stone);}
    tool.mouth=PrimitiveKit.Group("Working edge",blade,new Vector3(0,-.2f,.4f));
   }else{
    mount.localPosition=new Vector3(0,0,1.2f);var boom=PrimitiveKit.Group("Lift pivot",mount,Vector3.zero);tool.boom=boom;
    foreach(float side in new[]{-1f,1}){PrimitiveKit.Beam("Loader arm",boom,new Vector3(side*.8f,.1f,0),new Vector3(side*.8f,-.6f,2.1f),.14f,paint);PrimitiveKit.Beam("Hydraulic outer",mount,new Vector3(side*.7f,-.3f,-.4f),new Vector3(side*.7f,.1f,1.2f),.12f,p.dark);PrimitiveKit.Beam("Hydraulic rod",boom,new Vector3(side*.7f,-.1f,.5f),new Vector3(side*.7f,-.5f,1.9f),.07f,p.stone);}
    var bucket=PrimitiveKit.Group("Bucket tilt pivot",boom,new Vector3(0,-.65f,2.1f));tool.bucket=bucket;
    PrimitiveKit.Cube("Bucket floor",bucket,new Vector3(0,-.15f,.6f),new Vector3(a.width,.18f,1.45f),p.dark);
    PrimitiveKit.Cube("Bucket back",bucket,new Vector3(0,.25f,0),new Vector3(a.width,.85f,.18f),paint);
    foreach(float side in new[]{-1f,1})PrimitiveKit.Cube("Bucket side",bucket,new Vector3(side*a.width*.5f,.15f,.6f),new Vector3(.14f,.6f,1.3f),paint);
    for(int i=0;i<6;i++)PrimitiveKit.Cube("Bucket tooth",bucket,new Vector3(-a.width*.42f+i*a.width*.168f,-.2f,1.45f),new Vector3(.14f,.12f,.4f),p.stone);
    tool.mouth=PrimitiveKit.Group("Bucket discharge and intake",bucket,new Vector3(0,0,1.5f));
    tool.loadVisual=PrimitiveKit.Part("Carried snow | sphere",PrimitiveType.Sphere,bucket,new Vector3(0,.15f,.65f),Vector3.one,p.snow).transform;
   }
   var effects=PrimitiveKit.Group("Snow spray | preauthored particles",root,new Vector3(0,-.45f,3));var ps=effects.gameObject.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=ps.main;main.playOnAwake=false;main.startLifetime=.7f;main.startSpeed=2.5f;main.startSize=.12f;main.gravityModifier=1;main.maxParticles=150;main.simulationSpace=ParticleSystemSimulationSpace.World;
   var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=70;shape.radius=.7f;
   ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=p.snow;tool.spray=ps;
   return m;
  }
 }
}
