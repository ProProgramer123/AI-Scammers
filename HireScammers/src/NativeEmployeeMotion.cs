using System;
using System.Linq;
using FIMSpace.FProceduralAnimation;
using UnityEngine;
using HarmonyLib;
namespace HireScammers {
[DefaultExecutionOrder(20000)]
public sealed class NativeEmployeeMotion : MonoBehaviour {
 LegsAnimator legs;SitAnimator[] seats;SmugglerStepArmAnimator steps;MonitorArmIkDriver hands;Bot bot;bool seated;float stride;Vector3 previousPosition;bool trackedPosition;float gait;Vector3[] ankleHomes;
 Quaternion leftPalmFrame,rightPalmFrame;
 internal static bool Keep(MonoBehaviour component){return component is LegsAnimator||component is ProceduralArmAnimator||component is SmugglerStepArmAnimator||component is AnimationHandler||component is SitAnimator||component is PlayerJumpPoseAnimator||component is AnimFunctionCaller;}
 internal static void Configure(GameObject root,Transform motionRoot){
  var owner=motionRoot?motionRoot.GetComponentInParent<PlayerName>():null;var source=owner?owner.GetComponentsInChildren<Animator>(true).FirstOrDefault(a=>!a.GetComponentInParent<Bot>()&&a.parameters.Any(p=>p.name=="isWalking")):null;if(source){foreach(var body in root.GetComponentsInChildren<Animator>(true).Where(a=>a.parameters.Any(p=>p.name=="isWalking"))){body.runtimeAnimatorController=source.runtimeAnimatorController;}}
  var bodyAnimator=root.GetComponentsInChildren<Animator>(true).FirstOrDefault(a=>a.name=="Armature.002");if(source&&bodyAnimator)bodyAnimator.runtimeAnimatorController=source.runtimeAnimatorController;foreach(var handler in root.GetComponentsInChildren<AnimationHandler>(true))Plugin.Set(handler,"anim",bodyAnimator);foreach(var seat in root.GetComponentsInChildren<SitAnimator>(true))Plugin.Set(seat,"anim",bodyAnimator);foreach(var arm in root.GetComponentsInChildren<ProceduralArmAnimator>(true))Plugin.Set(arm,"animator",bodyAnimator);
  foreach(var legs in root.GetComponentsInChildren<LegsAnimator>(true)){legs.Mecanim=bodyAnimator;var saved=legs.Legs.ToArray();legs.Legs.Clear();foreach(var old in saved)legs.Legs.Add(new LegsAnimator.Leg{BoneStart=old.BoneStart,BoneMid=old.BoneMid,BoneEnd=old.BoneEnd,BoneFeet=old.BoneFeet,UseFeet=old.UseFeet,Side=old.Side,OppositeLegIndex=old.OppositeLegIndex,LegBlendWeight=1,ExtraGluingBlend=1,LegMoveSpeedMultiplier=1,LegRaiseMultiplier=1,GlueThresholdMultiplier=1,LegStretchMultiplier=1});Plugin.Set(legs,"LegsInitialized",false);
   Plugin.Set(legs,"baseTransform",root.transform);legs.Rigidbody=null;legs.UseRigidbodyVelocityForIsMoving=false;
   legs.enabled=true;legs.DelayedInitialization=false;legs.AnimatePhysics=false;legs.AnimateFeet=true;legs.OnlyLocalAnimation=false;legs.LegsAnimatorBlend=1;Plugin.Set(legs,"AllowUpdate",true);legs.DisableIfInvisible=null;legs.DisableIfInvisibleExtraRenderers.Clear();legs.FadeOffAtDistance=0;
   foreach(var leg in legs.Legs){leg.LegBlendWeight=1;leg.G_CustomForceAttach=false;leg.G_CustomForceNOTDetach=false;leg.G_CustomForceDetach=false;leg.G_CustomForceNOTAttach=false;}legs.UseHips=false;legs.UseGluing=false;legs.MainGlueBlend=0;legs.GlueMode=LegsAnimator.EGlueMode.Automatic;legs.RaycastStyle=LegsAnimator.ERaycastStyle.StraightDown;legs.GroundMask=-1;legs.RaycastHitTrigger=QueryTriggerInteraction.Ignore;legs.CastDistance=2;legs.GroundedParameter="";legs.MovingParameter="";legs.UseRaycastsForIsGrounded=true;legs.StepInfoReceiver=null;
   var steps=root.GetComponentInChildren<SmugglerStepArmAnimator>(true);legs.Event_OnStep=new UnityEngine.Events.UnityEvent();if(steps)legs.Event_OnStep.AddListener(steps.PlayNextArmSwingPose);
  }
  foreach(var handler in root.GetComponentsInChildren<AnimationHandler>(true))Plugin.Set(handler,"skipIsWalking",true);
  foreach(var step in root.GetComponentsInChildren<SmugglerStepArmAnimator>(true)){Plugin.Set(step,"targetAnimator",bodyAnimator);Plugin.Set(step,"playerController",null);Plugin.Set(step,"movementRoot",motionRoot?motionRoot:root.transform);}
  foreach(var arm in root.GetComponentsInChildren<ProceduralArmAnimator>(true)){Plugin.Set(arm,"playerController",null);Plugin.Set(arm,"movementRoot",motionRoot?motionRoot:root.transform);}
 }
 void Awake(){
  // Native Awake resolves parent controllers again after Configure. Detach those
  // bindings now that every helper has awakened; the wallet parent is not a driver.
  foreach(var arm in GetComponentsInChildren<ProceduralArmAnimator>(true))Plugin.Set(arm,"playerController",null);
  foreach(var step in GetComponentsInChildren<SmugglerStepArmAnimator>(true))Plugin.Set(step,"playerController",null);
  foreach(var jump in GetComponentsInChildren<PlayerJumpPoseAnimator>(true)){jump.enabled=false;Plugin.Set(jump,"ragdollManager",null);}
  bot=GetComponentInParent<Bot>();legs=GetComponentInChildren<LegsAnimator>(true);seats=GetComponentsInChildren<SitAnimator>(true);steps=GetComponentInChildren<SmugglerStepArmAnimator>(true);
  if(legs&&bot)ankleHomes=legs.Legs.Select(l=>bot.transform.InverseTransformPoint(l.BoneEnd.position)).ToArray();hands=gameObject.AddComponent<MonitorArmIkDriver>();
  Transform r=Bone("RightArm"),rm=Bone("RightForeArm"),rh=Bone("RightHand"),l=Bone("LeftArm"),lm=Bone("LeftForeArm"),lh=Bone("LeftHand");
  leftPalmFrame=RestPalm(lh,lm);rightPalmFrame=RestPalm(rh,rm);
  var nativeOwner=bot?bot.Owner:null;if(nativeOwner&&!nativeOwner.GetComponent<PlayerInteractor>())nativeOwner=PlayerName.LocalPlayerInstance;
  var interactor=nativeOwner?nativeOwner.GetComponent<PlayerInteractor>():null;var hold=interactor?Plugin.Get(interactor,"smugglerHoldPoint") as Transform:null;var nativeRig=nativeOwner?nativeOwner.GetComponentsInChildren<LegsAnimator>(true).FirstOrDefault(a=>!a.GetComponentInParent<Bot>()):null;var nativeTip=nativeRig?nativeRig.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.EndsWith(":RightHand",StringComparison.Ordinal)):null;if(interactor){var chain=new object[]{Plugin.Get(interactor,"smugglerRig"),hold,"Right",null,null,null};AccessTools.Method(typeof(PlayerInteractor),"GetMonitorArmChain").Invoke(null,chain);nativeTip=chain[5] as Transform;}Transform alignment=rh;if(hold&&nativeTip&&rh){var point=new GameObject("Employee native hand alignment");point.transform.SetParent(rh,false);point.transform.localPosition=nativeTip.InverseTransformPoint(hold.position);point.transform.localRotation=Quaternion.Inverse(nativeTip.rotation)*hold.rotation;alignment=point.transform;}var leftAlignment=interactor?Quaternion.Euler((Vector3)Plugin.Get(interactor,"monitorLeftHandAlignmentEuler")):Quaternion.identity;hands.Configure(null,null,null,null,null,null,null,r,rm,rh,alignment,l,lm,lh,null,Bone("Spine2"),leftAlignment);
 }
 Transform Bone(string name){return GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name||t.name.EndsWith(":"+name,StringComparison.Ordinal));}
 internal Transform HandBone(string name){return Bone(name);}
 Quaternion RestPalm(Transform hand,Transform forearm){var finger=hand.InverseTransformDirection((hand.position-forearm.position).normalized);var normal=Vector3.ProjectOnPlane(hand.InverseTransformDirection(bot.transform.forward),finger).normalized;return Quaternion.LookRotation(normal,finger);}
 internal Vector3 TypingFinger(string side){return Bone(side+"Hand").rotation*((side=="Left"?leftPalmFrame:rightPalmFrame)*Vector3.up);}
 Quaternion TypingRotation(string side,Transform keyboard){
  var localFrame=side=="Left"?leftPalmFrame:rightPalmFrame;var surfaceNormal=keyboard.forward;if(Vector3.Dot(surfaceNormal,Vector3.up)<0)surfaceNormal=-surfaceNormal;var forward=Vector3.ProjectOnPlane(bot.transform.forward,surfaceNormal).normalized;
  var desired=Quaternion.LookRotation(surfaceNormal,forward)*Quaternion.Inverse(localFrame);var chain=Plugin.Get(hands,"smuggler"+side+"Arm");return desired*(Quaternion)Plugin.Get(chain,"tipToHandAlignmentRotation");
 }
 public void Drive(float speed,bool sitting){
  if(bot){var current=bot.transform.position;if(trackedPosition&&!sitting&&speed>.1f){float travelled=FloorMotion.HorizontalDistanceSquared(current,previousPosition)<4?Vector3.Distance(current,previousPosition):0;stride+=travelled;gait+=travelled/(speed>2.5f?1.25f:.85f);if(stride>(speed>2.5f?.8f:.5f)){stride=0;if(steps)steps.PlayNextArmSwingPose();}}previousPosition=current;trackedPosition=true;}if(seated!=sitting){seated=sitting;foreach(var seat in seats){seat.Sit(sitting);AccessTools.Method(typeof(SitAnimator),"ApplySitOffset").Invoke(seat,new object[]{sitting});}}
  if(legs){legs.enabled=!sitting;legs.GlueMode=speed>.1f?LegsAnimator.EGlueMode.Moving:LegsAnimator.EGlueMode.Idle;Plugin.Set(legs,"AllowUpdate",!sitting);Plugin.Set(legs,"legsWasDisabled",false);legs.User_SetIsGrounded(true);legs.User_SetIsMoving(!sitting&&speed>.1f);if(!sitting&&bot)legs.User_SetDesiredMovementDirection(bot.transform.forward*(speed>.1f?1:0));}
  DriveFeet(speed,sitting);if(steps)steps.SetExternalIkActive(sitting);foreach(var arm in GetComponentsInChildren<ProceduralArmAnimator>(true))arm.SetExternalIkActive(sitting);
 }
 void DriveFeet(float speed,bool sitting){
  if(!legs||ankleHomes==null||!bot)return;
  for(int i=0;i<legs.Legs.Count&&i<ankleHomes.Length;i++){
   var leg=legs.Legs[i];if(sitting||speed<.1f){leg.OverrideTargetIKPosition(null);continue;}
   float phase=gait+i*.5f;phase-=Mathf.Floor(phase);float reach=speed>2.5f?.34f:.23f;
   var local=ankleHomes[i];float lift=0;if(phase<.5f)local.z+=Mathf.Lerp(reach,-reach,phase*2);else{float swing=(phase-.5f)*2;local.z+=Mathf.Lerp(-reach,reach,swing);lift=Mathf.Sin(swing*3.14159265f)*(speed>2.5f?.18f:.13f);}
   var target=bot.transform.TransformPoint(local);Vector3 floor;if(FloorMotion.TryGround(target,out floor))target.y=floor.y+Mathf.Max(.07f,ankleHomes[i].y)+lift;else target.y+=lift;
   leg.OverrideTargetIKPosition(target);
  }
 }
 void LateUpdate(){if(bot)bot.UpdateNameTagPlacement();if(!legs||seated||!bot||ankleHomes==null)return;foreach(var leg in legs.Legs){var processor=Plugin.Get(leg,"IKProcessor");if(processor!=null){Plugin.Set(processor,"IKWeight",1f);Plugin.Set(processor,"IKPositionWeight",1f);AccessTools.Method(leg.GetType(),"IK_PostUpdate").Invoke(leg,null);AccessTools.Method(processor.GetType(),"Update").Invoke(processor,null);}}}
 void Update(){
  if(!hands||!bot)return;var desk=bot.Workspace;var monitor=desk&&desk.callStateScreen?desk.callStateScreen.MonitorInteractable:null;
  if(!seated||!monitor){hands.SetTarget(false,false,Vector3.zero,Quaternion.identity,0);return;}
  bool typing=bot.DesktopTyping;
  if(typing){var keyboard=AccessTools.Method(typeof(MonitorInteractable),"GetKeyboardTarget").Invoke(monitor,null) as Transform;if(keyboard){var values=new object[]{keyboard,Vector3.zero,Quaternion.identity,Vector3.zero,Quaternion.identity};AccessTools.Method(typeof(MonitorInteractable),"GetKeyboardHandTargets").Invoke(monitor,values);hands.SetKeyboardTargets(true,false,(Vector3)values[1],TypingRotation("Left",keyboard),(Vector3)values[3],TypingRotation("Right",keyboard),1);return;}}
  var target=AccessTools.Method(typeof(MonitorInteractable),"GetOrCreateMouseHandIkTarget").Invoke(monitor,null) as Transform;if(target)hands.SetTarget(true,false,target.position,target.rotation,1);
 }
}
}























