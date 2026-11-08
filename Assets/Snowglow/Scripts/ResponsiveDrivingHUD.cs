using UnityEngine;
namespace Snowglow {
 // Authored references only: responsive positions never create UI objects.
 public sealed class ResponsiveDrivingHUD:MonoBehaviour {
  public PrototypeHUD hud;
  public RectTransform forward,reverse,lift,steerLeft,steerRight,cameraButton,garage,pause,lights,reset,stats;
  static void Put(RectTransform r,float x,float y,float w,float h,bool top=false,bool right=false){if(!r)return;r.localScale=Vector3.one;r.anchorMin=r.anchorMax=new Vector2(right?1:0,top?1:0);r.pivot=new Vector2(right?1:0,top?1:0);r.anchoredPosition=new Vector2(x,top?-y:y);r.sizeDelta=new Vector2(w,h);}
  public void Reflow(float width,float height){
   if(!hud||width<=0||height<=0)return;
   float fit=Mathf.Min(width/1280f,height/720f),bottom=height*.05f,top=height*.1f,gapY=height*.05f,gapX=width*.05f;
   if(hud.leftControls)hud.leftControls.localScale=Vector3.one;if(hud.rightControls)hud.rightControls.localScale=Vector3.one;if(hud.centreControls)hud.centreControls.localScale=Vector3.one;
   Put(reverse,0,bottom,110*fit,110*fit,false,true);
   Put(forward,0,bottom+110*fit+gapY,137.5f*fit,137.5f*fit,false,true);
   Put(lift,-110*fit-gapX,bottom,90*fit,90*fit,false,true);
   Put(steerLeft,0,bottom,112*fit,112*fit);Put(steerRight,112*fit+gapX,bottom,112*fit,112*fit);
   float angleY=bottom+112*fit+height*.08f;
   for(int i=0;i<hud.bladeAngleButtons.Length;i++)if(hud.bladeAngleButtons[i])Put((RectTransform)hud.bladeAngleButtons[i].transform,(84*fit+width*.015f)*i,angleY,84*fit,84*fit);
   if(hud.loaderDumpButton)Put((RectTransform)hud.loaderDumpButton.transform,0,angleY,84*fit,84*fit);
   Put(cameraButton,0,top,72*fit,72*fit,true);Put(pause,0,top,72*fit,72*fit,true,true);
   Put(garage,0,top+90*fit,72*fit,72*fit,true);Put(lights,0,top+148*fit,64*fit,64*fit,true,true);
   Put(reset,100*fit,top,60*fit,60*fit,true);
   // Keep diagnostic toggle inside the reserved 10% top band.
   if(stats){float statHeight=Mathf.Min(28*fit,height*.04f);Put(stats,0,height*.055f/fit,95,statHeight/fit,true,true);}
  }
  void LateUpdate(){if(hud&&hud.safeArea)Reflow(hud.safeArea.rect.width,hud.safeArea.rect.height);}
 }
}
