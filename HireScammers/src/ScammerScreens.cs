using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;
using ScamWYF.Modding.Core;
using ScamWYF.Modding.Core.Content;
namespace HireScammers {
internal static class ScammerScreens {
 internal const string App="scammer-screens";
 internal static void Register(ScamMod plugin){ContentRegistry.RegisterApp(plugin,new OsAppSpec{Id=App,Title="Scammer Screens",IconResource="KolkataOS/SVG/monitor",ContentType="placeholder",ShowOnDesktop=true,DesktopOrder=99,WindowLeft=60,WindowTop=40,WindowWidth=1100,WindowHeight=850,MinWindowWidth=640,MinWindowHeight=480,InitialState="closed",AccentColor="#27734A",Access=new OsAppAccessSpec{Mode="catalog",ProductId=Plugin.Product,UnlockDay=6},Content=new OsAppContentSpec{Title="Scammer Screens"}});}
 internal static void Build(VisualElement root){
  root.Clear();var scroll=new ScrollView(){name="scammer-screens-view"};scroll.style.flexGrow=1;root.Add(scroll);var body=scroll.contentContainer;
  var workers=new DropdownField("Worker",new System.Collections.Generic.List<string>{"No workers"},0);body.Add(workers);var notice=new Label();notice.style.whiteSpace=WhiteSpace.Normal;body.Add(notice);
  var image=new Image(){scaleMode=ScaleMode.ScaleToFit};image.style.height=360;image.style.flexShrink=0;body.Add(image);
  EmployeeState[] states=new EmployeeState[0];uint selected=0;string lastView="";var controls=new VisualElement();
  Action<string,DesktopCommand> send=(operation,command)=>{if(selected!=0)Runtime.Request(4,JsonConvert.SerializeObject(new ScreenCommand{Operation=operation,Command=command}),selected.ToString());};
  body.Add(new Button(()=>send("take",null)){text="Take control (60 seconds)"});body.Add(new Button(()=>send("release",null)){text="Return control to AI"});
  body.Add(new Label("Taking control pauses autonomous plans. Use the fields and buttons below to operate the selected scam app. Renew control to extend the session."));body.Add(controls);
  var reply=new TextField("Speak to caller"){maxLength=600};body.Add(reply);body.Add(new Button(()=>send("speak",new DesktopCommand{value=reply.value})){text="Send spoken response"});
  Action refresh=()=>{
   var owner=PlayerName.LocalPlayerInstance;states=Runtime.States.Where(s=>owner&&s.Owner==owner.netId).ToArray();var names=states.Select(s=>s.Name+" (#"+s.Id+")").ToList();if(names.Count==0)names.Add("No workers");if(!workers.choices.SequenceEqual(names)){workers.choices=names;if(!names.Contains(workers.value))workers.value=names[0];}
   int index=names.IndexOf(workers.value);var state=index>=0&&index<states.Length?states[index]:null;uint id=state==null?0:state.Id;if(id!=selected){selected=id;lastView="";controls.Clear();}
   var display=EmployeeDesktop.ForWorker(selected);image.image=display?display.ScreenTexture:null;notice.text=state==null?"No employees hired.":state.Name+" — "+state.Status+(state.ManualControl?" | You have control":"")+(display?"":"\nWaiting for this worker to use a computer.")+"\n"+Runtime.Notice;
   var json=state==null?null:state.ScreenView;if(string.IsNullOrEmpty(json)||lastView==json)return;lastView=json;JObject view;try{view=JObject.Parse(json);}catch{return;}controls.Clear();
   var apps=(view["scams"] as JArray??new JArray()).ToArray();if(apps.Length>0){var labels=apps.Select(a=>(string)a["name"]).ToList();var appChoice=new DropdownField("Scam app",labels,0);controls.Add(appChoice);controls.Add(new Button(()=>{var choice=apps[labels.IndexOf(appChoice.value)];send("select",new DesktopCommand{target=(string)choice["id"]});}){text="Open selected app"});}
   foreach(var f in view["fields"] as JArray??new JArray()){string target=(string)f["id"];string label=(string)f["label"]??target;if(label.Length>80)label=label.Substring(0,80);var field=new TextField(label){value=(string)f["value"]??"",maxLength=128,isDelayed=true};controls.Add(field);controls.Add(new Button(()=>send("action",new DesktopCommand{type="type",target=target,value=field.value})){text="Enter into "+target});}
   foreach(var b in view["buttons"] as JArray??new JArray()){string target=(string)b["id"];var button=new Button(()=>send("action",new DesktopCommand{type="click",target=target})){text=(string)b["label"]};button.SetEnabled((bool?)b["enabled"]==true);controls.Add(button);}
  };refresh();root.schedule.Execute(refresh).Every(1000);
 }
 internal static bool Handle(Bot bot,ScreenCommand request,out string error){
  error=null;if(!bot||!bot.CurrentCall||!bot.IsSeated){error="Worker is not on a call at a computer.";return false;}
  if(request==null){error="Invalid screen command.";return false;}
  if(request.Operation=="release"){bot.RemoteControlUntil=0;bot.Status="Returning control to AI";return true;}
  if(request.Operation=="take"){if(bot.DesktopBusy){error="Worker is finishing a desktop action; try again shortly.";return false;}bot.BeginRemoteControl();return true;}
  if(!bot.UnderRemoteControl){error="Take control first.";return false;}
  if(request.Operation=="select"){if(bot.DesktopBusy){error="Wait for the current action to finish.";return false;}return bot.ApplyPlan(new EmployeePlan{scam_id=request.Command==null?null:request.Command.target},out error);}
  if(request.Operation=="speak"){if(!bot.CurrentCall.CanAcceptTalkerTranscriptNow){error="Wait for the caller to finish.";return false;}string text=request.Command==null?null:request.Command.value;if(string.IsNullOrWhiteSpace(text)||text.Length>600){error="Enter a response of up to 600 characters.";return false;}bot.CurrentCall.SendPrompt(text,"en",null,default(ReviewUtteranceTiming));EmployeeAudio.Speak(bot,bot.CurrentCall,text);return true;}
  var command=request.Command;if(request.Operation!="action"||command==null||!(command.type=="type"||command.type=="click")||(command.value??"").Length>128){error="Invalid desktop command.";return false;}
  return bot.QueueRemoteAction(command,out error);
 }
}
[Serializable] internal sealed class ScreenCommand {public string Operation;public DesktopCommand Command;}
public sealed partial class Bot {
 internal string CallerFailureCode;internal float CallerRetryAfter;
 internal float RemoteControlUntil;internal bool UnderRemoteControl{get{return Time.unscaledTime<RemoteControlUntil;}}
 internal void BeginRemoteControl(){thinking=false;thoughtSequence++;thought=null;RemoteControlUntil=Time.unscaledTime+60;Status="Player controlling computer";}
 internal bool QueueRemoteAction(DesktopCommand command,out string error){error=null;int index;if(DesktopBusy){error="Wait for the current action to finish.";return false;}if(SelectedScam==null||!ControlIndex(command.target,command.type=="type"?"field/":"button/",out index)){error="No selected scam or invalid control.";return false;}desktopQueue.Enqueue(command);return true;}
 internal string ScreenView(){if(!desktop||!desktop.Ready)return null;var view=desktop.View();view.Remove("instructions");foreach(var field in view["fields"] as JArray??new JArray()){var label=(string)field["label"]??"";if(label.Length>80)field["label"]=label.Substring(0,80);}view["scams"]=JArray.FromObject(EligibleScams().Select(s=>new{id=s.id,name=s.displayName}));return view.ToString(Formatting.None);}
}
public sealed partial class EmployeeDesktop {
 internal RenderTexture ScreenTexture{get{return texture;}}
 internal static EmployeeDesktop ForWorker(uint id){return id==0?null:Resources.FindObjectsOfTypeAll<EmployeeDesktop>().FirstOrDefault(d=>d&&d.ready&&d.bot&&d.bot.netIdentity.netId==id);}
}
}
