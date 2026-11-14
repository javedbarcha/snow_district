using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snowglow {
 [Serializable] public sealed class CareerSnowLoad {
  public string vehicle;
  public Vector3 position;
  public float volume;
 }
 [Serializable] public sealed class CareerSnowState {
  public string town;
  public int size=SnowVolumeGrid.Size,version=1;
  public int[] cells=Array.Empty<int>();
  public float[] depths=Array.Empty<float>();
  public CareerSnowLoad[] loads=Array.Empty<CareerSnowLoad>();

  public static CareerSnowState Find(ProfileSave profile,string town){
   foreach(var state in profile.careerSnow??Array.Empty<CareerSnowState>())if(state!=null&&state.town==town&&state.version==1&&state.size==SnowVolumeGrid.Size&&state.cells!=null&&state.depths!=null&&state.cells.Length==state.depths.Length)return state;
   return null;
  }
  public static void Store(ProfileSave profile,CareerSnowState state){
   var states=new List<CareerSnowState>(profile.careerSnow??Array.Empty<CareerSnowState>());states.RemoveAll(s=>s==null||s.town==state.town);states.Add(state);profile.careerSnow=states.ToArray();
  }
  public static SnowVolumeGrid FromSeed(SnowVolumeGrid seed){
   return new SnowVolumeGrid{depth=(float[])seed.depth.Clone(),credit=new float[seed.depth.Length],blocked=(bool[])seed.blocked.Clone(),initialVolume=seed.initialVolume,authoredLayoutVersion=seed.authoredLayoutVersion};
  }
  public static CareerSnowState Capture(string town,SnowVolumeGrid grid,SnowVolumeGrid seed,MachineController[] fleet){
   var cells=new List<int>();var depths=new List<float>();
   for(int i=0;i<grid.depth.Length;i++)if(grid.depth[i]!=seed.depth[i]){cells.Add(i);depths.Add(grid.depth[i]);}
   var loads=new List<CareerSnowLoad>();foreach(var machine in fleet)if(machine.Tool.Load>0)loads.Add(new CareerSnowLoad{vehicle=machine.definition.id,position=machine.Tool.mouth.position,volume=machine.Tool.Load});
   return new CareerSnowState{town=town,cells=cells.ToArray(),depths=depths.ToArray(),loads=loads.ToArray()};
  }
  public static float ClearLegacyCompleted(SnowVolumeGrid grid,ProfileSave profile,string town,int except=-1){
   double removed=0;
   for(int kind=0;kind<CareerProgression.JobNames.Length;kind++)if(kind!=except&&CareerProgression.Completed(profile,town,kind)){
    var bounds=SnowField.ContractBounds(kind);
    for(int i=0;i<grid.depth.Length;i++)if(Inside(bounds,grid.X(i),grid.Z(i))){removed+=grid.depth[i]*SnowVolumeGrid.Area;grid.depth[i]=grid.credit[i]=0;}
   }
   grid.initialVolume-=(float)removed;return (float)removed;
  }
  public static bool Inside(Rect bounds,float x,float z)=>x>bounds.xMin&&x<bounds.xMax&&z>bounds.yMin&&z<bounds.yMax;
  public static CareerSnowState Ensure(ProfileSave profile,SnowVolumeGrid seed,string town){
   var state=Find(profile,town);if(state!=null)return state;
   var grid=FromSeed(seed);ClearLegacyCompleted(grid,profile,town);
   // Historical saves retained completion IDs but not the completed town's snow.
   var cells=new List<int>();var depths=new List<float>();for(int i=0;i<grid.depth.Length;i++)if(grid.depth[i]!=seed.depth[i]){cells.Add(i);depths.Add(grid.depth[i]);}
   state=new CareerSnowState{town=town,cells=cells.ToArray(),depths=depths.ToArray()};Store(profile,state);return state;
  }
  public SnowVolumeGrid Restore(SnowVolumeGrid seed,out Dictionary<string,float> remainingLoads){
   var grid=FromSeed(seed);remainingLoads=new Dictionary<string,float>();
   for(int n=0;n<cells.Length;n++){int i=cells[n];float depth=depths[n];if(i>=0&&i<grid.depth.Length&&!float.IsNaN(depth)&&!float.IsInfinity(depth)&&depth>=0)grid.depth[i]=depth;}
   SnowField.MigrateAuthoredLayout(grid);
   foreach(var load in loads??Array.Empty<CareerSnowLoad>())if(load!=null&&load.volume>0){
    float left=Mathf.Max(0,load.volume-SnowField.DropRecoveryLoad(grid,load.position,load.volume,null));
    if(left>.000001f){remainingLoads.TryGetValue(load.vehicle,out float previous);remainingLoads[load.vehicle]=previous+left;}
   }
   grid.initialVolume=grid.Total();foreach(float load in remainingLoads.Values)grid.initialVolume+=load;return grid;
  }
  public static void ReseedVenue(SnowVolumeGrid grid,SnowVolumeGrid seed,int kind){
   var bounds=SnowField.ContractBounds(kind);
   for(int i=0;i<grid.depth.Length;i++)if(Inside(bounds,grid.X(i),grid.Z(i))){grid.depth[i]=seed.depth[i];grid.credit[i]=0;}
  }
 }
}
