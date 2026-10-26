using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Snowglow.Editor {
 public sealed class AssetWorkshopWindow : EditorWindow {
  AssetCatalog catalog;CatalogAsset selected;GameObject source;UnityEngine.Object document;
  CatalogKind kind;AssetSource sourceType;string title="",notes="",search="",message="",json="";float scale=1;Vector3 rotation;
  int tab;Vector2 scroll;UnityEditor.Editor inspector,previewEditor;GameObject assemblyPreview;AssemblyRecipe recipe;TextAsset planFile;
  [MenuItem("Snowglow/Asset Workshop")]
  public static void Open(){GetWindow<AssetWorkshopWindow>("Snowglow Asset Workshop").minSize=new Vector2(800,600);}
  void OnEnable(){catalog=AssetDatabase.LoadAssetAtPath<AssetCatalog>(CatalogPreparation.LibraryPath);}
  void OnDisable(){ReleasePreview();if(inspector)DestroyImmediate(inspector);}
  void ReleasePreview(){if(previewEditor)DestroyImmediate(previewEditor);if(assemblyPreview)DestroyImmediate(assemblyPreview);}
  void Run(Action action){try{message="";action();if(message=="")message="Completed.";}catch(Exception e){message=e.Message;Debug.LogWarning("Asset Workshop: "+e.Message);}Repaint();}
  void Select(CatalogAsset a){selected=a;if(inspector)DestroyImmediate(inspector);inspector=a?UnityEditor.Editor.CreateEditor(a):null;ReleasePreview();if(a&&a.prefab)previewEditor=UnityEditor.Editor.CreateEditor(a.prefab);if(a&&a.status==PreparationStatus.Draft){title=a.displayName;kind=a.kind;sourceType=a.sourceType;notes=a.provenance;document=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(a.creationSourcePath);}}
  void OnGUI(){
   EditorGUILayout.LabelField("SNOWGLOW  /  ASSET WORKSHOP",EditorStyles.boldLabel);
   EditorGUILayout.HelpBox("Editor authoring only. Sources stay unchanged. Publish reviewed assets before using them in AI scene plans.",MessageType.Info);
   catalog=(AssetCatalog)EditorGUILayout.ObjectField("Catalog",catalog,typeof(AssetCatalog),false);
   if(!catalog&&GUILayout.Button("Create / open catalog"))Run(()=>catalog=CatalogPreparation.Library());
   tab=GUILayout.Toolbar(tab,new[]{"Catalog","Prepare asset","Vehicle assembly","AI scene plan"});
   scroll=EditorGUILayout.BeginScrollView(scroll);
   if(tab==0)Catalog();else if(tab==1)Prepare();else if(tab==2)Assembly();else Plans();
   if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.None);
   EditorGUILayout.EndScrollView();
  }
  void Catalog(){
   if(!catalog)return;search=EditorGUILayout.TextField("Search / category / tag",search);
   if(GUILayout.Button("Import prototype building blocks"))Run(()=>{CatalogBootstrap.ImportPrototype();catalog=CatalogPreparation.Library();});
   foreach(var a in catalog.entries.Where(a=>a&&(a.displayName+" "+a.kind+" "+string.Join(" ",a.tags)).IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0).ToArray()){
    Rect row=EditorGUILayout.GetControlRect(false,42);var thumb=AssetPreview.GetAssetPreview(a.prefab?a.prefab:a)??AssetPreview.GetMiniThumbnail(a);
    string stage=a.status.ToString();if(a.status==PreparationStatus.Ready&&a.reviewedPrefabHash!=CatalogValidation.Fingerprint(a))stage="Needs Review (changed)";
    GUI.Label(new Rect(row.x,row.y,40,40),thumb);if(GUI.Button(new Rect(row.x+44,row.y,row.width-44,40),a.displayName+"  |  "+a.kind+"  |  "+stage))Select(a);
    if(Event.current.type==EventType.MouseDrag&&row.Contains(Event.current.mousePosition)&&a.prefab){DragAndDrop.PrepareStartDrag();DragAndDrop.objectReferences=new UnityEngine.Object[]{a.prefab};DragAndDrop.StartDrag(a.displayName);Event.current.Use();}
   }
   if(!selected)return;
   EditorGUILayout.Space();EditorGUILayout.LabelField("Selected entry",EditorStyles.boldLabel);
   if(inspector)inspector.OnInspectorGUI();
   EditorGUILayout.BeginHorizontal();if(GUILayout.Button("Validate"))message=CatalogValidation.Check(selected).ToString();
   if(GUILayout.Button("Open prefab"))Run(()=>AssetDatabase.OpenAsset(selected.prefab));
   if(GUILayout.Button("Publish Ready"))Run(()=>CatalogPreparation.Publish(selected));
   if(GUILayout.Button("Return to review")){Undo.RecordObject(selected,"Review catalog asset");selected.status=PreparationStatus.NeedsReview;selected.visualReviewed=false;EditorUtility.SetDirty(selected);AssetDatabase.SaveAssets();}
   EditorGUILayout.EndHorizontal();
   if(previewEditor)previewEditor.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(200,260),EditorStyles.helpBox);
  }
  void Prepare(){
   EditorGUILayout.LabelField("Import a model into Unity's Project window first, then drop it here. FBX/OBJ and configured model importers are supported. Scripts/images create drafts until a 3D model is supplied.",EditorStyles.wordWrappedLabel);
   Rect drop=GUILayoutUtility.GetRect(100,65,GUILayout.ExpandWidth(true));GUI.Box(drop,"DROP MODEL / PREFAB / SCENE ASSEMBLY HERE");
   if(drop.Contains(Event.current.mousePosition)&&(Event.current.type==EventType.DragUpdated||Event.current.type==EventType.DragPerform)){
    var go=DragAndDrop.objectReferences.OfType<GameObject>().FirstOrDefault();DragAndDrop.visualMode=go?DragAndDropVisualMode.Copy:DragAndDropVisualMode.Rejected;
    if(go&&Event.current.type==EventType.DragPerform){DragAndDrop.AcceptDrag();source=go;title=go.name;}Event.current.Use();
   }
   source=(GameObject)EditorGUILayout.ObjectField("Visual model / assembly",source,typeof(GameObject),true);title=EditorGUILayout.TextField("Display name",title);
   kind=(CatalogKind)EditorGUILayout.EnumPopup("Category",kind);sourceType=(AssetSource)EditorGUILayout.EnumPopup("Created from",sourceType);
   document=EditorGUILayout.ObjectField("Reference image / script",document,typeof(UnityEngine.Object),false);notes=EditorGUILayout.TextArea(notes,GUILayout.MinHeight(55));
   scale=EditorGUILayout.FloatField("Uniform import scale",scale);rotation=EditorGUILayout.Vector3Field("Orientation correction",rotation);
   if(selected&&selected.status==PreparationStatus.Draft&&!selected.prefab)EditorGUILayout.HelpBox("Preparation will complete selected draft: "+selected.displayName,MessageType.Info);
   if(GUILayout.Button("Prepare separate game-ready prefab"))Run(()=>{var a=CatalogPreparation.Prepare(source,kind,title,scale,rotation,sourceType,notes,selected&&selected.status==PreparationStatus.Draft&&!selected.prefab?selected:null);if(document)a.creationSourcePath=AssetDatabase.GetAssetPath(document);EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();catalog=CatalogPreparation.Library();Select(a);tab=0;message=CatalogValidation.Check(a).ToString();});
   if(GUILayout.Button("Create source-only draft (prompt / image / Python)"))Run(()=>{catalog=CatalogPreparation.Library();Select(CatalogPreparation.Draft(title,kind,sourceType,document,notes));tab=0;});
  }
  void Assembly(){
   recipe=(AssemblyRecipe)EditorGUILayout.ObjectField("Assembly recipe",recipe,typeof(AssemblyRecipe),false);
   if(!recipe){if(GUILayout.Button("Create recipe"))Run(()=>{CatalogPreparation.EnsureFolders();recipe=CreateInstance<AssemblyRecipe>();recipe.id="assembly-"+Guid.NewGuid().ToString("N");recipe.displayName="New equipped vehicle";AssetDatabase.CreateAsset(recipe,CatalogPreparation.Root+"/Recipes/"+recipe.id+".asset");});return;}
   EditorGUI.BeginChangeCheck();recipe.displayName=EditorGUILayout.TextField("Name",recipe.displayName);recipe.vehicle=(CatalogAsset)EditorGUILayout.ObjectField("Vehicle body",recipe.vehicle,typeof(CatalogAsset),false);
   for(int n=0;n<recipe.fittings.Count;n++){
    EditorGUILayout.BeginHorizontal();recipe.fittings[n]=(FittingProfile)EditorGUILayout.ObjectField("Fitting "+n,recipe.fittings[n],typeof(FittingProfile),false);if(GUILayout.Button("Remove",GUILayout.Width(65))){recipe.fittings.RemoveAt(n);n--;}EditorGUILayout.EndHorizontal();
    if(n>=0&&n<recipe.fittings.Count&&recipe.fittings[n]){
     var f=recipe.fittings[n];EditorGUI.BeginChangeCheck();f.socketId=EditorGUILayout.TextField("Socket ID",f.socketId);f.part=(CatalogAsset)EditorGUILayout.ObjectField("Drop part here",f.part,typeof(CatalogAsset),false);f.positionOffset=EditorGUILayout.Vector3Field("Mount offset",f.positionOffset);f.rotationOffset=EditorGUILayout.Vector3Field("Mount rotation",f.rotationOffset);
     if(EditorGUI.EndChangeCheck()){f.fitReviewed=false;EditorUtility.SetDirty(f);}
     EditorGUILayout.HelpBox(CatalogValidation.CheckFit(f,false).ToString(),MessageType.None);
     if(GUILayout.Button("Approve this fitting after preview"))Run(()=>CatalogPreparation.ApproveFitting(f));
    }
   }
   if(EditorGUI.EndChangeCheck())EditorUtility.SetDirty(recipe);
   if(GUILayout.Button("Add drag-and-drop part slot"))Run(()=>{if(!recipe.vehicle)throw new InvalidOperationException("Select a vehicle first.");var f=CreateInstance<FittingProfile>();f.vehicle=recipe.vehicle;AssetDatabase.CreateAsset(f,AssetDatabase.GenerateUniqueAssetPath(CatalogPreparation.Root+"/Recipes/"+recipe.id+"-fit.asset"));recipe.fittings.Add(f);EditorUtility.SetDirty(recipe);});
   if(GUILayout.Button("Refresh assembly preview"))Run(()=>{ReleasePreview();assemblyPreview=CatalogAssembly.Preview(recipe,false);assemblyPreview.hideFlags=HideFlags.HideAndDontSave;previewEditor=UnityEditor.Editor.CreateEditor(assemblyPreview);});
   if(previewEditor&&assemblyPreview)previewEditor.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(200,260),EditorStyles.helpBox);
   if(GUILayout.Button("Bake complete drivable prefab"))Run(()=>{var p=CatalogAssembly.Bake(recipe);Selection.activeObject=p;message="Saved "+AssetDatabase.GetAssetPath(p)+". Register/publish it before AI placement; add it to a session fleet during Editor authoring.";});
   if(recipe.bakedPrefab&&GUILayout.Button("Register assembled vehicle in catalog"))Run(()=>{var a=CatalogBootstrap.RegisterAssembled(recipe);catalog=CatalogPreparation.Library();Select(a);tab=0;});
  }
  void Plans(){
   EditorGUILayout.HelpBox("Plans use Ready asset IDs. Validation rejects unknown assets, overlaps, bad transforms, invalid connections and cycles before touching a scene. Output is an editable layout; contracts, snow fields and session fleets need separate authoring.",MessageType.Info);
   if(GUILayout.Button("Export Ready catalog manifest"))Run(()=>{string contents=CatalogScenePlans.Manifest(catalog);string path=EditorUtility.SaveFilePanelInProject("Export catalog manifest","SnowglowCatalog","json","Save the manifest for AI scene planning",CatalogPreparation.Root);if(path!=""){File.WriteAllText(path,contents);AssetDatabase.Refresh();}});
   var next=(TextAsset)EditorGUILayout.ObjectField("Scene plan JSON",planFile,typeof(TextAsset),false);if(next!=planFile){planFile=next;json=next?next.text:"";}
   json=EditorGUILayout.TextArea(json,GUILayout.MinHeight(220));
   if(GUILayout.Button("Validate plan"))Run(()=>{message=CatalogScenePlans.Resolve(catalog,json).Count+" placements validated.";});
   if(GUILayout.Button("Build new scene from plan"))Run(()=>{string path=EditorUtility.SaveFilePanelInProject("New authored layout","CatalogLayout","unity","Pinefall will not be overwritten","Assets/Scenes");if(path!="")CatalogScenePlans.Build(catalog,json,path);});
  }
 }
}
