using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace HireScammers {
[Serializable] public sealed class DesktopCommand {public string type,target,value;}
[Serializable] public sealed class EmployeePlan {public string dialogue,scam_id;public DesktopCommand[] desktop_actions;}
internal static class AgentPlans {
 internal static string PublicText(string text){return Regex.Replace(text??"",@"<transcript\b[^>]*\btext=""([^""]*)""[^>]*>.*?</transcript>\s*",m=>m.Groups[1].Value,RegexOptions.Singleline|RegexOptions.IgnoreCase);}
 internal static ScamTypeDefinition ResolveScam(ScamTypeDefinition[] owned,string requested){Func<string,string> key=s=>new string((s??"").Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());var id=key(requested);return owned.FirstOrDefault(s=>key(s.id)==id)??owned.FirstOrDefault(s=>key(s.appId)==id||key(s.displayName)==id||key(ConversationScamCatalog.GetByAppId(s.appId)?.id)==id);}
 internal static DesktopCommand[] CodeActions(JObject desktop,string caller){caller=PublicText(caller);
  // Only automate unambiguous public codes in a single visible code field. Complex
  // forms remain AI planned; all submissions still run the native validation.
  var fields=(desktop["fields"] as JArray??new JArray()).Where(f=>(bool?)f["enabled"]==true&&Regex.IsMatch((string)f["label"]+" "+(string)f["name"]+" "+(string)desktop["appId"],"code|gift.?card|number|security|registration",RegexOptions.IgnoreCase)).ToArray();
  var buttons=(desktop["buttons"] as JArray??new JArray()).Where(b=>Regex.IsMatch((string)b["label"]??"","^(claim|redeem|verify|submit|apply|confirm|file|recover|register|connect)(\\b|$)",RegexOptions.IgnoreCase)).ToArray();
  if(fields.Length!=1||buttons.Length!=1)return new DesktopCommand[0];
  var codes=Regex.Matches(caller??"",@"\b\d{1,8}(?:[- ]\d{1,8}){1,5}\b|\b[A-Z0-9]{3,8}(?:-[A-Z0-9]{3,8}){1,4}\b|\b[A-Z0-9]{4,24}\b",RegexOptions.IgnoreCase).Cast<Match>().Select(m=>m.Value).Where(v=>v.Any(char.IsDigit)).Distinct().ToArray();
  if(codes.Length!=1||!SpokenValue(codes[0],caller))return new DesktopCommand[0];
  return new[]{new DesktopCommand{type="type",target=(string)fields[0]["id"],value=codes[0]},new DesktopCommand{type="click",target=(string)buttons[0]["id"]}};
 }
 internal static EmployeePlan Parse(JObject response){
  string raw=(string)response.SelectToken("choices[0].message.content");if(string.IsNullOrWhiteSpace(raw))return null;raw=raw.Trim();
  if(raw.StartsWith("```",StringComparison.Ordinal)){int line=raw.IndexOf('\n'),end=raw.LastIndexOf("```",StringComparison.Ordinal);if(line>=0&&end>line)raw=raw.Substring(line+1,end-line-1).Trim();}
  JObject obj=null;try{if(raw.StartsWith("{",StringComparison.Ordinal))obj=JObject.Parse(raw);}catch{return null;}
  if(obj==null)return new EmployeePlan{dialogue=raw,desktop_actions=new DesktopCommand[0]};
  string dialogue=(string)obj["dialogue"];if(dialogue!=null&&dialogue.TrimStart().StartsWith("{",StringComparison.Ordinal)){try{obj=JObject.Parse(dialogue);dialogue=(string)obj["dialogue"];}catch{return null;}}
  var plan=new EmployeePlan{dialogue=dialogue,scam_id=(string)obj["scam_id"],desktop_actions=new DesktopCommand[0]};
  var list=obj["desktop_actions"] as JArray;var single=obj["desktop_action"] as JObject;
  if(list==null&&single!=null)list=new JArray(single);
  if(list!=null){if(list.Count>6)return null;try{plan.desktop_actions=list.ToObject<DesktopCommand[]>();}catch{return null;}}
  if(plan.scam_id==null&&dialogue!=null&&dialogue.StartsWith("SELECT_SCAM:",StringComparison.Ordinal)){plan.scam_id=dialogue.Substring(12).Trim();plan.dialogue="";}
  if((plan.scam_id??"").Length>80||(plan.dialogue??"").Length>1200)return null;
  foreach(var action in plan.desktop_actions)if(action==null||!(new[]{"open_app","type","click","wait"}).Contains(action.type)||(action.target??"").Length>80||(action.value??"").Length>128)return null;
  return plan;
 }
 internal static bool SpokenValue(string value,string transcript){if(string.IsNullOrWhiteSpace(value)||value.Length>128)return false;Func<string,string> clean=s=>new string((s??"").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());var needle=clean(value);return needle.Length>0&&clean(PublicText(transcript)).Contains(needle);}
}
}
