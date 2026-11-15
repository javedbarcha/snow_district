using UnityEngine;
namespace Snowglow {
 // Authored identity; scenery and snow are saved in each town's own scene/assets.
 public sealed class TownLevel : MonoBehaviour {
  public string townId="pinefall";
  public static string SceneFor(string town)=>town=="alps"?"TheAlps":"Pinefall";
  public static string NameFor(string town)=>town=="alps"?"The Alps":"Pinefall";
 }
}
