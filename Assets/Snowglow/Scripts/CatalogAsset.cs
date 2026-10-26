using UnityEngine;
namespace Snowglow {
 public enum CatalogKind { Vehicle, Attachment, WheelSet, Accessory, BuildingAssembly, RoadAssembly, Landscape, GameplayAssembly }
 public enum PreparationStatus { Draft, NeedsReview, Ready }
 public enum AssetSource { ImportedModel, BlenderScript, ReferenceImage, Prompt, ExistingPrototype }
 [CreateAssetMenu(menuName="Snowglow/Catalog asset")]
 public sealed class CatalogAsset : ScriptableObject {
  public string id,displayName; public CatalogKind kind; public PreparationStatus status;
  public GameObject prefab; public string[] tags=new string[0];
  public AssetSource sourceType; public string sourceGuid,sourcePath,creationSourcePath,reviewedPrefabHash;
  [TextArea(3,8)]public string provenance,assumptions,reviewNotes;
  public bool visualReviewed; public Bounds localBounds;
  public int triangles,renderers,materialSlots; public int price;
  public VehicleDefinition vehicle; public AttachmentDefinition attachment;
  public string interfaceId; public float addedMass,torqueMultiplier=1,gripMultiplier=1;
  public float wheelRadius=.5f,wheelWidth=.3f,wheelMass=45;
  public bool mirrorRightWheels=true;
  public float minimumScale=1,maximumScale=1;public bool blocksSnow=true;
 }
}
