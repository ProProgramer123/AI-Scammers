using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace HireScammers {
public sealed partial class Bot {
 readonly Queue<DesktopCommand> desktopQueue=new Queue<DesktopCommand>();readonly Dictionary<string,string> desktopValues=new Dictionary<string,string>();
 readonly HashSet<string> completedScams=new HashSet<string>();readonly HashSet<string> enteredCodes=new HashSet<string>();
 float nextCodeScan;string cachedCallJson;JObject cachedCallView;string scannedCompletionJson;
 bool desktopExecuting;string desktopAction="wait",desktopTarget="",desktopOpenedApp;int desktopRevision;
 internal bool DesktopBusy{get{return desktopExecuting||desktopQueue.Count>0;}}
 public string DesktopAction{get{return !NetworkServer.active&&remoteState!=null?remoteState.DesktopAction:desktopAction;}}
 public string DesktopTarget{get{return !NetworkServer.active&&remoteState!=null?remoteState.DesktopTarget:desktopTarget;}}
 public string DesktopValuesJson{get{return !NetworkServer.active&&remoteState!=null?remoteState.Inputs:JsonConvert.SerializeObject(desktopValues);}}
 public int DesktopRevision{get{return !NetworkServer.active&&remoteState!=null?remoteState.DesktopRevision:desktopRevision;}}
 internal ScamTypeDefinition SelectedScam{get{return scam;}}
 internal ScamTypeDefinition[] EligibleScams(ScammerWorkspace workspace=null){var wallet=Owner?Owner.GetComponent<KolkataOsPlayer>():null;workspace=workspace?workspace:desk;return !wallet?new ScamTypeDefinition[0]:ScamTypeLibrary.LoadAll().Where(s=>s!=null&&AllowsScam(s.id)&&wallet.ServerCanUseScam(s.id,workspace)).ToArray();}
 internal JObject PublicCallView(){var json=CurrentCall?CurrentCall.PublicUiStateJson:null;if(cachedCallView!=null&&json==cachedCallJson)return cachedCallView;cachedCallJson=json;try{cachedCallView=string.IsNullOrEmpty(json)?new JObject():JObject.Parse(json);}catch{cachedCallView=new JObject();}return cachedCallView;}
 internal static string[] CompletedScams(JObject state){var definitions=ScamTypeLibrary.LoadAll().ToArray();return new[]{state["scams"] as JArray,state["conversationScams"] as JArray}.Where(a=>a!=null).SelectMany(a=>a).Where(s=>(bool?)s["completed"]==true).Select(s=>(string)s["id"]).Where(s=>!string.IsNullOrEmpty(s)).Select(id=>AgentPlans.ResolveScam(definitions,id)?.id??id).Distinct().ToArray();}
 void RefreshCompletedScams(){var json=CurrentCall?CurrentCall.PublicUiStateJson:null;if(json!=scannedCompletionJson){scannedCompletionJson=json;foreach(var id in CompletedScams(PublicCallView()))completedScams.Add(id);}if(scam!=null&&completedScams.Contains(scam.id)&&!DesktopBusy){scam=null;thinking=false;thoughtSequence++;thought=null;desktopValues.Clear();Status="Choosing next scam";nextThink=Time.time;}}
 internal string AgentMenu(){return JsonConvert.SerializeObject(new{phase=scam==null?"choose_scam":"call",selectedScam=scam==null?null:scam.id,completedScams=completedScams.ToArray(),call=new{trust=PublicCallView()["trustPercent"],emotion=PublicCallView()["emotion"],completed=completedScams.ToArray()},scams=EligibleScams().Where(s=>!completedScams.Contains(s.id)).Take(24).Select(s=>new{id=s.id,appId=s.appId,name=s.displayName}),desktop=desktop&&desktop.Ready?desktop.View():new JObject{["ready"]=false}});}
 bool TryEnterCallerCode(){
  if(!desktop||!desktop.Ready||scam==null||desktopOpenedApp!=scam.appId)return false;
  if(Time.time<nextCodeScan)return false;nextCodeScan=Time.time+1f;
  var view=desktop.View();DesktopCommand[] commands=new DesktopCommand[0];foreach(var line in CurrentCall.TranscriptEntries.Where(l=>!l.IsPlayer).Reverse().Take(8)){commands=AgentPlans.CodeActions(view,line.Text);if(commands.Length>0)break;}if(commands.Length==0)return false;
  string key=scam.id+":"+commands[0].value;if(!enteredCodes.Add(key))return false;
  foreach(var command in commands)desktopQueue.Enqueue(command);thinking=false;thoughtSequence++;thought=null;Status="Entering caller's code";return true;
 }
 void RequestAgentPlan(){
  if(thinking||!CurrentCall)return;var owner=Owner.connectionToClient;if(owner==null){Status="Waiting for owner connection";return;}
  var lines=CurrentCall.TranscriptEntries;var history=string.Join("\n",lines.Where(l=>l.IsPlayer||!EmployeeCallerRequests.IsServiceMessage(l.Text)).Select(l=>(l.IsPlayer?"Employee: ":"Caller: ")+AgentPlans.PublicText(l.Text)).ToArray());if(history.Length>12000)history=history.Substring(history.Length-12000);
  string goal=scam==null?"Choose another uncompleted purchased scam from the supplied menu, keeping this same call active. Return its exact scam_id. Do not speak until you have chosen and opened the app.":"Continue the fictional "+scam.displayName+" call. Read the visible desktop state and public call progress. If the caller supplied a code, type it into the corresponding visible field and click its submission button before asking again. Complete this scam, then choose another uncompleted purchased scam in this same call. Never count AI claims as verified completion.";
  if(scam!=null){var definition=ConversationScamCatalog.GetByAppId(scam.appId);if(definition!=null&&definition.objectives!=null)goal+=" Objectives: "+string.Join("; ",definition.objectives.Select(o=>o.text).ToArray());}
  if(goal.Length>2800)goal=goal.Substring(0,2800);thinking=true;thoughtAt=Time.time;thoughtSequence++;Status=scam==null?"Choosing a scam with owner's AI":"Planning dialogue and desktop actions";
  owner.Send(new AgentRequest{Bot=netIdentity.netId,Sequence=thoughtSequence,Goal=goal,History=history,Menu=AgentMenu()});
 }
 public void AcceptPlan(uint sequence,string text,string error,string json){
  if(!thinking||sequence!=thoughtSequence||!CurrentCall)return;
  if(!string.IsNullOrEmpty(error)){AcceptThought(sequence,null,error);return;}
  if(string.IsNullOrEmpty(json)||json.Length>8192){AcceptThought(sequence,null,"AI returned no valid employee plan.");return;}
  EmployeePlan plan;try{plan=AgentPlans.Parse(new JObject{["choices"]=new JArray(new JObject{["message"]=new JObject{["content"]=json}})});}catch{plan=null;}
  string problem=null;if(plan==null||!ApplyPlan(plan,out problem)){thinking=false;Status="AI plan rejected: "+(plan==null?"invalid format":problem);nextThink=Time.time+5;return;}
  thinking=false;nextThink=Time.time+.5f;lastProgress=Time.time;
 }
 internal bool ApplyPlan(EmployeePlan plan,out string error){
  error=null;if(!NetworkServer.active||!CurrentCall){error="no active employee call";return false;}
  var available=EligibleScams().Where(s=>!completedScams.Contains(s.id)).ToArray();
  var selected=string.IsNullOrWhiteSpace(plan.scam_id)?scam:AgentPlans.ResolveScam(available,plan.scam_id);
  if(selected==null&&scam==null&&string.IsNullOrWhiteSpace(plan.scam_id))selected=available.FirstOrDefault();
  if(selected==null){error="choose an available purchased scam";return false;}
  if(completedScams.Contains(selected.id)){error="that scam is already completed in this call; choose another";return false;}
  string caller=string.Join(" ",CurrentCall.TranscriptEntries.Where(l=>!l.IsPlayer).Select(l=>l.Text).ToArray());
  var actions=plan.desktop_actions??new DesktopCommand[0];if(actions.Length>6){error="too many actions";return false;}
  foreach(var action in actions){int index;if(action==null){error="empty action";return false;}
   if(action.type=="open_app"){if(action.target!=selected.appId){error="app does not match the selected purchased scam";return false;}}
   else if(action.type=="type"){if(!ControlIndex(action.target,"field/",out index)||!AgentPlans.SpokenValue(action.value,caller)){error="field value must come from the public caller transcript";return false;}}
   else if(action.type=="click"){if(!ControlIndex(action.target,"button/",out index)){error="unknown desktop button";return false;}}
   else if(action.type!="wait"){error="unsupported action";return false;}
  }
  bool choosing=scam==null;bool changing=scam!=selected;scam=selected;if(changing)desktopValues.Clear();
  if(changing||desktopOpenedApp!=selected.appId)desktopQueue.Enqueue(new DesktopCommand{type="open_app",target=selected.appId});
  foreach(var action in actions)if(action.type!="wait"&&action.type!="open_app")desktopQueue.Enqueue(action);
  thought=choosing||string.IsNullOrWhiteSpace(plan.dialogue)?null:NetworkedCall.NormalizeTranscriptDisplayText(plan.dialogue.Length>600?plan.dialogue.Substring(0,600):plan.dialogue);
  Status=DesktopBusy?"Using desktop: "+selected.displayName:"Scamming: "+selected.displayName;return true;
 }
 internal static bool ControlIndex(string target,string prefix,out int index){index=-1;return target!=null&&target.StartsWith(prefix,StringComparison.Ordinal)&&int.TryParse(target.Substring(prefix.Length),out index)&&index>=0&&index<32;}
 internal DesktopCommand NextDesktopAction(){if(!NetworkServer.active||desktopExecuting||desktopQueue.Count==0||!CurrentCall)return null;desktopExecuting=true;var action=desktopQueue.Dequeue();desktopAction=action.type;desktopTarget=action.target;desktopRevision++;return action;}
 internal void SetDesktopInput(string target,string value){desktopValues[target]=value;desktopInput=value;desktopRevision++;}
 internal void OpenedDesktop(string app){desktopOpenedApp=app;desktopRevision++;if(CurrentCall&&Owner)HarmonyLib.AccessTools.Method(typeof(VictimCall),"ServerRecordOpenedPromptApplications").Invoke(CurrentCall,new object[]{Owner.GetComponent<KolkataOsPlayer>()});}
 internal void CompleteDesktopAction(string error=null){desktopExecuting=false;desktopAction="wait";desktopRevision++;if(error!=null){enteredCodes.Clear();Status="Desktop: "+error;desktopQueue.Clear();nextThink=Time.time+3;}lastProgress=Time.time;}
 internal void ClearDesktopActions(){desktopQueue.Clear();desktopValues.Clear();desktopOpenedApp=null;desktopAction="wait";desktopTarget="";desktopExecuting=false;desktopRevision++;}
}
}

