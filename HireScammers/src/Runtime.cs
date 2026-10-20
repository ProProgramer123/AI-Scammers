using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mirror;
using Newtonsoft.Json;
using HarmonyLib;
using UnityEngine;

namespace HireScammers {
public struct HireRequest : NetworkMessage {public byte Action; public string Name,ModelId;}
public struct HireState : NetworkMessage {public string Json;}
[Serializable] public sealed class EmployeeState {
 public string ScreenView;public bool ManualControl;public WorkerSettings Settings;public string[] Products;public int Slot;public uint Id, Owner, Workspace;public string Name, Status, Speech,ModelId,AppId,InputValue,DesktopAction,DesktopTarget,Inputs;public int DesktopRevision;public float X,Y,Z,Yaw,Speed;public long Earned,DayEarned;public bool Sitting,Typing;
}
public sealed class Runtime : MonoBehaviour {
 public static Runtime Instance;
 public static string Notice="";
 public const uint Asset=0x48534341;
 public const string WorkerPrefix="hire-scammers-worker/";
 readonly Dictionary<int,Bot> workers=new Dictionary<int,Bot>();
 readonly Dictionary<uint,Bot> replicas=new Dictionary<uint,Bot>();
 readonly HashSet<int> peers=new HashSet<int>();
 internal static bool IsPeer(int id){return Instance&&Instance.peers.Contains(id);}
 internal static void ForgetPeer(int id){if(Instance){Instance.peers.Remove(id);Instance.requestTimes.Remove(id);}}
 readonly Dictionary<int,float> requestTimes=new Dictionary<int,float>();
 bool server,client;float tick,nextHello;int nextId=1;
 static EmployeeState[] remote=new EmployeeState[0];
 void Awake(){Instance=this; Wire();}
 void Wire(){
  Writer<HireRequest>.write=(w,v)=>{w.WriteByte(v.Action);w.WriteString(v.Name);w.WriteString(v.ModelId);};
  Reader<HireRequest>.read=r=>new HireRequest {Action=r.ReadByte(),Name=r.ReadString(),ModelId=r.ReadString()};
  Writer<HireState>.write=(w,v)=>w.WriteString(v.Json);
  Reader<HireState>.read=r=>new HireState {Json=r.ReadString()};
 }
 void Update(){
  if(NetworkClient.active&&!client){client=true;Wire();AgentAi.RegisterClient();NetworkClient.RegisterHandler<HireState>(Receive);RegisterSpawners();nextHello=0;}
  if(!NetworkClient.active&&client){client=false;remote=new EmployeeState[0];Notice="";}
  if(NetworkServer.active&&!server){server=true;Wire();AgentAi.RegisterServer();NetworkServer.RegisterHandler<HireRequest>(Handle);}
  if(!NetworkServer.active&&server){server=false;Clear();}
  if(server)GuestCompatibility.Advertise();
  if(client&&NetworkClient.isConnected&&Time.unscaledTime>nextHello){nextHello=Time.unscaledTime+5;if(GuestCompatibility.HostAvailable())NetworkClient.Send(new HireRequest {Action=2,Name=GuestCompatibility.Protocol});}
  foreach(var b in workers.Values.ToArray())if(b)b.ServerTick();
  if(!server||Time.unscaledTime<tick)return;tick=Time.unscaledTime+.5f;
  try{Reconcile();Broadcast();}catch(Exception e){Debug.LogError("[HireScammers] Runtime: "+e);}
 }
 static void RegisterSpawners(){
  ((IDictionary<uint,SpawnHandlerDelegate>)AccessTools.Field(typeof(NetworkClient),"spawnHandlers").GetValue(null))[Asset]=Spawn;
  ((IDictionary<uint,UnSpawnDelegate>)AccessTools.Field(typeof(NetworkClient),"unspawnHandlers").GetValue(null))[Asset]=Unspawn;
 }
 static void UnregisterSpawners(){
  ((IDictionary<uint,SpawnHandlerDelegate>)AccessTools.Field(typeof(NetworkClient),"spawnHandlers").GetValue(null)).Remove(Asset);
  ((IDictionary<uint,UnSpawnDelegate>)AccessTools.Field(typeof(NetworkClient),"unspawnHandlers").GetValue(null)).Remove(Asset);
 }
 static GameObject Spawn(SpawnMessage msg){var go=Proxy();go.transform.position=msg.position;return go;}
 static void Unspawn(GameObject go){Destroy(go);}
 internal static GameObject Proxy(){var go=new GameObject("Hired Scammer");go.SetActive(false);var ni=go.AddComponent<NetworkIdentity>();Plugin.Set(ni,"assetId",Asset);go.AddComponent<KolkataPlayerVoice>();go.AddComponent<Bot>();go.SetActive(true);return go;}
 static void Receive(HireState msg){
  if(msg.Json==null||msg.Json.Length>120000)return;
  try{
   var packet=JsonConvert.DeserializeObject<Packet>(msg.Json);if(packet==null)return;
   if(packet.Message!=null)Notice=packet.Message;
   if(packet.Workers==null)return;remote=packet.Workers;
   if(NetworkServer.active)return;
   var seen=new HashSet<uint>();
   foreach(var state in remote){seen.Add(state.Id);NetworkIdentity ni;if(!NetworkClient.spawned.TryGetValue(state.Id,out ni))continue;
    var b=ni.GetComponent<Bot>();if(!b)continue;b.Apply(state);Instance.replicas[state.Id]=b;
   }
   foreach(var key in Instance.replicas.Keys.ToArray())if(!seen.Contains(key)){var b=Instance.replicas[key];if(b)b.HideVisual();Instance.replicas.Remove(key);}
  }catch(Exception e){Debug.LogWarning("[HireScammers] Invalid state: "+e.Message);}
 }
 sealed class Packet{public string Message;public EmployeeState[] Workers;}
 public const string ProgressPrefix="hire-scammers-personal-progress/";
 public static bool Owns(KolkataOsPlayer p,string product){
  if(!p)return false;var g=ScammerGameManager.Instance;
  return NetworkServer.active&&g&&p.RunSaveSteamId!=0?g.ServerHasRunEntitlement(p.RunSaveSteamId,product):p.HasRunEntitlement(product);
 }
 public static int PersonalCount(KolkataOsPlayer p){
  if(!p)return 0;int count=p.UniqueScammedCallerCount;
  for(int i=1;i<=Plugin.Required.Value;i++)if(Owns(p,ProgressPrefix+i))count=Math.Max(count,i);
  return count;
 }
 public static void RecordPersonalScam(KolkataOsPlayer p,Guid caller){
  var g=ScammerGameManager.Instance;if(!NetworkServer.active||!p||!g||p.RunSaveSteamId==0||caller==Guid.Empty)return;
  var callers=Plugin.Get(p,"serverScammedCallerGuids") as HashSet<Guid>;
  if(callers!=null&&callers.Contains(caller))return;
  int target=Math.Min(Plugin.Required.Value,PersonalCount(p)+1);
  for(int i=1;i<=target;i++)if(!Owns(p,ProgressPrefix+i)){
   long balance;g.ServerTryPurchaseRunEntitlement(p,p.RunSaveSteamId,ProgressPrefix+i,"Personal scam progress",0,out balance,null);
  }
 }
 public static bool UnlockDayReached(int day){return day>=Plugin.UnlockDay;}
 public static string PurchaseBlockReason(KolkataOsPlayer p){
  var g=ScammerGameManager.Instance;
  if(!p||!g)return "Hire Scammers: the player store is not ready. Reopen Scamazon.";
  if(p.RunSaveSteamId==0)return "Hire Scammers: waiting for your Steam identity and run wallet.";
  if(g.DayZeroTutorialActive)return "Finish the introductory scam tutorial before buying Hire Scammers.";
  if(!UnlockDayReached(g.CurrentDay))return "Hire Scammers unlocks in Week 2 (Day 6). Current day: "+g.CurrentDay+".";
  int count=PersonalCount(p);
  if(count<Plugin.Required.Value)return "Hire Scammers requires "+Plugin.Required.Value+" personal scam calls. Your progress: "+count+" / "+Plugin.Required.Value+".";
  if(Owns(p,Plugin.Product))return "Hire Scammers is already owned. Open its desktop app to hire a recruit.";
  return null;
 }
 public static bool CanBuy(KolkataOsPlayer p){return PurchaseBlockReason(p)==null;}
 static bool Working(){var g=ScammerGameManager.Instance;return g&&g.ScammerGameStarted&&!g.IsReviewPhase&&!g.WaitingForDayStartDoor&&!g.DayZeroTutorialActive&&g.DayTimeRemaining>0;}
 public static void Request(byte action,string name="",string modelId=null){
  if(!Instance||!NetworkClient.isConnected||!GuestCompatibility.HostAvailable()){Notice="The host needs Hire Scammers to authorize hiring and earnings.";return;}
  NetworkClient.Send(new HireRequest{Action=action,Name=name??"",ModelId=modelId});Notice="Request sent to host…";
 }
 void Handle(NetworkConnectionToClient connection,HireRequest msg){
  if(msg.Action==2){if(!connection.isAuthenticated||msg.Name!=GuestCompatibility.Protocol)return;if(peers.Add(connection.connectionId)){GuestCompatibility.PeerJoined(connection);Reply(connection,"Connected to Hire Scammers host.");}return;}
  if(!peers.Contains(connection.connectionId))return;
  float last;if(requestTimes.TryGetValue(connection.connectionId,out last)&&Time.unscaledTime-last<.75f)return;requestTimes[connection.connectionId]=Time.unscaledTime;
  if(!connection.isAuthenticated||!connection.identity)return;
  var p=connection.identity.GetComponent<KolkataOsPlayer>();var owner=connection.identity.GetComponent<PlayerName>();
  var g=ScammerGameManager.Instance;
  if(!p||!owner||!g||p.RunSaveSteamId==0){Reply(connection,"Hiring is available during an active workday after the tutorial.");return;}
  if(msg.Action==0){
   if(!CanBuy(p)){Reply(connection,PurchaseBlockReason(p));return;}
   long balance;bool ok=g.ServerTryPurchaseRunEntitlement(p,p.RunSaveSteamId,Plugin.Product,"Hire Scammers app",Plugin.LicensePrice.Value,out balance,null);
   Reply(connection,ok?"Hire Scammers purchased with personal money.":"Not enough personal money to buy Hire Scammers.");return;
  }
  if(msg.Action==4){uint workerId;var worker=uint.TryParse(msg.ModelId,out workerId)?workers.Values.FirstOrDefault(b=>b&&b.Owner==owner&&b.netIdentity.netId==workerId):null;ScreenCommand command=null;try{if((msg.Name??"").Length<=8192)command=JsonConvert.DeserializeObject<ScreenCommand>(msg.Name);}catch{}string error=null;bool ok=Owns(p,Plugin.Product)&&ScammerScreens.Handle(worker,command,out error);Reply(connection,ok?"Screen command accepted.":error??"Buy Hire Scammers first.");return;}
  if(msg.Action==3){uint id;Bot bot=null;WorkerSettings settings=null;if(uint.TryParse(msg.ModelId,out id))bot=workers.Values.FirstOrDefault(b=>b&&b.netIdentity.netId==id&&b.Owner==owner);try{if((msg.Name??"").Length<=8192)settings=JsonConvert.DeserializeObject<WorkerSettings>(msg.Name);}catch{}if(!bot||!WorkerSettings.Valid(settings,OfficeFloorProgression.GetFloorIndex(g.CurrentDay)+1)){Reply(connection,"Invalid worker settings or worker not owned by you.");return;}bot.ConfigureWorker(settings);Reply(connection,"Updated "+bot.DisplayName+". Settings last for this lobby session.");return;}
  if(!Working()){Reply(connection,"Recruitment is available during an active workday; your app can be purchased between shifts.");return;}
  if(msg.Action!=1||!Owns(p,Plugin.Product)){Reply(connection,"Buy the Hire Scammers app first.");return;}
  var entries=Entitlements(p).Where(k=>k.StartsWith(WorkerPrefix,StringComparison.Ordinal)).ToArray();
  if(entries.Length>=Plugin.MaxWorkers.Value){Reply(connection,"You have reached your recruitment limit.");return;}
  if(workers.Count>=32){Reply(connection,"The call center has reached its 32-employee limit.");return;}
  string clean=CleanName(msg.Name);int slot=0;while(entries.Any(k=>k.StartsWith(WorkerPrefix+slot+"/",StringComparison.Ordinal)))slot++;
  var point=g.GetDayStartPoint(g.CurrentDay,false);var location=point?point.position:owner.transform.position;
  // Validate a real player visual before charging the wallet.
  if(!OfficeCharacters.Ready){Reply(connection,"Office characters are loading. Try again in a moment.");return;}
  Bot employee=null;
  try{
   string modelId=msg.ModelId;ulong parsedModel;if(!ulong.TryParse(modelId,out parsedModel)||!OfficeCharacters.Choices.Any(c=>c.Id==modelId))modelId=OfficeCharacters.Choose(slot);
   var usedLooks=workers.Values.Where(b=>b).Select(b=>b.ModelId).Concat(entries.Select(k=>k.Split('/')).Where(parts=>parts.Length==4).Select(parts=>parts[3])).ToArray();
   modelId=OfficeCharacters.DistinctModel(modelId,usedLooks,slot);
   employee=Create(owner,slot,clean,location,modelId);
   string entitlement=WorkerPrefix+slot+"/"+Convert.ToBase64String(Encoding.UTF8.GetBytes(clean)).Replace("/","_").Replace("+","-")+"/"+employee.ModelId;long balance;
   if(!g.ServerTryPurchaseRunEntitlement(p,p.RunSaveSteamId,entitlement,"Hire "+clean,Plugin.HirePrice.Value,out balance,null)){
    Remove(employee);Reply(connection,"Not enough personal money to hire "+clean+".");return;
   }
   Reply(connection,"Hired "+employee.DisplayName+" for $"+Plugin.HirePrice.Value+".");
  }catch(Exception e){if(employee)Remove(employee);Debug.LogError("[HireScammers] Recruit creation: "+e);Reply(connection,"Recruit could not spawn; no hiring charge was made.");}
 }
 internal static string CleanName(string name){name=new string((name??"").Where(c=>char.IsLetterOrDigit(c)||c==' '||c=='-'||c=='_').Take(24).ToArray()).Trim();return name.Length==0?"Alex":name;}
 internal static string[] ProductsFor(PlayerName owner){var p=owner?owner.GetComponent<KolkataOsPlayer>():null;return !p?new string[0]:NetworkServer.active?Entitlements(p).ToArray():ScamTypeLibrary.LoadAll().Select(s=>s.appId).ToArray();}
 static IEnumerable<string> Entitlements(KolkataOsPlayer p){
  var dict=Plugin.Get(ScammerGameManager.Instance,"serverRunSaves") as IDictionary;
  if(dict==null||!dict.Contains(p.RunSaveSteamId))return new string[0];
  return ((IEnumerable<string>)Plugin.Get(dict[p.RunSaveSteamId],"RunEntitlements")).ToArray();
 }
 Bot Create(PlayerName owner,int slot,string name,Vector3 pos,string modelId=null){
  var go=Proxy();var bot=go.GetComponent<Bot>();bot.Owner=owner;bot.Slot=slot;bot.RecruitName=name;bot.ModelId=modelId??OfficeCharacters.Choose(slot);bot.Position=pos;bot.LocalId=nextId++;
  // Parenting supplies native GetComponentInParent<KolkataOsPlayer>() to the call
  // pipeline. The bot has its own voice identity; never clone player input scripts.
  go.transform.SetParent(owner.transform,true);go.transform.position=pos;
  try{bot.EnsureVisual();if(go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length==0)throw new InvalidOperationException("Office character model not ready");NetworkServer.Spawn(go,owner.connectionToClient);bot.Vanilla=VanillaEmployee.Create(bot);workers.Add(bot.LocalId,bot);return bot;}catch{Destroy(go);throw;}
 }
 void Reconcile(){
  var g=ScammerGameManager.Instance;
  foreach(var bot in workers.Values.ToArray())if(!bot||!bot.Owner||!g||(!g.ScammerGameStarted&&!g.IsReviewPhase)||!Owns(bot.Owner.GetComponent<KolkataOsPlayer>(),Plugin.Product))Remove(bot);
  if(!g||(!g.ScammerGameStarted&&!g.IsReviewPhase))return;
  foreach(var conn in NetworkServer.connections.Values){
   if(conn==null||!conn.identity||!peers.Contains(conn.connectionId))continue;
   var p=conn.identity.GetComponent<KolkataOsPlayer>();var owner=conn.identity.GetComponent<PlayerName>();if(!p||!owner||!Owns(p,Plugin.Product))continue;
   foreach(string key in Entitlements(p).Where(k=>k.StartsWith(WorkerPrefix,StringComparison.Ordinal)).Take(Plugin.MaxWorkers.Value)){
    string[] parts=key.Split('/');int slot;if((parts.Length!=3&&parts.Length!=4)||!int.TryParse(parts[1],out slot)||workers.Values.Any(b=>b&&b.Owner==owner&&b.Slot==slot))continue;
    string name;try{name=CleanName(Encoding.UTF8.GetString(Convert.FromBase64String(parts[2].Replace("_","/").Replace("-","+"))));}catch{continue;}
    var start=g.GetDayStartPoint(g.CurrentDay,false);if(OfficeCharacters.Ready&&workers.Count<32)Create(owner,slot,name,start?start.position:owner.transform.position,parts.Length==4?parts[3]:OfficeCharacters.Choose(slot));
   }
  }
 }
 void Remove(Bot bot){if(!bot){foreach(var key in workers.Where(pair=>!pair.Value).Select(pair=>pair.Key).ToArray())workers.Remove(key);return;}bot.Release();workers.Remove(bot.LocalId);if(bot.netIdentity&&bot.netIdentity.netId!=0&&NetworkServer.active)NetworkServer.Destroy(bot.gameObject);else Destroy(bot.gameObject);}
 void Reply(NetworkConnectionToClient c,string message){c.Send(new HireState{Json=JsonConvert.SerializeObject(new Packet{Message=message})});}
 void Broadcast(){
  var states=workers.Values.Where(b=>b).Select(b=>b.Snapshot()).ToArray();
  var msg=new HireState{Json=JsonConvert.SerializeObject(new Packet{Workers=states})};
  foreach(var c in NetworkServer.connections.Values)if(c!=null&&c.isReady&&peers.Contains(c.connectionId))c.Send(msg);
 }
 public static string RosterText(){var data=NetworkServer.active&&Instance?Instance.workers.Values.Where(b=>b).Select(b=>b.Snapshot()).ToArray():remote;
  return data.Length==0?"No scammers hired yet.":string.Join("\n",data.Select(s=>s.Name+" — "+s.Status+" | earned $"+s.Earned+(string.IsNullOrEmpty(s.Speech)?"":"\n  "+s.Speech)));}
 public static bool Workday{get{return Working();}}
 internal static EmployeeState[] States{get{return NetworkServer.active?Employees.Select(b=>b.Snapshot()).ToArray():remote;}}
 public static Bot[] Employees{get{return Instance?Instance.workers.Values.Where(b=>b).ToArray():new Bot[0];}}
 public static Bot ForCall(Guid call){return Instance?Instance.workers.Values.FirstOrDefault(b=>b&&b.CurrentCall&&b.CurrentCall.CallGuid==call):null;}
 void Clear(){foreach(var bot in workers.Values.ToArray())Remove(bot);workers.Clear();peers.Clear();requestTimes.Clear();replicas.Clear();}
 void OnDestroy(){Clear();if(server)NetworkServer.UnregisterHandler<HireRequest>();if(client){NetworkClient.UnregisterHandler<HireState>();UnregisterSpawners();}Instance=null;}
}
}


























