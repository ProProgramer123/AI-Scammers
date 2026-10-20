using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;
namespace HireScammers {
internal static class FloorDiagnostics {
 internal static int Run(Bot bot){
  int checks=0;var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.SetActive(false);var parent=new GameObject("Grounding diagnostic parent");
  var originalParent=bot.transform.parent;var originalPosition=bot.transform.position;var originalRotation=bot.transform.rotation;
  try{
   foreach(int day in new[]{1,6,11}){
    var g=ScammerGameManager.Instance;var spawn=g.GetDayStartPoint(day,false);Vector3 safe;
    if(!spawn)throw new Exception("Missing native day spawn "+day);bot.Position=spawn.position;bot.transform.position=bot.Position;
    if(!bot.PlaceOnWorkFloor(day))throw new Exception("Native day "+day+" capsule-height spawn cannot reach connected office navigation");safe=bot.Position;checks++;
    bool moved=false;int attempts=0;
    foreach(var workspace in ScammerWorkspace.Instances){
     if(!workspace||!workspace.SupportsPhoneCalls||!workspace.IsOnSameFloor(safe)||!workspace.IsOnWorkdayFloor(day,false))continue;
     var chair=workspace.chairInteractable;var exit=chair?Plugin.Get(chair,"exitPoint") as Transform:null;if(!exit)continue;
     bot.Position=safe;bot.transform.position=safe;
     if(!(bool)AccessTools.Method(typeof(Bot),"BuildPath").Invoke(bot,new object[]{exit.position})){Debug.Log("[HireScammers] SPAWN route rejected: "+bot.LastBlockReason);continue;}
     attempts++;
     for(int i=0;i<400&&(bot.Position-safe).sqrMagnitude<4;i++)AccessTools.Method(typeof(Bot),"Move").Invoke(bot,new object[]{exit.position,1.45f});
     if((bot.Position-safe).sqrMagnitude>=4){moved=true;Debug.Log("[HireScammers] SPAWN WALK PASS day="+day+" spawn="+spawn.position+" grounded="+safe+" movedTo="+bot.Position+" chairExit="+exit.position);break;}
     if(attempts>=5)break;
    }
    if(!moved)throw new Exception("No movement from native day "+day+" spawn to a real capsule-height chair exit; attempts="+attempts+" reason="+bot.LastBlockReason);checks++;
   }
   floor.transform.position=new Vector3(10000,99.5f,10000);floor.transform.localScale=new Vector3(20,1,20);Physics.SyncTransforms();
   Vector3 grounded;var origin=new Vector3(10000,100,10000);
   if(!FloorMotion.TryGround(origin+Vector3.up*.9f,out grounded)||Mathf.Abs(grounded.y-100)>.01f)throw new Exception("Elevated spawn did not settle on floor");checks++;
   if(FloorMotion.TryGround(new Vector3(11000,100,11000),out grounded))throw new Exception("Missing floor accepted");checks++;
   if(FloorMotion.HorizontalDistanceSquared(origin,origin+Vector3.up*1.5f)>.001f)throw new Exception("Capsule-center height prevents desk arrival");checks++;
   bot.transform.SetParent(parent.transform,true);bot.Position=origin+Vector3.up*.9f;bot.transform.position=bot.Position;
   AccessTools.Method(typeof(Bot),"Step").Invoke(bot,new object[]{origin+Vector3.right*3,1.45f});
   if(Mathf.Abs(bot.Position.y-100)>.01f||bot.Position.x<=origin.x)throw new Exception("Elevated recruit could not ground and walk");checks++;
   var animator=bot.GetComponentsInChildren<Animator>(true).First(a=>a.parameters.Any(p=>p.name=="isWalking"));int walking=Animator.StringToHash("isWalking");
   Debug.Log("[HireScammers] Grounding animator parameters: "+string.Join(",",animator.parameters.Select(p=>p.name).ToArray()));
   if(!animator.parameters.Any(p=>p.name=="isWalking")||!animator.GetBool(walking))throw new Exception("Native walk animation was not enabled");checks++;
   bot.AnchorPose();var pose=bot.transform.position;var rotation=bot.transform.rotation;parent.transform.position+=Vector3.up*3;parent.transform.rotation=Quaternion.Euler(0,80,0);bot.RestorePose();
   if((bot.transform.position-pose).sqrMagnitude>.0001f||Quaternion.Angle(bot.transform.rotation,rotation)>.01f)throw new Exception("Owner movement drags recruit pose");checks++;
   AccessTools.Method(typeof(Bot),"Animate").Invoke(bot,new object[]{0f});if(animator.GetBool(walking))throw new Exception("Stopped recruit retained walk animation");checks++;
   bot.Position=origin;bot.transform.position=origin;
   if((bool)AccessTools.Method(typeof(Bot),"BuildPath").Invoke(bot,new object[]{origin+Vector3.right*3}))throw new Exception("Accepted a route without a navmesh");
   AccessTools.Method(typeof(Bot),"Move").Invoke(bot,new object[]{origin+Vector3.right*3,4.5f});
   if((bot.Position-origin).sqrMagnitude>.0001f)throw new Exception("Failed route fell back to walking straight through obstacles");checks++;
   wall.transform.position=origin+new Vector3(.6f,1,0);wall.transform.localScale=new Vector3(.12f,2,4);wall.SetActive(true);Physics.SyncTransforms();
   for(int i=0;i<50;i++)AccessTools.Method(typeof(Bot),"Step").Invoke(bot,new object[]{origin+Vector3.right*3,4.5f});
   if(bot.Position.x>origin.x+.28f||!FloorMotion.Clear(bot.Position))throw new Exception("Recruit passed through a thin wall");checks++;
   bot.Position=origin+Vector3.right*.6f;bot.transform.position=bot.Position;
   if(FloorMotion.Clear(bot.Position))throw new Exception("Existing wall overlap went undetected");
   var embedded=bot.Position;AccessTools.Method(typeof(Bot),"Step").Invoke(bot,new object[]{origin+Vector3.right*3,4.5f});
   if((bot.Position-embedded).sqrMagnitude>.0001f)throw new Exception("Embedded recruit moved deeper into wall");checks++;
   wall.transform.position=origin+new Vector3(.5f,.35f,0);wall.transform.localScale=new Vector3(.12f,.25f,4);Physics.SyncTransforms();bot.Position=origin;bot.transform.position=origin;
   for(int i=0;i<50;i++)AccessTools.Method(typeof(Bot),"Step").Invoke(bot,new object[]{origin+Vector3.right*3,4.5f});
   if(bot.Position.x>origin.x+.18f)throw new Exception("Recruit passed through low furniture");checks++;
   wall.SetActive(false);Physics.SyncTransforms();
   Vector3 recovered;var desk=ScammerWorkspace.Instances.FirstOrDefault(d=>d&&FloorMotion.Recover(d.transform.position,out recovered));
   if(!desk||!FloorMotion.Recover(desk.transform.position,out recovered)||!FloorMotion.Clear(recovered))throw new Exception("Could not recover to clear office navmesh");checks++;
   var positions=new System.Collections.Generic.List<Vector3>();
   foreach(var workspace in ScammerWorkspace.Instances){if(!workspace)continue;var chair=workspace.chairInteractable;var exit=chair?Plugin.Get(chair,"exitPoint") as Transform:null;Vector3 point;
    if(FloorMotion.TryWalkable(exit?exit.position:workspace.transform.position,1f,out point))positions.Add(point);
   }
   bool completed=false;int routes=0;
   foreach(var start in positions){foreach(var end in positions){
    if(Mathf.Abs(start.y-end.y)>.5f||FloorMotion.HorizontalDistanceSquared(start,end)<36)continue;
    var route=new NavMeshPath();if(!NavMesh.CalculatePath(start,end,-1,route)||route.status!=NavMeshPathStatus.PathComplete||route.corners.Length<3)continue;
    bot.Position=start;bot.transform.position=start;
    if(!(bool)AccessTools.Method(typeof(Bot),"BuildPath").Invoke(bot,new object[]{end}))continue;
    routes++;
    for(int i=0;i<1500&&FloorMotion.HorizontalDistanceSquared(bot.Position,end)>.09f;i++)AccessTools.Method(typeof(Bot),"Move").Invoke(bot,new object[]{end,4.5f});
    if(FloorMotion.HorizontalDistanceSquared(bot.Position,end)<.09f&&FloorMotion.Clear(bot.Position)){completed=true;Debug.Log("[HireScammers] OFFICE ROUTE PASS: "+route.corners.Length+" corners from "+start+" to "+end);break;}
    Debug.Log("[HireScammers] OFFICE ROUTE blocked safely at "+bot.Position+"; goal="+end+" reason="+bot.LastBlockReason+" corners="+string.Join(";",route.corners.Select(v=>v.ToString()).ToArray()));
    if(routes>=5)break;
   }if(completed||routes>=5)break;}
   if(!completed)throw new Exception("Could not walk any multi-corner native office route (attempts="+routes+")");checks++;
   Debug.Log("[HireScammers] FLOOR PASS: elevated spawn recovery, safe floor rejection, horizontal arrival, real movement, native walking/idle and independent owner pose.");
   Debug.Log("[HireScammers] WALL PASS: no failed-route fallback, thin-wall sweep, embedded-body rejection, low-furniture collision and clear office recovery.");
  }finally{bot.transform.SetParent(originalParent,true);bot.transform.position=originalPosition;bot.transform.rotation=originalRotation;bot.Position=originalPosition;bot.AnchorPose();UnityEngine.Object.Destroy(floor);UnityEngine.Object.Destroy(wall);UnityEngine.Object.Destroy(parent);}
  return checks;
 }
}
}

