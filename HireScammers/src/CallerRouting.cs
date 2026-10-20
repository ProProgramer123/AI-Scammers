using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Mirror;
namespace HireScammers {
internal static class CallerRouting {
 internal static Guid DiagnosticRequest;internal static int DiagnosticDispatches;
 internal static void Install(Harmony harmony){
  harmony.Patch(AccessTools.Method(typeof(NetworkedCall),"UserCode_TargetEdgeRequest__NetworkConnectionToClient__Guid__Byte[]"),prefix:new HarmonyMethod(typeof(CallerRouting),"Receive"));
  harmony.Patch(AccessTools.Method(typeof(NetworkedCall),"RunEdgeClientRequestAsync"),prefix:new HarmonyMethod(typeof(CallerRouting),"DiagnosticIntercept"));
  EmployeeAudio.Install(harmony);
  harmony.Patch(AccessTools.Method(typeof(NetworkedCall),"UserCode_CmdEdgeResponse__Guid__Byte[]__String__NetworkConnectionToClient"),prefix:new HarmonyMethod(typeof(CallerRouting),"ReportFailure"));
 }
 static void ReportFailure(NetworkedCall __instance,Guid __0,byte[] __1,string __2,NetworkConnectionToClient __3){if(string.IsNullOrEmpty(__2)||!__instance.talker)return;var bot=__instance.talker.GetComponent<Bot>();var pending=AccessTools.Field(typeof(NetworkedCall),"edgePending").GetValue(__instance) as System.Collections.IDictionary;if(!bot||!bot.Owner||__3==null||__3!=bot.Owner.connectionToClient||pending==null||!pending.Contains(__0))return;bot.CallerFailureCode=__2;bot.CallerRetryAfter=UnityEngine.Time.time+30;UnityEngine.Debug.LogWarning("[HireScammers] Caller request rejected: worker="+bot.DisplayName+" code="+__2);}
 internal static bool CanReceive(NetworkedCall call,byte[] payload){
  var bot=call&&call.talker?call.talker.GetComponent<Bot>():null;var owner=bot&&bot.Owner?Plugin.OwnerVoice(bot.Owner):null;
  var pending=call?AccessTools.Field(typeof(NetworkedCall),"edgeClientRequests").GetValue(call) as Dictionary<Guid,System.Threading.CancellationTokenSource>:null;
  return bot&&owner&&owner.isLocalPlayer&&call.callState==(NetworkedCall.CallState)1&&payload!=null&&payload.Length<=131072&&pending!=null&&pending.Count<4;
 }
 static bool Receive(NetworkedCall __instance,Guid __1,byte[] __2){
  if(!__instance.talker||!__instance.talker.GetComponent<Bot>())return true;
  if(!CanReceive(__instance,__2)){AccessTools.Method(typeof(NetworkedCall),"CmdEdgeResponse").Invoke(__instance,new object[]{__1,null,"CALL_UNAVAILABLE",null});return false;}
  // Keep native transport, cancellation, response validation and provider settings.
  // Only the employee owner's actual local player may execute its victim request.
  ((UniTaskVoid)AccessTools.Method(typeof(NetworkedCall),"RunEdgeClientRequestAsync").Invoke(__instance,new object[]{__1,__2})).Forget();return false;
 }
 static bool DiagnosticIntercept(NetworkedCall __instance,Guid __0,byte[] __1,ref UniTaskVoid __result){if(DiagnosticRequest!=Guid.Empty&&__0==DiagnosticRequest){DiagnosticDispatches++;__result=default(UniTaskVoid);return false;}if(!__instance.talker||!__instance.talker.GetComponent<Bot>())return true;Runtime.Instance.StartCoroutine(EmployeeCallerRequests.Run(__instance,__0,__1));__result=default(UniTaskVoid);return false;}
}
}
