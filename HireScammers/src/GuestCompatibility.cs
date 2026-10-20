using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mirror;
using Steamworks;
using UnityEngine;
namespace HireScammers {
internal static class GuestCompatibility {
 const string LobbyKey="hire_scammers_protocol";internal const string Protocol="3";static ulong advertised;
 internal static void Install(Harmony harmony){
  harmony.Patch(AccessTools.Method(typeof(NetworkIdentity),"AddObserver"),prefix:new HarmonyMethod(typeof(GuestCompatibility),"Observe"));
  harmony.Patch(AccessTools.Method(typeof(NetworkServer),"SendSpawnMessage"),prefix:new HarmonyMethod(typeof(GuestCompatibility),"Spawn"));
  harmony.Patch(AccessTools.Method(typeof(NpcAiController),"OnStartServer"),prefix:new HarmonyMethod(typeof(GuestCompatibility),"StartBrain"));
  harmony.Patch(AccessTools.Method(typeof(NetworkServer),"OnTransportDisconnected"),prefix:new HarmonyMethod(typeof(GuestCompatibility),"Disconnected"));
 }
 internal static bool Allows(NetworkIdentity identity,NetworkConnectionToClient connection){
  if(!identity||connection==null)return false;bool peer=NetworkServer.localConnection==connection||Runtime.IsPeer(connection.connectionId);
  if(identity.assetId==Runtime.Asset)return peer;
  if(identity.GetComponent<VanillaEmployee>()){var game=ScammerGameManager.Instance;return !peer&&(Runtime.Workday||(game&&game.IsReviewPhase));}
  if(OfficeCharacters.IsDefault(identity))return peer;
  return true;
 }
 static bool Observe(NetworkIdentity __instance,NetworkConnectionToClient __0){return Allows(__instance,__0);}
 static bool Spawn(NetworkIdentity __0,NetworkConnectionToClient __1){return Allows(__0,__1);}
 static bool StartBrain(NpcAiController __instance){return !__instance.GetComponent<VanillaEmployee>();}
 static void Disconnected(int __0){Runtime.ForgetPeer(__0);}
 internal static void Hide(NetworkIdentity identity,NetworkConnectionToClient connection){
  AccessTools.Method(typeof(NetworkServer),"HideForConnection").Invoke(null,new object[]{identity,connection});
  AccessTools.Method(typeof(NetworkIdentity),"RemoveObserver").Invoke(identity,new object[]{connection});
  var observing=Plugin.Get(connection,"observing") as HashSet<NetworkIdentity>;if(observing!=null)observing.Remove(identity);
 }
 internal static void Advertise(){
  if(!SteamManager.Initialized)return;var lobby=SteamCreateLobbyForServer.Instance;ulong id=lobby?lobby.LobbyId:0;
  if(id!=0&&advertised!=id&&SteamMatchmaking.SetLobbyData(new CSteamID(id),LobbyKey,Protocol))advertised=id;
 }
 internal static bool HostAvailable(){
  if(NetworkServer.active)return true;if(!SteamManager.Initialized)return false;var join=SteamJoinFriendsThroughOverlay.Instance;ulong id=join?join.LobbyId:0;
  return id!=0&&SteamMatchmaking.GetLobbyData(new CSteamID(id),LobbyKey)==Protocol;
 }
 internal static void PeerJoined(NetworkConnectionToClient connection){
  foreach(var identity in NetworkServer.spawned.Values.ToArray()){
   if(identity.GetComponent<VanillaEmployee>()){
    if(identity.observers.ContainsKey(connection.connectionId)){Hide(identity,connection);var observing=Plugin.Get(connection,"observing") as HashSet<NetworkIdentity>;if(observing!=null)observing.Remove(identity);}
   }else if((identity.assetId==Runtime.Asset||OfficeCharacters.IsDefault(identity))&&connection.isReady)AccessTools.Method(typeof(NetworkIdentity),"AddObserver").Invoke(identity,new object[]{connection});
  }
 }
}
// An unchanged native NetworkBehaviour layout lets vanilla clients deserialize
// the NPC. This server-only MonoBehaviour copies the simulated employee pose.
[DefaultExecutionOrder(21000)]
public sealed class VanillaEmployee : MonoBehaviour {
 internal Bot Employee;Transform source,target;KeyValuePair<Transform,Transform>[] bones;Renderer[] renderers;float nextObservers;
 internal static VanillaEmployee Create(Bot bot){
  var template=NetworkManager.singleton.spawnPrefabs.FirstOrDefault(p=>p&&p.GetComponent<NetworkIdentity>()&&p.GetComponent<NetworkIdentity>().assetId==3921000073U);
  if(!template)throw new InvalidOperationException("Native Scammer NPC prefab is unavailable");
  var go=UnityEngine.Object.Instantiate(template,bot.Position,bot.transform.rotation);go.name="Hired vanilla employee";var driver=go.AddComponent<VanillaEmployee>();driver.Employee=bot;
  try{
   NetworkServer.Spawn(go);var npc=go.GetComponent<NpcInteractor>();npc.ServerSetFixedNpcDisplayName(bot.DisplayName);ulong skin;if(ulong.TryParse(bot.ModelId,out skin))go.GetComponent<NetworkedNpc>().ServerSetSkinSeed(skin);
   var brain=go.GetComponent<NpcAiController>();if(brain){brain.enabled=false;var ai=Plugin.Get(brain,"serverAiGo") as GameObject;if(ai)ai.SetActive(false);}
   var agent=go.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=false;
   foreach(var collider in go.GetComponentsInChildren<Collider>(true))collider.enabled=false;
   foreach(var body in go.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.useGravity=false;}
   driver.source=bot.GetComponentInChildren<NativeEmployeeMotion>(true).transform;driver.target=Visuals.FindNpcModel(npc);
   var origin=driver.source.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.First().name,g=>g.First());driver.bones=driver.target.GetComponentsInChildren<Transform>(true).Where(t=>origin.ContainsKey(t.name)).Select(t=>new KeyValuePair<Transform,Transform>(origin[t.name],t)).ToArray();
   foreach(var animator in go.GetComponentsInChildren<Animator>(true))animator.enabled=false;
   foreach(var script in go.GetComponentsInChildren<MonoBehaviour>(true))if(NativeEmployeeMotion.Keep(script))script.enabled=false;
   driver.renderers=go.GetComponentsInChildren<Renderer>(true);driver.LateUpdate();return driver;
  }catch{NetworkServer.Destroy(go);throw;}
 }
 void LateUpdate(){
  if(!NetworkServer.active)return;if(!Employee){NetworkServer.Destroy(gameObject);return;}
  if(Time.unscaledTime>=nextObservers){nextObservers=Time.unscaledTime+.5f;var identity=GetComponent<NetworkIdentity>();foreach(var connection in NetworkServer.connections.Values.ToArray()){if(connection==null||!connection.isReady)continue;bool allowed=GuestCompatibility.Allows(identity,connection),observing=identity.observers.ContainsKey(connection.connectionId);if(allowed&&!observing)AccessTools.Method(typeof(NetworkIdentity),"AddObserver").Invoke(identity,new object[]{connection});else if(!allowed&&observing)GuestCompatibility.Hide(identity,connection);}}
  transform.SetPositionAndRotation(Employee.transform.position,Employee.transform.rotation);
  if(source&&target){target.SetPositionAndRotation(source.position,source.rotation);foreach(var pair in bones)if(pair.Key&&pair.Value){pair.Value.localPosition=pair.Key.localPosition;pair.Value.localRotation=pair.Key.localRotation;}}
  if(renderers!=null)foreach(var renderer in renderers)if(renderer)renderer.enabled=false;
 }
}
}
