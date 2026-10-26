using System.Collections.Generic;
using UnityEngine;
namespace Snowglow {
 [CreateAssetMenu(menuName="Snowglow/Vehicle assembly recipe")]
 public sealed class AssemblyRecipe : ScriptableObject {
  public string id,displayName;public CatalogAsset vehicle;
  public List<FittingProfile> fittings=new List<FittingProfile>();
  public GameObject bakedPrefab;
 }
}
