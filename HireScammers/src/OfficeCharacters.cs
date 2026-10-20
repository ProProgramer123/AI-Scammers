using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
namespace HireScammers {
// Retain the game's spawned office cast as inactive appearance sources. Only the
// exact Scammer NPC prefab family is affected; no other NPC system is changed.
public sealed class OfficeCharacters : MonoBehaviour {
 public sealed class Character {public string Id,Name;}
 public static OfficeCharacters Instance;
 internal static bool IsDefault(NetworkIdentity identity){return Instance&&identity&&Instance.hidden.Any(n=>n&&n.gameObject==identity.gameObject);}
 readonly Dictionary<string,Character> cast=new Dictionary<string,Character>();
 readonly List<NpcInteractor> hidden=new List<NpcInteractor>();float nextScan;
 public static Character[] Choices {get{return Instance?Instance.cast.Values.OrderBy(c=>c.Name,StringComparer.Ordinal).ThenBy(c=>c.Id,StringComparer.Ordinal).ToArray():new Character[0];}}
 public static string ModelForName(string name){var c=Choices.FirstOrDefault(v=>v.Name==name);return c==null?null:c.Id;}
 public static string Choose(int slot){var list=Choices;return list.Length==0?null:list[Math.Abs(slot)%list.Length].Id;}
 public static string DistinctModel(string preferred,IEnumerable<string> used,int slot){
  var list=Choices;var occupied=new HashSet<string>(used??new string[0],StringComparer.Ordinal);
  if(preferred!=null&&!occupied.Contains(preferred))return preferred;
  for(int i=0;i<list.Length;i++){var candidate=list[(Math.Abs(slot)+i)%list.Length].Id;if(!occupied.Contains(candidate))return candidate;}
  return preferred??Choose(slot); // Unique clothing palettes still distinguish a full cast.
 }
 public static bool Ready{get{return Instance&&Instance.hidden.Any(n=>n&&n.GetComponentInChildren<RandomNpcGenerator>(true));}}
 void Awake(){Instance=this;}
 void Update(){if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+.5f;Scan();}}
 internal void Scan(){
  foreach(var npc in Resources.FindObjectsOfTypeAll<NpcInteractor>()){
   if(!npc||string.IsNullOrEmpty(npc.gameObject.scene.name)||!npc.name.StartsWith("Scammer NPC",StringComparison.Ordinal)||npc.GetComponentInParent<Bot>())continue;
   var generator=npc.GetComponentInChildren<RandomNpcGenerator>(true);if(!generator||generator.CurrentSkinId==0||!Visuals.FindNpcModel(npc))continue;
   string id=generator.CurrentSkinId.ToString(System.Globalization.CultureInfo.InvariantCulture);
   string name=(string)Plugin.Get(npc,"npcDisplayName");if(string.IsNullOrWhiteSpace(name))continue;
   if(!cast.ContainsKey(id))cast[id]=new Character{Id=id,Name=Runtime.CleanName(name)};
   if(!hidden.Contains(npc))hidden.Add(npc);
   var identity=npc.GetComponent<NetworkIdentity>();if(NetworkServer.active&&identity&&identity.observers!=null)foreach(var connection in identity.observers.Values.ToArray())if(connection!=NetworkServer.localConnection&&!Runtime.IsPeer(connection.connectionId))GuestCompatibility.Hide(identity,connection);
   // Release the chair's authoritative reservation before removing its occupant.
   if(NetworkServer.active)foreach(var chair in Resources.FindObjectsOfTypeAll<ChairInteractable>())
    if(chair&&chair.sitter==npc)ChairInteractable.ServerClearSitter(npc);
   if(npc.gameObject.activeSelf)npc.gameObject.SetActive(false);
  }
 }
 public static GameObject Clone(string id,Transform parent){
  if(!Ready)return null;ulong skin;if(!ulong.TryParse(id,out skin)){
   id=Choose(0);if(!ulong.TryParse(id,out skin))return null;
  }
  var available=Instance.hidden.Where(n=>n&&n.GetComponentInChildren<RandomNpcGenerator>(true)).ToArray();
  var npc=available.FirstOrDefault(n=>n.GetComponentInChildren<RandomNpcGenerator>(true).CurrentSkinId==skin)??available[0];
  var generator=npc.GetComponentInChildren<RandomNpcGenerator>(true);ulong original=generator.CurrentSkinId;
  try{
   if(skin!=original)generator.LoadSkinPreset(skin);var source=Visuals.FindNpcModel(npc);if(!source)return null;
   var copy=Visuals.CloneModel(source,parent);
   foreach(var sit in npc.GetComponentsInChildren<SitAnimator>(true)){
    if(Plugin.Get(sit,"modelTransform") as GameObject!=source.gameObject)continue;
    if((bool)Plugin.Get(sit,"hasBaseModelLocalPosition"))copy.transform.localPosition=(Vector3)Plugin.Get(sit,"baseModelLocalPosition");
   }
   FloorMotion.StandModel(copy);
   return copy;
  }finally{if(skin!=original)generator.LoadSkinPreset(original);}
 }
 void OnDestroy(){foreach(var npc in hidden)if(npc)npc.gameObject.SetActive(true);hidden.Clear();cast.Clear();Instance=null;}
}
}






