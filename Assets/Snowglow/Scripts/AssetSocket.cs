using UnityEngine;
namespace Snowglow {
 public sealed class AssetSocket : MonoBehaviour {
  public string id="front",interfaceId="front-medium";public CatalogKind accepts=CatalogKind.Attachment;
  public float maximumMass=900,maximumWidth=4;public bool hydraulics=true;
  public Vector3 minimumAngles=new Vector3(-45,-35,-10),maximumAngles=new Vector3(45,35,10);
  void OnDrawGizmosSelected(){Gizmos.color=Color.yellow;Gizmos.DrawWireCube(transform.position,Vector3.one*.4f);Gizmos.DrawRay(transform.position,transform.forward);}
 }
}
