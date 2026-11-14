using System;
using System.Linq;
using UnityEngine;
namespace Snowglow {
 public enum MachineryPartKind { Tyres, Engine, Blade }
 // Gameplay catalogue, independent of the Editor's source-model preparation library.
 // Multipliers are audit starting values; these variants reuse the reviewed physical rig.
 public sealed class MachineryPart {
  public readonly string key,name,description;public readonly MachineryPartKind kind;public readonly int price;public readonly float multiplier;
  public bool Default=>price==0;
  public float PerLevel=>kind==MachineryPartKind.Tyres?.12f:kind==MachineryPartKind.Engine?.18f:.10f;
  public MachineryPart(string key,string name,MachineryPartKind kind,int price,float multiplier,string description){this.key=key;this.name=name;this.kind=kind;this.price=price;this.multiplier=multiplier;this.description=description;}
 }
 [Serializable]public sealed class MachineryPartSave {public string id;public bool owned;public int level;}
 public static class MachineryParts {
  public const int Count=7,MaxLevel=3;
  static MachineryPart P(string key,string name,MachineryPartKind kind,int price,float multiplier,string description)=>new MachineryPart(key,name,kind,price,multiplier,description);
  static readonly MachineryPart[] Tractor={
   P("tyres-standard","Standard tractor tyres",MachineryPartKind.Tyres,0,1,"Factory snow tyres."),
   P("engine-standard","Standard tractor diesel",MachineryPartKind.Engine,0,1,"Factory diesel engine."),
   P("tyres-winter","Winter tractor tyres",MachineryPartKind.Tyres,450,1.12f,"Winter compound for extra grip."),
   P("tyres-chained","Chained tractor tyres",MachineryPartKind.Tyres,750,1.25f,"Chains for the strongest grip."),
   P("engine-turbo","Turbo tractor diesel",MachineryPartKind.Engine,900,1.22f,"Higher torque for snow work."),
   P("blade-standard","Standard tractor plow",MachineryPartKind.Blade,0,1,"Factory road-clearing blade."),
   P("blade-heavy","Heavy-duty tractor plow",MachineryPartKind.Blade,650,1.20f,"Improved snow transfer rate; same fitted dimensions.")};
  static readonly MachineryPart[] Loader={
   P("tyres-standard","Standard loader tyres",MachineryPartKind.Tyres,0,1,"Factory loader tyres."),
   P("engine-standard","Standard loader diesel",MachineryPartKind.Engine,0,1,"Factory loader engine."),
   P("tyres-winter","Winter loader tyres",MachineryPartKind.Tyres,550,1.12f,"Winter compound for extra grip."),
   P("tyres-chained","Chained loader tyres",MachineryPartKind.Tyres,850,1.25f,"Chains for the strongest grip."),
   P("engine-turbo","Turbo loader diesel",MachineryPartKind.Engine,1100,1.22f,"Higher torque for loaded work."),
   P("blade-standard","Standard loader bucket",MachineryPartKind.Blade,0,1,"Factory snow bucket."),
   P("blade-heavy","Heavy-duty loader bucket",MachineryPartKind.Blade,800,1.20f,"Improved collection rate; same capacity and fitted dimensions.")};
  public static MachineryPart At(bool loader,int index)=>(loader?Loader:Tractor)[Mathf.Clamp(index,0,Count-1)];
  public static string Id(MachineController machine,int index)=>machine.definition.id+"/"+At(machine.definition.loader,index).key;
  public static int DefaultIndex(MachineryPartKind kind)=>kind==MachineryPartKind.Tyres?0:kind==MachineryPartKind.Engine?1:5;
  public static int UpgradePrice(int level)=>level>=MaxLevel?0:200+Mathf.Clamp(level,0,MaxLevel)*200;
  public static string Effect(MachineryPart part,int level)=>((part.multiplier*(1+Mathf.Clamp(level,0,3)*part.PerLevel)-1)*100).ToString("F0")+"% bonus "+(part.kind==MachineryPartKind.Tyres?"grip":part.kind==MachineryPartKind.Engine?"torque":"snow rate");
  public static MachineryPartSave[] Normalize(MachineController machine,MachineryPartSave[] saved,int tyres=0,int engine=0){
   var result=new MachineryPartSave[Count];for(int i=0;i<Count;i++){
    string id=Id(machine,i);var old=saved?.FirstOrDefault(s=>s!=null&&s.id==id);var part=At(machine.definition.loader,i);
    result[i]=new MachineryPartSave{id=id,owned=part.Default||(old!=null&&old.owned),level=old!=null?Mathf.Clamp(old.level,0,3):i==0?Mathf.Clamp(tyres,0,3):i==1?Mathf.Clamp(engine,0,3):0};
    if(!result[i].owned)result[i].level=0;
   }return result;
  }
 }
}
