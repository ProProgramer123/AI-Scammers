using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using Mirror;
namespace HireScammers {
internal static class SpeechPriority {
 internal static void Install(Harmony harmony){harmony.Patch(AccessTools.Method(typeof(PocketTtsSingleton),"EnqueueSpeak"),prefix:new HarmonyMethod(typeof(SpeechPriority),"BeforeSpeak"),postfix:new HarmonyMethod(typeof(SpeechPriority),"AfterSpeak"));}
 static NetworkedCall Resolve(uint id){NetworkIdentity identity;if(id==0)return null;if(!NetworkClient.spawned.TryGetValue(id,out identity)&&(!NetworkServer.active||!NetworkServer.spawned.TryGetValue(id,out identity)))return null;return identity?identity.GetComponent<NetworkedCall>():null;}
 internal static bool IsWorkerCall(uint id){var call=Resolve(id);return call&&call.talker&&call.talker.GetComponent<Bot>();}
 internal static bool IsHumanCall(uint id){var call=Resolve(id);return call&&call.talker&&!call.talker.GetComponent<Bot>();}
 static void BeforeSpeak(PocketTtsSingleton __instance,uint __2){if(!IsHumanCall(__2))return;uint processing=(uint)Plugin.Get(__instance,"processingCallNetId");if(IsWorkerCall(processing))PocketTtsSingleton.CancelCall(processing);}
 static void AfterSpeak(PocketTtsSingleton __instance){Prioritize(Plugin.Get(__instance,"pendingCallRequests"));}
 internal static void Prioritize(object queue){var items=((IEnumerable)queue).Cast<object>().ToArray();if(items.Length<2)return;var sorted=items.OrderBy(item=>IsWorkerCall((uint)Plugin.Get(item,"CallNetId"))?1:0).ToArray();if(items.SequenceEqual(sorted))return;AccessTools.Method(queue.GetType(),"Clear").Invoke(queue,null);var enqueue=AccessTools.Method(queue.GetType(),"Enqueue");foreach(var item in sorted)enqueue.Invoke(queue,new[]{item});}
}
}
