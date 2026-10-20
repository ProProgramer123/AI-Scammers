using System;
using HarmonyLib;
using Mirror;
namespace HireScammers {
internal static class EmployeeAudio {
 internal static bool DiagnosticCapture;internal static KolkataPlayerVoice DiagnosticReceiver;internal static uint DiagnosticToken;
 internal static KolkataPlayerVoice OwnerVoice(NetworkedCall call){var bot=call&&call.talker?call.talker.GetComponent<Bot>():null;return bot&&bot.Owner?Plugin.OwnerVoice(bot.Owner):null;}
 internal static void Install(Harmony harmony){
  harmony.Patch(AccessTools.Method(typeof(KolkataPlayerVoice),"ServerSendTts"),prefix:new HarmonyMethod(typeof(EmployeeAudio),"RouteTts"));
  harmony.Patch(AccessTools.Method(typeof(ScammerWorkspace),"get_IsLocalSpeakerPublisher"),prefix:new HarmonyMethod(typeof(EmployeeAudio),"LocalPublisher"));
  harmony.Patch(AccessTools.Method(typeof(ScammerWorkspace),"get_SpeakerPublisherId"),prefix:new HarmonyMethod(typeof(EmployeeAudio),"PublisherId"));
  harmony.Patch(AccessTools.Method(typeof(ScammerWorkspace),"AuthorizeSpeakerPublisher"),prefix:new HarmonyMethod(typeof(EmployeeAudio),"Authorize"));
  harmony.Patch(AccessTools.Method(typeof(NetworkedCall),"ServerCanAcceptTrackedTtsCompletion"),prefix:new HarmonyMethod(typeof(EmployeeAudio),"Completion"));
 }
 static bool RouteTts(KolkataPlayerVoice __instance,NetworkedCall __0,string __1,string __2,uint __3,string __4,bool __5){
  if(__instance.GetComponent<Bot>()&&EmployeeCallerRequests.IsServiceMessage(__1)){var bot=__instance.GetComponent<Bot>();bot.CallerFailureCode=bot.CallerFailureCode??"AI_UNAVAILABLE";bot.CallerRetryAfter=UnityEngine.Time.time+30;bot.Status="Caller AI error: "+bot.CallerFailureCode;Runtime.Instance.StartCoroutine(EmployeeCallerRequests.CompleteServiceNotice(__0,__instance,__3));return false;}
  if(DiagnosticCapture&&!__instance.GetComponent<Bot>()){DiagnosticReceiver=__instance;DiagnosticToken=__3;return false;}
  var owner=OwnerVoice(__0);if(!owner||!__instance.GetComponent<Bot>())return true;
  // Native generation runs once on the hiring player's client. The desk's native
  // Opus stream distributes both voices to nearby players, including vanilla guests.
  owner.ServerSendTts(__0,__1,__2,__3,__4,__5);return false;
 }
 static bool LocalPublisher(ScammerWorkspace __instance,ref bool __result){var owner=OwnerVoice(__instance.activeCall);if(!owner)return true;__result=owner.isLocalPlayer;return false;}
 static bool PublisherId(ScammerWorkspace __instance,ref uint __result){var owner=OwnerVoice(__instance.activeCall);if(!owner)return true;__result=owner.netId;return false;}
 static bool Authorize(ScammerWorkspace __instance,NetworkConnectionToClient __0,uint __1,ref bool __result){var owner=OwnerVoice(__instance.activeCall);if(!owner)return true;__result=__0!=null&&__0==owner.connectionToClient;return false;}
 static bool Completion(NetworkedCall __instance,KolkataPlayerVoice __0,ref bool __result){var owner=OwnerVoice(__instance);if(!owner)return true;__result=__0==owner;return false;}
 internal static void Speak(Bot bot,NetworkedCall call,string text){if(!NetworkServer.active||!bot||bot.Settings.Muted||!call||string.IsNullOrWhiteSpace(text))return;var voice=bot.GetComponent<KolkataPlayerVoice>();if(voice)voice.ServerSendTts(call,text,bot.Settings.Voice=="Alternative"?"kolkata-voices/Connor/wei_kevin_chen.wav": "kolkata-voices/Connor/dorothy_mayfield.wav",0,"en",false);}
}
}
