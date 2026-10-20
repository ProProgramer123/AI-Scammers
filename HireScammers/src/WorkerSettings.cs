using System;
using System.Linq;
using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;
namespace HireScammers {
[Serializable] public sealed class WorkerSettings {
 public int Floor;public bool Paused,Muted;public string[] Scams;public string Voice="Default";
 internal static readonly string[] Voices={"Default","Alternative"};
 internal static bool Valid(WorkerSettings s,int maxFloor){return s!=null&&s.Floor>=0&&s.Floor<=maxFloor&&Voices.Contains(s.Voice)&&(s.Scams==null||s.Scams.Length<=32&&s.Scams.All(id=>ScamTypeLibrary.LoadAll().Any(c=>c.id==id)));}
 internal static void Build(VisualElement root){
  var box=new VisualElement(){name="worker-settings"};root.Add(box);string rosterKey="";
  Action refresh=()=>{var owner=PlayerName.LocalPlayerInstance;var states=Runtime.States.Where(s=>owner&&s.Owner==owner.netId).ToArray();var key=string.Join(",",states.Select(s=>s.Id.ToString()).ToArray());if(key==rosterKey)return;rosterKey=key;box.Clear();
   foreach(var state in states){var current=state.Settings??new WorkerSettings();var card=new Foldout(){text=state.Name+" — configuration"};box.Add(card);
    var g=ScammerGameManager.Instance;int max=g?OfficeFloorProgression.GetFloorIndex(g.CurrentDay)+1:1;var floors=new List<string>{"Automatic (current work floor)"};for(int i=1;i<=max;i++)floors.Add("Floor "+i);
    var floor=new DropdownField("Assigned floor",floors,Mathf.Clamp(current.Floor,0,max));card.Add(floor);
    var pause=new Toggle("Pause worker"){value=current.Paused};card.Add(pause);var mute=new Toggle("Mute worker voice"){value=current.Muted};card.Add(mute);
    var voice=new DropdownField("Worker voice",Voices.ToList(),Math.Max(0,Array.IndexOf(Voices,current.Voice)));card.Add(voice);
    var choices=new List<Tuple<string,Toggle>>();var all=new Toggle("Allow all owned scams"){value=current.Scams==null};card.Add(all);
    foreach(var scam in ScamTypeLibrary.LoadAll().Where(s=>s!=null)){var toggle=new Toggle(scam.displayName){value=current.Scams==null||current.Scams.Contains(scam.id)};card.Add(toggle);choices.Add(Tuple.Create(scam.id,toggle));}
    card.Add(new Button(()=>Runtime.Request(3,JsonConvert.SerializeObject(new WorkerSettings{Floor=floors.IndexOf(floor.value),Paused=pause.value,Muted=mute.value,Voice=voice.value,Scams=all.value?null:choices.Where(c=>c.Item2.value).Select(c=>c.Item1).ToArray()}),state.Id.ToString())){text="Apply worker settings"});
    var note=new Label("Settings apply for this lobby session. Only owned scams are usable. Other floors may have no incoming calls outside Sandbox mode.");note.style.whiteSpace=WhiteSpace.Normal;card.Add(note);
   }
  };refresh();box.schedule.Execute(refresh).Every(1000);
 }
}
public sealed partial class Bot {
 public WorkerSettings Settings=new WorkerSettings();OfficeFloorStreaming retainedFloor;int retainedIndex=-1;
 internal bool AllowsScam(string id){return Settings.Scams==null||Settings.Scams.Contains(id);}
 internal int AssignedDay(int day){return Settings.Floor==0?day:(Settings.Floor-1)*5+1;}
 internal void ConfigureWorker(WorkerSettings settings){bool relocate=Settings.Floor!=settings.Floor;Settings=settings;if(relocate||settings.Paused||(SelectedScam!=null&&!AllowsScam(SelectedScam.id)))Release();if(relocate)lastDay=-1;nextThink=0;RefreshRetainedFloor();}
 internal void RefreshRetainedFloor(){if(Settings.Floor==0){if(retainedFloor)retainedFloor.ReleasePresentationFloor(this);retainedFloor=null;retainedIndex=-1;return;}if(!retainedFloor)retainedFloor=FindObjectsByType<OfficeFloorStreaming>(FindObjectsSortMode.None).FirstOrDefault();if(retainedFloor&&retainedIndex!=Settings.Floor-1){retainedFloor.RetainPresentationFloor(this,Settings.Floor-1);retainedIndex=Settings.Floor-1;}}
 internal void ClearRetainedFloor(){if(retainedFloor)retainedFloor.ReleasePresentationFloor(this);}
}
}
