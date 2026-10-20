using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using Kolkata.UI;
using Mirror;
namespace HireScammers {
internal static class EmployeeBoard {
 internal static void Install(Harmony harmony){harmony.Patch(AccessTools.Method(typeof(QuotaBoardController),"BuildPlayerRows"),postfix:new HarmonyMethod(typeof(EmployeeBoard),"Rows"));}
 internal static void Rows(object __result){
  var rows=__result as IList;if(rows==null)return;var type=__result.GetType().GetGenericArguments()[0];var employees=Runtime.States;
  foreach(var employee in employees){
   var row=Activator.CreateInstance(type,true);Plugin.Set(row,"DisplayName",employee.Name);Plugin.Set(row,"Earnings",(int)Math.Min(2147483647L,employee.DayEarned));rows.Add(row);
   for(int i=0;i<rows.Count-1;i++){var owner=Plugin.Get(rows[i],"Player") as PlayerName;if(owner&&owner.netId==employee.Owner){var original=(int)Plugin.Get(rows[i],"Earnings");Plugin.Set(rows[i],"Earnings",Math.Max(0,original-(int)Math.Min(2147483647L,employee.DayEarned)));}}
  }
  var sorted=rows.Cast<object>().OrderByDescending(r=>(int)Plugin.Get(r,"Earnings")).ThenBy(r=>(string)Plugin.Get(r,"DisplayName"),StringComparer.Ordinal).ToArray();
  rows.Clear();int rank=0,prior=-1;for(int i=0;i<sorted.Length;i++){var earned=(int)Plugin.Get(sorted[i],"Earnings");if(i==0||earned!=prior)rank=i+1;prior=earned;Plugin.Set(sorted[i],"Rank",rank);Plugin.Set(sorted[i],"TotalPlayers",sorted.Length);rows.Add(sorted[i]);}
 }
}
}
