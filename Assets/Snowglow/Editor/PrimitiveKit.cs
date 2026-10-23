using UnityEngine;
namespace Snowglow { public static class PrimitiveKit {
 public static Transform Group(string name,Transform parent,Vector3 at) {
  var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;return t;
 }
 public static GameObject Part(string name,PrimitiveType type,Transform parent,Vector3 at,Vector3 size,Material mat,bool collider=false) {
  var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=at;g.transform.localScale=size;
  g.GetComponent<Renderer>().sharedMaterial=mat;
  if(!collider){var c=g.GetComponent<Collider>();c.enabled=false;if(Application.isPlaying)Object.Destroy(c);else Object.DestroyImmediate(c);}return g;
 }
 public static GameObject Cube(string name,Transform parent,Vector3 at,Vector3 size,Material mat,bool collider=false) {return Part(name,PrimitiveType.Cube,parent,at,size,mat,collider);}
 public static void Beam(string name,Transform parent,Vector3 a,Vector3 b,float r,Material mat) {
  var g=Part(name,PrimitiveType.Cylinder,parent,(a+b)*.5f,new Vector3(r,(b-a).magnitude*.5f,r),mat);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
 }
}}
