using System;
using UnityEngine;
namespace Snowglow {
 // Visual sky wind only: this never drives machinery, particles or snow volume.
 public sealed class CloudWindMotion {
  readonly System.Random random;readonly float minSpeed,maxSpeed,minInterval,maxInterval,turnTime;
  float startYaw,startSpeed,targetSpeed,turnAge;
  public float Yaw{get;private set;}public float Speed{get;private set;}public float TargetYaw{get;private set;}public float Remaining{get;private set;}public float Interval{get;private set;}public int Changes{get;private set;}
  public Vector3 Direction=>Quaternion.Euler(0,Yaw,0)*Vector3.forward;
  float Range(float lo,float hi)=>lo+(hi-lo)*(float)random.NextDouble();
  public CloudWindMotion(System.Random rng,float slow,float fast,float minimum,float maximum,float turn){random=rng;minSpeed=slow;maxSpeed=fast;minInterval=minimum;maxInterval=maximum;turnTime=Mathf.Max(.01f,turn);Yaw=TargetYaw=Range(0,360);Speed=startSpeed=targetSpeed=Range(slow,fast);startYaw=Yaw;turnAge=turnTime;Remaining=Interval=Range(minimum,maximum);}
  public void Advance(float seconds){
   if(seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
   while(seconds>0){float step=Mathf.Min(seconds,Remaining);turnAge+=step;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(turnAge/turnTime));Yaw=Mathf.Repeat(Mathf.LerpAngle(startYaw,TargetYaw,t),360);Speed=Mathf.Lerp(startSpeed,targetSpeed,t);Remaining-=step;seconds-=step;
    if(Remaining<=.0001f){startYaw=Yaw;startSpeed=Speed;TargetYaw=Mathf.Repeat(Yaw+Range(40,320),360);targetSpeed=Range(minSpeed,maxSpeed);turnAge=0;Remaining=Interval=Range(minInterval,maxInterval);Changes++;}
   }
  }
 }
 [Serializable]public sealed class CloudWindSlot {
  public Transform initialCloud;public GameObject prefab;public string placementId;
  public Vector3 baselineScale;public Bounds sourceBounds;
  [NonSerialized]public Transform current;
  [NonSerialized]public Renderer[] currentRenderers;
  [NonSerialized]public float opacity=1;
 }
 public struct CloudWindPose {public Vector3 position,scale;public Quaternion rotation;}
 [DisallowMultipleComponent]
 public sealed class CloudWindController:MonoBehaviour {
  public Vector3 centre=new Vector3(0,5,0);
  public float radius=1500;
  public float fadeSeconds=12;
  MaterialPropertyBlock fadeProperties;
  static readonly int OpacityProperty=Shader.PropertyToID("_CloudOpacity");
  public int minPopulation=10,maxPopulation=15,distributionVersion;
  float arrivalRemaining,targetRemaining;int targetPopulation;
  public int ActiveCount {get {int n=0;foreach(var s in slots)if(s.current&&s.current.gameObject.activeSelf)n++;return n;}}
  public float minHeight=250,maxHeight=450,minScale=.5f,maxScale=3;
  public float minSpeed=3,maxSpeed=8,minChangeSeconds=300,maxChangeSeconds=900,turnSeconds=20;
  public CloudWindSlot[] slots=Array.Empty<CloudWindSlot>();
  public CloudWindMotion Wind{get;private set;}public int RespawnCount{get;private set;}
  System.Random random;
  void Awake(){Initialize(unchecked(Environment.TickCount^GetInstanceID()));}
  public void Initialize(int seed){
   random=new System.Random(seed);Wind=new CloudWindMotion(random,minSpeed,maxSpeed,minChangeSeconds,maxChangeSeconds,turnSeconds);RespawnCount=0;
   targetPopulation=random.Next(minPopulation,maxPopulation+1);arrivalRemaining=Range(5,20);targetRemaining=Range(30,90);
   foreach(var slot in slots){slot.current=null;slot.currentRenderers=null;slot.opacity=1;if(slot.initialCloud){slot.initialCloud.gameObject.SetActive(false);Place(slot.initialCloud,RandomPoseAt(slot,centre));}}
   // Stratified area sampling guarantees central coverage without retaining the old ring.
   int[] order=new int[slots.Length];for(int i=0;i<order.Length;i++)order[i]=i;
   for(int i=order.Length-1;i>0;i--){int j=random.Next(i+1);int t=order[i];order[i]=order[j];order[j]=t;}
   int count=Mathf.Min(targetPopulation,slots.Length);float phase=Range(0,360);
   for(int i=0;i<count;i++){
    var slot=slots[order[i]];if(!slot.initialCloud)continue;
    float r=radius*Mathf.Sqrt((i+(float)random.NextDouble())/count)*.98f;
    if(i==0)r=Range(0,150);
    float angle=(phase+i*137.50776f+Range(-25,25))*Mathf.Deg2Rad;
    var pose=RandomPoseAt(slot,centre+new Vector3(Mathf.Cos(angle)*r,0,Mathf.Sin(angle)*r));
    slot.current=slot.initialCloud;Place(slot.current,pose);slot.current.gameObject.SetActive(true);SetOpacity(slot,OpacityAt(slot.current.position,Wind.Direction*Wind.Speed));
   }
  }
  static void Place(Transform t,CloudWindPose pose){t.SetPositionAndRotation(pose.position,pose.rotation);t.localScale=pose.scale;}
  float Range(float lo,float hi)=>lo+(hi-lo)*(float)random.NextDouble();
  public CloudWindPose RandomPoseAt(CloudWindSlot slot,Vector3 horizontal){
   if(random==null)Initialize(711140);
   var scale=slot.baselineScale*Range(minScale,maxScale);float low=centre.y+minHeight-slot.sourceBounds.min.y*scale.y,high=centre.y+maxHeight-slot.sourceBounds.max.y*scale.y;
   if(high<low)throw new InvalidOperationException("Cloud height band cannot fit maximum scale");
   horizontal.y=Range(low,high);return new CloudWindPose{position=horizontal,scale=scale,rotation=Quaternion.Euler(0,Range(0,360),0)};
  }
  public CloudWindPose EntryPose(CloudWindSlot slot){
   if(Wind==null)Initialize(711140);
   var forward=Wind.Direction;var right=new Vector3(forward.z,0,-forward.x);
   float side=Range(-radius*.95f,radius*.95f),inside=radius-2;
   return RandomPoseAt(slot,centre-forward*Mathf.Sqrt(inside*inside-side*side)+right*side);
  }
  // Radius applies to cloud roots; mesh edges may extend across this distant boundary.
  // Time to the circular downwind exit avoids fading newly arrived upwind clouds.
  public float OpacityAt(Vector3 position,Vector3 velocity){
   var p=position-centre;p.y=0;velocity.y=0;float distance=p.sqrMagnitude;
   if(distance>=radius*radius)return 0;
   float speed=velocity.sqrMagnitude;if(speed<.00001f||fadeSeconds<=0)return 1;
   float dot=Vector3.Dot(p,velocity);float time=(-dot+Mathf.Sqrt(Mathf.Max(0,dot*dot+speed*(radius*radius-distance))))/speed;
   return Mathf.SmoothStep(0,1,Mathf.Clamp01(time/fadeSeconds));
  }
  void SetOpacity(CloudWindSlot slot,float opacity){
   slot.opacity=Mathf.Clamp01(opacity);if(!slot.current)return;
   if(slot.currentRenderers==null)slot.currentRenderers=slot.current.GetComponentsInChildren<Renderer>(true);
   if(fadeProperties==null)fadeProperties=new MaterialPropertyBlock();
   foreach(var renderer in slot.currentRenderers){if(!renderer)continue;renderer.GetPropertyBlock(fadeProperties);fadeProperties.SetFloat(OpacityProperty,slot.opacity);renderer.SetPropertyBlock(fadeProperties);}
  }
  public bool NeedsReplacement(Vector3 position){var p=position-centre;p.y=0;return p.sqrMagnitude>radius*radius;}
  public void Simulate(float seconds,bool running){
   if(!running||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
#if UNITY_EDITOR
   if(!Application.isPlaying&&(gameObject.hideFlags&HideFlags.DontSave)==0)throw new InvalidOperationException("Editor wind simulation requires a disposable fixture");
#endif
   if(Wind==null)Initialize(711140);
   // Substeps handle a hitch without skipping wind transitions or boundary cleanup.
   while(seconds>0){float dt=Mathf.Min(seconds,.25f);seconds-=dt;var old=Wind.Direction*Wind.Speed;Wind.Advance(dt);var offset=(old+Wind.Direction*Wind.Speed)*(.5f*dt);
    foreach(var slot in slots){if(!slot.current)continue;slot.current.position+=offset;if(NeedsReplacement(slot.current.position)){
      SetOpacity(slot,0);var expired=slot.current.gameObject;slot.current=null;slot.currentRenderers=null;expired.SetActive(false);
      if(Application.isPlaying)Destroy(expired);else DestroyImmediate(expired);
    }else SetOpacity(slot,OpacityAt(slot.current.position,offset/dt));}
    targetRemaining-=dt;arrivalRemaining-=dt;
    if(targetRemaining<=0){targetPopulation=random.Next(minPopulation,maxPopulation+1);targetRemaining=Range(30,90);}
    while(ActiveCount<minPopulation)if(!SpawnAvailable())break;
    if(arrivalRemaining<=0){if(ActiveCount<targetPopulation&&ActiveCount<maxPopulation)SpawnAvailable();arrivalRemaining=Range(5,20);}
   }
  }
  bool SpawnAvailable(){
   int first=random.Next(slots.Length);
   for(int i=0;i<slots.Length;i++){var slot=slots[(first+i)%slots.Length];if(slot.current)continue;
    var pose=EntryPose(slot);
    // The saved inactive reservoir is used at most once per root, then normal destroy/spawn applies.
    var go=slot.initialCloud?slot.initialCloud.gameObject:Instantiate(slot.prefab,transform);
    go.name=slot.placementId+" | wind cloud";Place(go.transform,pose);go.SetActive(true);
    var placement=go.GetComponent<CatalogPlacement>();if(placement)placement.placementId=slot.placementId;
    slot.current=go.transform;slot.currentRenderers=null;SetOpacity(slot,1);RespawnCount++;return true;
   }return false;
  }
  void Update(){var game=PrototypeSession.Instance;Simulate(Time.deltaTime,!game||game.IsRunning);}
 }
}
