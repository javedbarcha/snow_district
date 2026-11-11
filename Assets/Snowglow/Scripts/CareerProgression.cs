using System;
using System.Collections.Generic;
namespace Snowglow {
 // Stable content identities, separate from menu order and the snow simulation.
 public readonly struct TownJob {
  public readonly string town;public readonly int kind;
  public TownJob(string town,int kind){this.town=town;this.kind=kind;}
 }
 public static class CareerProgression {
  public const string Pinefall="pinefall";
  public static readonly string[][] TownIds={new[]{Pinefall,"alps","svalbard","andes"},new[]{"aomori","sapporo","harbin"},new[]{"rogers-pass","coquihalla","tromso"},new[]{"buffalo","anchorage","minneapolis"},new[]{"norilsk","yakutsk","mcmurdo"}};
  public static readonly string[] JobNames={"Depot Access","Parking Lot","Main Street"};
  static readonly string[] JobIds={"depot-access","parking-lot","main-street"};
  public static void Normalize(ProfileSave p,bool legacy=false){
   if(p.careerCompletedJobs==null)p.careerCompletedJobs=Array.Empty<string>();
   if(legacy)p.legacyUnlockedJobs=3; // Old builds offered all three; do not revoke access.
   p.legacyUnlockedJobs=Math.Max(0,Math.Min(3,p.legacyUnlockedJobs));p.careerVersion=1;
   if(string.IsNullOrEmpty(p.jobTownId))p.jobTownId=Pinefall;
  }
  public static string Key(string town,int kind)=>town+":"+(kind>=0&&kind<JobIds.Length?JobIds[kind]:"invalid");
  public static bool Available(string town)=>town==Pinefall;
  public static bool Completed(ProfileSave p,string town,int kind)=>Array.IndexOf(p.careerCompletedJobs??Array.Empty<string>(),Key(town,kind))>=0;
  public static bool TownComplete(ProfileSave p,string town){if(!Available(town))return false;for(int k=0;k<3;k++)if(!Completed(p,town,k))return false;return true;}
  public static bool RegionUnlocked(ProfileSave p,int region){if(region<0||region>=TownIds.Length)return false;if(region==0)return true;foreach(string town in TownIds[region-1])if(!TownComplete(p,town))return false;return RegionUnlocked(p,region-1);}
  public static bool TownUnlocked(ProfileSave p,int region,int town){if(!RegionUnlocked(p,region)||town<0||town>=TownIds[region].Length)return false;return town==0||TownComplete(p,TownIds[region][town-1]);}
  public static bool JobUnlocked(ProfileSave p,string town,int kind){
   if(!Available(town)||kind<0||kind>=3)return false;
   return kind==0||kind<p.legacyUnlockedJobs||Completed(p,town,kind)||Completed(p,town,kind-1);
  }
  public static void Complete(ProfileSave p,string town,int kind){
   if(!Available(town)||kind<0||kind>=3||Completed(p,town,kind))return;
   var jobs=new List<string>(p.careerCompletedJobs??Array.Empty<string>()){Key(town,kind)};p.careerCompletedJobs=jobs.ToArray();
  }
  public static TownJob[] QuickJobs(ProfileSave p,Random random){
   var jobs=new List<TownJob>();foreach(var region in TownIds)foreach(string town in region)for(int k=0;k<3;k++)if(JobUnlocked(p,town,k))jobs.Add(new TownJob(town,k));
   for(int i=jobs.Count-1;i>0;i--){int j=random.Next(i+1);var a=jobs[i];jobs[i]=jobs[j];jobs[j]=a;}return jobs.ToArray();
  }
 }
}
