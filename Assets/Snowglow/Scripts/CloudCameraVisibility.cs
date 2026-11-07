using UnityEngine;

namespace Snowglow {
 // Camera layer distances are native transient state: keep their authored values here.
 [ExecuteAlways,DisallowMultipleComponent]
 public sealed class CloudCameraVisibility:MonoBehaviour {
  public Camera view;
  public float farDistance=4000;
  public float[] layerDistances;
  public void Apply(){
   if(!view||layerDistances==null||layerDistances.Length!=32)return;
   view.farClipPlane=farDistance;
   view.layerCullDistances=layerDistances;
   view.cullingMask|=1<<10;
  }
  void OnEnable()=>Apply();
  void OnValidate()=>Apply();
 }
}
