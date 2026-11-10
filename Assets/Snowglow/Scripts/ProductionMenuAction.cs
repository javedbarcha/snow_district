using UnityEngine;
namespace Snowglow {
 public sealed class ProductionMenuAction:MonoBehaviour {
  public ProductionMenu menu;public string command;
  public void Click(){if(menu)menu.Execute(command);}
 }
}
