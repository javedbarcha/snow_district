using UnityEngine;
namespace Snowglow {
 public sealed class UICommand : MonoBehaviour {
  public string command;
  public void Click(){var g=PrototypeSession.Instance;if(g==null)return;
   string[] c=command.Split(':');string verb=c[0],arg=c.Length>1?c[1]:"";
   switch(verb){
    case "open":g.Open(arg);break;case "continue":g.Continue();break;case "pause":g.TogglePause();break;
    case "job":g.StartJob(int.Parse(arg));break;case "free":g.StartJob(0,true);break;case "abandon":g.Abandon();break;case "fresh":g.FreshDistrict();break;case "finish":g.Finish();break;
    case "switch":g.Switch(int.Parse(arg));break;case "buy":g.BuyMachine(int.Parse(arg));break;
    case "tyres":g.Upgrade(true);break;case "engine":g.Upgrade(false);break;
    case "fuel":g.Service(true);break;case "repair":g.Service(false);break;
    case "camera":if(arg=="reset")g.CameraRig.ResetView();else if(arg=="in")g.CameraRig.Zoom(1);else if(arg=="out")g.CameraRig.Zoom(-1);else g.CameraRig.Toggle();break;case "reverse":g.Input.Reverse=!g.Input.Reverse;break;
    case "work":g.Active.Tool.ToggleWork();break;case "angle":g.Active.Tool.CycleAngle();break;
    case "auto":g.Active.Tool.ToggleAuto();break;case "dump":g.Active.Tool.RequestDump();break;case "recover":g.Recover();break;
    case "dummy":g.Dummy(arg);break;
    case "sound":g.Profile.muted=!g.Profile.muted;g.Audio.SetMuted(g.Profile.muted);g.Save();break;
    case "quality":g.Profile.quality=1-g.Profile.quality;g.ApplyQuality();g.Save();break;
    case "assist":g.Profile.assists=!g.Profile.assists;g.ApplyQuality();g.Save();break;
    case "debug":g.Profile.debug=!g.Profile.debug;g.Save();break;
   }
  }
 }
}
