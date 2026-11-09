using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Snowglow.Editor {
 public static class PhoneMarginChecks {
  const string Request="Logs/PhoneMargin.request";
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;try{File.Delete(Request);}catch(IOException){return;}EditorApplication.update-=Pending;try{Run();}catch(Exception e){Directory.CreateDirectory("Logs/PhoneMargin");File.WriteAllText("Logs/PhoneMargin/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
  [MenuItem("Snowglow/UI/Validate phone side margins")]
  public static void Run(){
   int checks=0;var root=new GameObject("Disposable phone margins");try{var hud=root.AddComponent<PrototypeHUD>();hud.leftControls=new GameObject("Left",typeof(RectTransform)).GetComponent<RectTransform>();hud.leftControls.SetParent(root.transform);hud.rightControls=new GameObject("Right",typeof(RectTransform)).GetComponent<RectTransform>();hud.rightControls.SetParent(root.transform);hud.leftControls.anchorMin=new Vector2(.05f,0);hud.leftControls.anchorMax=new Vector2(.05f,1);hud.rightControls.anchorMin=new Vector2(.95f,0);hud.rightControls.anchorMax=new Vector2(.95f,1);
    foreach(float width in new[]{1280f,1600f,1920f,2340f})foreach(var inset in new[]{Vector2.zero,new Vector2(64,0),new Vector2(0,64),new Vector2(120,0),new Vector2(0,120),new Vector2(72,32)}){
     var safe=new Rect(inset.x,0,width-inset.x-inset.y,720);hud.ApplySideMargins(width,safe);float left=safe.x+hud.leftControls.anchorMin.x*safe.width,right=safe.x+hud.rightControls.anchorMin.x*safe.width;
     Check(Mathf.Abs(left-Mathf.Max(width*.05f,inset.x))<.001f,"Left padding added to cutout");Check(Mathf.Abs(width-right-Mathf.Max(width*.05f,inset.y))<.001f,"Right padding added to cutout");Check(hud.leftControls.anchorMin.y==0&&hud.leftControls.anchorMax.y==1&&hud.rightControls.anchorMin.y==0&&hud.rightControls.anchorMax.y==1,"Vertical anchors altered");checks+=3;
    }
   }finally{UnityEngine.Object.DestroyImmediate(root);}
   Directory.CreateDirectory("Logs/PhoneMargin");File.WriteAllText("Logs/PhoneMargin/Validation.txt","PASS: Unity "+Application.unityVersion+"; "+checks+" assertions across four screen widths and six symmetric/asymmetric cutout configurations, including opposite landscape orientations and cutouts wider than 5%. Controls use max(5% full screen width, safe inset), preserving vertical anchors. Previous safe-inset-plus-5%-safe-width double padding removed. Runtime-only change, no scene or player saves. Physical phone confirmation requires rebuilding/installing.");Debug.Log(File.ReadAllText("Logs/PhoneMargin/Validation.txt"));
  }
 }
}
