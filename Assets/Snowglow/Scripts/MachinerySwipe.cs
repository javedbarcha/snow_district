using UnityEngine;
using UnityEngine.EventSystems;
namespace Snowglow {
 // One authored preview. No runtime cards, textures or machine clones.
 public sealed class MachinerySwipe:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler {
  public ProductionMenu menu;Vector2 start;int pointer;bool dragging;
  public void OnBeginDrag(PointerEventData e){if(dragging)return;pointer=e.pointerId;start=e.position;dragging=true;}
  public void OnDrag(PointerEventData e){}
  public void OnEndDrag(PointerEventData e){if(!dragging||e.pointerId!=pointer)return;dragging=false;var d=e.position-start;var canvas=GetComponentInParent<Canvas>();float threshold=45*(canvas?canvas.scaleFactor:1);if(Mathf.Abs(d.x)>threshold&&Mathf.Abs(d.x)>Mathf.Abs(d.y)*1.4f&&menu)menu.Browse(d.x<0?1:-1);}
  void OnDisable(){dragging=false;}
 }
}
