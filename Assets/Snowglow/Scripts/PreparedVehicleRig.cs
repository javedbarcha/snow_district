using UnityEngine;
namespace Snowglow {
 // Authored bindings only: no runtime construction or automatic fitting.
 public sealed class PreparedVehicleRig : MonoBehaviour {
  public Transform visualRoot,boom;public WheelCollider[] wheels=new WheelCollider[4];public Transform[] wheelVisuals=new Transform[4];
  public float maximumWheelRadius=1,maximumWheelWidth=.8f;
 }
}
