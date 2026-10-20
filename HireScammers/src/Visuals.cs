using System;
using System.Linq;
using UnityEngine;
namespace HireScammers {
public static class Visuals {
 public static Transform FindModel(PlayerName owner){
  if(!owner)return null;
  foreach(var sitting in owner.GetComponentsInChildren<SitAnimator>(true)){
   var model=Plugin.Get(sitting,"modelTransform") as GameObject;if(model&&model.GetComponentInChildren<SkinnedMeshRenderer>(true))return model.transform;
  }
  var anim=owner.GetComponentsInChildren<Animator>(true).OrderByDescending(a=>a.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length).FirstOrDefault();
  return anim&&anim.GetComponentInChildren<SkinnedMeshRenderer>(true)?anim.transform:null;
 }
 public static Transform FindNpcModel(NpcInteractor npc){
  var nativeRig=npc.GetComponentInChildren<FIMSpace.FProceduralAnimation.LegsAnimator>(true);if(nativeRig)return nativeRig.transform;
  foreach(var sitting in npc.GetComponentsInChildren<SitAnimator>(true)){
   var model=Plugin.Get(sitting,"modelTransform") as GameObject;if(model&&model.GetComponentInChildren<SkinnedMeshRenderer>(true))return model.transform;
  }
  var anim=npc.GetComponentsInChildren<Animator>(true).OrderByDescending(a=>a.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length).FirstOrDefault();
  return anim?anim.transform:null;
 }
 public static GameObject Clone(PlayerName owner,Transform parent){return CloneModel(FindModel(owner),parent);}
 public static GameObject CloneModel(Transform source,Transform parent){
  if(!source)throw new InvalidOperationException("No character model found.");
  var sourceRenderers=source.GetComponentsInChildren<Renderer>(true);
  bool wasActive=source.gameObject.activeSelf;GameObject copy;
  // Instantiate while inactive, strip behaviours before activation, and retain
  // serialized avatar/controller/bone references that Unity's stripped API cannot set.
  source.gameObject.SetActive(false);
  try {copy=UnityEngine.Object.Instantiate(source.gameObject,parent,false);}finally{source.gameObject.SetActive(wasActive);}
  try {
   foreach(var sit in copy.GetComponentsInChildren<SitAnimator>(true)){var model=Plugin.Get(sit,"modelTransform") as GameObject;if(model&&(bool)Plugin.Get(sit,"hasBaseModelLocalPosition"))model.transform.localPosition=(Vector3)Plugin.Get(sit,"baseModelLocalPosition");sit.Sit(false);}
   foreach(var script in copy.GetComponentsInChildren<MonoBehaviour>(true))if(!NativeEmployeeMotion.Keep(script))UnityEngine.Object.DestroyImmediate(script);
   foreach(var joint in copy.GetComponentsInChildren<Joint>(true))UnityEngine.Object.DestroyImmediate(joint);
   foreach(var c in copy.GetComponentsInChildren<Component>(true))
    if(c&&!(c is Transform)&&!(c is Animator)&&!(c is Renderer)&&!(c is MeshFilter)&&!(c is MonoBehaviour&&NativeEmployeeMotion.Keep((MonoBehaviour)c)))UnityEngine.Object.DestroyImmediate(c);
   foreach(var animator in copy.GetComponentsInChildren<Animator>(true)){animator.enabled=true;animator.speed=1;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;}
   foreach(var renderer in copy.GetComponentsInChildren<Renderer>(true)){renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;var skin=renderer as SkinnedMeshRenderer;if(skin)skin.updateWhenOffscreen=true;}
   var resources=copy.AddComponent<AppearanceResources>();var instances=new System.Collections.Generic.Dictionary<Material,Material>();
   var renderers=copy.GetComponentsInChildren<Renderer>(true);
   for(int i=0;i<renderers.Length;i++){
    var renderer=renderers[i];var materials=renderer.sharedMaterials;
    for(int j=0;j<materials.Length;j++)if(materials[j]){
     Material instance;if(!instances.TryGetValue(materials[j],out instance)){instance=new Material(materials[j]);instances[materials[j]]=instance;resources.Materials.Add(instance);}materials[j]=instance;
    }
    renderer.sharedMaterials=materials;
    if(i<sourceRenderers.Length){
     renderer.enabled=sourceRenderers[i].enabled;
     var block=new MaterialPropertyBlock();sourceRenderers[i].GetPropertyBlock(block);renderer.SetPropertyBlock(block);
     for(int j=0;j<materials.Length;j++){var perMaterial=new MaterialPropertyBlock();sourceRenderers[i].GetPropertyBlock(perMaterial,j);renderer.SetPropertyBlock(perMaterial,j);}
    }
   }
   NativeEmployeeMotion.Configure(copy,parent);copy.name="Hired office character model";copy.SetActive(true);return copy;
  }catch{UnityEngine.Object.Destroy(copy);throw;}
 }
}
}




