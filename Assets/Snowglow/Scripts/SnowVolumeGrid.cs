using System;
namespace Snowglow {
 // Authoritative, engine-independent volume ledger. One metre cells; depths are metres.
 [Serializable] public sealed class SnowVolumeGrid {
  public const int Size=256;public const float Origin=-128;public const float Cell=1;
  public float[] depth=new float[Size*Size],credit=new float[Size*Size];
  public bool[] blocked=new bool[Size*Size];
  public float initialVolume,disposed;public int revision;
  public int Index(float x,float z){int ix=(int)Math.Floor(x-Origin),iz=(int)Math.Floor(z-Origin);return ix<0||iz<0||ix>=Size||iz>=Size?-1:iz*Size+ix;}
  public float X(int i){return Origin+i%Size+.5f;}public float Z(int i){return Origin+i/Size+.5f;}
  public float Take(int i,float amount,out float newlyCredited) {
   newlyCredited=0;if(i<0||i>=depth.Length||amount<=0)return 0;
   float taken=Math.Min(amount,depth[i]);depth[i]-=taken;newlyCredited=Math.Min(credit[i],taken);credit[i]-=newlyCredited;revision++;return taken;
  }
  public bool CanDeposit(float x,float z){return x>Origin+4&&x<Origin+Size-4&&z>Origin+4&&z<Origin+Size-4;}
  public float Deposit(float x,float z,float volume,Action<int> changed) {
   if(volume<=0||!CanDeposit(x,z))return 0;float left=volume;
   // Spread rather than create an infinitely tall column. No credit is created by moving snow.
   int center=Index(x,z),cx=center%Size,cz=center/Size;
   for(int ring=0;ring<=3&&left>.000001f;ring++)for(int dz=-ring;dz<=ring&&left>.000001f;dz++)for(int dx=-ring;dx<=ring&&left>.000001f;dx++) {
    if(Math.Max(Math.Abs(dx),Math.Abs(dz))!=ring)continue;int ix=cx+dx,iz=cz+dz;if(ix<0||iz<0||ix>=Size||iz>=Size)continue;
    int i=iz*Size+ix;if(blocked!=null&&blocked[i])continue;float room=Math.Max(0,2.5f-depth[i]);float add=Math.Min(left,Math.Min(room,.12f));
    if(add<=0)continue;depth[i]+=add;left-=add;changed?.Invoke(i);
   }revision++;return volume-left;
  }
  public float Total(){double sum=0;foreach(float d in depth)sum+=d;return (float)sum;}
 }
}
