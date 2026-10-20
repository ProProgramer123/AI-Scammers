using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Kolkata.UI;
namespace HireScammers {
internal static class PayoutDiagnostics {
 internal static int Run(Bot bot){
  var call=bot.CurrentCall as VictimCall;var scam=bot.SelectedScam;var definition=ConversationScamCatalog.GetByAppId(scam.appId);
  var display=EmployeeDesktop.ForWorker(bot.netIdentity.netId);var wallet=bot.Owner.GetComponent<KolkataOsPlayer>();var manager=ScammerGameManager.Instance;
  var workers=(Dictionary<int,Bot>)Plugin.Get(Runtime.Instance,"workers");var oldSentinels=Plugin.Get(call,"sentinelCallState");var oldSession=Plugin.Get(call,"conversationScams");var oldUnlocked=Plugin.Get(call,"serverUnlockedConversationScams");var oldMoney=Plugin.Get(manager,"moneyPool");
  var sentinels=new SentinelCallState();var session=new ConversationScamSession(new[]{definition},null,null,()=>Time.time,()=>.5);
  int checks=0;workers.Add(-998,bot);
  try{
   Plugin.Set(manager,"moneyPool",0L);Plugin.Set(call,"sentinelCallState",sentinels);Plugin.Set(call,"conversationScams",session);Plugin.Set(call,"serverUnlockedConversationScams",new[]{definition});
   var progress=((IDictionary)Plugin.Get(session,"progress"))[definition.id];var completed=(HashSet<string>)Plugin.Get(progress,"Completed");foreach(var objective in definition.objectives)foreach(var prerequisite in objective.prerequisites)completed.Add(prerequisite);
   string tag=scam.relevantSentinels.Single();sentinels.SeedUnrevealedValue(tag,"645-34-1538");sentinels.EnsureSpokenValue(tag,"en");
   var save=(KolkataOsPlayerRunSave)((IDictionary)Plugin.Get(manager,"serverRunSaves"))[(ulong)Plugin.Get(wallet,"runSaveSteamId")];long spendable=save.SpendableMoney;
   long money=wallet.ScamMoneyUrn;long earned=bot.Earned,day=bot.DayEarned;int personal=wallet.UniqueScammedCallerCount;
   Plugin.Set(display,"verificationDispatching",true);var verify=AccessTools.Method(typeof(EmployeeDesktop),"VerifyEmployee");var controller=Plugin.Get(display,"controller");
   verify.Invoke(null,new[]{controller,scam.appCompletionSource,tag,"645-34-1538"});int payout=display.LastVerification.PayoutAwarded;
   if(payout<=0||display.LastVerification.Status!="scam_completed")throw new Exception("Native payout fixture failed: "+display.LastVerification.Status);checks++;
   if(wallet.ScamMoneyUrn!=money+payout||manager.MoneyPoolLong!=payout||bot.Earned!=earned+payout||bot.DayEarned!=day+payout)throw new Exception("Verified payout missed owner, team or worker earnings");checks++;
   if(save.SpendableMoney!=spendable+payout)throw new Exception("Verified payout missed owner's spendable balance");checks++;
   if(wallet.UniqueScammedCallerCount!=personal)throw new Exception("Worker payout unlocked personal progress");checks++;
   verify.Invoke(null,new[]{controller,scam.appCompletionSource,tag,"645-34-1538"});if(display.LastVerification.PayoutAwarded!=0||wallet.ScamMoneyUrn!=money+payout||manager.MoneyPoolLong!=payout||bot.Earned!=earned+payout)throw new Exception("Repeated verification paid twice");checks++;
   if(!Bot.CompletedScams(bot.PublicCallView()).Contains(scam.id))throw new Exception("Native completed conversation ID did not resolve to selected scam");checks++;
   var snapshots=(IList)AccessTools.Method(typeof(ReviewDaySummaryTracker),"BuildPlayerSnapshots").Invoke(null,null);var row=snapshots.Cast<object>().FirstOrDefault(r=>(string)Plugin.Get(r,"DisplayName")==bot.DisplayName);if(row==null||(int)Plugin.Get(row,"DayEarnings")!=bot.DayEarned)throw new Exception("Native leaderboard snapshot missed credited employee");checks++;
   var board=Resources.FindObjectsOfTypeAll<QuotaBoardController>().First();var rows=(IList)AccessTools.Method(typeof(QuotaBoardController),"BuildPlayerRows").Invoke(board,null);var boardRow=rows.Cast<object>().FirstOrDefault(r=>(string)Plugin.Get(r,"DisplayName")==bot.DisplayName);if(boardRow==null||(int)Plugin.Get(boardRow,"Earnings")!=bot.DayEarned)throw new Exception("Agent board missed credited worker earnings");checks++;
   Debug.Log("[HireScammers] PAYOUT PASS: native verification +$"+payout+", owner/team/worker credit, leaderboard snapshot, canonical completion and repeat submission pays zero. Temporary wallet only.");return checks;
  }finally{Plugin.Set(display,"verificationDispatching",false);workers.Remove(-998);Plugin.Set(call,"sentinelCallState",oldSentinels);Plugin.Set(call,"conversationScams",oldSession);Plugin.Set(call,"serverUnlockedConversationScams",oldUnlocked);Plugin.Set(manager,"moneyPool",oldMoney);session.Dispose();}
 }
}
}
