using UnityEngine;
namespace Snowglow {
 // Saved display meshes only; never simulates the player's machinery.
 public sealed class WorkshopPreview:MonoBehaviour {
  public Camera previewCamera;public GameObject[] vehicles,parts;
  public float yaw=38,pitch=22,distance=20f;
  public void Orbit(Vector2 delta){yaw=Mathf.Repeat(yaw+delta.x*.22f,360);pitch=Mathf.Clamp(pitch-delta.y*.15f,12,40);ApplyOrbit();}
  public void Zoom(float amount){distance=Mathf.Clamp(distance-amount,12,20);ApplyOrbit();}
  public void ResetView(){yaw=38;pitch=22;distance=20f;ApplyOrbit();}
  public void ApplyOrbit(){if(!previewCamera)return;var target=transform.position+Vector3.up*1.5f;previewCamera.transform.position=target+Quaternion.Euler(-pitch,yaw,0)*new Vector3(0,0,Mathf.Min(distance,10/Mathf.Sin(pitch*Mathf.Deg2Rad)));previewCamera.transform.LookAt(target);}
  public void Show(int vehicle,int part,bool shop){
   for(int i=0;i<vehicles.Length;i++)vehicles[i].SetActive(!shop&&i==vehicle);
   for(int i=0;i<parts.Length;i++)parts[i].SetActive(shop&&i==vehicle*2+part);
   if(previewCamera)previewCamera.enabled=true;ApplyOrbit();
  }
  void OnDisable(){if(previewCamera)previewCamera.enabled=false;}
 }
}
