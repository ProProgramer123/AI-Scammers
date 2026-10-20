using System.Linq;
using UnityEngine;
namespace HireScammers {
internal static class NativeNameTags {
 internal static PlayerNameText Create(Transform parent,string displayName,out GameObject root){
  root=null;var template=Resources.FindObjectsOfTypeAll<PlayerNameText>().FirstOrDefault(n=>n&&!n.IsForcedPlayer&&!n.GetComponentInParent<Bot>()&&n.GetComponentInChildren<TMPro.TMP_Text>(true));
  if(!template)return null;var source=template.gameObject;bool active=source.activeSelf;source.SetActive(false);
  GameObject label;try{label=Object.Instantiate(source);}finally{source.SetActive(active);}
  root=new GameObject("Employee nametag anchor");root.transform.SetParent(parent,false);root.transform.localPosition=Vector3.up*2.15f;
  root.name="Employee native nametag";
  // Awake outside the owner's hierarchy avoids subscribing this label to the owner's name.
  label.SetActive(true);var tag=label.GetComponentInChildren<PlayerNameText>(true);tag.enabled=true;label.transform.SetParent(root.transform,false);
  label.transform.localPosition=Vector3.zero;label.transform.localRotation=Quaternion.identity;label.transform.localScale=template.transform.lossyScale;
  Plugin.Set(tag,"playerPosY",0f);Plugin.Set(tag,"npcPosY",0f);Plugin.Set(tag,"playerName",null);Plugin.Set(tag,"npcInteractor",null);Plugin.Set(tag,"npcGenerator",null);
  Plugin.Set(tag,"text",tag.GetComponent<TMPro.TMP_Text>());Plugin.Set(tag,"localPlayerDisplayNameOverride",null);
  Plugin.Set(tag,"playerNameHeight",.25f);tag.SetForcedPlayerName(PlayerName.SanitizeUnityRichText(displayName));var text=tag.GetComponentInChildren<TMPro.TMP_Text>(true);if(text){text.enableAutoSizing=false;text.textWrappingMode=TMPro.TextWrappingModes.NoWrap;text.overflowMode=TMPro.TextOverflowModes.Overflow;text.fontSize=36f;}return tag;
 }
}
}





