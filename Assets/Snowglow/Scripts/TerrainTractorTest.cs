using UnityEngine;
using UnityEngine.InputSystem;
namespace Snowglow {
 // Deliberately has no PrototypeSession, snow field, menus, economy or save access.
 public sealed class TerrainTractorTest : MonoBehaviour {
  public MachineController tractor;
  public PrototypeInput input;
  public CameraRig cameraRig;
  public bool diagnosticDrive;
  void Awake(){Time.timeScale=1;Application.runInBackground=Application.isEditor;Application.targetFrameRate=60;tractor.Initialize();tractor.Owned=true;tractor.Fuel=tractor.definition.fuelCapacity;tractor.Condition=100;tractor.Tool.ResetRaised();tractor.SetActiveMachine(true);cameraRig.SetMode(CameraMode.Orbit);}
  void Update(){var k=Keyboard.current;if(k==null)return;if(k.rKey.wasPressedThisFrame){input.Clear();tractor.RecoverToDepot();cameraRig.ResetView();}if(k.cKey.wasPressedThisFrame)cameraRig.Toggle();}
  void FixedUpdate(){if(diagnosticDrive){input.Throttle=1;input.Steer=0;input.Brake=0;input.Reverse=false;}tractor.SimulateDriving(input,null);if(tractor.Body.position.y< -50)tractor.RecoverToDepot();}
  void LateUpdate()=>cameraRig.UpdateForTarget(tractor.transform,Time.unscaledDeltaTime,true);
  void OnApplicationFocus(bool focus){if(!focus)input.Clear();}
  void OnDisable(){input.Clear();}
 }
}
