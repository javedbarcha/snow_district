using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Snowglow.Editor {
 // Deterministic Edit-mode source for the reusable Shop models. No runtime geometry.
 public static class MachineryPartModels {
  public const string Folder="Assets/Snowglow/Catalog/Sources/MachineryParts";
  const int Layer=12;
  enum Surface { Rubber, Tread, Paint, Steel, Dark, Silver, Copper, Accent }
  sealed class Geometry {
   public readonly List<Vector3> vertices=new List<Vector3>();
   public readonly List<int>[] faces=Enumerable.Range(0,8).Select(_=>new List<int>()).ToArray();
   public void Tri(Vector3 a,Vector3 b,Vector3 c,Vector3 normal,Surface surface){
    if(Vector3.Cross(b-a,c-a).sqrMagnitude<.0000000001f)return;
    if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var swap=b;b=c;c=swap;}
    int offset=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);faces[(int)surface].AddRange(new[]{offset,offset+1,offset+2});
   }
   public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,Surface s){Tri(a,b,c,normal,s);Tri(a,c,d,normal,s);}
   public void Box(Vector3 center,Vector3 size,Surface surface,Quaternion rotation=default){
    if(rotation.Equals(default(Quaternion)))rotation=Quaternion.identity;
    Vector3[] p=new Vector3[8];for(int i=0;i<8;i++)p[i]=center+rotation*Vector3.Scale(size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
    int[][] f={new[]{0,2,6,4},new[]{1,5,7,3},new[]{0,4,5,1},new[]{2,3,7,6},new[]{0,1,3,2},new[]{4,6,7,5}};
    foreach(var q in f)Quad(p[q[0]],p[q[1]],p[q[2]],p[q[3]],(p[q[0]]+p[q[1]]+p[q[2]]+p[q[3]])*.25f-center,surface);
   }
   public void Bevel(Vector3 center,Vector3 size,Surface surface,float bevel=.07f,Quaternion rotation=default){
    if(rotation.Equals(default(Quaternion)))rotation=Quaternion.identity;
    float x=size.x*.5f,z=size.z*.5f,y=size.y*.5f,b=Mathf.Min(bevel,Mathf.Min(x,Mathf.Min(y,z))*.4f);
    var ring=new[]{new Vector2(-x+b,-z),new Vector2(x-b,-z),new Vector2(x,-z+b),new Vector2(x,z-b),new Vector2(x-b,z),new Vector2(-x+b,z),new Vector2(-x,z-b),new Vector2(-x,-z+b)};
    Vector3[][] levels=new Vector3[4][];float[] ys={-y,-y+b,y-b,y};
    for(int j=0;j<4;j++)levels[j]=ring.Select(v=>center+rotation*new Vector3(v.x*((j==0||j==3)?(x-b)/x:1),ys[j],v.y*((j==0||j==3)?(z-b)/z:1))).ToArray();
    for(int j=0;j<3;j++)for(int i=0;i<8;i++){int n=(i+1)%8;var a=levels[j][i];var c=levels[j+1][n];Quad(a,levels[j][n],c,levels[j+1][i],(a+c)*.5f-center,surface);}
    for(int i=1;i<7;i++){Tri(levels[0][0],levels[0][i],levels[0][i+1],rotation*Vector3.down,surface);Tri(levels[3][0],levels[3][i],levels[3][i+1],rotation*Vector3.up,surface);}
   }
   public void Tube(Vector3 a,Vector3 b,float radius,Surface surface,int sides=8,bool caps=true){
    var axis=(b-a).normalized;var u=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.right:Vector3.up).normalized*radius;var v=Vector3.Cross(axis,u);
    for(int i=0;i<sides;i++){float t=i*Mathf.PI*2/sides,tn=(i+1)*Mathf.PI*2/sides;var p=u*Mathf.Cos(t)+v*Mathf.Sin(t);var q=u*Mathf.Cos(tn)+v*Mathf.Sin(tn);Quad(a+p,a+q,b+q,b+p,p+q,surface);if(caps){Tri(a,a+p,a+q,-axis,surface);Tri(b,b+p,b+q,axis,surface);}}
   }
   public void Beam(Vector3 a,Vector3 b,float width,float depth,Surface surface){Box((a+b)*.5f,new Vector3(width,depth,Vector3.Distance(a,b)),surface,Quaternion.LookRotation(b-a));}
   public void Lathe(Vector2[] profile,int segments,Surface surface,Vector3 center=default){
    for(int j=0;j<profile.Length;j++){var a=profile[j];var b=profile[(j+1)%profile.Length];for(int i=0;i<segments;i++){
     float angle=i*Mathf.PI*2/segments,next=(i+1)*Mathf.PI*2/segments,mid=(angle+next)*.5f;
     Vector3 radial=new Vector3(0,Mathf.Cos(mid),Mathf.Sin(mid));Vector3 normal=new Vector3(a.y-b.y,0,0)+radial*(b.x-a.x);
     Quad(center+new Vector3(a.x,a.y*Mathf.Cos(angle),a.y*Mathf.Sin(angle)),center+new Vector3(a.x,a.y*Mathf.Cos(next),a.y*Mathf.Sin(next)),center+new Vector3(b.x,b.y*Mathf.Cos(next),b.y*Mathf.Sin(next)),center+new Vector3(b.x,b.y*Mathf.Cos(angle),b.y*Mathf.Sin(angle)),normal,surface);
    }}
   }
   public void Link(Vector3 center,Vector3 u,Vector3 v,float thickness,Surface surface){
    var plane=Vector3.Cross(u,v).normalized;var points=new Vector3[6,4];var centers=new Vector3[6];
    for(int i=0;i<6;i++){float a=i*Mathf.PI/3;centers[i]=center+u*Mathf.Cos(a)+v*Mathf.Sin(a);var side=Vector3.Cross(plane,-u*Mathf.Sin(a)+v*Mathf.Cos(a)).normalized;for(int j=0;j<4;j++)points[i,j]=centers[i]+thickness*(side*Mathf.Cos(j*Mathf.PI*.5f)+plane*Mathf.Sin(j*Mathf.PI*.5f));}
    for(int i=0;i<6;i++)for(int j=0;j<4;j++){int n=(i+1)%6,k=(j+1)%4;Quad(points[i,j],points[n,j],points[n,k],points[i,k],(points[i,j]+points[n,j]+points[n,k]+points[i,k])*.25f-(centers[i]+centers[n])*.5f,surface);}
   }
   public Mesh Mesh(string name){
    var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt16};mesh.SetVertices(vertices);mesh.subMeshCount=8;for(int i=0;i<8;i++)mesh.SetTriangles(faces[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
   }
  }
  static Material[] Materials(bool loader,int index){
   Color paint=index==1?(loader?new Color(.88f,.60f,.08f):new Color(.19f,.46f,.28f)):index==4?(loader?new Color(.67f,.15f,.10f):new Color(.12f,.40f,.62f)):new Color(.96f,.63f,.07f);
   var colors=new[]{new Color(.055f,.068f,.075f),new Color(.09f,.105f,.115f),paint,new Color(.32f,.38f,.42f),new Color(.055f,.08f,.10f),new Color(.61f,.69f,.74f),new Color(.66f,.36f,.13f),new Color(.92f,.91f,.75f)};
   var names=new[]{"Rubber","Tread","Paint-"+(loader?"loader":"tractor")+"-"+index,"Steel","Dark","Silver","Copper","Ivory"};
   var result=new Material[8];for(int i=0;i<8;i++){string path=Folder+"/"+names[i]+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",colors[i]);m.SetFloat("_Smoothness",i==5?.36f:.13f);m.SetFloat("_Metallic",i==5?.5f:0);EditorUtility.SetDirty(m);result[i]=m;}return result;
  }
  static Geometry Tyre(bool loader,int variant){
   var g=new Geometry();float w=loader?1.08f:.74f;float h=w*.5f;var center=Vector3.zero;
   g.Lathe(new[]{new Vector2(-h*.50f,.57f),new Vector2(-h*.85f,.72f),new Vector2(-h,.98f),new Vector2(-h*.95f,1.20f),new Vector2(-h*.65f,1.34f),new Vector2(h*.65f,1.34f),new Vector2(h*.95f,1.20f),new Vector2(h,.98f),new Vector2(h*.85f,.72f),new Vector2(h*.50f,.57f)},24,Surface.Rubber,center);
   // Deep dished rims on BOTH faces: no flat circular cap masquerading as a tyre.
   g.Lathe(new[]{new Vector2(-h*.72f,.12f),new Vector2(-h*.4f,.20f),new Vector2(-h*.48f,.43f),new Vector2(-h*.90f,.56f),new Vector2(-h*.85f,.60f),new Vector2(h*.85f,.60f),new Vector2(h*.90f,.56f),new Vector2(h*.48f,.43f),new Vector2(h*.4f,.20f),new Vector2(h*.72f,.12f)},24,variant==1?Surface.Silver:Surface.Paint,center);
   foreach(int side in new[]{-1,1}){
    g.Tube(center+Vector3.right*(side*h*.40f),center+Vector3.right*(side*h*.78f),.20f,Surface.Steel,12);
    for(int bolt=0;bolt<(loader?8:6);bolt++){float a=bolt*Mathf.PI*2/(loader?8:6);var p=center+new Vector3(side*h*.58f,.32f*Mathf.Cos(a),.32f*Mathf.Sin(a));g.Tube(p,p+Vector3.right*side*.055f,.041f,Surface.Silver,6);}
    // Raised sidewall/bead rings keep the inner face readable when orbiting.
    g.Lathe(new[]{new Vector2(side*h*.91f-.018f,.75f),new Vector2(side*h*.91f,.78f),new Vector2(side*h*.91f+.018f,.75f),new Vector2(side*h*.91f,.73f)},24,Surface.Tread,center);
   }
   int rows=variant==1?3:2,steps=loader?(variant==1?22:18):(variant==1?26:20);
   for(int i=0;i<steps;i++)for(int row=0;row<rows;row++){
    float angle=(i+(row%2)*.42f)*360/steps;float x=(row-(rows-1)*.5f)*w/(rows+.4f);
    var rotation=Quaternion.AngleAxis(angle,Vector3.right)*Quaternion.Euler(0,rows==2?(row==0?-1:1)*(loader?18:36):(row%2==0?18:-18),0);
    var pos=center+Quaternion.AngleAxis(angle,Vector3.right)*new Vector3(x,1.355f,0);
    g.Box(pos,new Vector3(w/(rows==2?1.72f:2.9f),variant==1?.10f:.15f,variant==1?.14f:loader?.27f:.22f),Surface.Tread,rotation);
   }
   if(variant==2){
    // Two side chains plus linked cross straps wrapping shoulder, crown, shoulder.
    foreach(int side in new[]{-1,1})g.Lathe(new[]{new Vector2(side*h*1.03f-.025f,1.04f),new Vector2(side*h*1.03f,1.07f),new Vector2(side*h*1.03f+.025f,1.04f),new Vector2(side*h*1.03f,1.01f)},24,Surface.Silver,center);
    int straps=loader?16:12;
    for(int band=0;band<straps;band++){
     float angle=band*Mathf.PI*2/straps;var radial=new Vector3(0,Mathf.Cos(angle),Mathf.Sin(angle));var tangent=new Vector3(0,-Mathf.Sin(angle),Mathf.Cos(angle));
     var points=new[]{center+Vector3.left*h*1.04f+radial*1.04f,center+Vector3.left*h*.7f+radial*1.38f,center+radial*1.45f,center+Vector3.right*h*.7f+radial*1.38f,center+Vector3.right*h*1.04f+radial*1.04f};
     for(int j=0;j<4;j++){var longAxis=(points[j+1]-points[j])*.55f;var shortAxis=(j%2==0?tangent:(tangent+radial*.65f).normalized)*.10f;g.Link((points[j]+points[j+1])*.5f,longAxis,shortAxis,.025f,Surface.Silver);}
    }
   }return g;
  }
  static Geometry Engine(bool loader,bool turbo){
   var g=new Geometry();int cylinders=loader?(turbo?4:3):(turbo?6:4);float length=loader?(turbo?3.0f:2.5f):(turbo?3.2f:2.4f),width=loader?1.42f:1.02f;
   g.Bevel(new Vector3(0,.42f,0),new Vector3(length*.84f,.58f,width*.85f),Surface.Dark);
   g.Bevel(new Vector3(0,.99f,0),new Vector3(length, .90f,width),Surface.Paint,.09f);
   if(loader){
    foreach(int side in new[]{-1,1})for(int i=0;i<cylinders;i++){
     float x=-length*.38f+i*length*.76f/(cylinders-1);var rot=Quaternion.Euler(side*28,0,0);
     g.Bevel(new Vector3(x,1.57f,side*.48f),new Vector3(length/cylinders*.85f,.55f,.62f),Surface.Paint,.07f,rot);
     g.Tube(new Vector3(x,1.55f,side*.74f),new Vector3(x,1.10f,side*.98f),.09f,Surface.Steel);
     g.Tube(new Vector3(x,1.88f,side*.43f),new Vector3(x,1.93f,side*.43f),.045f,Surface.Silver,6);
    }
    g.Bevel(new Vector3(0,1.65f,0),new Vector3(length*.78f,.36f,.44f),Surface.Steel);
   }else{
    g.Bevel(new Vector3(0,1.56f,0),new Vector3(length*.94f,.38f,.83f),Surface.Paint);
    for(int i=0;i<cylinders;i++){float x=-length*.38f+i*length*.76f/(cylinders-1);g.Box(new Vector3(x,1.77f,0),new Vector3(.06f,.06f,.78f),Surface.Silver);g.Tube(new Vector3(x,1.43f,.45f),new Vector3(x,1.03f,.77f),.085f,Surface.Steel);g.Tube(new Vector3(x,1.40f,-.50f),new Vector3(x,1.70f,-.40f),.024f,Surface.Copper,6);}
   }
   float front=-length*.5f-.28f,back=length*.5f+.17f;
   g.Tube(new Vector3(back-.18f,.91f,0),new Vector3(back+.10f,.91f,0),loader?.68f:.54f,Surface.Steel,16);
   g.Tube(new Vector3(back+.10f,.91f,0),new Vector3(back+.18f,.91f,0),.23f,Surface.Silver,12);
   g.Bevel(new Vector3(front,1.10f,0),new Vector3(.21f,1.65f,width+.32f),Surface.Steel);
   for(int i=0;i<10;i++)g.Box(new Vector3(front-.125f,.40f+i*.145f,0),new Vector3(.055f,.045f,width+.14f),Surface.Silver);
   g.Tube(new Vector3(front+.14f,1.05f,0),new Vector3(front+.32f,1.05f,0),.16f,Surface.Paint,10);
   for(int i=0;i<6;i++)g.Box(new Vector3(front+.25f,1.05f,0)+Quaternion.AngleAxis(i*60,Vector3.right)*new Vector3(0,.28f,0),new Vector3(.07f,.42f,.15f),Surface.Dark,Quaternion.AngleAxis(i*60,Vector3.right));
   // Side manifold, alternator, starter, mount feet, fuel filter and oil filler.
   g.Tube(new Vector3(-length*.4f,1.02f,loader?1.0f:.80f),new Vector3(length*.4f,1.02f,loader?1.0f:.80f),.13f,Surface.Steel,8);
   g.Tube(new Vector3(front+.35f,.72f,-width*.55f),new Vector3(front+.75f,.72f,-width*.55f),.22f,Surface.Silver,10);
   g.Tube(new Vector3(length*.30f,.6f,-width*.60f),new Vector3(length*.48f,.6f,-width*.60f),.17f,Surface.Dark,10);
   g.Tube(new Vector3(-.3f,.65f,-width*.65f),new Vector3(-.3f,1.03f,-width*.65f),.13f,Surface.Accent,10);
   g.Tube(new Vector3(length*.22f,1.78f,0),new Vector3(length*.22f,1.93f,0),.10f,Surface.Dark,10);
   for(int side=-1;side<=1;side+=2)foreach(float x in new[]{-length*.32f,length*.32f}){g.Bevel(new Vector3(x,.23f,side*(width*.5f+.12f)),new Vector3(.40f,.16f,.42f),Surface.Steel);g.Tube(new Vector3(x,.31f,side*(width*.5f+.12f)),new Vector3(x,.37f,side*(width*.5f+.12f)),.055f,Surface.Silver,6);}
   if(turbo){
    foreach(int side in loader?new[]{-1,1}:new[]{1}){
     Vector3 c=new Vector3(length*.28f,1.48f,side*(width*.5f+.42f));
     g.Lathe(new[]{new Vector2(-.15f,.12f),new Vector2(-.21f,.25f),new Vector2(-.12f,.37f),new Vector2(.12f,.37f),new Vector2(.21f,.25f),new Vector2(.15f,.12f)},12,Surface.Silver,c);
     g.Tube(c+Vector3.left*.24f,c+Vector3.left*.54f,.15f,Surface.Dark,10);
     g.Tube(c+Vector3.up*.25f,c+Vector3.up*.55f,.12f,Surface.Copper,8);
     g.Tube(c+Vector3.up*.55f,new Vector3(-length*.15f,2.03f,side*(width*.5f+.42f)),.12f,Surface.Copper,8);
    }
    g.Bevel(new Vector3(front-.18f,1.12f,0),new Vector3(.12f,1.13f,width+.1f),Surface.Silver,.025f);
    for(int i=0;i<7;i++)g.Box(new Vector3(front-.26f,.64f+i*.14f,0),new Vector3(.035f,.045f,width),Surface.Dark);
   }
   foreach(int side in loader&&turbo?new[]{-1,1}:new[]{-1}){
    Vector3 a=new Vector3(-length*.12f,loader?2.0f:1.95f,side*(loader?.55f:.72f));g.Tube(a,a+Vector3.up*(loader?.48f:.34f),loader?.25f:.20f,Surface.Dark,12);g.Tube(a+Vector3.up*(loader?.48f:.34f),a+Vector3.up*(loader?.53f:.39f),loader?.28f:.23f,Surface.Silver,12);
    g.Bevel(a-Vector3.up*.045f,new Vector3(.50f,.09f,.48f),Surface.Steel,.025f);
    g.Beam(new Vector3(a.x,1.30f,side*width*.48f),a-Vector3.up*.07f,.09f,.10f,Surface.Steel);
    var elbow=a+Vector3.right*.45f;var inlet=new Vector3(elbow.x,1.42f,side*width*.45f);
    g.Tube(a,elbow,.10f,Surface.Dark,8);g.Tube(elbow,inlet,.10f,Surface.Dark,8);g.Tube(inlet,new Vector3(elbow.x,1.42f,0),.10f,Surface.Dark,8);
   }
   return g;
  }
  static Geometry Blade(bool loader){
   var g=new Geometry();float half=loader?1.8f:2.05f;
   if(loader){
    // Open bucket: floor, back and closed solid cheeks; five separate cutting teeth.
    g.Box(new Vector3(0,.17f,-.25f),new Vector3(3.6f,.16f,1.80f),Surface.Steel,Quaternion.Euler(-8,0,0));
    g.Bevel(new Vector3(0,.88f,.55f),new Vector3(3.6f,1.42f,.16f),Surface.Steel);
    var profile=new[]{new Vector2(.10f,-1.20f),new Vector2(.10f,.66f),new Vector2(1.65f,.66f),new Vector2(1.50f,.36f),new Vector2(.35f,-1.15f)};
    foreach(int side in new[]{-1,1})for(int i=0;i<profile.Length;i++){
     var a=profile[i];var b=profile[(i+1)%profile.Length];var p=new Vector3(side*half,a.x,a.y);var q=new Vector3(side*half,b.x,b.y);
     g.Quad(p,q,q+Vector3.right*side*.14f,p+Vector3.right*side*.14f,new Vector3(0,a.y-b.y,b.x-a.x),Surface.Paint);
    }
    foreach(int side in new[]{-1,1})for(int i=1;i<profile.Length-1;i++)foreach(float x in new[]{side*half,side*(half+.14f)})g.Tri(new Vector3(x,profile[0].x,profile[0].y),new Vector3(x,profile[i].x,profile[i].y),new Vector3(x,profile[i+1].x,profile[i+1].y),Vector3.right*(Mathf.Abs(x)>half?side:-side),Surface.Paint);
    for(int i=0;i<5;i++){float x=-1.48f+i*.74f;g.Bevel(new Vector3(x,.16f,-1.22f),new Vector3(.29f,.23f,.52f),Surface.Silver,.055f,Quaternion.Euler(-8,0,0));g.Tube(new Vector3(x,.27f,-1.1f),new Vector3(x,.30f,-1.1f),.05f,Surface.Dark,6);}
    g.Box(new Vector3(0,1.56f,.49f),new Vector3(3.65f,.15f,.21f),Surface.Paint);
   }else{
    Vector2[] profile={new Vector2(.12f,-.65f),new Vector2(.40f,-.30f),new Vector2(.80f,-.12f),new Vector2(1.20f,-.16f),new Vector2(1.52f,-.30f)};
    for(int column=0;column<3;column++){
     float x0=column==0?-half:column==1?-1.55f:1.55f,x1=column==0?-1.55f:column==1?1.55f:half;
     float z0=column==0?-.25f:0,z1=column==2?-.25f:0;
     for(int i=0;i<profile.Length-1;i++){
      Vector3 a=new Vector3(x0,profile[i].x,profile[i].y+z0),b=new Vector3(x1,profile[i].x,profile[i].y+z1),c=new Vector3(x1,profile[i+1].x,profile[i+1].y+z1),d=new Vector3(x0,profile[i+1].x,profile[i+1].y+z0);
      g.Quad(a,b,c,d,Vector3.back,Surface.Paint);g.Quad(a+Vector3.forward*.13f,b+Vector3.forward*.13f,c+Vector3.forward*.13f,d+Vector3.forward*.13f,Vector3.forward,Surface.Steel);
      g.Quad(a,d,d+Vector3.forward*.13f,a+Vector3.forward*.13f,Vector3.left,Surface.Steel);g.Quad(b,c,c+Vector3.forward*.13f,b+Vector3.forward*.13f,Vector3.right,Surface.Steel);
     }
     foreach(int end in new[]{0,profile.Length-1}){var a=new Vector3(x0,profile[end].x,profile[end].y+z0);var b=new Vector3(x1,profile[end].x,profile[end].y+z1);g.Quad(a,b,b+Vector3.forward*.13f,a+Vector3.forward*.13f,end==0?Vector3.down:Vector3.up,Surface.Steel);}
     g.Box(new Vector3((x0+x1)*.5f,.12f,-.68f+(z0+z1)*.5f),new Vector3(x1-x0,.16f,.18f),Surface.Dark);
    }
    for(int i=0;i<9;i++){float x=-1.9f+i*.475f;g.Tube(new Vector3(x,.17f,-.81f),new Vector3(x,.17f,-.85f),.045f,Surface.Silver,6);}
    g.Box(new Vector3(0,1.53f,-.25f),new Vector3(4.12f,.11f,.17f),Surface.Dark);
    foreach(int side in new[]{-1,1}){g.Box(new Vector3(side*1.86f,1.13f,-.36f),new Vector3(.22f,.56f,.07f),Surface.Accent);for(int stripe=0;stripe<3;stripe++)g.Box(new Vector3(side*1.86f,.94f+stripe*.17f,-.405f),new Vector3(.24f,.065f,.025f),Surface.Dark,Quaternion.Euler(0,0,side*25));}
   }
   // Rear reinforcement and recognisable mounting ears, visible from an orbit.
   for(int i=0;i<4;i++){float x=-1.35f+i*.9f;if(loader)g.Beam(new Vector3(x,.25f,.74f),new Vector3(x,1.42f,.74f),.12f,.13f,Surface.Dark);else{g.Beam(new Vector3(x,.25f,-.25f),new Vector3(x,.80f,.12f),.12f,.13f,Surface.Dark);g.Beam(new Vector3(x,.80f,.12f),new Vector3(x,1.42f,-.05f),.12f,.13f,Surface.Dark);}}
   g.Box(new Vector3(0,.58f,loader?.82f:.04f),new Vector3(2.9f,.20f,.24f),Surface.Dark);
   if(loader)foreach(int side in new[]{-1,1}){var center=new Vector3(side*.64f,.85f,.93f);g.Bevel(center,new Vector3(.15f,.68f,.47f),Surface.Steel);g.Tube(center+Vector3.left*.12f,center+Vector3.right*.12f,.11f,Surface.Silver,10);}
   return g;
  }
  static Mesh SaveMesh(Geometry geometry,string path,string name){
   var mesh=geometry.Mesh(name);var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!saved){AssetDatabase.CreateAsset(mesh,path);return mesh;}
   saved.Clear();saved.indexFormat=IndexFormat.UInt16;saved.vertices=mesh.vertices;saved.normals=mesh.normals;saved.subMeshCount=mesh.subMeshCount;for(int i=0;i<mesh.subMeshCount;i++)saved.SetTriangles(mesh.GetTriangles(i),i);saved.RecalculateBounds();saved.UploadMeshData(false);EditorUtility.SetDirty(saved);UnityEngine.Object.DestroyImmediate(mesh);return saved;
  }
  static void AddOriginalHydraulics(Transform parent){
   string path=Folder+"/TractorDefaultHydraulics.fbx";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new InvalidOperationException("Export original tractor hydraulics first");
   var assembly=UnityEngine.Object.Instantiate(source,parent);assembly.name="Original tractor lift hydraulics";assembly.transform.localPosition=new Vector3(0,.09f,-2.35f);
   foreach(var t in assembly.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
   foreach(var r in assembly.GetComponentsInChildren<MeshRenderer>(true)){var materials=r.sharedMaterials;for(int i=0;i<materials.Length;i++){int code;if(!materials[i]||!int.TryParse(materials[i].name.Substring(0,2),out code))throw new InvalidOperationException("Unknown original hydraulic palette");materials[i]=AssetDatabase.LoadAssetAtPath<Material>("Assets/Snowglow/Catalog/Sources/LowPolySnowPlower/Materials/Color"+code.ToString("00")+".mat");if(!materials[i])throw new InvalidOperationException("Missing original hydraulic material");}r.sharedMaterials=materials;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
  }
  public static void RefineLoaderMounts(GameObject model){
   var node=model.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name=="Rear mounting lugs");var g=new Geometry();
   foreach(int side in new[]{-1,1}){var center=new Vector3(side*.48f,0,.065f);g.Bevel(center,new Vector3(.15f,.24f,.22f),Surface.Steel,.045f);g.Tube(center+new Vector3(-.11f,.035f,-.02f),center+new Vector3(.11f,.035f,-.02f),.062f,Surface.Silver,10);g.Bevel(center+new Vector3(0,-.08f,.055f),new Vector3(.20f,.08f,.13f),Surface.Steel,.025f);}
   node.sharedMesh=SaveMesh(g,Folder+"/LoaderFactoryMountLugs.asset","Shaped factory bucket mounting lugs");node.GetComponent<MeshRenderer>().sharedMaterials=Materials(true,5);
  }
  public static string Key(bool loader,int index)=>(loader?"loader-":"tractor-")+MachineryParts.At(loader,index).key;
  public static GameObject Get(bool loader,int index){return AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/"+Key(loader,index)+".prefab");}
  public static void Prepare(){
   Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var library=CatalogPreparation.Library();
   foreach(bool loader in new[]{false,true})foreach(int index in new[]{0,1,2,3,4,6}){
    var part=MachineryParts.At(loader,index);string key=Key(loader,index),path=Folder+"/"+key;Geometry geometry=part.kind==MachineryPartKind.Tyres?Tyre(loader,index==0?0:index==2?1:2):part.kind==MachineryPartKind.Engine?Engine(loader,index==4):Blade(loader);
    if(part.kind==MachineryPartKind.Blade)for(int v=0;v<geometry.vertices.Count;v++)geometry.vertices[v]=Quaternion.Euler(0,180,0)*geometry.vertices[v];
    Mesh mesh=SaveMesh(geometry,path+".asset",key);
    var root=new GameObject(part.name);root.layer=Layer;var filter=root.AddComponent<MeshFilter>();filter.sharedMesh=mesh;var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterials=Materials(loader,index);renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    try{
     if(!loader&&index==6)AddOriginalHydraulics(root.transform);
     var prefab=PrefabUtility.SaveAsPrefabAsset(root,path+".prefab");string id="machinery-shop-"+key;var entry=library.Find(id);if(!entry){entry=ScriptableObject.CreateInstance<CatalogAsset>();entry.id=id;AssetDatabase.CreateAsset(entry,CatalogPreparation.Root+"/Entries/"+id+".asset");library.entries.Add(entry);}
     entry.displayName=part.name;entry.prefab=prefab;entry.kind=part.kind==MachineryPartKind.Tyres?CatalogKind.WheelSet:part.kind==MachineryPartKind.Engine?CatalogKind.Accessory:CatalogKind.Attachment;entry.status=PreparationStatus.NeedsReview;entry.sourceType=AssetSource.Prompt;entry.sourcePath=entry.creationSourcePath="Assets/Snowglow/Editor/MachineryPartModels.cs";entry.sourceGuid=AssetDatabase.AssetPathToGUID(entry.sourcePath);entry.provenance="User-directed low-poly Shop models, deterministically authored by the retained Editor generator. Factory blade models preserved.";entry.assumptions="Shop presentation asset. Physical rig, source machinery and performance values unchanged. Requires pair-specific fitting before any gameplay visual replacement.";entry.reviewNotes="Inspect both tyre faces, chained tread, engine components and both attachment sides. Not a Ready gameplay assembly.";entry.price=part.price;entry.blocksSnow=false;CatalogPreparation.RecordMetrics(entry);EditorUtility.SetDirty(entry);
    }finally{UnityEngine.Object.DestroyImmediate(root);}
   }
   EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
  }
 }
}
