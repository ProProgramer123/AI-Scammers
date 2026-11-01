using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using UnityEngine;
namespace HireScammers {
internal static class DesktopPerformanceDiagnostics {
 internal static int Checks;
 internal static IEnumerator Exercise(Bot bot,EmployeeDesktop original){
  Checks=0;EmployeeDesktop first=null,second=null;
  int registryCount=EmployeeDesktop.RegistryOwnerCount,panelCount=EmployeeDesktop.PanelOwnerCount;
  var assets=Plugin.Get(original.GetComponent<Kolkata.UI.KolkataOsController>(),"m_assets");
  try{
   first=EmployeeDesktop.Create(bot,bot.Workspace);second=EmployeeDesktop.Create(bot,bot.Workspace);
   if(first.Document||second.Document)throw new Exception("Desktop allocation ran before the startup queue yielded");Checks++;
   float deadline=Time.realtimeSinceStartup+15,peakFrame=0;
   while(first&&second&&(!first.Ready||!second.Ready)&&Time.realtimeSinceStartup<deadline){yield return null;peakFrame=Mathf.Max(peakFrame,Time.unscaledDeltaTime);}
   if(!first||!second||!first.Ready||!second.Ready)throw new Exception("Queued desktops failed to initialize");Checks++;
   foreach(var display in new[]{first,second}){
    if(Plugin.Get(display.GetComponent<Kolkata.UI.KolkataOsController>(),"m_assets")!=assets||display.SkippedWallpaperPreloads!=1)throw new Exception("Queued desktop reloaded the wallpaper library");Checks++;
    if(display.Document.rootVisualElement.panel==original.Document.rootVisualElement.panel)throw new Exception("Workers share keyboard focus panels");Checks++;
   }
   if(EmployeeDesktop.RegistryOwnerCount!=registryCount+2||EmployeeDesktop.PanelOwnerCount!=panelCount+2)throw new Exception("Direct click routing missing for concurrent desktops");Checks++;
   Debug.Log("[HireScammers] DESKTOP QUEUE PASS: two simultaneous requests, shared assets, isolated panels; warm startup="+first.InitializationMilliseconds.ToString("F1")+"/"+second.InitializationMilliseconds.ToString("F1")+" ms across frames; largest observed frame="+(peakFrame*1000).ToString("F1")+" ms (whole game).");
   var os=first.GetComponent<Kolkata.UI.KolkataOsController>();var oldRegistry=Plugin.Get(os,"m_eventRegistry");
   AccessTools.Method(typeof(Kolkata.UI.KolkataOsController),"BuildDesktop").Invoke(os,new object[]{null});
   var owners=(IDictionary)AccessTools.Field(typeof(EmployeeDesktop),"registryOwners").GetValue(null);var newRegistry=Plugin.Get(os,"m_eventRegistry");
   if(owners.Contains(oldRegistry)||!owners.Contains(newRegistry))throw new Exception("Desktop rebuild retains stale click routing and fails to register replacement callbacks");Checks++;
   yield return null;yield return null;
   if(!first.PhoneVisible)throw new Exception("Desktop rebuild loses the live Phone window");Checks++;
   var contentBuilder=Plugin.Get(os,"m_contentBuilder");var disposedField=AccessTools.Field(contentBuilder.GetType(),"m_recurringContentSchedules");
   var schedules=disposedField.GetValue(contentBuilder) as IDictionary;var schedule=first.Document.rootVisualElement.schedule.Execute(()=>{}).Every(1000);
   var app=(Plugin.Get(os,"m_appsById") as IDictionary)["phone"];if(!schedules.Contains(app))schedules.Add(app,schedule);else schedule.Pause();
   UnityEngine.Object.Destroy(first.gameObject);yield return null;first=null;
   if(schedules.Count!=0)throw new Exception("Employee teardown never disposes native recurring content schedules");Checks++;
   if(EmployeeDesktop.RegistryOwnerCount!=registryCount+1||EmployeeDesktop.PanelOwnerCount!=panelCount+1)throw new Exception("Rebuilt desktop leaves stale routing on teardown");Checks++;
  }finally{if(first)UnityEngine.Object.Destroy(first.gameObject);if(second)UnityEngine.Object.Destroy(second.gameObject);}
  yield return null;
  if(EmployeeDesktop.RegistryOwnerCount!=registryCount||EmployeeDesktop.PanelOwnerCount!=panelCount)throw new Exception("Destroyed desktops left stale click routing entries");Checks++;
  if(!original.Ready||!original.ScreenTexture||!original.ScreenTexture.IsCreated())throw new Exception("Closing a desktop broke another worker's screen");Checks++;
  Debug.Log("[HireScammers] DESKTOP CLEANUP PASS: routing entries removed; original screen and shared assets retained.");
 }
}
}
