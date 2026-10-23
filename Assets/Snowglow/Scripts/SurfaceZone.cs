using UnityEngine;
using System.Collections.Generic;
namespace Snowglow {
 public sealed class SurfaceZone : MonoBehaviour {
  [Range(.1f,2)]public float grip=1;static readonly List<SurfaceZone> zones=new List<SurfaceZone>();
  void OnEnable(){if(!zones.Contains(this))zones.Add(this);}void OnDisable(){zones.Remove(this);}
  public static float Sample(Vector3 world){foreach(var zone in zones){if(zone==null)continue;var p=zone.transform.InverseTransformPoint(world);if(Mathf.Abs(p.x)<=.5f&&Mathf.Abs(p.z)<=.5f)return zone.grip;}return 1;}
 }
}
