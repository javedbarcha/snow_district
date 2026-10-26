using UnityEngine;
namespace Snowglow {
 public enum AnchorRole { Connection, RoadConnection, Entrance, Parking, Service, Disposal, WorkingEdge, LoadVisual }
 public sealed class AssetAnchor : MonoBehaviour {
  public string id="connection";public AnchorRole role;public string interfaceId;
  void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(transform.position,.15f);Gizmos.DrawRay(transform.position,transform.forward*.8f);}
 }
}
