using System;
using UnityEditor;
using UnityEngine;
namespace Snowglow.Editor {
 [CustomEditor(typeof(CatalogAsset))]
 public sealed class CatalogAssetInspector : UnityEditor.Editor {
  public override void OnInspectorGUI(){
   var asset=(CatalogAsset)target;serializedObject.Update();
   using(new EditorGUI.DisabledScope(true)){EditorGUILayout.PropertyField(serializedObject.FindProperty("id"));EditorGUILayout.PropertyField(serializedObject.FindProperty("status"));}
   string before=CatalogValidation.Fingerprint(asset);
   DrawPropertiesExcluding(serializedObject,"m_Script","id","status","reviewedPrefabHash");
   if(serializedObject.ApplyModifiedProperties()&&asset.status==PreparationStatus.Ready&&before!=CatalogValidation.Fingerprint(asset)){asset.status=PreparationStatus.NeedsReview;asset.visualReviewed=false;EditorUtility.SetDirty(asset);}
   EditorGUILayout.HelpBox("IDs are stable. Publish through Asset Workshop after reviewing scale, pivots, collision and fit. Imported geometry and phone performance need separate testing.",MessageType.Info);
  }
 }
 [CustomEditor(typeof(FittingProfile))]
 public sealed class FittingProfileInspector : UnityEditor.Editor {
  string report="";
  public override void OnInspectorGUI(){
   var fitting=(FittingProfile)target;serializedObject.Update();
   using(new EditorGUI.DisabledScope(true))EditorGUILayout.PropertyField(serializedObject.FindProperty("fitReviewed"));
   DrawPropertiesExcluding(serializedObject,"m_Script","fitReviewed","reviewedVehicleHash","reviewedPartHash","reviewedFitHash");
   if(serializedObject.ApplyModifiedProperties()){fitting.fitReviewed=false;EditorUtility.SetDirty(fitting);}
   if(GUILayout.Button("Validate fitting"))report=CatalogValidation.CheckFit(fitting,false).ToString();
   if(GUILayout.Button("Approve fitting after visual preview")){try{CatalogPreparation.ApproveFitting(fitting);report="Approved.";}catch(Exception e){report=e.Message;}}
   if(report!="")EditorGUILayout.HelpBox(report,MessageType.None);
  }
 }
}
