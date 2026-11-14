using UnityEngine;
using UnityEngine.EventSystems;
namespace Snowglow {
 // One authored preview. No runtime cards, textures or machine clones.
 public sealed class MachinerySwipe:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler,IScrollHandler {
  public ProductionMenu menu;public bool orbit;Vector2 start,last;int pointer;bool dragging;
  public void OnBeginDrag(PointerEventData e){if(dragging)return;pointer=e.pointerId;start=last=e.position;dragging=true;}
  public void OnDrag(PointerEventData e){if(!dragging||pointer!=e.pointerId)return;if(orbit&&menu&&menu.workshopPreview){var canvas=GetComponentInParent<Canvas>();menu.workshopPreview.Orbit((e.position-last)/Mathf.Max(.01f,canvas?canvas.scaleFactor:1));}last=e.position;}
  public void OnScroll(PointerEventData e){if(orbit&&menu&&menu.workshopPreview)menu.workshopPreview.Zoom(e.scrollDelta.y*.4f);}
  public void OnEndDrag(PointerEventData e){if(!dragging||e.pointerId!=pointer)return;if(orbit){OnDrag(e);dragging=false;return;}dragging=false;var d=e.position-start;var canvas=GetComponentInParent<Canvas>();float threshold=45*(canvas?canvas.scaleFactor:1);if(Mathf.Abs(d.x)>threshold&&Mathf.Abs(d.x)>Mathf.Abs(d.y)*1.4f&&menu)menu.Browse(d.x<0?1:-1);}
  void LateUpdate(){
   if(!orbit||!menu||!menu.workshopPreview)return;
   var rect=(RectTransform)transform;var parent=(RectTransform)rect.parent;var canvas=GetComponentInParent<Canvas>();var camera=canvas&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
   Vector2 bottom,leftTop;RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,Vector2.zero,camera,out bottom);RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,new Vector2(Screen.width,Screen.height),camera,out leftTop);
   float width=leftTop.x-bottom.x,height=-bottom.y;if(width>0&&height>0){rect.anchoredPosition=new Vector2(bottom.x,0);rect.sizeDelta=new Vector2(width,height);menu.workshopPreview.previewCamera.aspect=width/height;}
  }
  void OnDisable(){dragging=false;}
 }
}
