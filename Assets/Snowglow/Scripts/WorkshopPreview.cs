using UnityEngine;
namespace Snowglow {
 // All meshes, camera and target are authored. No gameplay machinery is simulated here.
 public sealed class WorkshopPreview:MonoBehaviour {
  public Camera previewCamera;public GameObject[] vehicles,parts;
  public void Show(int vehicle,int part,bool shop){
   for(int i=0;i<vehicles.Length;i++)vehicles[i].SetActive(!shop&&i==vehicle);
   for(int i=0;i<parts.Length;i++)parts[i].SetActive(shop&&i==vehicle*2+part);
   if(previewCamera)previewCamera.enabled=true;
  }
  void OnDisable(){if(previewCamera)previewCamera.enabled=false;}
 }
}
