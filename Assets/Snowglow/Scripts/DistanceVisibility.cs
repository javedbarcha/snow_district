using System;
using UnityEngine;
namespace Snowglow {
 [DefaultExecutionOrder(1000)]
 public sealed class DistanceVisibility:MonoBehaviour {
  public float distance=230;
  public Renderer[] nearParts=Array.Empty<Renderer>(),distantParts=Array.Empty<Renderer>();
  public float distantDistance=650,switchBuffer=12;
  Renderer[] parts;Camera observer;float next;bool initialized,near=true;
  public bool ShowingGeometry=>initialized&&near;
  void Awake(){parts=nearParts.Length>0?nearParts:GetComponentsInChildren<Renderer>(true);next=0;}
  // Geometry and cards are saved objects; switching only enables existing renderers.
  public void Evaluate(Vector3 observerPosition){
   if(parts==null)parts=nearParts.Length>0?nearParts:GetComponentsInChildren<Renderer>(true);
   float d=(transform.position-observerPosition).magnitude;
   bool cards=distantParts.Length>0;
   if(!initialized||!cards)near=d<distance;
   else if(near&&d>distance+switchBuffer)near=false;
   else if(!near&&d<distance-switchBuffer)near=true;
   initialized=true;
   foreach(var r in parts)if(r)r.enabled=near;
   foreach(var r in distantParts)if(r)r.enabled=!near&&d<distantDistance;
  }
  void LateUpdate(){
   if(Time.unscaledTime<next)return;
   next=Time.unscaledTime+(distantParts.Length>0?.2f:.7f);
   var game=PrototypeSession.Instance;
   if(distantParts.Length>0){
    if(!observer)observer=game&&game.CameraRig?game.CameraRig.view:Camera.main;
    if(observer)Evaluate(observer.transform.position);
   }else if(game&&game.Active)Evaluate(game.Active.transform.position);
  }
 }
}
