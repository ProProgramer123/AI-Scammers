using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Kolkata.UI;
namespace HireScammers {
internal static class IntegrationDiagnostics {
 internal static void Inspect(){
  var owner=PlayerName.LocalPlayerInstance;
  foreach(var npc in Resources.FindObjectsOfTypeAll<NpcInteractor>().Where(n=>n&&n.name.StartsWith("Scammer NPC",StringComparison.Ordinal))){var ni=npc.GetComponent<Mirror.NetworkIdentity>();Debug.Log("[HireScammers] COMPAT NPC "+npc.name+" scene="+npc.gameObject.scene.name+" asset="+(ni?ni.assetId:0)+" net="+(ni?ni.netId:0)+" behaviours="+(ni&&ni.NetworkBehaviours!=null?string.Join(",",ni.NetworkBehaviours.Select(b=>b.GetType().FullName).ToArray()):"none"));}
  foreach(var npc in Resources.FindObjectsOfTypeAll<NpcInteractor>().Where(n=>n&&n.name.StartsWith("Scammer NPC",StringComparison.Ordinal)).Take(1))foreach(var script in npc.GetComponentsInChildren<MonoBehaviour>(true).Where(s=>s&&s.GetType().Name.Contains("Anim")))Debug.Log("[HireScammers] NATIVE NPC component="+script.GetType().Name+" object="+script.name);
  foreach(var ui in Resources.FindObjectsOfTypeAll<ScammerScreenController>()){
   var doc=ui.GetComponent<UIDocument>();Debug.Log("[HireScammers] NATIVE SCREEN object="+ui.name+" scene="+ui.gameObject.scene.name+" asset="+(doc&&doc.visualTreeAsset?doc.visualTreeAsset.name:"none"));
  }
  var desk=ScammerWorkspace.Instances.FirstOrDefault(d=>d&&d.callStateScreen);if(desk)foreach(var renderer in desk.GetComponentsInChildren<Renderer>(true))Debug.Log("[HireScammers] DESK RENDERER name="+renderer.name+" materials="+string.Join(",",renderer.sharedMaterials.Where(m=>m).Select(m=>m.name).ToArray()));
  var point=Plugin.Get(ScammerGameManager.Instance,"reviewRoomPoint") as Transform;Debug.Log("[HireScammers] REVIEW ROOM="+(point?point.position.ToString():"none"));
 }
}
}





