using UnityEngine;
namespace HireScammers {
public sealed partial class Bot {
 string fittedName;Transform headBone;
 internal void UpdateNameTagPlacement(){
  if(!nameTag||!tagRoot||!visual)return;
  if(!headBone){foreach(var bone in visual.GetComponentsInChildren<Transform>(true))if(bone.name=="mixamorig:Head"){headBone=bone;break;}}
  var camera=Camera.main;var top=headBone?headBone.position:transform.position+Vector3.up*(sitting?1.25f:1.65f);
  tagRoot.transform.position=top+Vector3.up*.35f+(camera?camera.transform.up:Vector3.up)*.5f;
  if(fittedName==DisplayName)return;var text=nameTag.GetComponentInChildren<TMPro.TMP_Text>(true);if(!text)return;
  text.ForceMeshUpdate(true,true);var size=text.textBounds.size;if(size.x<=0||size.y<=0||float.IsNaN(size.x)||float.IsInfinity(size.x))return;float scale=Mathf.Min(1.65f/size.x,.23f/size.y);nameTag.transform.localScale=Vector3.one*scale;fittedName=DisplayName;
 }
}
}
