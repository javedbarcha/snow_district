using UnityEngine;
using UnityEngine.UI;

namespace Snowglow {
 // All graphics and sprite references are authored before Play.
 public sealed class CareerCardGallery:MonoBehaviour {
  public ProductionMenu menu;
  public Sprite[] destinationArt;
  public Image[] townPictures;
  public Outline[] regionBorders;
  int shownRegion=-1;
  void OnEnable(){shownRegion=-1;}
  public void Refresh(int region){
   region=Mathf.Clamp(region,0,CareerProgression.TownIds.Length-1);
   if(shownRegion==region)return;
   shownRegion=region;
   int count=CareerProgression.TownIds[region].Length,offset=0;
   for(int r=0;r<region;r++)offset+=CareerProgression.TownIds[r].Length;
   float width=(1192-23*(count-1))/(float)count,height=width*1440/2960;
   for(int i=0;i<4;i++){
    var button=menu.Button("town-"+i);button.gameObject.SetActive(i<count);
    if(i>=count)continue;
    var root=(RectTransform)button.transform;
    root.anchoredPosition=new Vector2(44+i*(width+23),-270);root.sizeDelta=new Vector2(width,height+94);
    var picture=townPictures[i];picture.sprite=destinationArt[offset+i];
    picture.rectTransform.sizeDelta=new Vector2(width,height);
    var label=menu.Label("town-"+i);var rect=label.rectTransform;
    rect.anchoredPosition=new Vector2(12,-height-8);rect.sizeDelta=new Vector2(width-24,78);
   }
  }
  public void SetRegionUnlocked(int region,bool unlocked){
   if(regionBorders!=null&&region<regionBorders.Length&&regionBorders[region])regionBorders[region].enabled=unlocked;
  }
 }
}
