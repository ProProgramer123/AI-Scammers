using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace HireScammers {
internal static class EmployeeCallerRequests {
 internal static bool IsServiceMessage(string text){var type=typeof(NetworkedCall).Assembly.GetType("BackendAiMessages");var method=type==null?null:AccessTools.Method(type,"IsServiceMessage");return method!=null&&(bool)method.Invoke(null,new object[]{text??""});}
 internal static IEnumerator CompleteServiceNotice(NetworkedCall call,KolkataPlayerVoice voice,uint token){yield return null;if(call&&voice&&token!=0)call.ServerReportTrackedTtsCompletion(voice,token,true);}
 internal static bool Retryable(Exception error){var service=error as KolkataAiServiceException;return error is TimeoutException||service!=null&&(service.ErrorCode=="AI_UNAVAILABLE"||service.ErrorCode=="AI_TIMEOUT"||service.ErrorCode=="CELERIS_INVALID_OUTPUT_FORMAT");}
 internal static IEnumerator Run(NetworkedCall call,Guid id,byte[] payload){
  var pending=(Dictionary<Guid,CancellationTokenSource>)AccessTools.Field(typeof(NetworkedCall),"edgeClientRequests").GetValue(call);if(pending.ContainsKey(id))yield break;
  using(var lifetime=CancellationTokenSource.CreateLinkedTokenSource(new[]{call.GetCancellationTokenOnDestroy()})){
   pending.Add(id,lifetime);try{
    for(int attempt=0;attempt<2;attempt++){
     Exception failure=null;JObject response=null;UniTask<JObject>.Awaiter awaiter=default(UniTask<JObject>.Awaiter);bool started=false;
     try{var api=KolkataApi.Instance;if(!api)throw new InvalidOperationException("AI service not ready");var body=JObject.Parse(Encoding.UTF8.GetString(payload));awaiter=((UniTask<JObject>)AccessTools.Method(typeof(KolkataApi),"CompleteEdgeLocalAsync").Invoke(api,new object[]{body,lifetime.Token})).GetAwaiter();started=true;}catch(Exception e){failure=e.InnerException??e;}
     if(started){while(!awaiter.IsCompleted&&!lifetime.IsCancellationRequested)yield return null;if(lifetime.IsCancellationRequested)yield break;try{response=awaiter.GetResult();}catch(Exception e){failure=e;}}
     if(!call||lifetime.IsCancellationRequested)yield break;
     if(failure==null&&response!=null){var bytes=Encoding.UTF8.GetBytes(response.ToString(Formatting.None));if(bytes.Length<=262144){AccessTools.Method(typeof(NetworkedCall),"CmdEdgeResponse").Invoke(call,new object[]{id,bytes,null,null});yield break;}failure=new InvalidOperationException("Response too large");}
     string code=(failure as KolkataAiServiceException)?.ErrorCode??"AI_UNAVAILABLE";var bot=call.talker?call.talker.GetComponent<Bot>():null;if(bot)bot.Status="Caller AI error: "+code;
     Debug.LogWarning("[HireScammers] Caller provider failure call="+call.netId+" code="+code+" type="+(failure==null?"EmptyResponse":failure.GetType().Name)+" attempt="+(attempt+1));
     if(attempt==0&&Retryable(failure)){yield return new WaitForSeconds(2);continue;}
     AccessTools.Method(typeof(NetworkedCall),"CmdEdgeResponse").Invoke(call,new object[]{id,null,code,null});yield break;
    }
   }finally{pending.Remove(id);}
  }
 }
}
}
