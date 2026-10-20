using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
namespace HireScammers {
internal static class FloorMotion {
 internal const float Radius=.3f;
 static readonly Collider[] overlaps=new Collider[64];
 internal static string LastObstacle;
 static bool Obstacle(Collider collider){
  if(!collider||collider.GetComponentInParent<PlayerName>()||collider.GetComponentInParent<NpcInteractor>()||collider.GetComponentInParent<Bot>())return false;
  var size=collider.bounds.size;
  if(collider.attachedRigidbody&&size.y<.45f&&size.x<.7f&&size.z<.7f)return false;
  LastObstacle=collider.name+" size="+size+" at="+collider.bounds.center;return true;
 }
 internal static bool OfficeSpawn(Vector3 reference,int day,bool sandbox,out Vector3 position){
  position=reference;var approaches=new List<Vector3>();
  foreach(var desk in ScammerWorkspace.Instances){
   if(!desk||!desk.SupportsPhoneCalls||!desk.IsOnWorkdayFloor(day,sandbox))continue;
   var chair=desk.chairInteractable;var exit=chair?Plugin.Get(chair,"exitPoint") as Transform:null;Vector3 point;
   if(TryWalkable(exit?exit.position:desk.transform.position-desk.transform.forward,1f,out point))approaches.Add(point);
  }
  foreach(var start in approaches.OrderBy(p=>(p-reference).sqrMagnitude)){
   foreach(var end in approaches){
    if(Mathf.Abs(start.y-end.y)>.5f||HorizontalDistanceSquared(start,end)<9)continue;
    var path=new NavMeshPath();if(!NavMesh.CalculatePath(start,end,-1,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
    position=start;return true;
   }
  }
  return false;
 }
 internal static bool Clear(Vector3 feet){
  for(float height=.34f;height<1.7f;height+=.4f){
   var center=feet+Vector3.up*height;int count=Physics.OverlapSphereNonAlloc(center,Radius,overlaps,-5,QueryTriggerInteraction.Ignore);
   for(int i=0;i<count;i++)if(Obstacle(overlaps[i]))return false;
   if(count==overlaps.Length)foreach(var collider in Physics.OverlapSphere(center,Radius,-5,QueryTriggerInteraction.Ignore))if(Obstacle(collider))return false;
  }
  return true;
 }
 internal static bool ClearSweep(Vector3 from,Vector3 to){
  int steps=Mathf.Max(1,Mathf.CeilToInt((to-from).magnitude/.08f));
  for(int i=1;i<=steps;i++)if(!Clear(Vector3.Lerp(from,to,(float)i/steps)))return false;
  return true;
 }
 internal static bool TryWalkable(Vector3 position,float radius,out Vector3 grounded){
  grounded=position;NavMeshHit nav;Vector3 floor;
  // Native player spawn/exit markers are capsule centers, over two units above
  // the floor. Radius is horizontal clearance, not a limit on that marker height.
  return TryGround(position,out floor)&&NavMesh.SamplePosition(floor,out nav,radius,-1)&&Mathf.Abs(nav.position.y-floor.y)<.6f&&TryGround(nav.position,out grounded)&&Clear(grounded);
 }
 internal static bool Recover(Vector3 position,out Vector3 safe){
  safe=position;float best=1e20f;
  // A sampled navmesh point can still overlap a moved prop. Check body clearance too.
  for(int ring=0;ring<=5;ring++)for(int direction=0;direction<(ring==0?1:16);direction++){
   float angle=direction*3.14159265f/8;var probe=position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*ring*.4f;Vector3 candidate;
   if(!TryWalkable(probe,.65f,out candidate))continue;
   float distance=(candidate-position).sqrMagnitude;if(distance>=best)continue;safe=candidate;best=distance;
  }
  return best<1e20f;
 }
 // Positions represent the floor under the shoes, rather than a player's capsule center.
 internal static float HorizontalDistanceSquared(Vector3 a,Vector3 b){a.y=b.y;return (a-b).sqrMagnitude;}
 internal static bool TryGround(Vector3 position,out Vector3 grounded){
  grounded=position;NavMeshHit nav;
  bool onMesh=NavMesh.SamplePosition(position,out nav,3,-1)&&Mathf.Abs(nav.position.y-position.y)<3;
  float reference=onMesh?nav.position.y:position.y;
  var hits=Physics.RaycastAll(new Vector3(position.x,reference+.6f,position.z),Vector3.down,3.1f,-5,QueryTriggerInteraction.Ignore);
  float best=1e20f;
  foreach(var hit in hits){
   if(hit.normal.y<.65f||hit.collider.GetComponentInParent<PlayerName>()||hit.collider.GetComponentInParent<NpcInteractor>()||hit.collider.GetComponentInParent<Bot>())continue;
   float difference=Mathf.Abs(hit.point.y-reference);
   if(onMesh&&difference>.45f)continue;
   if(difference>=best)continue;best=difference;grounded.y=hit.point.y;
  }
  return best<1e20f;
 }
 internal static void StandModel(GameObject model){
  foreach(var animator in model.GetComponentsInChildren<Animator>(true)){
   animator.Rebind();
   foreach(var parameter in animator.parameters)if(parameter.type==AnimatorControllerParameterType.Bool&&(parameter.name=="isSitting"||parameter.name=="isWalking"))animator.SetBool(parameter.nameHash,false);
   animator.Update(0);
  }
  // Renderer bounds include animation padding; use the actual skinned vertices.
  float sole=LowestVertex(model,model.transform.parent);
  if(sole<1e20f){var local=model.transform.localPosition;local.y-=sole;model.transform.localPosition=local;}
 }
 internal static float LowestVertex(GameObject model,Transform relativeTo){
  float lowest=1e20f;var mesh=new Mesh();
  try{foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
   if(!renderer.enabled||!renderer.gameObject.activeInHierarchy||!renderer.sharedMesh)continue;
   // The imported skin already includes its bone scaling. Apply only rotation and translation. These
   // imported meshes have scales as large as 55; TransformPoint would scale twice.
   renderer.BakeMesh(mesh,false);
   foreach(var vertex in mesh.vertices){var point=renderer.transform.rotation*vertex+renderer.transform.position;if(relativeTo)point=relativeTo.InverseTransformPoint(point);lowest=Mathf.Min(lowest,point.y);}
  }}finally{UnityEngine.Object.Destroy(mesh);}
  return lowest;
 }
}
}



