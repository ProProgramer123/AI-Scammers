using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using Mirror;
using UnityEngine;
namespace HireScammers {
public sealed class DiagnosticVictimCall : VictimCall {
 protected override bool ServerPrepareCall(){return true;}
 protected override void ServerOnCallStateChanged(NetworkedCall.CallState before,NetworkedCall.CallState after){}
}
internal static class ResponseDiagnostics {
 internal static Newtonsoft.Json.Linq.JObject Fixture(){
  string path=System.IO.Path.Combine(Application.dataPath,"../HireScammers/fixtures/current-run.json");if(System.IO.File.Exists(path))return Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(path));
  var products=new Newtonsoft.Json.Linq.JArray();var catalog=typeof(Kolkata.UI.KolkataOsController).Assembly.GetType("Kolkata.UI.KolkataOsApplicationStoreCatalog");foreach(var product in (IEnumerable)AccessTools.Property(catalog,"Products").GetValue(null,null))products.Add((string)Plugin.Get(product,"id"));
  return new Newtonsoft.Json.Linq.JObject{["Day"]=17,["Products"]=products};
 }
 internal static int Run(int day,string[] owned=null){
  var g=ScammerGameManager.Instance;string[] fields={"currentDay","dayZeroTutorialActive","waitingForDayStartDoor","isReviewPhase","scammerGameStarted","dayTimeRemaining"};
  var old=fields.Select(f=>Plugin.Get(g,f)).ToArray();const ulong id=18446744073709550999UL;var saves=(IDictionary)Plugin.Get(g,"serverRunSaves");
  var ownerObject=new GameObject("Temporary worker response owner");ownerObject.SetActive(false);ownerObject.AddComponent<NetworkIdentity>();var owner=ownerObject.AddComponent<PlayerName>();var wallet=ownerObject.AddComponent<KolkataOsPlayer>();ownerObject.SetActive(true);
  GameObject actor=null,phone=null;Transform alteredExit=null;Vector3 originalExit=Vector3.zero;int checks=0;
  try{
   NetworkServer.Spawn(ownerObject);Plugin.Set(g,"currentDay",day);Plugin.Set(g,"dayZeroTutorialActive",false);Plugin.Set(g,"waitingForDayStartDoor",false);Plugin.Set(g,"isReviewPhase",false);Plugin.Set(g,"scammerGameStarted",true);Plugin.Set(g,"dayTimeRemaining",600f);
   var save=new KolkataOsPlayerRunSave();var catalog=typeof(Kolkata.UI.KolkataOsController).Assembly.GetType("Kolkata.UI.KolkataOsApplicationStoreCatalog");
   if(owned!=null)foreach(var product in owned)save.RunEntitlements.Add(product);
   else foreach(var product in (IEnumerable)AccessTools.Property(catalog,"Products").GetValue(null,null))save.RunEntitlements.Add((string)Plugin.Get(product,"id"));
   saves.Add(id,save);wallet.ServerApplyRunSave(new KolkataOsRunSaveSnapshot(id,0,save.RunEntitlements,save.PersonalSpendingEntries,false,false));
   actor=Runtime.Proxy();actor.transform.SetParent(owner.transform,true);var bot=actor.GetComponent<Bot>();bot.Owner=owner;bot.EnsureVisual();NetworkServer.Spawn(actor);bot.ServerTick();
   if(!Runtime.Workday)throw new Exception("Response diagnostic could not activate temporary workday");checks++;
   var start=bot.Position;ScammerWorkspace selected=null;
   foreach(var desk in ScammerWorkspace.Instances){
    if(!desk||!desk.SupportsPhoneCalls||desk.activeCall||!desk.IsOnSameFloor(start)||!desk.IsOnWorkdayFloor(day,false)||!desk.chairInteractable||desk.chairInteractable.IsOccupied)continue;
    var exit=Plugin.Get(desk.chairInteractable,"exitPoint") as Transform;if(!exit||FloorMotion.HorizontalDistanceSquared(start,exit.position)<36)continue;
    if(AccessTools.Method(typeof(Bot),"ChooseScam").Invoke(bot,new object[]{desk})==null)continue;
    if(!(bool)AccessTools.Method(typeof(Bot),"BuildPath").Invoke(bot,new object[]{exit.position}))continue;selected=desk;break;
   }
   if(!selected)throw new Exception("No usable desk and owned scam for response test");checks++;
   alteredExit=Plugin.Get(selected.chairInteractable,"exitPoint") as Transform;originalExit=alteredExit.position;alteredExit.position=originalExit+Vector3.right*100;
   if((bool)AccessTools.Method(typeof(Bot),"BuildPath").Invoke(bot,new object[]{alteredExit.position}))throw new Exception("Invalid fixed chair exit unexpectedly reachable");checks++;
   phone=new GameObject("Temporary no-provider victim call");phone.SetActive(false);phone.AddComponent<NetworkIdentity>();var call=phone.AddComponent<DiagnosticVictimCall>();phone.SetActive(true);NetworkServer.Spawn(phone);
   if(!call.ServerInitialize(selected))throw new Exception("Diagnostic phone did not ring");
   var ringing=(System.Collections.Generic.Dictionary<uint,float>)Plugin.Get(bot,"ringing");ringing[call.netId]=Time.time-Plugin.Grace.Value-1;Plugin.Set(bot,"pathAt",0f);bot.ServerTick();
   if((NetworkedCall)Plugin.Get(bot,"pursuing")!=call)throw new Exception("Recruit did not claim the real ringing desk; status="+bot.Status);checks++;
   bot.ServerTick();if(bot.Status!="Running to a call"||FloorMotion.HorizontalDistanceSquared(start,bot.Position)<=.0001f)throw new Exception("Claimed call did not produce running movement");checks++;
   for(int i=0;i<4000&&!bot.CurrentCall;i++)bot.ServerTick();
   if(bot.CurrentCall!=call||call.talker!=actor.GetComponent<KolkataPlayerVoice>()||selected.workerPlayerVoice!=call.talker)throw new Exception("Recruit did not reach and answer computer; position="+bot.Position+" status="+bot.Status+" block="+bot.LastBlockReason);checks++;
   if(!(bool)Plugin.Get(bot,"sitting"))throw new Exception("Answered recruit did not sit");checks++;
   bot.Release();if(selected.workerPlayerVoice)throw new Exception("Desk reservation not released");checks++;
   Debug.Log("[HireScammers] RESPONSE PASS day="+day+": autonomous claim, run, arrival, native pickup, seated computer assignment and release. No provider call.");
  }finally{
   if(alteredExit)alteredExit.position=originalExit;if(actor){var bot=actor.GetComponent<Bot>();bot.Release();NetworkServer.Destroy(actor);}if(phone)NetworkServer.Destroy(phone);saves.Remove(id);NetworkServer.Destroy(ownerObject);for(int i=0;i<fields.Length;i++)Plugin.Set(g,fields[i],old[i]);
  }
  return checks;
 }
}
}

