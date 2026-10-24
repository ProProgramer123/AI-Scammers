using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UIElements;
namespace HireScammers {
internal static class DesktopDiagnostics {
 internal static int Checks;
 internal static IEnumerator Exercise(Bot bot){
  Checks=0;var display=Resources.FindObjectsOfTypeAll<EmployeeDesktop>().First(d=>d.Ready);var nativeContent=AccessTools.Method(typeof(EmployeeDesktop),"ContentRoot").Invoke(display,null) as VisualElement;
  if(nativeContent==null)throw new Exception("Selected native app has no content");
  var field=new TextField("Diagnostic public code");var button=new Button();button.text="Diagnostic click";nativeContent.Add(field);nativeContent.Add(button);int changed=0,clicked=0;field.RegisterValueChangedCallback(e=>changed++);button.clicked+=()=>clicked++;
  try{
   yield return new WaitForEndOfFrame();var view=display.View();string fieldId=(string)view["fields"].Last["id"],buttonId=(string)view["buttons"].Last["id"];
   var humanDoc=Resources.FindObjectsOfTypeAll<UIDocument>().FirstOrDefault(d=>d!=display.Document&&d.rootVisualElement.panel!=null);var humanRoot=humanDoc?humanDoc.rootVisualElement:null;var focused=humanRoot==null?null:humanRoot.focusController.focusedElement;field.Focus();if(display.Document.rootVisualElement.focusController.focusedElement==field||(humanRoot!=null&&humanRoot.focusController.focusedElement!=focused))throw new Exception("Employee desktop stole player keyboard focus");Checks++;
   AccessTools.Method(typeof(NetworkedCall),"ServerAppendTranscript").Invoke(bot.CurrentCall,new object[]{false,"Diagnostic Caller","My diagnostic code is 1234-5678-9012."});string error;
   AccessTools.Method(typeof(NetworkedCall),"ServerAppendTranscript").Invoke(bot.CurrentCall,new object[]{true,bot.DisplayName,"I have your code. I will enter it in the app now."});
   if(bot.ApplyPlan(new EmployeePlan{desktop_actions=new[]{new DesktopCommand{type="type",target=fieldId,value="invented-value"}}},out error))throw new Exception("Invented payment value accepted");Checks++;
   if(!bot.ApplyPlan(new EmployeePlan{desktop_actions=new[]{new DesktopCommand{type="type",target=fieldId,value="1234-5678-9012"},new DesktopCommand{type="click",target=buttonId}}},out error))throw new Exception(error);
   bool keyboardSeen=false;float until=Time.time+8;int typingFrames=0;while(Time.time<until&&bot.DesktopBusy){yield return new WaitForEndOfFrame();if(bot.DesktopTyping){keyboardSeen=true;if(++typingFrames==20){CheckHands(bot);CaptureWorker(bot,"employee-desktop-side",false);CaptureWorker(bot,"employee-desktop-overhead",true);}}}
   if(bot.DesktopBusy||field.value!="1234-5678-9012"||changed<10||clicked!=1||!keyboardSeen)throw new Exception("Native desktop input fixture failed: "+field.value+" changes="+changed+" clicks="+clicked+" typing="+keyboardSeen+" status="+bot.Status);Checks+=4;
   Debug.Log("[HireScammers] DESKTOP ACTION PASS: actual native UI callbacks, visible typing and click; synthetic controls only, no provider or payment.");

  }finally{field.RemoveFromHierarchy();button.RemoveFromHierarchy();}
  string remoteError;if(!ScammerScreens.Handle(bot,new ScreenCommand{Operation="take"},out remoteError)||!bot.UnderRemoteControl)throw new Exception("Remote control lease failed: "+remoteError);Checks++;
  var actual=display.View();var publicCode="<transcript sentinel=\"FICTIONAL_SSN\" text=\"645-\">six four five</transcript><transcript sentinel=\"FICTIONAL_SSN\" text=\"34-1538\">three four one five three eight</transcript>";AccessTools.Method(typeof(NetworkedCall),"ServerAppendTranscript").Invoke(bot.CurrentCall,new object[]{false,"Diagnostic Caller",publicCode});
  var commands=AgentPlans.CodeActions(actual,publicCode);if(commands.Length!=2)throw new Exception("Native app controls did not match the visible tagged code");int before=display.VerificationAttempts;
  foreach(var command in commands){if(!ScammerScreens.Handle(bot,new ScreenCommand{Operation="action",Command=command},out remoteError))throw new Exception("Remote desktop action rejected: "+remoteError);float deadline=Time.time+6;while(bot.DesktopBusy&&Time.time<deadline)yield return new WaitForEndOfFrame();if(bot.DesktopBusy)throw new Exception("Remote desktop action timed out");}
  if(display.VerificationAttempts!=before+1)throw new Exception("Real native submit button never reached employee verification: "+display.View());Checks+=2;
  if(ScammerScreens.Handle(bot,new ScreenCommand{Operation="action",Command=new DesktopCommand{type="shell"}},out remoteError))throw new Exception("Invalid remote action accepted");Checks++;
  ScammerScreens.Handle(bot,new ScreenCommand{Operation="release"},out remoteError);if(bot.UnderRemoteControl)throw new Exception("Remote control did not release");if(EmployeeDesktop.ForWorker(bot.netIdentity.netId)!=display)throw new Exception("Remote screen did not resolve the worker's live render texture");Checks+=2;
  Debug.Log("[HireScammers] REMOTE SCREEN PASS: control lease, actual native field typing and submit verification, rejected invalid command, release and live texture binding. Invalid fixture code awards no money.");
  yield return display.CaptureCallWindow(System.IO.Path.Combine(Application.dataPath,"../HireScammers/call-window-preview.png"));
 }
 static void CheckHands(Bot bot){
  var motion=bot.GetComponentInChildren<NativeEmployeeMotion>();var driver=motion.GetComponent<MonitorArmIkDriver>();var monitor=bot.Workspace.callStateScreen.MonitorInteractable;var keyboard=AccessTools.Method(typeof(MonitorInteractable),"GetKeyboardTarget").Invoke(monitor,null) as Transform;var targets=NativeEmployeeMotion.KeyboardTargets(monitor,keyboard,Time.time+bot.Slot*.37f);
  foreach(var side in new[]{"Left","Right"}){var chain=Plugin.Get(driver,"smuggler"+side+"Arm");var tip=Plugin.Get(chain,"tip") as Transform;var alignment=Plugin.Get(chain,"handAlignment") as Transform;var rotation=alignment?alignment.rotation:tip.rotation*(Quaternion)Plugin.Get(chain,"tipToHandAlignmentRotation");int index=side=="Left"?1:3;float distance=Vector3.Distance(alignment?alignment.position:tip.position,(Vector3)targets[index]),angle=Quaternion.Angle(rotation,(Quaternion)AccessTools.Method(typeof(NativeEmployeeMotion),"TypingRotation").Invoke(motion,new object[]{side,keyboard}));Debug.Log("[HireScammers] PALM "+side+" distance="+distance+" angle="+angle);if(distance>.3f||angle>15)throw new Exception("Native palm alignment misses keyboard: "+side+" distance="+distance+" angle="+angle);Checks++;}
  foreach(var side in new[]{"Left","Right"}){var finger=motion.TypingFinger(side);float upright=Mathf.Abs(Vector3.Dot(finger,keyboard.forward));Debug.Log("[HireScammers] FINGER SURFACE "+side+" upright="+upright);if(upright>.2f)throw new Exception("Typing fingers point out of the keyboard plane");Checks++;}
 }
 static void CaptureWorker(Bot bot,string name,bool overhead){
  var go=new GameObject("Employee desktop verification camera");var target=new RenderTexture(1280,960,24);var pixels=new Texture2D(1280,960,TextureFormat.RGB24,false);var previous=RenderTexture.active;var tag=bot.GetComponentInChildren<PlayerNameText>();var original=tag.transform.rotation;var main=Camera.main;var mainPosition=main?main.transform.position:Vector3.zero;var mainRotation=main?main.transform.rotation:Quaternion.identity;if(!main)go.tag="MainCamera";
  try{var camera=go.AddComponent<Camera>();camera.fieldOfView=55;camera.nearClipPlane=.05f;camera.farClipPlane=150;camera.targetTexture=target;var look=bot.transform.position+Vector3.up*1.1f;camera.transform.position=overhead?look+Vector3.up*2.6f-bot.transform.forward*.5f:look-bot.transform.forward*2+bot.transform.right*1.4f+Vector3.up*.9f;camera.transform.rotation=Quaternion.LookRotation(look-camera.transform.position);if(main)main.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);bot.UpdateNameTagPlacement();tag.transform.rotation=Quaternion.LookRotation(camera.transform.position-tag.transform.position,Vector3.up)*Quaternion.Euler(0,180,0);camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,960),0,0);System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../HireScammers/"+name+".png"),ImageConversion.EncodeToPNG(pixels));}finally{if(main)main.transform.SetPositionAndRotation(mainPosition,mainRotation);bot.UpdateNameTagPlacement();tag.transform.rotation=original;RenderTexture.active=previous;UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(pixels);}
 }
}
}
