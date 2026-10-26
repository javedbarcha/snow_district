using System;
using UnityEditor;
using UnityEngine;
namespace Snowglow.Editor {
 public static class MechanicsChecks {
  const string PendingCheck="Logs/PileChecks.request";
  [InitializeOnLoadMethod]static void OnReload(){EditorApplication.update+=RunPending;}
  static void RunPending(){
   if(Application.isBatchMode||!System.IO.File.Exists(PendingCheck)){EditorApplication.update-=RunPending;return;}
   if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=RunPending;
   try{Run();System.IO.File.WriteAllText("Logs/PileChecks.txt","PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+"; all 19 ledger and pile-spreading checks passed. Live rendering, handling and Galaxy A07 performance remain untested.");System.IO.File.Delete(PendingCheck);}
   catch(Exception e){System.IO.File.WriteAllText("Logs/PileChecks.txt","FAIL: "+e);throw;}
  }
  static void Check(bool ok,string name){if(!ok)throw new Exception("Mechanics check failed: "+name);}
  static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
  [MenuItem("Snowglow/Run snow ledger checks")]
  public static void Run(){
   var g=new SnowVolumeGrid();int source=g.Index(0,0);g.depth[source]=1/SnowVolumeGrid.Area;g.credit[source]=1/SnowVolumeGrid.Area;g.initialVolume=1;
   float deposited=g.Deposit(8,8,.4f,null);float moved=g.Take(source,deposited,out float credit);
   Check(Near(moved,.4f)&&Near(credit,.4f),"transfer amount and original work credit");Check(Near(g.Total(),1),"volume conserved through pushing");
   int target=g.Index(8,8);float take=g.Take(target,.05f,out float recycled);Check(Near(recycled,0),"moved snow cannot create reward credit");
   float carried=take;Check(Near(g.Total()+carried,1),"carried volume conservation");float accepted=g.Deposit(15,15,carried,null);carried-=accepted;Check(Near(g.Total()+carried,1),"dump volume conservation");
   Check(Near(g.Deposit(600,600,.2f,null),0),"out-of-bounds deposit refused");
   var blocked=new SnowVolumeGrid();for(int i=0;i<blocked.depth.Length;i++)blocked.depth[i]=2.5f;
   Check(Near(blocked.Deposit(0,0,.5f,null),0),"full destination refused");
   var rng=new System.Random(42);g=new SnowVolumeGrid();for(int i=0;i<500;i++){int index=g.Index((float)rng.NextDouble()*100-50,(float)rng.NextDouble()*100-50);g.depth[index]+=.2f;}float initial=g.Total();
   for(int n=0;n<1000;n++){int index=rng.Next(g.depth.Length);float amount=Math.Min(.1f,g.depth[index]*SnowVolumeGrid.Area);float a=g.Deposit((float)rng.NextDouble()*100-50,(float)rng.NextDouble()*100-50,amount,null);g.Take(index,a,out float unused);}
   Check(Math.Abs(g.Total()-initial)<.002f,"random transfers conserve volume");
   g=new SnowVolumeGrid();g.blocked[g.Index(8,8)]=true;float placed=g.Deposit(8,8,.1f,null);Check(Near(placed,.1f)&&Near(g.depth[g.Index(8,8)],0),"static obstacle rejected and snow redirected");
   g=new SnowVolumeGrid();source=g.Index(0,0);g.depth[source]=.3f;g.credit[source]=.3f;
   float removed=g.Take(source,1,out float earned);Check(Near(removed,.3f*SnowVolumeGrid.Area)&&Near(earned,removed),"half-metre cells transfer cubic metres");
   Check(SnowVolumeGrid.Cell==.5f&&g.Index(-128,-128)==0&&g.Index(128,0)==-1&&g.Index(g.X(source),g.Z(source))==source,"half-size grid retains world bounds");
   var legacy=new SnowVolumeGrid{depth=new float[65536],credit=new float[65536],blocked=new bool[65536],initialVolume=.8f,disposed=.2f};legacy.depth[0]=.8f;legacy.credit[0]=.4f;legacy.blocked[0]=true;
   var upgraded=SnowVolumeGrid.Upgrade(legacy);Check(Near(upgraded.Total(),.8f)&&Near(upgraded.credit[1],.4f)&&upgraded.blocked[1]&&Near(upgraded.disposed,.2f),"old seeds and saves retain volume, credit and obstacles");
   g=new SnowVolumeGrid();float delivered=0;for(int n=0;n<100;n++)delivered+=g.Deposit(0,0,.02f,null);
   float peak=0;int footprint=0;foreach(float d in g.depth){peak=Math.Max(peak,d);if(d>.001f)footprint++;}
   Check(Near(g.Total(),delivered)&&peak<1.2f&&footprint*SnowVolumeGrid.Area>2.5f,"repeated dumping forms a broad mound instead of a rod");
   g=new SnowVolumeGrid();source=g.Index(0,0);g.depth[source]=2.4f;g.credit[source]=2.4f;g.WakePiles();initial=g.Total();
   Check(g.Settle(0,512,null)==0&&Near(g.depth[source],2.4f),"paused piles do not slump");
   for(int n=0;n<400;n++)g.Settle(1f/30,512,null);
   Check(Near(g.Total(),initial)&&g.depth[source]<1&&g.depth[source+1]>0,"tall piles spread sideways while conserving volume");
   int mid=SnowVolumeGrid.Size/2;float maxDifference=0;
   for(int z=mid-12;z<mid+12;z++)for(int x=mid-12;x<mid+12;x++){int i=z*SnowVolumeGrid.Size+x;maxDifference=Math.Max(maxDifference,Math.Abs(g.depth[i]-g.depth[i+1]));maxDifference=Math.Max(maxDifference,Math.Abs(g.depth[i]-g.depth[i+SnowVolumeGrid.Size]));}
   Check(maxDifference<=SnowVolumeGrid.Cell*SnowVolumeGrid.ReposeSlope+SnowVolumeGrid.Cohesion+.003f,"settled pile respects the tuned side slope");
   Check(Near(g.credit[source+1],0),"passive spreading creates no work credit");
   g=new SnowVolumeGrid();source=g.Index(0,0);g.depth[source]=2;g.blocked[source+1]=true;g.WakePiles();
   for(int n=0;n<80;n++)g.Settle(1f/30,128,null);
   Check(Near(g.depth[source+1],0)&&Near(g.Total(),2*SnowVolumeGrid.Area),"spreading avoids blocked cells");
   g=new SnowVolumeGrid();g.depth[0]=2;g.WakePiles();for(int n=0;n<80;n++)g.Settle(1f/30,128,null);
   Check(Near(g.Total(),2*SnowVolumeGrid.Area),"edge piles retain their volume");
   Debug.Log("Snowglow: 19 snow ledger checks passed. Vehicle/URP/mobile playtests are separate.");
  }
 }
}
