using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using UnityEngine;
namespace HireScammers {
internal static class VisualDiagnostics {
 internal static string Failure;internal static int Checks;
 internal static IEnumerator Observe(){
  Failure=null;Checks=0;var owner=PlayerName.LocalPlayerInstance;
  var recruits=new System.Collections.Generic.List<GameObject>();try{
   for(int n=0;n<3;n++){var actor=Runtime.Proxy();recruits.Add(actor);actor.transform.SetParent(owner.transform,true);var bot=actor.GetComponent<Bot>();bot.Owner=owner;bot.RecruitName="Nametag check "+n;bot.ModelId=OfficeCharacters.Choose(n);bot.EnsureVisual();}
   yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
   try{foreach(var actor in recruits){var bot=actor.GetComponent<Bot>();var tag=actor.GetComponentInChildren<PlayerNameText>(true);var text=tag?tag.GetComponent<TMPro.TMP_Text>():null;if(!tag||!tag.enabled||!text||!text.enabled||!text.gameObject.activeInHierarchy||text.text!=bot.DisplayName)throw new Exception("One of three simultaneous employees lost its independent nametag");foreach(var arm in actor.GetComponentsInChildren<ProceduralArmAnimator>(true))if(Plugin.Get(arm,"playerController")!=null)throw new Exception("Employee arm driver still follows owner controller");foreach(var step in actor.GetComponentsInChildren<SmugglerStepArmAnimator>(true))if(Plugin.Get(step,"playerController")!=null)throw new Exception("Employee step driver still follows owner controller");if(actor.GetComponentsInChildren<PlayerJumpPoseAnimator>(true).Any(j=>j.enabled))throw new Exception("Employee jump driver still follows owner ragdoll");Checks+=2;}Debug.Log("[HireScammers] INDEPENDENCE PASS: three concurrent nametags and detached player animation drivers.");}catch(Exception error){Failure=error.ToString();}
  }finally{foreach(var actor in recruits)UnityEngine.Object.Destroy(actor);}if(Failure!=null)yield break;
  foreach(var appearance in OfficeCharacters.Choices){
   var actor=Runtime.Proxy();var bot=actor.GetComponent<Bot>();bot.Owner=owner;bot.ModelId=appearance.Id;
   actor.transform.SetParent(owner.transform,true);bot.Position=ScammerGameManager.Instance.GetDayStartPoint(1,false).position;bot.PlaceOnWorkFloor(1);bot.EnsureVisual();
   var model=actor.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Hired office character model").gameObject;
   var bodyRig=model.GetComponentsInChildren<Animator>(true).First(a=>a.parameters.Any(p=>p.name=="isWalking"));var leg=bodyRig.GetComponentsInChildren<Transform>(true).First(t=>t.name.EndsWith("LeftUpLeg",StringComparison.Ordinal));Quaternion walkPose=leg.localRotation,runPose=leg.localRotation;float walkChange=0,runChange=0;
   for(int i=0;i<200;i++){
    yield return new WaitForEndOfFrame();
    try{
     if(i==1)walkPose=leg.localRotation;if(i>1&&i<90)walkChange=Mathf.Max(walkChange,Quaternion.Angle(walkPose,leg.localRotation));if(i==91)runPose=leg.localRotation;if(i>91&&i<180)runChange=Mathf.Max(runChange,Quaternion.Angle(runPose,leg.localRotation));
     AccessTools.Method(typeof(Bot),"Animate").Invoke(bot,new object[]{i<90?1.45f:i<180?4.5f:0f});
     if(i<180)AccessTools.Method(typeof(Bot),"Step").Invoke(bot,new object[]{bot.Position+Vector3.forward*3,i<90?1.45f:4.5f});
     var body=model.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(r=>r.enabled&&r.gameObject.activeInHierarchy&&(r.name=="IndianGuyBody"||r.name=="IndianGirlBody"));
     if(!body)throw new Exception("Missing active body mesh for "+appearance.Name);
     var min=body.bounds.min-actor.transform.position;var max=body.bounds.max-actor.transform.position;
     if(min.y<-.2f||min.y>.2f||max.y<1||max.y>2.8f||Mathf.Abs(body.bounds.center.x-actor.transform.position.x)>.8f||Mathf.Abs(body.bounds.center.z-actor.transform.position.z)>.8f)
      throw new Exception("Rendered body displaced from recruit: "+appearance.Name+" min="+min+" max="+max+" modelLocal="+model.transform.localPosition);
     if(Checks==0&&(i==45||i==135))Preview(model,actor.transform.position,i==45?"-walk":"-run");if(i==199)Debug.Log("[HireScammers] VISIBLE PASS: "+appearance.Name+" rendered body min="+min+" max="+max+" after 200 rendered frames; walk="+walkChange+" run="+runChange+"");
    }catch(Exception error){Failure=error.ToString();break;}
   }
   if(Failure==null&&(walkChange<18||runChange<18))Failure="No visible skeletal locomotion: walk="+walkChange+" run="+runChange;
   if(Checks==0&&Failure==null)Preview(model,actor.transform.position);
   UnityEngine.Object.Destroy(actor);if(Failure!=null)break;Checks+=3;
  }
 }
 static void Preview(GameObject model,Vector3 origin,string suffix=""){
  var cameraObject=new GameObject("Recruit diagnostic camera");var lightObject=new GameObject("Recruit diagnostic light");var target=new RenderTexture(512,768,24);var texture=new Texture2D(512,768,TextureFormat.RGB24,false);var previous=RenderTexture.active;
  try{
   foreach(var child in model.GetComponentsInChildren<Transform>(true))child.gameObject.layer=30;
   var camera=cameraObject.AddComponent<Camera>();camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.18f,.22f);camera.fieldOfView=40;camera.nearClipPlane=.1f;camera.farClipPlane=20;
   camera.transform.position=origin+new Vector3(2.8f,1.3f,3.2f);camera.transform.rotation=Quaternion.LookRotation(origin+Vector3.up-camera.transform.position);camera.targetTexture=target;
   var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(35,-35,0);
   camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,512,768),0,0);
   System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../HireScammers/visibility-preview"+suffix+".png"),ImageConversion.EncodeToPNG(texture));
  }catch(Exception error){Debug.LogWarning("[HireScammers] Preview capture unavailable: "+error.Message);}
  finally{RenderTexture.active=previous;UnityEngine.Object.Destroy(cameraObject);UnityEngine.Object.Destroy(lightObject);UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(texture);}
 }
}
}


















