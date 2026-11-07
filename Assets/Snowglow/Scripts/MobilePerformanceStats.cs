using System;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
namespace Snowglow {
 // Fixed storage and unscaled wall time: gameplay timeScale and frame caps do not fabricate FPS.
 public sealed class PerformanceFrameWindow {
  const int Capacity=2048;
  readonly double[] times=new double[Capacity];readonly float[] seconds=new float[Capacity],sorted=new float[Capacity];
  int head,count;double clock,sum;
  public int Count=>count;
  public double Fps=>sum>0?count/sum:0;
  public double AverageMs=>count>0?sum*1000/count:0;
  public double Duration=>sum;
  public void Clear(){head=count=0;clock=sum=0;}
  void Remove(){sum-=seconds[head];head=(head+1)%Capacity;count--;}
  public void Add(float dt){if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;clock+=dt;while(count>0&&clock-times[head]>5)Remove();if(count==Capacity)Remove();int index=(head+count)%Capacity;times[index]=clock;seconds[index]=dt;sum+=dt;count++;}
  public float Percentile99Ms(){if(count==0)return 0;for(int i=0;i<count;i++)sorted[i]=seconds[(head+i)%Capacity];Array.Sort(sorted,0,count);return sorted[Math.Max(0,(int)Math.Ceiling(count*.99)-1)]*1000;}
 }
 public sealed class MobilePerformanceStats:MonoBehaviour {
  public RectTransform safeArea,buttonGroup,readoutRect;
  public GameObject readout;
  public Text readoutText,buttonLabel;
  public Button toggleButton;
  readonly PerformanceFrameWindow frames=new PerformanceFrameWindow();
  readonly StringBuilder builder=new StringBuilder(1024);
  readonly Dictionary<string,ProfilerRecorder> counters=new Dictionary<string,ProfilerRecorder>();
  readonly List<ProfilerRecorderHandle> handles=new List<ProfilerRecorderHandle>();
  static readonly string[] Names={"CPU Main Thread Frame Time","Main Thread","GPU Frame Time","Total Used Memory","Total Reserved Memory","GC Used Memory","GC Allocated In Frame","Draw Calls Count","Batches Count","SetPass Calls Count","Triangles Count","Vertices Count"};
  double last=double.NaN,next;bool visible,suspended,applicationPaused,hasFocus=true;string previousState;
  public bool Visible=>visible;
  public int RecorderCount=>counters.Count;
  public void Toggle(){SetVisible(!visible);}
  public void SetVisible(bool value){visible=value;if(readout)readout.SetActive(value);if(buttonLabel)buttonLabel.text=value?"STATS ON":"STATS";ResetWindow();if(value&&!suspended)StartCounters();else StopCounters();if(value)RefreshDisplay();}
  void Awake(){SetVisible(false);}
  void OnDisable(){visible=false;if(readout)readout.SetActive(false);if(buttonLabel)buttonLabel.text="STATS";StopCounters();ResetWindow();}
  void OnDestroy(){StopCounters();}
  void OnApplicationPause(bool pause){applicationPaused=pause;SetSuspended(applicationPaused||!hasFocus);}
  void OnApplicationFocus(bool focus){hasFocus=focus;SetSuspended(applicationPaused||!hasFocus);}
  void SetSuspended(bool value){suspended=value;ResetWindow();if(suspended)StopCounters();else if(visible)StartCounters();}
  void ResetWindow(){frames.Clear();last=double.NaN;next=0;}
  void StartCounters(){StopCounters();handles.Clear();ProfilerRecorderHandle.GetAvailable(handles);foreach(var handle in handles){var desc=ProfilerRecorderHandle.GetDescription(handle);if(Array.IndexOf(Names,desc.Name)<0||counters.ContainsKey(desc.Name))continue;var recorder=ProfilerRecorder.StartNew(desc.Category,desc.Name,1);if(recorder.Valid)counters.Add(desc.Name,recorder);else recorder.Dispose();}}
  void StopCounters(){foreach(var recorder in counters.Values)recorder.Dispose();counters.Clear();}
  bool Read(string name,out long value){value=0;if(!counters.TryGetValue(name,out var r)||!r.Valid||r.Count==0)return false;value=r.LastValue;return true;}
  public static string CounterText(bool available,long value,double divisor=1,string format="N0")=>available?(value/divisor).ToString(format,System.Globalization.CultureInfo.InvariantCulture):"n/a";
  string Counter(string name,double divisor=1,string format="N0",bool positive=false){bool available=Read(name,out long value);return CounterText(available&&(!positive||value>0),value,divisor,format);}
  string State(){var game=PrototypeSession.Instance;return game&&game.IsRunning?"PLAY":Time.timeScale==0?"PAUSED":"MENU";}
  void Update(){
   if(!visible||suspended)return;
   var state=State();if(state!=previousState){previousState=state;ResetWindow();}
   double now=Time.realtimeSinceStartupAsDouble;if(!double.IsNaN(last))frames.Add((float)(now-last));last=now;
   if(now>=next){next=now+.5;RefreshDisplay();}
  }
  void LateUpdate(){if(!safeArea)return;float fit=Mathf.Min(safeArea.rect.width/1280f,safeArea.rect.height/720f);if(buttonGroup)buttonGroup.localScale=Vector3.one*fit;if(readoutRect){readoutRect.localScale=Vector3.one*fit;readoutRect.anchoredPosition=new Vector2(0,-72*fit);}}
  public void RefreshDisplay(){
   if(!readoutText)return;builder.Clear();builder.Append("PERFORMANCE | ").Append(State()).Append("\n");
   if(frames.Count>1)builder.Append("FPS avg (5s) ").Append(frames.Fps.ToString("F1")).Append(" | target 30\nFrame ").Append(frames.AverageMs.ToString("F1")).Append(" ms | p99 ").Append(frames.Percentile99Ms().ToString("F1")).Append(" ms\n");else builder.Append("FPS / frame time: sampling...\n\n");
   string cpu=Counter("CPU Main Thread Frame Time",1e6,"F1",true);if(cpu=="n/a")cpu=Counter("Main Thread",1e6,"F1",true);
   builder.Append("CPU main ").Append(cpu).Append(" ms | GPU ").Append(Counter("GPU Frame Time",1e6,"F1",true)).Append(" ms\n");
   builder.Append("Unity memory ").Append(Counter("Total Used Memory",1048576,"F0")).Append(" / ").Append(Counter("Total Reserved Memory",1048576,"F0")).Append(" MB used/res\n");
   builder.Append("GC heap ").Append(Counter("GC Used Memory",1048576,"F1")).Append(" MB | alloc ").Append(Counter("GC Allocated In Frame",1024,"F1")).Append(" KB/frame\n");
   builder.Append("Draws ").Append(Counter("Draw Calls Count")).Append(" | batches ").Append(Counter("Batches Count")).Append(" | SetPass ").Append(Counter("SetPass Calls Count")).Append('\n');
   builder.Append("Tris ").Append(Counter("Triangles Count")).Append(" | verts ").Append(Counter("Vertices Count")).Append('\n');
   builder.Append(Screen.width).Append('x').Append(Screen.height).Append(" | ").Append(SystemInfo.graphicsDeviceType).Append(Debug.isDebugBuild?" | DEV":" | RELEASE").Append('\n');
   string model=SystemInfo.deviceModel;builder.Append(model.Length>40?model.Substring(0,40):model).Append("\nCounters: last frame; CPU may include waits\nn/a = unavailable");readoutText.text=builder.ToString();
  }
 }
}
