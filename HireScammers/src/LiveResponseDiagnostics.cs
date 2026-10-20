using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using Mirror;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace HireScammers {
internal static class LiveResponseDiagnostics {
 internal static string Failure;internal static int Checks;
 internal static IEnumerator Observe(){
  Failure=null;Checks=0;
  var g=ScammerGameManager.Instance;string[] fields={"currentDay","dayZeroTutorialActive","waitingForDayStartDoor","isReviewPhase","scammerGameStarted","dayTimeRemaining"};var old=fields.Select(f=>Plugin.Get(g,f)).ToArray();
  const ulong id=18446744073709550998UL;var saves=(IDictionary)Plugin.Get(g,"serverRunSaves");GameObject ownerObject=null,actor=null,phone=null;Bot bot=null;DiagnosticVictimCall call=null;ScammerWorkspace selected=null;Vector3 start=Vector3.zero,originalExit=Vector3.zero;Transform alteredExit=null;
  try{
   try{
    var fixture=ResponseDiagnostics.Fixture();int day=(int)fixture["Day"];
    ownerObject=new GameObject("Temporary live-response owner");ownerObject.SetActive(false);ownerObject.AddComponent<NetworkIdentity>();var owner=ownerObject.AddComponent<PlayerName>();Plugin.Set(owner,"playerName","unknown");var wallet=ownerObject.AddComponent<KolkataOsPlayer>();ownerObject.SetActive(true);NetworkServer.Spawn(ownerObject);
    Plugin.Set(g,"currentDay",day);Plugin.Set(g,"dayZeroTutorialActive",false);Plugin.Set(g,"waitingForDayStartDoor",false);Plugin.Set(g,"isReviewPhase",false);Plugin.Set(g,"scammerGameStarted",true);Plugin.Set(g,"dayTimeRemaining",600f);
    var save=new KolkataOsPlayerRunSave();foreach(var product in fixture["Products"])save.RunEntitlements.Add((string)product);saves.Add(id,save);wallet.ServerApplyRunSave(new KolkataOsRunSaveSnapshot(id,0,save.RunEntitlements,save.PersonalSpendingEntries,false,false));
    actor=Runtime.Proxy();actor.transform.SetParent(owner.transform,true);bot=actor.GetComponent<Bot>();bot.Owner=owner;bot.RecruitName="Raj";bot.ModelId=OfficeCharacters.Choose(0);bot.EnsureVisual();NetworkServer.Spawn(actor);bot.ServerTick();start=bot.Position;bot.Vanilla=VanillaEmployee.Create(bot);
    var nativeIdentity=bot.Vanilla.GetComponent<NetworkIdentity>();if(nativeIdentity.assetId!=3921000073U||nativeIdentity.NetworkBehaviours.Length!=25||nativeIdentity.GetComponent<Bot>())throw new Exception("Vanilla NPC prefab layout changed");Checks++;
    var vanillaConnection=new NetworkConnectionToClient(98765);if(GuestCompatibility.Allows(actor.GetComponent<NetworkIdentity>(),vanillaConnection)||!GuestCompatibility.Allows(nativeIdentity,vanillaConnection))throw new Exception("Vanilla observer isolation failed");Checks+=2;
    foreach(var desk in ScammerWorkspace.Instances.OrderBy(d=>d?(d.transform.position-start).sqrMagnitude:1e20f)){
     if(!desk||!desk.SupportsPhoneCalls||desk.activeCall||!desk.IsOnSameFloor(start)||!desk.IsOnWorkdayFloor(day,false)||!desk.chairInteractable||desk.chairInteractable.IsOccupied)continue;
     var exit=Plugin.Get(desk.chairInteractable,"exitPoint") as Transform;if(!exit||FloorMotion.HorizontalDistanceSquared(start,exit.position)<16)continue;
     if(AccessTools.Method(typeof(Bot),"ChooseScam").Invoke(bot,new object[]{desk})==null)continue;
     if(!(bool)AccessTools.Method(typeof(Bot),"BuildPath").Invoke(bot,new object[]{exit.position}))continue;selected=desk;break;
    }
    if(!selected)throw new Exception("No eligible live response desk");
    alteredExit=Plugin.Get(selected.chairInteractable,"exitPoint") as Transform;originalExit=alteredExit.position;alteredExit.position+=Vector3.right*100;
    phone=new GameObject("Temporary live no-provider victim call");phone.SetActive(false);phone.AddComponent<NetworkIdentity>();call=phone.AddComponent<DiagnosticVictimCall>();phone.SetActive(true);NetworkServer.Spawn(phone);if(!call.ServerInitialize(selected))throw new Exception("Live phone did not ring");
    var portraitCaller=CallerIdentityLibrary.LoadAll().First(c=>c.useWebcam&&!string.IsNullOrEmpty(c.webcamImage)&&!c.id.StartsWith("workshop:",StringComparison.Ordinal));Plugin.Set(call,"callerIdentity",portraitCaller);
   }catch(Exception error){Failure=error.ToString();}
   float deadline=Time.time+32;string previous=null;
   while(Failure==null&&Time.time<deadline&&bot&&!bot.CurrentCall){
    yield return null;
    try{bot.ServerTick();if(previous!=bot.Status){Debug.Log("[HireScammers] LIVE RESPONSE status="+bot.Status+" position="+bot.Position+" block="+bot.LastBlockReason);previous=bot.Status;}}
    catch(Exception error){Failure=error.ToString();}
   }
   if(Failure==null){
    if(!bot.CurrentCall||bot.CurrentCall!=call)Failure="Live frame response did not answer: "+bot.Status+" position="+bot.Position+" block="+bot.LastBlockReason;
    else{
     Checks++;AccessTools.Method(typeof(VictimCall),"RefreshPublicUiState").Invoke(call,null);var selectedScam=bot.EligibleScams().First();string problem;
     if(bot.ApplyPlan(new EmployeePlan{scam_id="unowned-diagnostic-scam"},out problem))Failure="Unowned scam was accepted";else Checks++;
     if(!bot.ApplyPlan(new EmployeePlan{scam_id=selectedScam.id},out problem))Failure="AI choice rejected: "+problem;
     float openDeadline=Time.time+10;while(Failure==null&&Time.time<openDeadline&&bot.DesktopBusy)yield return null;
     if(Failure==null){try{
      if(bot.DesktopBusy||bot.DesktopApp!=selectedScam.appId)throw new Exception("Native AI-selected desktop did not open: "+bot.Status);
      Checks++;if(selected.workerPlayerVoice!=actor.GetComponent<KolkataPlayerVoice>())throw new Exception("Native updates cleared the worker binding");Checks++;
      var display=Resources.FindObjectsOfTypeAll<EmployeeDesktop>().FirstOrDefault(d=>d.Ready);if(!display)throw new Exception("Native employee desktop not ready");
      if(!display.PhoneVisible)throw new Exception("Employee Phone window did not open during normal runtime");Checks++;
      Debug.Log("[HireScammers] DESKTOP VIEW "+display.View());
      display.Capture(System.IO.Path.Combine(Application.dataPath,"../HireScammers/desktop-preview.png"));Checks++;
     }catch(Exception error){Failure=error.ToString();}}
     if(Failure==null){var exercise=DesktopDiagnostics.Exercise(bot);while(true){bool more;try{more=exercise.MoveNext();}catch(Exception error){Failure=error.ToString();break;}if(!more)break;yield return exercise.Current;}Checks+=DesktopDiagnostics.Checks;}
     if(Failure==null){try{Checks+=PayoutDiagnostics.Run(bot);}catch(Exception error){Failure=error.ToString();}}
     if(Failure==null){try{var guid=call.CallGuid;var completedScam=bot.SelectedScam;var another=bot.EligibleScams().First(s=>s.id!=completedScam.id);string oldState=call.PublicUiStateJson;try{Plugin.Set(call,"publicUiStateJson",new JObject{["conversationScams"]=new JArray(new JObject{["id"]=ConversationScamCatalog.GetByAppId(completedScam.appId).id,["completed"]=true})}.ToString());AccessTools.Method(typeof(Bot),"RefreshCompletedScams").Invoke(bot,null);if(bot.SelectedScam!=null)throw new Exception("Completed scam did not advance selection");string error;if(!bot.ApplyPlan(new EmployeePlan{scam_id=another.id},out error)||call.CallGuid!=guid||bot.CurrentCall!=call||call.callState!=(NetworkedCall.CallState)1)throw new Exception("Second scam could not start inside the same active call: "+error);Checks+=2;Debug.Log("[HireScammers] MULTI SCAM PASS: public completion fixture advances to another owned scam without replacing the active call; no payment awarded.");}finally{Plugin.Set(call,"publicUiStateJson",oldState);}}catch(Exception error){Failure=error.ToString();}}
     if(Failure==null)Debug.Log("[HireScammers] LIVE RESPONSE PASS: native pickup, explicit AI scam selection, native app launch and real UI input/click callbacks.");
    }
   }
  }finally{
   if(alteredExit)alteredExit.position=originalExit;if(actor){bot.Release();NetworkServer.Destroy(actor);}if(phone)NetworkServer.Destroy(phone);saves.Remove(id);if(ownerObject)NetworkServer.Destroy(ownerObject);for(int i=0;i<fields.Length;i++)Plugin.Set(g,fields[i],old[i]);
  }
 }
}
}


