using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kolkata.UI;
using Mirror;
using UnityEngine;
namespace HireScammers {
internal static class EmployeeReview {
 internal static void Install(Harmony harmony){
  harmony.Patch(AccessTools.Method(typeof(ReviewDaySummaryTracker),"BuildPlayerSnapshots"),postfix:new HarmonyMethod(typeof(EmployeeReview),"Snapshots"));
  foreach(var method in typeof(ReviewDaySummaryTracker).GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Where(m=>m.Name=="BuildReviewDeck"))harmony.Patch(method,postfix:new HarmonyMethod(typeof(EmployeeReview),"Deck"));
  harmony.Patch(AccessTools.Method(typeof(ReviewWhiteboardController),"NormalizeDeck"),prefix:new HarmonyMethod(typeof(EmployeeReview),"BeforeNormalize"));
 }
 static object Row(Type type,string name,string key,int money,int earned){return Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{name,key,money,earned},null);}
 internal static void Snapshots(object __result){
  if(!NetworkServer.active)return;var rows=__result as IList;if(rows==null)return;var employees=Runtime.Employees;var type=__result.GetType().GetGenericArguments()[0];
  foreach(var group in employees.Where(b=>b.Owner).GroupBy(b=>b.Owner)){
   string ownerKey=group.First().Owner.steamId!=0?group.First().Owner.steamId.ToString():group.First().Owner.playerName.Trim().ToLowerInvariant();
   int total=(int)Math.Min(2147483647,group.Sum(b=>b.DayEarned));
   for(int i=0;i<rows.Count;i++)if((string)Plugin.Get(rows[i],"Key")==ownerKey){var row=rows[i];rows[i]=Row(type,(string)Plugin.Get(row,"DisplayName"),ownerKey,(int)Plugin.Get(row,"CurrentMoney"),Math.Max(0,(int)Plugin.Get(row,"DayEarnings")-total));break;}
   foreach(var bot in group){string key=bot.DisplayName.Trim().ToLowerInvariant();var row=Row(type,bot.DisplayName,key,0,(int)Math.Min(2147483647,bot.DayEarned));int existing=-1;for(int i=0;i<rows.Count;i++)if((string)Plugin.Get(rows[i],"Key")==key){existing=i;break;}if(existing>=0)rows[existing]=row;else rows.Add(row);}
  }
 }
 static void Deck(ReviewSlideDeckDefinition __result){Append(__result);}
 static void BeforeNormalize(ReviewSlideDeckDefinition __0){Append(__0);}
 internal static void Append(ReviewSlideDeckDefinition deck){
  if(!NetworkServer.active||deck==null||Runtime.Employees.Length==0)return;
  if((deck.slides??new ReviewSlideDefinition[0]).Any(s=>s!=null&&s.id=="hire-scammers-employees"))return;
  var rows=Runtime.Employees.OrderByDescending(b=>b.DayEarned).Select(b=>new[]{b.DisplayName,"$"+b.DayEarned,"$"+b.Earned}).ToArray();
  var slide=new ReviewSlideDefinition{id="hire-scammers-employees",layout="ranking",title="Hired Scammers",subtitle="Employee earnings are paid into their owner's wallet.",durationSeconds=12,table=new ReviewTableDefinition{headers=new[]{"Employee","Today","Total"},rows=rows,highlightRow=-1},narration="Today's hired employee results."};
  deck.slides=(deck.slides??new ReviewSlideDefinition[0]).Concat(new[]{slide}).ToArray();
 }
 internal static bool Place(Bot bot){
  var manager=ScammerGameManager.Instance;var point=manager?Plugin.Get(manager,"reviewRoomPoint") as Transform:null;if(!point)return false;
  int index=Array.IndexOf(Runtime.Employees.OrderBy(b=>b.LocalId).ToArray(),bot);index=Math.Max(0,index);
  for(int attempt=0;attempt<12;attempt++){
   var candidate=point.position+point.right*((index%6-2.5f)*.85f)+point.forward*(1.8f+(index/6)*.9f+attempt*.15f);Vector3 floor;
   if(!FloorMotion.TryGround(candidate,out floor)||!FloorMotion.Clear(floor))continue;
   bot.Position=floor;bot.transform.position=floor;bot.transform.rotation=point.rotation;return true;
  }return false;
 }
}
}

