using UnityEngine;
namespace Snowglow {
 // Session-only direction interlock. No torque crosses a stopped direction change.
 public struct DirectionalDriveState {
  public const float StopSpeed=.05f,StopBuffer=.2f;
  float engaged,stoppedTime;
  public float Resolve(float requested,float throttle,float explicitBrake,float forwardSpeed,float horizontalSpeed,float dt,out float brake,out bool holdStopped){
   brake=explicitBrake;holdStopped=false;
   if(throttle<.02f||explicitBrake>.02f){stoppedTime=0;return 0;}
   if(engaged==0)engaged=Mathf.Abs(forwardSpeed)>StopSpeed?Mathf.Sign(forwardSpeed):requested;
   bool changing=requested!=engaged||forwardSpeed*requested < -StopSpeed;
   if(!changing){stoppedTime=0;return throttle;}
   brake=1;
   if(horizontalSpeed>StopSpeed){stoppedTime=0;return 0;}
   holdStopped=true;stoppedTime+=Mathf.Max(0,dt);
   if(stoppedTime<StopBuffer)return 0;
   engaged=requested;stoppedTime=0;holdStopped=false;brake=explicitBrake;return throttle;
  }
 }
}
