using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kolkata.UI;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace HireScammers {
public sealed partial class EmployeeDesktop {
 bool runningAction,verificationDispatching;int observedRevision=-1;internal int VerificationAttempts;internal ScamFieldVerificationResult LastVerification;
 uint visiblePhoneCall;string arrangedScam;
 internal bool PhoneVisible{get{var phone=App("phone");return phone!=null&&Convert.ToInt32(Plugin.Get(phone,"State"))==1;}}
 void MaintainPhoneWindow(){
  var call=workspace.activeCall as VictimCall;if(!call||document.rootVisualElement.layout.width<1)return;
  if(visiblePhoneCall!=call.netId){controller.BindVictimCall(call);var phone=App("phone");if(phone==null)return;AccessTools.Method(typeof(KolkataOsController),"OnDesktopIconClicked").Invoke(controller,new[]{phone});var window=Plugin.Get(phone,"Window") as VisualElement;if(window!=null){window.style.left=20;window.style.top=20;window.style.width=860;window.style.height=Mathf.Min(800,document.rootVisualElement.layout.height-136);}visiblePhoneCall=call.netId;}
  if(!string.IsNullOrEmpty(opened)&&arrangedScam!=opened){var app=App(opened);var window=app==null?null:Plugin.Get(app,"Window") as VisualElement;if(window!=null){window.style.width=960;window.style.minWidth=480;window.style.left=900;window.style.top=20;}arrangedScam=opened;}
 }
 object App(string id){var apps=Plugin.Get(controller,"m_appsById") as IDictionary;return apps!=null&&id!=null&&apps.Contains(id)?apps[id]:null;}
 VisualElement ContentRoot(){var app=App(opened);return app==null?null:Plugin.Get(app,"WindowContent") as VisualElement;}
 static bool Visible(VisualElement element){for(var parent=element;parent!=null;parent=parent.parent)if(parent.resolvedStyle.display==DisplayStyle.None||parent.resolvedStyle.visibility==UnityEngine.UIElements.Visibility.Hidden)return false;return true;}
 T[] Controls<T>() where T:VisualElement {var content=ContentRoot();return content==null?new T[0]:content.Query<T>().ToList().Where(Visible).Take(32).ToArray();}
 internal JObject View(){
  var fields=Controls<TextField>();var buttons=Controls<Button>();var labels=Controls<Label>().Select(l=>l.text).Where(t=>!string.IsNullOrWhiteSpace(t)).Take(24).ToArray();return new JObject{["ready"]=ready,["appId"]=opened,["instructions"]=JArray.FromObject(labels),["fields"]=JArray.FromObject(fields.Select((f,i)=>new{id="field/"+i,label=string.IsNullOrEmpty(f.label)&&fields.Length==1?string.Join(" ",labels):f.label,name=f.name,value=f.value,enabled=f.enabledInHierarchy})),["buttons"]=JArray.FromObject(buttons.Select((b,i)=>new{id="button/"+i,label=b.text,enabled=b.enabledInHierarchy}))};
 }
 internal static void ClickNative(Button button){
  if(button==null||!button.enabledInHierarchy)throw new InvalidOperationException("The desktop control is disabled");
  // Invoke the native registry's actual click action. Its ClickEvent subscriptions
  // are separate from Clickable.clicked, and offscreen dispatch can be deferred.
  foreach(var display in Resources.FindObjectsOfTypeAll<EmployeeDesktop>()){
   if(!display||!display.document||display.document.rootVisualElement.panel!=button.panel)continue;
   Action captured;if(display.nativeClicks.TryGetValue(button,out captured)){Debug.Log("[HireScammers] Native click "+button.name+" ready="+Plugin.Get(captured.Target,"ready")+" pending="+Plugin.Get(captured.Target,"pending")+" fieldLength="+((Plugin.Get(captured.Target,"input") as TextField)?.value??"").Length);captured();return;}
   Debug.Log("[HireScammers] Native click lookup "+button.name+" captured="+display.nativeClicks.Count);
   var registry=Plugin.Get(display.controller,"m_eventRegistry");var registrations=Plugin.Get(registry,"m_unregisterActions") as Action;if(registrations==null)continue;
   foreach(var registration in registrations.GetInvocationList().Reverse()){
    if(Plugin.Get(registration.Target,"visualElement") as VisualElement!=button)continue;
    var unregister=Plugin.Get(registration.Target,"unregisterCallback") as Delegate;if(unregister==null||unregister.Target==null||!unregister.Target.GetType().ToString().Contains("ClickEvent"))continue;
    var callback=Plugin.Get(unregister.Target,"callback") as Action;if(callback!=null){callback();return;}
   }
  }
  var clickable=Plugin.Get(button,"clickable");if(clickable!=null)AccessTools.Method(clickable.GetType(),"Invoke").Invoke(clickable,new object[]{null});
 }
 IEnumerator MoveCursor(VisualElement element){
  var monitor=workspace.callStateScreen.MonitorInteractable;var original=(Vector2)Plugin.Get(monitor,"syncedMousePointerPosition");var rect=element.worldBound;var root=document.rootVisualElement.layout;var target=new Vector2(Mathf.Clamp01(rect.center.x/Mathf.Max(1,root.width)),Mathf.Clamp01(rect.center.y/Mathf.Max(1,root.height)));
  float until=Time.time+.3f;while(Time.time<until){if(!bot||!bot.CurrentCall)yield break;var point=Vector2.Lerp(original,target,1-(until-Time.time)/.3f);AccessTools.Method(typeof(MonitorInteractable),"ApplyMousePointerOnServer").Invoke(monitor,new object[]{point});yield return null;}AccessTools.Method(typeof(MonitorInteractable),"ApplyMousePointerOnServer").Invoke(monitor,new object[]{target});
 }
 IEnumerator Execute(DesktopCommand command){
  runningAction=true;var routine=ExecuteCore(command);string problem=null;
  while(true){bool more;try{more=routine.MoveNext();}catch(Exception error){var cause=error.InnerException??error;problem=cause.Message;break;}if(!more)break;yield return routine.Current;}
  runningAction=false;if(bot)bot.CompleteDesktopAction(problem);
 }
 IEnumerator ExecuteCore(DesktopCommand command){
  if(!NetworkServer.active||!bot||!bot.CurrentCall||bot.SelectedScam==null)yield break;
  if(Plugin.Get(controller,"m_boundWorkspace") as ScammerWorkspace!=workspace)controller.BindWorkspace(workspace);if(Plugin.Get(controller,"m_boundVictimCall") as VictimCall!=bot.CurrentCall)controller.BindVictimCall(bot.CurrentCall);
  if(command.type=="open_app"){
   if(command.target!=bot.SelectedScam.appId)throw new InvalidOperationException("The AI selected a different scam app");
   ApplyOwnership();yield return new WaitForEndOfFrame();
   var app=App(command.target);if(app==null)throw new InvalidOperationException("Selected app is missing from the desktop: "+command.target);if(!(bool)Plugin.Get(app,"IsAvailable"))throw new InvalidOperationException("Selected purchased app is unavailable: "+command.target);var icon=Plugin.Get(app,"DesktopButton") as Button;if(icon!=null)yield return MoveCursor(icon);
   // Desktop icons use the OS's own double-click interaction controller rather
   // than Button.clicked. Dispatch its native opening callback after moving there.
   AccessTools.Method(typeof(KolkataOsController),"OnDesktopIconClicked").Invoke(controller,new[]{app});
   yield return new WaitForEndOfFrame();if(Convert.ToInt32(Plugin.Get(app,"State"))!=1)throw new InvalidOperationException("The native desktop did not open the selected app");AccessTools.Method(typeof(KolkataOsController),"ClampWindowToDesktop").Invoke(controller,new[]{app});opened=command.target;bot.OpenedDesktop(opened);yield break;
  }
  if(opened!=bot.SelectedScam.appId)throw new InvalidOperationException("Open the selected scam app first");int index;
  if(command.type=="type"){
   if(!Bot.ControlIndex(command.target,"field/",out index))throw new InvalidOperationException("Invalid field identifier");var fields=Controls<TextField>();if(index>=fields.Length||!fields[index].enabledInHierarchy)throw new InvalidOperationException("That field is unavailable");var field=fields[index];yield return MoveCursor(field);field.value="";bot.SetDesktopInput(command.target,"");
   string text=command.value??"";for(int i=1;i<=text.Length;i++){if(!bot||!bot.CurrentCall)yield break;field.value=text.Substring(0,i);bot.SetDesktopInput(command.target,field.value);yield return new WaitForSeconds(.06f);}yield break;
  }
  if(command.type=="click"){
   if(!Bot.ControlIndex(command.target,"button/",out index))throw new InvalidOperationException("Invalid button identifier");var buttons=Controls<Button>();if(index>=buttons.Length||!buttons[index].enabledInHierarchy)throw new InvalidOperationException("That button is not ready");var button=buttons[index];yield return MoveCursor(button);var original=button.style.backgroundColor;button.style.backgroundColor=new Color(.35f,.7f,.45f);verificationDispatching=true;try{ClickNative(button);yield return new WaitForEndOfFrame();yield return new WaitForSeconds(.25f);}finally{verificationDispatching=false;button.style.backgroundColor=original;}
  }
 }
 void ApplyOwnership(){
  var access=Plugin.Get(controller,"m_applicationAccess");AccessTools.Method(access.GetType(),"ApplyRunEntitlements").Invoke(access,new object[]{bot.DesktopProducts});AccessTools.Method(typeof(KolkataOsController),"RefreshApplicationAccessRuntimes").Invoke(controller,null);var interaction=Plugin.Get(controller,"m_desktopInteraction");if(interaction!=null)AccessTools.Method(interaction.GetType(),"ReflowAllIconsToGrid").Invoke(interaction,null);
 }
 internal IEnumerator CaptureCallWindow(string path){
  controller.BindVictimCall(bot.CurrentCall);var phone=App("phone");if(phone==null)throw new InvalidOperationException("Native Phone app is unavailable");AccessTools.Method(typeof(KolkataOsController),"OnDesktopIconClicked").Invoke(controller,new[]{phone});yield return new WaitForSeconds(.5f);AccessTools.Method(typeof(KolkataOsController),"ClampWindowToDesktop").Invoke(controller,new[]{phone});yield return new WaitForEndOfFrame();Capture(path);
  var content=Plugin.Get(phone,"WindowContent") as VisualElement;foreach(var element in content.Query<VisualElement>().ToList().Where(e=>e.layout.width>100&&e.layout.height>200))Debug.Log("[HireScammers] PHONE REGION "+element.name+" classes="+element.ToString()+" rect="+element.layout+" background="+element.resolvedStyle.backgroundColor);
  foreach(var label in content.Query<Label>().ToList())Debug.Log("[HireScammers] PHONE LABEL "+label.text+" rect="+label.layout+" display="+label.resolvedStyle.display);
  var labels=content.Query<Label>().ToList();if(!labels.Any(l=>l.text!=null&&l.text.Contains("My diagnostic code is"))||!labels.Any(l=>l.text!=null&&l.text.Contains("I have your code.")))throw new InvalidOperationException("Phone transcript is missing a caller or employee reply");DesktopDiagnostics.Checks+=2;
  var presenter=Plugin.Get(controller,"m_callerWebcamPresenter");var portrait=Plugin.Get(presenter,"m_portraitRoot") as VisualElement;if(presenter==null||!(bool)Plugin.Get(presenter,"m_isBound")||portrait==null||portrait.layout.width<20||portrait.layout.height<20)throw new InvalidOperationException("Native victim portrait is not bound and visible");DesktopDiagnostics.Checks++;
 }
 static bool VerifyEmployee(KolkataOsController __instance,string __0,string __1,string __2){
  var display=__instance.GetComponent<EmployeeDesktop>();if(!display)return true;
  Debug.Log("[HireScammers] Native verification callback source="+__0+" item="+__1+" activeClick="+display.verificationDispatching);
  var employee=display.bot;if(!NetworkServer.active||!display.verificationDispatching||!employee||!employee.CurrentCall||employee.SelectedScam==null)return false;
  var selected=employee.SelectedScam;if(__0!=selected.appCompletionSource||selected.relevantSentinels==null||!selected.relevantSentinels.Contains(__1))return false;
  var caller=string.Join(" ",employee.CurrentCall.TranscriptEntries.Where(l=>!l.IsPlayer).Select(l=>l.Text).ToArray());if(!AgentPlans.SpokenValue(__2,caller))return false;
  var wallet=employee.Owner?employee.Owner.GetComponent<KolkataOsPlayer>():null;if(!wallet||!wallet.ServerCanUseScam(selected.id,employee.Workspace))return false;
  display.VerificationAttempts++;display.LastVerification=employee.CurrentCall.ServerVerifyScamField(__0,__1,__2);
  // The native player command credits this return value separately from verification.
  // Repeated submissions return zero, so only newly awarded payouts enter the ledger.
  if(display.LastVerification.PayoutAwarded>0)wallet.ServerCreditScamPayout(employee.CurrentCall.CallGuid,display.LastVerification.PayoutAwarded);
  employee.Status=display.LastVerification.Status=="scam_completed"?"Scam complete":display.LastVerification.FieldVerified?"Code verified through desktop":display.LastVerification.Status;
  return false;
 }
 void PassiveRefresh(){
  if(opened!=bot.DesktopApp&&!string.IsNullOrEmpty(bot.DesktopApp)){var access=Plugin.Get(controller,"m_applicationAccess");AccessTools.Method(access.GetType(),"ApplyRunEntitlements").Invoke(access,new object[]{bot.DesktopProducts});AccessTools.Method(typeof(KolkataOsController),"RefreshApplicationAccessRuntimes").Invoke(controller,null);controller.BindWorkspace(workspace);controller.BindVictimCall(workspace.activeCall as VictimCall);AccessTools.Method(typeof(KolkataOsController),"OpenAppById").Invoke(controller,new object[]{bot.DesktopApp});opened=bot.DesktopApp;}
  if(observedRevision==bot.DesktopRevision)return;observedRevision=bot.DesktopRevision;var values=string.IsNullOrEmpty(bot.DesktopValuesJson)?null:JsonConvert.DeserializeObject<Dictionary<string,string>>(bot.DesktopValuesJson);if(values==null)return;var fields=Controls<TextField>();foreach(var pair in values){int index;if(Bot.ControlIndex(pair.Key,"field/",out index)&&index<fields.Length)fields[index].SetValueWithoutNotify(pair.Value);}
 }
}
}

