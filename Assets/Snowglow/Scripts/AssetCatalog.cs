using System.Collections.Generic;
using UnityEngine;
namespace Snowglow {
 [CreateAssetMenu(menuName="Snowglow/Asset catalog")]
 public sealed class AssetCatalog : ScriptableObject {
  public List<CatalogAsset> entries=new List<CatalogAsset>();
  public CatalogAsset Find(string id){return entries.Find(e=>e&&e.id==id);}
 }
}
