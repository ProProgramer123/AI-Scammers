using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

namespace HireScammers {
public sealed partial class Bot : MonoBehaviour {
 public PlayerName Owner;public int Slot,LocalId;public string RecruitName="Alex",ModelId;
 public Vector3 Position;public VictimCall CurrentCall;public long Earned,DayEarned;
 public string Status="Roaming",Speech="";
 internal VanillaEmployee Vanilla;
 public NetworkIdentity netIdentity{get{return GetComponent<NetworkIdentity>();}}
 public string DisplayName{get{return !NetworkServer.active&&remoteState!=null?remoteState.Name:(Owner?Owner.playerName:"Owner")+"'s "+RecruitName;}}
 KolkataPlayerVoice voice;GameObject visual,tagRoot;Animator animator;NativeEmployeeMotion locomotion;PlayerNameText nameTag;
 public ScammerWorkspace Workspace{get{if(desk)return desk;NetworkIdentity identity;return remoteState!=null&&NetworkClient.spawned.TryGetValue(remoteState.Workspace,out identity)?identity.GetComponent<ScammerWorkspace>():null;}}
 EmployeeDesktop desktop;float nextDesktop,motionSpeed;
 public string[] DesktopProducts{get{return !NetworkServer.active&&remoteState!=null?remoteState.Products??new string[0]:Runtime.ProductsFor(Owner);}}
 public bool IsSeated{get{return sitting;}}
 public string DesktopApp{get{return !NetworkServer.active&&remoteState!=null?remoteState.AppId:desktopOpenedApp;}}
 public string DesktopInput{get{return !NetworkServer.active&&remoteState!=null?remoteState.InputValue:desktopInput;}}
 public bool DesktopTyping{get{return !NetworkServer.active&&remoteState!=null?remoteState.Typing:sitting&&desktopExecuting&&desktopAction=="type";}}
 Vector3 destination,remotePosition;float remoteYaw,nextThink,ringSeen,callStarted,lastProgress,nextWander,pathAt;int turn,lastSequence,lastDay=-1;
 NetworkedCall pursuing;ScammerWorkspace desk;ScamTypeDefinition scam;string lastCaller="",callWaitReason;
 readonly HashSet<string> submitted=new HashSet<string>();readonly List<Vector3> path=new List<Vector3>();
 uint thoughtSequence; bool thinking;float thoughtAt;string thought;
 float lastMove,nextTrace;int steerSide=1;bool sitting,routeReady;EmployeeState remoteState;internal string LastBlockReason;
 Vector3 anchoredPosition;Quaternion anchoredRotation;bool hasAnchor,reviewPlaced;string desktopInput;
 internal void AnchorPose(){anchoredPosition=transform.position;anchoredRotation=transform.rotation;hasAnchor=true;}
 internal void RestorePose(){if(hasAnchor){transform.position=anchoredPosition;transform.rotation=anchoredRotation;}}
 void Ground(){Vector3 grounded;if(!sitting&&FloorMotion.TryGround(Position,out grounded)){Position=grounded;transform.position=Position;}}
 void RecoverPosition(){Vector3 safe;if(FloorMotion.Recover(Position,out safe)){Position=safe;transform.position=safe;}routeReady=false;path.Clear();}
 internal bool PlaceOnWorkFloor(int day){Vector3 safe;var g=ScammerGameManager.Instance;if(!FloorMotion.OfficeSpawn(Position,day,g&&g.IsSandboxMode,out safe))return false;Position=safe;transform.position=safe;routeReady=false;path.Clear();return true;}
 public void EnsureVisual(){if(visual||!Owner)return;visual=OfficeCharacters.Clone(ModelId,transform);if(!visual)return;RecruitAppearance.Apply(visual,Slot,Owner.steamId);animator=visual.GetComponentsInChildren<Animator>(true).FirstOrDefault(a=>a.parameters.Any(p=>p.name=="isWalking"));locomotion=visual.AddComponent<NativeEmployeeMotion>();
  nameTag=NativeNameTags.Create(transform,DisplayName,out tagRoot);
 }
 void Start(){voice=GetComponent<KolkataPlayerVoice>();}
 public void HideVisual(){if(visual)visual.SetActive(false);if(tagRoot)tagRoot.SetActive(false);}
 public void Apply(EmployeeState state){RestorePose();remoteState=state;Settings=state.Settings??new WorkerSettings();RefreshRetainedFloor();Slot=state.Slot;ModelId=state.ModelId;remotePosition=new Vector3(state.X,state.Y,state.Z);remoteYaw=state.Yaw;RecruitName=state.Name.Contains("'s ")?state.Name.Substring(state.Name.IndexOf("'s ",StringComparison.Ordinal)+3):state.Name;
  NetworkIdentity owner;if(NetworkClient.spawned.TryGetValue(state.Owner,out owner)){Owner=owner.GetComponent<PlayerName>();if(Owner&&transform.parent!=Owner.transform)transform.SetParent(Owner.transform,true);}
  EnsureVisual();if(visual)visual.SetActive(true);Status=state.Status;Speech=state.Speech;Earned=state.Earned;DayEarned=state.DayEarned;sitting=state.Sitting;
  if(!hasAnchor||(transform.position-remotePosition).sqrMagnitude>100)transform.position=remotePosition;AnchorPose();
 }
 void Update(){if(NetworkServer.active||remoteState==null)return;RestorePose();var before=transform.position;transform.position=Vector3.Lerp(before,remotePosition,Time.deltaTime*12);transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(0,remoteYaw,0),Time.deltaTime*12);Animate(remoteState.Speed);AnchorPose();}
 void LateUpdate(){RestorePose();if(visual&&Owner&&!nameTag)nameTag=NativeNameTags.Create(transform,DisplayName,out tagRoot);if(sitting&&!desktop&&Workspace&&Time.time>nextDesktop){nextDesktop=Time.time+10;try{desktop=EmployeeDesktop.Create(this,Workspace);}catch(Exception error){Debug.LogWarning("[HireScammers] Employee desktop unavailable: "+error.GetType().Name);}}if(nameTag)nameTag.SetForcedPlayerName(PlayerName.SanitizeUnityRichText(DisplayName));UpdateNameTagPlacement();}
 public EmployeeState Snapshot(){return new EmployeeState{ScreenView=ScreenView(),ManualControl=UnderRemoteControl,Settings=Settings,Slot=Slot,Id=netIdentity.netId,Owner=Owner?Owner.netId:0,Name=DisplayName,Status=Status,Speech=Speech,Earned=Earned,ModelId=ModelId,X=Position.x,Y=Position.y,Z=Position.z,Yaw=transform.eulerAngles.y,Sitting=sitting,Workspace=desk?desk.netId:0,Typing=DesktopTyping,DayEarned=DayEarned,AppId=DesktopApp,InputValue=desktopInput,Products=DesktopProducts,Speed=motionSpeed,DesktopAction=DesktopAction,DesktopTarget=DesktopTarget,Inputs=DesktopValuesJson,DesktopRevision=DesktopRevision};}
 public void ServerTick(){
  if(!Owner||!NetworkServer.active)return;if(!voice)voice=GetComponent<KolkataPlayerVoice>();
  // This identity is parented for the game's wallet lookup, but its world position
  // is simulated independently, so movement of the owner cannot drag the recruit.
  RestorePose();transform.position=Position;
  try{Tick();}catch(Exception e){Debug.LogError("[HireScammers] "+DisplayName+": "+e);Release();nextThink=Time.time+10;}
  transform.position=Position;AnchorPose();
  if(Time.time>=nextTrace){nextTrace=Time.time+15;Debug.Log("[HireScammers] Worker "+DisplayName+" status="+Status+" position="+Position+" route="+routeReady+" target="+(desk?desk.netId.ToString():"none")+" lastBlock="+LastBlockReason);}
 }
 void OnDestroy(){ClearRetainedFloor();if(Vanilla&&NetworkServer.active)NetworkServer.Destroy(Vanilla.gameObject);if(desktop)Destroy(desktop.gameObject);if(tagRoot)Destroy(tagRoot);}
 void Tick(){
  var review=ScammerGameManager.Instance;if(review&&review.IsReviewPhase){if(!reviewPlaced){Release();reviewPlaced=EmployeeReview.Place(this);}EnsureVisual();if(visual)visual.SetActive(true);if(tagRoot)tagRoot.SetActive(true);Status="Presentation";Animate(0);return;}reviewPlaced=false;
  if(!Runtime.Workday){Release();Status="Off duty";HideVisual();return;}
  if(!visual)EnsureVisual();if(visual)visual.SetActive(true);if(tagRoot)tagRoot.SetActive(true);
  var g=ScammerGameManager.Instance;
  if(lastDay!=g.CurrentDay){Release();DayEarned=0;var start=g.GetDayStartPoint(AssignedDay(g.CurrentDay),false);if(start)Position=start.position;if(!PlaceOnWorkFloor(AssignedDay(g.CurrentDay))){Status="Waiting for work-floor navigation";return;}lastDay=g.CurrentDay;destination=Position;path.Clear();}
  if(Settings.Paused){if(CurrentCall)Release();Status="Paused";Animate(0);return;}
  if(CurrentCall){HandleCall();return;}
  if(sitting)Release();
  if(Time.time<nextThink)return;
  if(pursuing){
   if(!desk||!pursuing||pursuing.callState!=NetworkedCall.CallState.Ringing||desk.workerPlayerVoice|| (desk.chairInteractable&&desk.chairInteractable.IsOccupied)){Release();nextThink=Time.time+2;return;}
   if(!routeReady){Release();nextThink=Time.time+2;return;}
   Status="Running to a call";Move(destination,4.5f);if(!pursuing)return;
   if(FloorMotion.HorizontalDistanceSquared(Position,destination)<.55f){
    var call=pursuing as VictimCall;
    if(call&&call.ServerTryPickUp(voice)){
     CurrentCall=call;callStarted=lastProgress=Time.time;turn=0;lastSequence=-1;submitted.Clear();completedScams.Clear();enteredCodes.Clear();lastCaller="";scam=null;ClearDesktopActions();
     
     // Worker/speaker sync makes native desk screens reflect the call.
     desk.NetworkworkerPlayerVoice=voice;desk.NetworkisOnSpeaker=true;desk.ServerRefreshCallStateScreen();sitting=true;
     var chair=desk.chairInteractable;var sit=chair?Plugin.Get(chair,"sitPoint") as Transform:null;
     if(sit){Position=sit.position;transform.rotation=sit.rotation;}
     Status="Choosing a scam with owner's AI";Speech="";nextThink=Time.time+3;Animate(0);
    }else {Release();nextThink=Time.time+2;}
   }
   return;
  }
  if(Time.time>=pathAt){pathAt=Time.time+.75f;FindCall();if(pursuing)return;}
  Status=callWaitReason??"Roaming";
  if(Time.time>nextWander||FloorMotion.HorizontalDistanceSquared(Position,destination)<.7f){
   nextWander=Time.time+UnityEngine.Random.Range(5f,11f);var desks=ScammerWorkspace.Instances.Where(d=>d&&d.SupportsPhoneCalls&&d.IsOnSameFloor(Position)&&d.IsOnWorkdayFloor(AssignedDay(g.CurrentDay),g.IsSandboxMode)).ToArray();
   bool found=false;foreach(var d in desks.OrderBy(d=>UnityEngine.Random.value).Take(10))if(BuildDeskPath(d)){found=true;break;}
   if(!found){nextWander=Time.time+1;Status="Finding a clear route";}
  }
  Move(destination,1.45f);
 }
 static Vector3 DeskApproach(ScammerWorkspace d){var chair=d.chairInteractable;if(chair){var exit=Plugin.Get(chair,"exitPoint") as Transform;if(exit)return exit.position;return chair.transform.position-chair.transform.forward*.7f;}return d.transform.position-d.transform.forward;}
 readonly Dictionary<uint,float> ringing=new Dictionary<uint,float>();
 void FindCall(){
  var g=ScammerGameManager.Instance;if(!g)return;
  callWaitReason=null;
  foreach(var d in ScammerWorkspace.Instances){if(!d||!d.IsOnWorkdayFloor(AssignedDay(g.CurrentDay),g.IsSandboxMode)||!d.HasRingingCall||d.workerPlayerVoice||(d.chairInteractable&&d.chairInteractable.IsOccupied)||!d.IsOnSameFloor(Position))continue;
   var c=d.activeCall as VictimCall;if(!c||c is ScambaiterCall||Runtime.Instance==null)continue;
   // A claim is local to the host and checked against all employees before assignment.
   if(FindObjectsByType<Bot>(FindObjectsSortMode.None).Any(b=>b!=this&&b.pursuing==c))continue;
   float first;if(!ringing.TryGetValue(c.netId,out first)){ringing[c.netId]=Time.time;continue;}if(Time.time-first<Plugin.Grace.Value)continue;
   if(ChooseScam(d)==null){callWaitReason="Needs your scam app for this floor";continue;}
   if(!BuildDeskPath(d)){callWaitReason="Cannot reach ringing desk";continue;}desk=d;pursuing=c;ringSeen=Time.time;return;
  }
  if(ringing.Count>100)ringing.Clear();
 }
 ScamTypeDefinition ChooseScam(ScammerWorkspace target=null){
  var p=Owner.GetComponent<KolkataOsPlayer>();target=target?target:desk;
  var usable=ScamTypeLibrary.LoadAll().Where(s=>s!=null&&AllowsScam(s.id)&&p.ServerCanUseScam(s.id,target)).ToArray();
  return usable.OrderBy(s=>s.id.IndexOf("gift",StringComparison.OrdinalIgnoreCase)>=0?0:s.UsesConversationObjectives?1:2).FirstOrDefault();
 }
 void HandleCall(){
  if(!desk||CurrentCall.callState!=(NetworkedCall.CallState)1||CurrentCall.talker!=voice){Release();nextThink=Time.time+3;return;}
  // Humans always have priority if they sit down at a claimed desk.
  if(desk.chairInteractable&&desk.chairInteractable.IsOccupied){Release();nextThink=Time.time+3;return;}
  if(Time.time-callStarted>600||turn>=48){Release();nextThink=Time.time+4;return;}
  if(UnderRemoteControl){Status="Player controlling computer";return;}
  RefreshCompletedScams();
  if(DesktopBusy)return;if(scam==null){if(thinking&&Time.time-thoughtAt>65){thinking=false;nextThink=Time.time+10;}if(!thinking&&Time.time>=nextThink)RequestAgentPlan();return;}
  if(Time.time<nextThink)return;
  if(TryEnterCallerCode())return;
  if(Time.time<CallerRetryAfter){Status="Caller AI error: "+CallerFailureCode+" (waiting before retry)";return;}
  if(!CurrentCall.CanAcceptTalkerTranscriptNow){if(Time.time-lastProgress>65){Status="Waiting for caller AI…";}return;}

  var lines=CurrentCall.TranscriptEntries;
  int sequence=lines.Count==0?-1:lines[lines.Count-1].Sequence;
  if(turn>0&&sequence==lastSequence&&Time.time-lastProgress<25)return;
  lastCaller=string.Join(" ",lines.Where(l=>!l.IsPlayer).Select(l=>l.Text).ToArray());
  
  if(thinking){if(Time.time-thoughtAt>65){thinking=false;Status="Your AI timed out";nextThink=Time.time+20;}return;}
  if(thought==null){RequestAgentPlan();return;}
  string response=thought;thought=null;
  Speech=response;CurrentCall.SendPrompt(response,"en",null,default(ReviewUtteranceTiming));
  EmployeeAudio.Speak(this,CurrentCall,response);
  turn++;lastSequence=sequence;lastProgress=Time.time;nextThink=Time.time+7;
  Status="Scamming: "+scam.displayName+" (turn "+turn+")";
  Debug.Log("[HireScammers] "+DisplayName+" call="+CurrentCall.CallGuid+" turn="+turn);
 }
  bool BuildPath(Vector3 goal){Ground();path.Clear();routeReady=false;Vector3 start,end;
  if(!FloorMotion.Clear(Position))RecoverPosition();
  if(!FloorMotion.TryWalkable(Position,.65f,out start)||FloorMotion.HorizontalDistanceSquared(start,Position)>.16f){LastBlockReason="Off navigation at "+Position+" sampled="+start;return false;}
  if(!FloorMotion.TryWalkable(goal,1f,out end)){LastBlockReason="Invalid desk approach "+goal+" grounded="+end;return false;}
  if(Mathf.Abs(end.y-start.y)>.5f){LastBlockReason="Desk approach landed above the work floor";return false;}
  var p=new NavMeshPath();if(!NavMesh.CalculatePath(start,end,-1,p)||p.status!=NavMeshPathStatus.PathComplete){LastBlockReason="Disconnected navigation from "+start+" to "+end;return false;}
  destination=end;path.AddRange(p.corners.Skip(1));routeReady=true;lastMove=Time.time;return true;
 }
 bool BuildDeskPath(ScammerWorkspace workspace){
  var chair=workspace.chairInteractable;var center=chair?chair.transform.position:workspace.transform.position;
  var candidates=new List<Vector3>{DeskApproach(workspace)};
  for(int ring=0;ring<3;ring++)for(int angle=0;angle<8;angle++){
   float radians=angle*.78539816f;var offset=new Vector3(Mathf.Cos(radians),0,Mathf.Sin(radians))*(.65f+ring*.4f);candidates.Add(center+offset);
  }
  Vector3 best=Position;Vector3[] corners=null;float cost=1e20f;
  foreach(var candidate in candidates){
   if(FloorMotion.HorizontalDistanceSquared(candidate,center)>6.25f)continue;
   var goal=candidate;goal.y=Position.y;
   if(!BuildPath(goal))continue;
   float distance=0;var before=Position;foreach(var corner in path){distance+=Vector3.Distance(before,corner);before=corner;}
   if(distance>=cost)continue;cost=distance;best=destination;corners=path.ToArray();
  }
  path.Clear();routeReady=corners!=null;if(!routeReady)return false;
  path.AddRange(corners);destination=best;lastMove=Time.time;return true;
 }
 void Move(Vector3 goal,float speed){
  if(!routeReady){Animate(0);return;}
  if(path.Count>0){goal=path[0];if(FloorMotion.HorizontalDistanceSquared(Position,goal)<.0025f){path.RemoveAt(0);goal=path.Count>0?path[0]:destination;}}
  Step(goal,speed);
 }
 void Blocked(string reason){LastBlockReason=reason;Animate(0);Status="Route blocked";if(Time.time-lastMove>2){Release();RecoverPosition();nextWander=0;nextThink=Time.time+1;}}
 void Step(Vector3 goal,float speed){
  Ground();
  if(!FloorMotion.Clear(Position)){Blocked("Current body overlaps geometry");return;}
  var delta=goal-Position;delta.y=0;if(delta.sqrMagnitude<.0025f){Animate(0);return;}
  var step=delta.normalized*Mathf.Min(Mathf.Min(speed*Time.deltaTime,.35f),delta.magnitude);
  if(!FloorMotion.ClearSweep(Position,Position+step)){
   bool detour=false;
   foreach(float angle in new[]{45f,75f,100f}){for(int side=0;side<2;side++){
    int sign=side==0?steerSide:-steerSide;var candidate=Quaternion.Euler(0,angle*sign,0)*step;Vector3 floor;NavMeshHit walk;
    if(!FloorMotion.ClearSweep(Position,Position+candidate)||!FloorMotion.TryGround(Position+candidate,out floor)||Mathf.Abs(floor.y-Position.y)>.4f||!NavMesh.SamplePosition(floor,out walk,.15f,-1)||FloorMotion.HorizontalDistanceSquared(floor,walk.position)>.0025f)continue;
    step=candidate;steerSide=sign;detour=true;break;
   }if(detour)break;}
   if(!detour){Blocked("Body sweep: "+FloorMotion.LastObstacle);return;}
  }
  var next=Position+step;
  Vector3 grounded;if(!FloorMotion.TryGround(next,out grounded)||Mathf.Abs(grounded.y-Position.y)>.55f||!FloorMotion.Clear(grounded)){Blocked("Ground or destination body clearance: "+next);return;}
  NavMeshHit mesh;if(NavMesh.SamplePosition(Position,out mesh,.2f,-1)){
   NavMeshHit nextMesh;if(!NavMesh.SamplePosition(grounded,out nextMesh,.15f,-1)||FloorMotion.HorizontalDistanceSquared(grounded,nextMesh.position)>.0025f){Blocked("Navmesh edge: "+grounded);return;}
  }
  Position=grounded;lastMove=Time.time;transform.position=Position;transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(step),Time.deltaTime*10);Animate(speed);
 }
 void Animate(float speed){motionSpeed=speed;
  if(locomotion)locomotion.Drive(speed,sitting);
  if(!animator)return;bool moving=!sitting&&speed>.1f;bool running=moving&&speed>2.5f;
  animator.enabled=true;animator.speed=running?1.6f:1f;
  foreach(var p in animator.parameters){
   if(p.type==AnimatorControllerParameterType.Bool){
    if(p.name=="isWalking")animator.SetBool(p.nameHash,moving);
    else if(p.name=="isMoving"||p.name=="isReallyMoving")animator.SetBool(p.nameHash,moving);
    else if(p.name=="isGrounded")animator.SetBool(p.nameHash,true);
    else if(p.name.IndexOf("sit",StringComparison.OrdinalIgnoreCase)>=0)animator.SetBool(p.nameHash,sitting);
   }
   if(p.type==AnimatorControllerParameterType.Float){
    if(p.name=="Blend")animator.SetFloat(p.nameHash,running?1:0);
    else if(p.name.Equals("Speed",StringComparison.OrdinalIgnoreCase)||p.name.IndexOf("velocity",StringComparison.OrdinalIgnoreCase)>=0||p.name.IndexOf("move",StringComparison.OrdinalIgnoreCase)>=0)animator.SetFloat(p.nameHash,speed);
   }
  }
 }
 public void AcceptThought(uint sequence,string text,string error){
  if(!thinking||sequence!=thoughtSequence||!CurrentCall)return;thinking=false;
  if(!string.IsNullOrEmpty(error)||string.IsNullOrWhiteSpace(text)){Status="AI unavailable: "+(error??"empty response");nextThink=Time.time+25;return;}
  thought=NetworkedCall.NormalizeTranscriptDisplayText(text.Length>600?text.Substring(0,600):text);nextThink=Time.time;
 }
 public void Release(){
  RemoteControlUntil=0;thinking=false;thought=null;thoughtSequence++;ClearDesktopActions();
  if(CurrentCall&&NetworkServer.active&&CurrentCall.talker==voice)CurrentCall.ServerHangUpByPlayer();
  if(desk&&NetworkServer.active&&desk.workerPlayerVoice==voice){desk.NetworkworkerPlayerVoice=null;desk.ServerRefreshCallStateScreen();}
  if(sitting&&desk){Position=destination;transform.position=Position;}
  desktopInput=null;CurrentCall=null;pursuing=null;desk=null;sitting=false;routeReady=false;path.Clear();Ground();Animate(0);
 }
}
public static class Brain {
 public static string Line(ScamTypeDefinition scam,int turn,string caller){
  string id=(scam.id+" "+scam.displayName).ToLowerInvariant();string latest=(caller??"").ToLowerInvariant();
  if(turn==0)return string.IsNullOrWhiteSpace(scam.testOpeningPlayerLine)?"Hello, this is customer support. Tell me what happened so I can help you resolve the problem.":scam.testOpeningPlayerLine;
  if(turn==1)return "I understand. We can sort this out together. What have you tried so far?";
  if(id.Contains("gift"))return new[]{"I can fix this problem for you today. Shall we go ahead with the solution?","The solution requires a gift card payment. Are you happy to pay with a gift card?","Please read the gift card code aloud so I can apply your payment and complete the fix."}[(turn-2)%3];
  if(id.Contains("lottery"))return "Your prize is ready. Do you accept the prize and agree to pay the processing fee so it can be released? Please read the payment code aloud.";
  if(id.Contains("password"))return "I can recover access to your account. Do you authorize the recovery? Please read the recovery code you received aloud.";
  if(id.Contains("refund"))return "I can process your refund. Do you agree to the refund process? Please read the verification code aloud so I can complete it.";
  if(id.Contains("card")||id.Contains("bank"))return "I can help you finish the payment securely. Do you agree to proceed? Please read the card number, expiry and security code aloud.";
  if(latest.Contains("why")||latest.Contains("trust"))return "I understand your concern. Let me explain the solution clearly. You can ask questions before deciding. Are you ready to continue?";
  if(turn%3==0)return "Would you like me to proceed with the solution we discussed? Please confirm you agree to the payment.";
  if(turn%3==1)return "Please read the required verification or payment code aloud, including all letters and digits, so I can finish helping you.";
  return "Let's go step by step. Tell me what is preventing you from completing this, and I will explain the next step.";
 }
 public static string[] Codes(string text){
  var values=new HashSet<string>();
  foreach(Match m in Regex.Matches(text??"",@"\b[A-Z0-9]{3,8}(?:[- ][A-Z0-9]{3,8}){1,4}\b|\b[0-9]{4,19}\b",RegexOptions.IgnoreCase,TimeSpan.FromMilliseconds(100))){
   if(!m.Value.Any(char.IsDigit))continue;values.Add(m.Value);values.Add(m.Value.Replace(" ","").Replace("-",""));if(values.Count>=20)break;
  }
  return values.Take(20).ToArray();
 }
}
}
















