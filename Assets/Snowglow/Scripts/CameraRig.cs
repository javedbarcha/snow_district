using UnityEngine;
namespace Snowglow {
 public sealed class CameraRig : MonoBehaviour {
  public Camera view;public bool Follow;Vector3 velocity;
  public void Toggle(){Follow=!Follow;}
  void LateUpdate(){var g=PrototypeSession.Instance;if(g==null||g.Active==null)return;var target=g.Active.transform;
   view.orthographic=!Follow;view.orthographicSize=Mathf.Max(17,22/Mathf.Max(.6f,view.aspect));
   Vector3 wanted=Follow?target.position-target.forward*13+Vector3.up*6:target.position+new Vector3(-24,30,-24);
   transform.position=Vector3.SmoothDamp(transform.position,wanted,ref velocity,.18f,1000,Time.unscaledDeltaTime);
   Quaternion rotation=Follow?Quaternion.LookRotation(target.position+Vector3.up*1.2f-transform.position):Quaternion.Euler(41,45,0);
   transform.rotation=Quaternion.Slerp(transform.rotation,rotation,1-Mathf.Exp(-Time.unscaledDeltaTime*9));
  }
 }
}
