using UnityEngine;
namespace Snowglow { public sealed class DistanceVisibility : MonoBehaviour {
 public float distance=230;Renderer[] parts;float next;
 void Awake(){parts=GetComponentsInChildren<Renderer>();next=Random.value;}
 void Update(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.7f;var g=PrototypeSession.Instance;if(g==null||g.Active==null)return;bool visible=(transform.position-g.Active.transform.position).sqrMagnitude<distance*distance;foreach(var r in parts)r.enabled=visible;}
}}
