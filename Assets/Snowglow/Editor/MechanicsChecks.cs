using System;
using UnityEditor;
using UnityEngine;
namespace Snowglow.Editor {
 public static class MechanicsChecks {
  static void Check(bool ok,string name){if(!ok)throw new Exception("Mechanics check failed: "+name);}
  static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
  [MenuItem("Snowglow/Run snow ledger checks")]
  public static void Run(){
   var g=new SnowVolumeGrid();int source=g.Index(0,0);g.depth[source]=1;g.credit[source]=1;g.initialVolume=1;
   float deposited=g.Deposit(8,8,.4f,null);float moved=g.Take(source,deposited,out float credit);
   Check(Near(moved,.4f)&&Near(credit,.4f),"transfer amount and original work credit");Check(Near(g.Total(),1),"volume conserved through pushing");
   int target=g.Index(8,8);float take=g.Take(target,.05f,out float recycled);Check(Near(recycled,0),"moved snow cannot create reward credit");
   float carried=take;Check(Near(g.Total()+carried,1),"carried volume conservation");float accepted=g.Deposit(15,15,carried,null);carried-=accepted;Check(Near(g.Total()+carried,1),"dump volume conservation");
   Check(Near(g.Deposit(600,600,.2f,null),0),"out-of-bounds deposit refused");
   var blocked=new SnowVolumeGrid();for(int i=0;i<blocked.depth.Length;i++)blocked.depth[i]=2.5f;
   Check(Near(blocked.Deposit(0,0,.5f,null),0),"full destination refused");
   var rng=new System.Random(42);g=new SnowVolumeGrid();for(int i=0;i<500;i++){int index=g.Index((float)rng.NextDouble()*100-50,(float)rng.NextDouble()*100-50);g.depth[index]+=.2f;}float initial=g.Total();
   for(int n=0;n<1000;n++){int index=rng.Next(g.depth.Length);float amount=Math.Min(.1f,g.depth[index]);float a=g.Deposit((float)rng.NextDouble()*100-50,(float)rng.NextDouble()*100-50,amount,null);g.Take(index,a,out float unused);}
   Check(Math.Abs(g.Total()-initial)<.002f,"random transfers conserve volume");
   g=new SnowVolumeGrid();g.blocked[g.Index(8,8)]=true;float placed=g.Deposit(8,8,.1f,null);Check(Near(placed,.1f)&&Near(g.depth[g.Index(8,8)],0),"static obstacle rejected and snow redirected");
   Debug.Log("Snowglow: 9 snow ledger checks passed. Vehicle/URP/mobile playtests are separate.");
  }
 }
}
