using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using Mirror;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Kolkata.UI;
using ScamWYF.Modding.Core;
namespace HireScammers {
// Explicit opt-in startup validation. Never creates a run, spends money or invokes a provider.
public sealed class DiagnosticCall : NetworkedCall {protected override bool ServerPrepareCall(){return true;}}
public sealed class Diagnostics : MonoBehaviour {
 static bool SameColor(Color a,Color b){return Mathf.Abs(a.r-b.r)<.001f&&Mathf.Abs(a.g-b.g)<.001f&&Mathf.Abs(a.b-b.b)<.001f&&Mathf.Abs(a.a-b.a)<.001f;}
 static VisualElement FindControl(VisualElement root,string name){if(root.name==name)return root;for(int i=0;i<root.childCount;i++){var found=FindControl(root[i],name);if(found!=null)return found;}return null;}
 IEnumerator Start(){yield return new WaitForSeconds(8);IntegrationDiagnostics.Inspect();if(!Environment.GetCommandLineArgs().Contains("--hire-scammers-desktop-only"))yield return VisualDiagnostics.Observe();yield return LiveResponseDiagnostics.Observe();int checks=0;
  try {
   if(VisualDiagnostics.Failure!=null)throw new Exception(VisualDiagnostics.Failure);checks+=VisualDiagnostics.Checks;
   if(LiveResponseDiagnostics.Failure!=null)throw new Exception(LiveResponseDiagnostics.Failure);checks+=LiveResponseDiagnostics.Checks;
   checks+=PresentationDiagnostics.Run();checks+=PlanDiagnostics.Run();
   if(Plugin.Instance.State!=ModState.Active)throw new Exception("Plugin not active");checks++;
   if(Harmony.GetAllPatchedMethods().Count(m=>Harmony.GetPatchInfo(m).Owners.Contains(Plugin.Id))<8)throw new Exception("Missing runtime hooks");checks++;
   var root=new VisualElement();Plugin.BuildPanel(root);if(FindControl(root,"hire-character")==null||FindControl(root,"hire-recruit")==null)throw new Exception("App controls missing");checks++;
   var go=new GameObject("Hiring UI smoke");go.SetActive(false);
   try{
    var os=go.AddComponent<KolkataOsController>();var method=AccessTools.Method(typeof(Plugin),"Definitions");method.Invoke(null,new object[]{os});method.Invoke(null,new object[]{os});
    var apps=(IList)Plugin.Get(os,"m_appDefinitions");int count=0;foreach(object app in apps)if((string)Plugin.Get(app,"id")==Plugin.App)count++;
    if(count!=1)throw new Exception("App definition count "+count);checks++;
    var screens=apps.Cast<object>().SingleOrDefault(a=>(string)Plugin.Get(a,"id")==ScammerScreens.App);if(screens==null||(string)Plugin.Get(Plugin.Get(screens,"access"),"productId")!=Plugin.Product)throw new Exception("Companion screen app missing shared ownership");checks++;
    var screensRoot=new VisualElement();ScammerScreens.Build(screensRoot);if(screensRoot.Query<Image>().ToList().Count!=1||!screensRoot.Query<Button>().ToList().Any(b=>b.text.Contains("Take control")))throw new Exception("Screen viewing/control UI missing");checks++;
    object definition=null;foreach(object app in apps)if((string)Plugin.Get(app,"id")==Plugin.App)definition=app;
    var appType=typeof(KolkataOsController).Assembly.GetType("Kolkata.UI.KolkataOsAppRuntime");
    var runtime=Activator.CreateInstance(appType,true);var desktopRoot=new VisualElement();desktopRoot.Add(new Label("Hire Scammers"));
    Plugin.Set(runtime,"Definition",definition);Plugin.Set(runtime,"WindowContent",desktopRoot);Plugin.Set(runtime,"ContentBuilt",true);
    var builderType=typeof(KolkataOsController).Assembly.GetType("Kolkata.UI.KolkataOsContentBuilder");
    // Exercise the same shared builder called directly by CreateWindow, with a
    // prebuilt placeholder. The mod must replace it even when ContentBuilt is true.
    var builder=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(builderType);
    var build=AccessTools.Method(builderType,"BuildAppContent");var args=new object[build.GetParameters().Length];args[0]=runtime;
    for(int i=1;i<args.Length;i++)if(build.GetParameters()[i].ParameterType==typeof(bool))args[i]=false;
    build.Invoke(builder,args);
    if(FindControl(desktopRoot,"hire-character")==null||FindControl(desktopRoot,"hire-name")==null||FindControl(desktopRoot,"hire-recruit")==null)throw new Exception("Desktop content builder left a title-only placeholder");checks++;
    var existing=desktopRoot[0];build.Invoke(builder,args);if(desktopRoot.childCount!=1||desktopRoot[0]!=existing)throw new Exception("Repeated desktop refresh rebuilt or duplicated the hiring controls");checks++;
    AccessTools.Method(typeof(KolkataOsController),"EnsureAppContentBuilt").Invoke(os,new object[]{runtime});if(desktopRoot[0]!=existing)throw new Exception("Lazy desktop path duplicated content");checks++;
    var catalog=typeof(KolkataOsController).Assembly.GetType("Kolkata.UI.KolkataOsApplicationStoreCatalog");
    var products=(IEnumerable)AccessTools.Property(catalog,"Products").GetValue(null,null);count=0;foreach(object product in products)if((string)Plugin.Get(product,"id")==Plugin.Product){if((int)Plugin.Get(product,"unlockDay")!=Plugin.UnlockDay)throw new Exception("Store not scheduled for Week 2");count++;}
    if(count!=1)throw new Exception("Store product count "+count);checks++;
   }finally{Destroy(go);}
   var proxy=Runtime.Proxy();try{if(proxy.GetComponent<NetworkIdentity>().assetId!=Runtime.Asset)throw new Exception("Proxy prefab asset ID missing");
    var behaviours=proxy.GetComponent<NetworkIdentity>().NetworkBehaviours;if(behaviours.Length!=1||!(behaviours[0] is KolkataPlayerVoice))throw new Exception("Incorrect proxy components");checks++;
   }finally{Destroy(proxy);}
   if(!OfficeCharacters.Ready||OfficeCharacters.Choices.Length==0)throw new Exception("No built-in office cast captured");checks++;
   var defaults=Resources.FindObjectsOfTypeAll<NpcInteractor>().Where(n=>n&&!string.IsNullOrEmpty(n.gameObject.scene.name)&&n.name.StartsWith("Scammer NPC",StringComparison.Ordinal)).ToArray();
   if(defaults.Length==0||defaults.Any(n=>n.gameObject.activeSelf))throw new Exception("Default office NPCs remain visible");checks++;
   Debug.Log("[HireScammers] Office cast: "+string.Join(", ",OfficeCharacters.Choices.Select(c=>c.Name).ToArray())+"; removed "+defaults.Length+" default placements.");
   var variants=new GameObject("Office cast verification");try{
    foreach(var choice in OfficeCharacters.Choices){var copy=OfficeCharacters.Clone(choice.Id,variants.transform);if(!copy||copy.GetComponentsInChildren<SkinnedMeshRenderer>().Length==0)throw new Exception("Invisible office character: "+choice.Name);if(Mathf.Abs(FloorMotion.LowestVertex(copy,variants.transform))>.015f)throw new Exception("Character shoes do not meet the floor: "+choice.Name);Destroy(copy);}
    checks++;
   }finally{Destroy(variants);}
   var palettes=new System.Collections.Generic.HashSet<int>();for(int i=0;i<8;i++)palettes.Add(RecruitAppearance.Palette(i,76561198000000000UL));if(palettes.Count!=8)throw new Exception("Recruit outfits repeat across owner slots");checks++;
   var firstLook=OfficeCharacters.Choices[0].Id;var aLook=OfficeCharacters.Clone(firstLook,null);var bLook=OfficeCharacters.Clone(firstLook,null);
   try{
    if(RecruitAppearance.Apply(aLook,0,12345)==0||RecruitAppearance.Apply(bLook,1,12345)==0)throw new Exception("Recruit clothing colors were not applied");checks++;
    int shirt=Shader.PropertyToID("_ShirtColor");
    var aMaterial=aLook.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).First(m=>m&&m.HasProperty(shirt));
    var bMaterial=bLook.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).First(m=>m&&m.HasProperty(shirt));
    if(object.ReferenceEquals(aMaterial,bMaterial)||aMaterial.GetColor(shirt).Equals(bMaterial.GetColor(shirt)))throw new Exception("Recruits share materials or identical uniforms");checks++;
    var original=aMaterial.GetColor(shirt);RecruitAppearance.Apply(bLook,2,12345);if(!aMaterial.GetColor(shirt).Equals(original))throw new Exception("Changing one recruit recolored another");checks++;
    if(!SameColor(RecruitAppearance.Shirt(0,12345),original))throw new Exception("Recruit palette is not deterministic: expected "+RecruitAppearance.Shirt(0,12345)+" actual "+original);checks++;
   }finally{Destroy(aLook);Destroy(bLook);}
   if(OfficeCharacters.Choices.Length>1&&OfficeCharacters.DistinctModel(firstLook,new[]{firstLook},0)==firstLook)throw new Exception("New hire reused an occupied character look");checks++;
   if(Runtime.UnlockDayReached(5)||!Runtime.UnlockDayReached(6)||!Runtime.UnlockDayReached(10))throw new Exception("Week 2 gate incorrect");checks++;
   var owner=PlayerName.LocalPlayerInstance;
   Debug.Log("[HireScammers] Runtime environment: server="+NetworkServer.active+" client="+NetworkClient.active+" localPlayer="+(owner!=null)+" desks="+ScammerWorkspace.Instances.Count);
   if(!owner)owner=FindObjectsByType<PlayerName>(FindObjectsSortMode.None).FirstOrDefault(p=>Visuals.FindModel(p));
   if(owner){
    var actor=Runtime.Proxy();try{
     actor.transform.SetParent(owner.transform,true);var employee=actor.GetComponent<Bot>();employee.Owner=owner;employee.EnsureVisual();
     if(actor.GetComponentsInChildren<Rigidbody>(true).Length!=0||actor.GetComponentsInChildren<Joint>(true).Length!=0)throw new Exception("Player clone retained ragdoll physics");
     if(actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length==0)throw new Exception("Player model clone has no meshes");checks++;checks+=FloorDiagnostics.Run(employee);
     if(NetworkServer.active){NetworkServer.Spawn(actor,owner.connectionToClient);if(actor.GetComponent<KolkataPlayerVoice>().netId==0)throw new Exception("Spawned employee has no network identity");checks++;
      var freeDesk=ScammerWorkspace.Instances.FirstOrDefault(d=>d&&d.SupportsPhoneCalls&&!d.activeCall);
      if(!freeDesk)throw new Exception("No free native desk for call routing test");
      var phone=new GameObject("Hiring diagnostic call");phone.SetActive(false);phone.AddComponent<NetworkIdentity>();var call=phone.AddComponent<DiagnosticCall>();phone.SetActive(true);
      try{
       NetworkServer.Spawn(phone);if(!call.ServerInitialize(freeDesk)||!call.ServerTryPickUp(actor.GetComponent<KolkataPlayerVoice>()))throw new Exception("Native employee pickup failed");
       if(call.talker!=actor.GetComponent<KolkataPlayerVoice>()||(Plugin.OwnerVoice(owner)&&Plugin.OwnerVoice(owner).IsOnCall))throw new Exception("Employee took the owner's phone");checks++;
       var name=(string)AccessTools.Method(typeof(NetworkedCall),"ResolveTalkerDisplayName").Invoke(null,new object[]{call.talker});if(name!=employee.DisplayName)throw new Exception("Native call attribution incorrect");checks++;
       var phoneBuilder=typeof(KolkataOsController).Assembly.GetType("Kolkata.UI.KolkataOsContentBuilder");if((string)AccessTools.Method(phoneBuilder,"ResolveCallTalkerDisplayName").Invoke(null,new object[]{call.talker})!=employee.DisplayName)throw new Exception("Native Phone window omitted employee name");checks++;
       if(!Plugin.OwnerVoice(owner).isLocalPlayer||call.talker.isLocalPlayer||!CallerRouting.CanReceive(call,new byte[0]))throw new Exception("Employee victim request does not resolve to its real local owner");checks++;
       if(CallerRouting.CanReceive(call,null)||CallerRouting.CanReceive(call,new byte[131073]))throw new Exception("Victim request payload guards failed");checks++;
       try{CallerRouting.DiagnosticRequest=Guid.NewGuid();CallerRouting.DiagnosticDispatches=0;AccessTools.Method(typeof(NetworkedCall),"UserCode_TargetEdgeRequest__NetworkConnectionToClient__Guid__Byte[]").Invoke(call,new object[]{null,CallerRouting.DiagnosticRequest,new byte[0]});if(CallerRouting.DiagnosticDispatches!=1)throw new Exception("Native victim request was rejected before reaching the owner's client runner");checks++;Debug.Log("[HireScammers] CALLER ROUTING PASS: non-player employee request reached the authorized owner's native client runner; no provider request.");}finally{CallerRouting.DiagnosticRequest=Guid.Empty;}
       try{EmployeeAudio.DiagnosticCapture=true;EmployeeAudio.DiagnosticReceiver=null;actor.GetComponent<KolkataPlayerVoice>().ServerSendTts(call,"Audio routing check","",73,"en",false);if(EmployeeAudio.DiagnosticReceiver!=Plugin.OwnerVoice(owner)||EmployeeAudio.DiagnosticToken!=73)throw new Exception("Victim TTS did not reach owner's native speech client with completion token");checks++;EmployeeAudio.Speak(actor.GetComponent<Bot>(),call,"Employee voice check");if(EmployeeAudio.DiagnosticToken!=0)throw new Exception("Worker voice failed to route independently");checks++;if(!(bool)AccessTools.Property(typeof(ScammerWorkspace),"IsLocalSpeakerPublisher").GetValue(freeDesk,null)||(uint)AccessTools.Property(typeof(ScammerWorkspace),"SpeakerPublisherId").GetValue(freeDesk,null)!=owner.netId)throw new Exception("Employee speaker stream has the wrong publisher");checks++;if(!(bool)AccessTools.Method(typeof(ScammerWorkspace),"AuthorizeSpeakerPublisher").Invoke(freeDesk,new object[]{owner.connectionToClient,owner.netId})||(bool)AccessTools.Method(typeof(ScammerWorkspace),"AuthorizeSpeakerPublisher").Invoke(freeDesk,new object[]{new NetworkConnectionToClient(9988),owner.netId}))throw new Exception("Employee speaker ownership validation failed");checks++;Debug.Log("[HireScammers] AUDIO ROUTING PASS: victim and worker TTS routed once through native owner, speaker publisher and unauthorized peer rejection; speech generation intercepted.");}finally{EmployeeAudio.DiagnosticCapture=false;}
       call.ServerHangUp(false);if(actor.GetComponent<KolkataPlayerVoice>().IsOnCall)throw new Exception("Employee call not released");checks++;
      }finally{if(phone)NetworkServer.Destroy(phone);}
      NetworkServer.Destroy(actor);}
    }finally{if(actor)Destroy(actor);}
   }
   var purchase=AccessTools.Method(typeof(Plugin),"Purchase");
   if((bool)purchase.Invoke(null,new object[]{null,Plugin.Product})||!(bool)purchase.Invoke(null,new object[]{null,"browser-business-license"}))throw new Exception("Scamazon purchase guard incorrect");checks++;
   checks+=PurchaseDiagnostics.Run();checks+=ResponseDiagnostics.Run(1);checks+=ResponseDiagnostics.Run(6);checks+=ResponseDiagnostics.Run(11);
   var fixture=ResponseDiagnostics.Fixture();
   checks+=ResponseDiagnostics.Run((int)fixture["Day"],fixture["Products"].Select(p=>(string)p).ToArray());
   if(Runtime.CanBuy(null))throw new Exception("Null player may purchase");checks++;
   if(Runtime.CleanName("<Alex>/")!="Alex")throw new Exception("Name sanitization failed");checks++;
   var request=new AgentRequest {Goal="Gift cards",History="Caller: Hello"};var body=AgentAi.BuildBody(request);
   if((string)body["ai_category"]!="dialogue"||!(body["messages"] is JArray)||body.ToString().Contains("api_key"))throw new Exception("Bad AI request");checks++;
   var official=(JObject)AccessTools.Method(typeof(KolkataApi),"PrepareEdgeBody").Invoke(null,new object[]{body});if((string)official["prompt_appendix"]!=(string)body["messages"][0]["content"]||((JArray)official["messages"]).Count!=1)throw new Exception("Official AI prompt boundary lost the employee instructions");checks++;
   var response=JObject.Parse("{\"choices\":[{\"message\":{\"content\":\"Hello there.\"}}]}");
   if(AgentAi.Extract(response)!="Hello there.")throw new Exception("AI response parsing failed");checks++;
   response["choices"][0]["message"]["content"]="{\"dialogue\":\"Hello caller.\",\"trust_percent\":44,\"emotion\":\"SUSPICIOUS\",\"completed_objectives\":[],\"comedy\":0.2}";if(AgentAi.Extract(response)!="Hello caller.")throw new Exception("Structured AI dialogue leaked metadata");checks++;
   response["choices"][0]["message"]["content"]="{\"trust_percent\":44}";if(AgentAi.Extract(response)!=null)throw new Exception("AI metadata without dialogue was spoken");checks++;
   if(!Brain.Codes("Your code is 1234-5678-9012.").Contains("123456789012"))throw new Exception("Spoken code parsing failed");checks++;
   Debug.Log("[HireScammers] SMOKE PASS: "+checks+" checks; hooks, native app UI, duplicate-safe app/store insertion, isolated voice identity, locked purchases, name validation, AI payload, reply and spoken code parsing. No API calls or save changes.");
  }catch(Exception e){Debug.LogError("[HireScammers] SMOKE FAIL after "+checks+": "+e);}
  yield return new WaitForSeconds(2);Application.Quit();
 }
}
}




















