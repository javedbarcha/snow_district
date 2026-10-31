using UnityEngine;
using UnityEngine.UI;
namespace Snowglow {
 public sealed class PrototypeHUD : MonoBehaviour {
  public RectTransform safeArea,layout;public GameObject menuRoot;public GameObject driving;public Text status,objective,notice,debugText,menuInfo;
  public GameObject[] panels;public string[] names;
  public Text cameraModeText,cameraHelp;public GameObject cameraTools;public RectTransform leftControls,rightControls,centreControls;
  public Image workSkinImage;public Sprite plowWorkSprite,loaderWorkSprite;
  float next;
  public void RefreshMenu(){var g=PrototypeSession.Instance;if(g==null)return;for(int i=0;i<panels.Length;i++)panels[i].SetActive(names[i]==g.Panel);driving.SetActive(g.Panel=="");menuRoot.SetActive(g.Panel!="");}
  void Update(){
   var g=PrototypeSession.Instance;if(g==null||g.Profile==null)return;
   Rect safe=Screen.safeArea;safeArea.anchorMin=new Vector2(safe.x/Screen.width,safe.y/Screen.height);safeArea.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
   float available=((RectTransform)safeArea).rect.width;float height=((RectTransform)safeArea).rect.height;float fit=Mathf.Min(available/1280f,height/720f);layout.localScale=Vector3.one*fit;if(leftControls)leftControls.localScale=Vector3.one*fit;if(rightControls)rightControls.localScale=Vector3.one*fit;if(centreControls)centreControls.localScale=Vector3.one*fit;
   if(Time.unscaledTime<next)return;next=Time.unscaledTime+.15f;
   var m=g.Active;var p=g.Profile;var a=m.Tool;
   if(workSkinImage)workSkinImage.sprite=m.definition.loader?loaderWorkSprite:plowWorkSprite;
   if(cameraModeText)cameraModeText.text="CAM: "+g.CameraRig.ModeName;
   if(cameraTools)cameraTools.SetActive(g.CameraRig.Interactive);
   if(cameraHelp){cameraHelp.gameObject.SetActive(g.CameraRig.Interactive);cameraHelp.text=g.CameraRig.ModeName+" | full 360-degree vehicle view"+"\n"+(Application.isMobilePlatform?"Drag empty view to rotate | pinch to zoom | RESET recentres":"Right-drag or J/L + I/K to orbit | scroll or +/- zoom | Home resets");}
   status.text="SNOWGLOW  |  "+m.definition.displayName+"\n"+Mathf.RoundToInt(m.Speed*3.6f)+" km/h   "+(g.Input.Reverse?"REVERSE":"FORWARD")+"   FUEL "+(m.FuelRatio*100).ToString("F0")+"%   CONDITION "+m.Condition.ToString("F0")+"%";
   objective.text=(p.freeWork?"FREE WORK":("CONTRACT "+(p.job+1)))+"   "+(g.Snow.Progress*100).ToString("F0")+"% cleared\n"+g.Multiplier+"x combo   "+(p.remaining<0?"LATE ":"TIME ")+Mathf.Abs(p.remaining/60).ToString("F0")+" min   "+p.coins+" coins";
   notice.text=g.Notice+"\n"+(m.definition.loader?"Bucket "+a.Load.ToString("F2")+" / "+m.equipment.capacity+" m³":"Blade "+(a.Working?"DOWN":"UP")+" | angle "+a.Angle.ToString("F0"));
   menuInfo.text="PINEFALL, ALASKA-INSPIRED TEST DISTRICT\nWallet: "+p.coins+" coins   |   Level "+g.Level+"   XP "+p.xp+"\n"+m.definition.displayName+" | "+(m.AtDepot?"AT DEPOT":"RETURN TO DEPOT FOR EQUIPMENT / SERVICE")+"\nTyres "+m.Tyres+"/3   Engine "+m.Engine+"/3   Fuel "+m.Fuel.ToString("F1")+" L   Condition "+m.Condition.ToString("F0")+"%\n"+g.Notice;
   debugText.gameObject.SetActive(p.debug&&g.IsRunning);
   if(p.debug){float carried=0;foreach(var vehicle in g.Fleet)carried+=vehicle.Tool.Load;
    float error=g.Snow.Grid.initialVolume-g.Snow.Grid.Total()-carried-g.Snow.Grid.disposed;
    debugText.text="PHYSICS / SNOW\nSpeed: "+m.Speed.ToString("F1")+" m/s | slip "+m.Slip.ToString("F2")+"\nSnow force: "+m.LastSnowForce.ToString("F0")+" N\nCarried: "+carried.ToString("F2")+" m³ | disposed: "+g.Snow.Grid.disposed.ToString("F2")+"\nLedger error: "+error.ToString("F4")+" m³\nX "+m.transform.position.x.ToString("F0")+" Z "+m.transform.position.z.ToString("F0");}
  }
 }
}
