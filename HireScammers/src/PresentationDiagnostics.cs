using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kolkata.UI;
using UnityEngine;
using UnityEngine.UIElements;
namespace HireScammers {
internal static class PresentationDiagnostics {
 internal static int Run(){
  var actor=Runtime.Proxy();var bot=actor.GetComponent<Bot>();var workers=(Dictionary<int,Bot>)Plugin.Get(Runtime.Instance,"workers");
  try{
   bot.Owner=PlayerName.LocalPlayerInstance;bot.RecruitName="Presentation check";bot.LocalId=-999;bot.DayEarned=20;bot.Earned=40;bot.EnsureVisual();workers.Add(-999,bot);
   var tag=actor.GetComponentInChildren<PlayerNameText>(true);if(!tag||!tag.IsForcedPlayer)throw new Exception("Employee did not use native forced player nametag");
   tag.SetForcedPlayerName(bot.DisplayName);if(tag.transform.position.y-bot.transform.position.y<1.9f)throw new Exception("Native tag fell below head height after presentation refresh");
   var board=Resources.FindObjectsOfTypeAll<QuotaBoardController>().FirstOrDefault();if(board){var rows=(IList)AccessTools.Method(typeof(QuotaBoardController),"BuildPlayerRows").Invoke(board,null);if(!rows.Cast<object>().Any(r=>(string)Plugin.Get(r,"DisplayName")==bot.DisplayName))throw new Exception("Employee missing from native agent board");var employeeRow=rows.Cast<object>().First(r=>(string)Plugin.Get(r,"DisplayName")==bot.DisplayName);if(AccessTools.Method(typeof(QuotaBoardController),"BuildPlayerRow").Invoke(board,new[]{employeeRow})==null)throw new Exception("Native employee agent row did not render");}
   var text=tag.GetComponentInChildren<TMPro.TMP_Text>(true);if(!text||text.text!=PlayerName.SanitizeUnityRichText(bot.DisplayName))throw new Exception("Native employee name incorrect");
   bot.UpdateNameTagPlacement();text.ForceMeshUpdate(true,true);var size=Vector3.Scale(text.textBounds.size,text.transform.lossyScale);var head=bot.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:Head");if(size.x>1.7f||size.y>.25f||size.x<.1f||tag.transform.position.y<head.position.y+.4f)throw new Exception("Native nametag size/clearance invalid: "+size+" head="+head.position+" tag="+tag.transform.position);
   var snapshots=(IList)AccessTools.Method(typeof(ReviewDaySummaryTracker),"BuildPlayerSnapshots").Invoke(null,null);
   var employee=snapshots.Cast<object>().FirstOrDefault(row=>(string)Plugin.Get(row,"DisplayName")==bot.DisplayName);
   if(employee==null||(int)Plugin.Get(employee,"DayEarnings")!=20)throw new Exception("Employee missing from native review snapshots");
   var deck=new ReviewSlideDeckDefinition();EmployeeReview.Append(deck);EmployeeReview.Append(deck);if(deck.slides.Length!=1||deck.slides[0].table.rows.Length!=1||deck.slides[0].table.rows[0][0]!=bot.DisplayName)throw new Exception("Employee slide missing or duplicated");
   var review=Resources.FindObjectsOfTypeAll<ReviewWhiteboardController>().First();var originalBody=Plugin.Get(review,"m_slideBody");var body=new VisualElement();try{Plugin.Set(review,"m_slideBody",body);AccessTools.Method(typeof(ReviewWhiteboardController),"BuildSlideContent").Invoke(review,new object[]{deck.slides[0]});if(!body.Query<Label>().ToList().Any(l=>l.text==bot.DisplayName)||!body.Query<Label>().ToList().Any(l=>l.text=="$20"))throw new Exception("Employee slide did not render its earnings table");}finally{Plugin.Set(review,"m_slideBody",originalBody);}
   if(!EmployeeReview.Place(bot)||!FloorMotion.Clear(bot.Position))throw new Exception("No clear native presentation-room employee placement");
   Debug.Log("[HireScammers] PRESENTATION PASS: native nametag, native summary roster, rendered employee earnings table and clear review-room placement.");return board?9:7;
  }finally{workers.Remove(-999);UnityEngine.Object.Destroy(actor);}
 }
}
}


