using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using Mirror;
using UnityEngine;
namespace HireScammers {
internal static class SpeechPriorityDiagnostics {
 internal static int Run(NetworkedCall employeeCall,PlayerName owner){
  var go=new GameObject("Temporary human speech priority call");go.SetActive(false);go.AddComponent<NetworkIdentity>();var human=go.AddComponent<DiagnosticCall>();go.SetActive(true);
  try{
   NetworkServer.Spawn(go);Plugin.Set(human,"talker",Plugin.OwnerVoice(owner));
   if(!SpeechPriority.IsWorkerCall(employeeCall.netId)||SpeechPriority.IsHumanCall(employeeCall.netId)||SpeechPriority.IsWorkerCall(human.netId)||!SpeechPriority.IsHumanCall(human.netId))throw new Exception("Speech priority confused call identities");
   var queueType=AccessTools.Field(typeof(PocketTtsSingleton),"pendingCallRequests").FieldType;var requestType=queueType.GetGenericArguments()[0];var queue=Activator.CreateInstance(queueType);var enqueue=AccessTools.Method(queueType,"Enqueue");
   Func<uint,string,object> request=(id,text)=>{var item=Activator.CreateInstance(requestType,true);Plugin.Set(item,"CallNetId",id);Plugin.Set(item,"Text",text);return item;};
   var w1=request(employeeCall.netId,"worker first");var h1=request(human.netId,"human first");var w2=request(employeeCall.netId,"worker second");var h2=request(human.netId,"human second");
   foreach(var item in new[]{w1,h1,w2,h2})enqueue.Invoke(queue,new[]{item});SpeechPriority.Prioritize(queue);if(!((IEnumerable)queue).Cast<object>().SequenceEqual(new[]{h1,h2,w1,w2}))throw new Exception("Human speech did not take queue priority");
   SpeechPriority.Prioritize(queue);if(!((IEnumerable)queue).Cast<object>().SequenceEqual(new[]{h1,h2,w1,w2}))throw new Exception("Repeated scheduling changed call order");
   if(!(bool)AccessTools.Method(typeof(CallerRouting),"Receive").Invoke(null,new object[]{human,Guid.NewGuid(),null}))throw new Exception("Human request entered employee provider routing");
   if(EmployeeAudio.OwnerVoice(human)!=null)throw new Exception("Human audio entered employee routing");
   Debug.Log("[HireScammers] HUMAN CALL PASS: provider/audio passthrough, native speech queue priority and stable ordering. No provider or speech generation.");return 5;
  }finally{NetworkServer.Destroy(go);}
 }
}
}
