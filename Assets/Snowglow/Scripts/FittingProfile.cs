using UnityEngine;
namespace Snowglow {
 [CreateAssetMenu(menuName="Snowglow/Fitting profile")]
 public sealed class FittingProfile : ScriptableObject {
  public CatalogAsset vehicle,part;public string socketId="front";
  public Vector3 positionOffset,rotationOffset;
  public bool fitReviewed;public string reviewedVehicleHash,reviewedPartHash,reviewedFitHash;[TextArea]public string reviewNotes;
 }
}
