using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Mirror;
using UnityEngine;
namespace HireScammers {
// Opt-in diagnostics only: a temporary in-memory wallet, no real player's state or saves.
public static class PurchaseDiagnostics {
 static KolkataOsPlayer player;static bool success;static string message;
 static void Transaction(KolkataOsPlayer __instance,bool __1,string __4){if(__instance==player){success=__1;message=__4;}}
 static void Require(bool ok,string why){if(!ok)throw new Exception("Purchase test: "+why);}
 public static int Run(){
  var g=ScammerGameManager.Instance;Require(g&&NetworkServer.active,"requires a local server");
  var saves=(IDictionary)Plugin.Get(g,"serverRunSaves");const ulong id=18446744073709551000UL;Require(!saves.Contains(id),"test ID collision");
  string[] fields={"currentDay","dayZeroTutorialActive","waitingForDayStartDoor","isReviewPhase","scammerGameStarted","hostGameMode","nextLedgerSequence"};var old=new object[fields.Length];for(int i=0;i<fields.Length;i++)old[i]=Plugin.Get(g,fields[i]);
  var patch=new Harmony("hirescammers.purchase.diagnostic");var go=new GameObject("Temporary purchase diagnostic wallet");go.SetActive(false);go.AddComponent<NetworkIdentity>();player=go.AddComponent<KolkataOsPlayer>();go.SetActive(true);
  int checks=0;
  try{
   NetworkServer.Spawn(go);patch.Patch(AccessTools.Method(typeof(KolkataOsPlayer),"SendApplicationTransaction"),prefix:new HarmonyMethod(typeof(PurchaseDiagnostics),"Transaction"));
   Plugin.Set(g,"currentDay",6);Plugin.Set(g,"dayZeroTutorialActive",false);Plugin.Set(g,"waitingForDayStartDoor",true);Plugin.Set(g,"isReviewPhase",true);Plugin.Set(g,"scammerGameStarted",false);Plugin.Set(g,"hostGameMode",(HostGameMode)0);
   var save=new KolkataOsPlayerRunSave();save.SpendableMoney=Plugin.LicensePrice.Value+1000;save.RunEntitlements.Add("scam-gift-card");saves.Add(id,save);
   Action apply=()=>player.ServerApplyRunSave(new KolkataOsRunSaveSnapshot(id,save.SpendableMoney,save.RunEntitlements,save.PersonalSpendingEntries,false,false));
   var purchase=AccessTools.Method(typeof(KolkataOsPlayer),"UserCode_CmdPurchaseApplication__String");
   Action buy=()=>{success=false;message=null;purchase.Invoke(player,new object[]{Plugin.Product});};
   Plugin.Set(player,"uniqueScammedCallerCount",Plugin.Required.Value);apply();
   Require(Runtime.CanBuy(player),"between-shift shopping is still blocked");checks++;
   long before=save.SpendableMoney;buy();Require(success&&save.RunEntitlements.Contains(Plugin.Product)&&save.SpendableMoney<before,"native purchase failed or did not debit personal wallet: "+message);checks++;
   Require(Runtime.Owns(player,Plugin.Product),"server does not recognize a remote player's purchase");checks++;
   long paid=save.SpendableMoney;buy();Require(!success&&message!=null&&save.SpendableMoney==paid,"repeat purchase lacks feedback or charges twice");checks++;
   save.RunEntitlements.Remove(Plugin.Product);Plugin.Set(player,"uniqueScammedCallerCount",0);apply();buy();Require(!success&&message!=null&&message.Contains("personal scam")&&save.SpendableMoney==paid,"personal-progress rejection lacks feedback");checks++;
   Plugin.Set(player,"uniqueScammedCallerCount",Plugin.Required.Value);Plugin.Set(g,"currentDay",5);buy();Require(!success&&message!=null&&message.Contains("Week 2"),"early purchase lacks unlock feedback");checks++;
   Plugin.Set(g,"currentDay",6);save.SpendableMoney=0;apply();buy();Require(!success&&message!=null&&!save.RunEntitlements.Contains(Plugin.Product),"insufficient-money failure is missing");checks++;
   save.SpendableMoney=Plugin.LicensePrice.Value+1000;apply();Plugin.Set(player,"uniqueScammedCallerCount",Plugin.Required.Value-1);
   Runtime.RecordPersonalScam(player,Guid.NewGuid());Plugin.Set(player,"uniqueScammedCallerCount",0);
   Require(Runtime.PersonalCount(player)>=Plugin.Required.Value,"earned unlock progress was lost after resetting session count");checks++;
   before=save.SpendableMoney;buy();Require(success&&save.RunEntitlements.Contains(Plugin.Product)&&save.SpendableMoney<before,"purchase with persisted progress failed: "+message);checks++;
   Debug.Log("[HireScammers] PURCHASE PASS: "+checks+" checks; real native purchase and personal wallet debit, between-shift shopping, duplicate-charge prevention, failure feedback, remote ownership and persisted scam progress. Temporary in-memory wallet only.");
  }finally{
   patch.UnpatchSelf();saves.Remove(id);for(int i=0;i<fields.Length;i++)Plugin.Set(g,fields[i],old[i]);if(go)NetworkServer.Destroy(go);player=null;
  }
  return checks;
 }
}
}
