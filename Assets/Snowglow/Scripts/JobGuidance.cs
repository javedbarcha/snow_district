using System;
using UnityEngine;

namespace Snowglow {
 [Serializable] public sealed class JobVenueVisual {
  public GameObject root;
  public LineRenderer outline;
  public Vector3[] points;
 }

 // Saved visual objects only: no colliders, job mutations or runtime asset factories.
 [DefaultExecutionOrder(1100)]
 public sealed class JobGuidance : MonoBehaviour {
  public Transform arrow;
  public JobVenueVisual[] venues;
  public float[] vehicleHeights;
  public float arrivalDistance=10,departureDistance=14;
  int contract=-1,vehicle=-1;bool nearby;float age,outlineTimer;

  public static Vector3 ClosestVenuePoint(Rect bounds,Vector3 position) =>
   new Vector3(Mathf.Clamp(position.x,bounds.xMin,bounds.xMax),position.y,Mathf.Clamp(position.z,bounds.yMin,bounds.yMax));

  public void Step(PrototypeSession game,float dt) {
   bool visible=game&&game.Profile!=null&&game.Profile.inJob&&!game.Profile.freeWork&&game.IsRunning&&game.Snow&&game.Fleet!=null&&game.Fleet.Length>0;
   if(!visible){Hide();return;}
   int kind=Mathf.Clamp(game.Snow.ContractKind,0,venues.Length-1),active=game.Profile.active;
   bool changed=contract!=kind||vehicle!=active;
   if(changed){contract=kind;vehicle=active;nearby=false;outlineTimer=0;}
   for(int i=0;i<venues.Length;i++)venues[i].root.SetActive(i==kind);
   var machine=game.Active;var position=machine.transform.position;
   var target=ClosestVenuePoint(SnowField.ContractBounds(kind),position);
   var direction=target-position;float distance=direction.magnitude;
   if(nearby){if(distance>departureDistance)nearby=false;}else if(distance<=arrivalDistance)nearby=true;
   arrow.gameObject.SetActive(!nearby);
   age+=Mathf.Max(0,dt);
   if(!nearby){
    float height=active<vehicleHeights.Length?vehicleHeights[active]:4;
    arrow.position=position+Vector3.up*(height+.75f+Mathf.Sin(age*2)*.10f);
    if(direction.sqrMagnitude>.001f){var rotation=Quaternion.LookRotation(direction,Vector3.up);arrow.rotation=changed?rotation:Quaternion.Slerp(arrow.rotation,rotation,1-Mathf.Exp(-8*Mathf.Max(0,dt)));}
   }
   outlineTimer-=Mathf.Max(0,dt);
   if(outlineTimer<=0){
    outlineTimer=.25f;var venue=venues[kind];
    for(int i=0;i<venue.points.Length;i++){var point=venue.points[i];point.y=SnowField.Ground+Mathf.Max(0,game.Snow.Depth(point))+.09f;venue.outline.SetPosition(i,point);}
   }
  }
  public void Hide(){if(arrow)arrow.gameObject.SetActive(false);if(venues!=null)foreach(var venue in venues)if(venue.root)venue.root.SetActive(false);contract=vehicle=-1;nearby=false;}
  void LateUpdate(){Step(PrototypeSession.Instance,Time.deltaTime);}
  void OnDisable(){Hide();}
 }
}
