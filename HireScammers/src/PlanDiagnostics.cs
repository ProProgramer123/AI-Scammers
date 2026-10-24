using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace HireScammers {
internal static class PlanDiagnostics {
 static JObject Response(string content){return new JObject{["choices"]=new JArray(new JObject{["message"]=new JObject{["content"]=content}})};}
 internal static int Run(){
  var plan=new EmployeePlan{dialogue="Hello caller.",scam_id="tech-support",desktop_actions=new[]{new DesktopCommand{type="type",target="field/0",value="1234"},new DesktopCommand{type="click",target="button/0"}}};string json=JsonConvert.SerializeObject(plan);
  var parsed=AgentPlans.Parse(Response(json));if(parsed==null||parsed.scam_id!=plan.scam_id||parsed.desktop_actions.Length!=2||parsed.dialogue!=plan.dialogue)throw new Exception("Direct employee plan parsing failed");
  parsed=AgentPlans.Parse(Response(new JObject{["dialogue"]=json,["trust_percent"]=44}.ToString()));if(parsed==null||parsed.scam_id!=plan.scam_id||parsed.dialogue!=plan.dialogue)throw new Exception("Official nested employee plan leaked metadata");
  parsed=AgentPlans.Parse(Response("```json\n"+json+"\n```"));if(parsed==null||parsed.desktop_actions.Length!=2)throw new Exception("Fenced employee plan parsing failed");
  parsed=AgentPlans.Parse(Response("{\"trust_percent\":44}"));if(parsed==null||parsed.dialogue!=null)throw new Exception("Metadata was spoken");
  if(AgentPlans.Parse(Response("{\"desktop_actions\":[{\"type\":\"shell\"}]}"))!=null)throw new Exception("Unknown desktop action accepted");
  if(!AgentPlans.SpokenValue("1234-5678","The code is 1234 5678.")||AgentPlans.SpokenValue("9999","The code is 1234 5678.")||AgentPlans.SpokenValue("--","hello"))throw new Exception("Public transcript value validation failed");
  var view=JObject.Parse("{\"fields\":[{\"id\":\"field/0\",\"label\":\"Gift card code\",\"enabled\":true}],\"buttons\":[{\"id\":\"button/0\",\"label\":\"Claim\",\"enabled\":false}]}");
  var actions=AgentPlans.CodeActions(view,"The code is 1234-5678-9012.");if(actions.Length!=2||actions[0].value!="1234-5678-9012"||actions[1].type!="click")throw new Exception("Caller code did not produce native typing then submission");
  actions=AgentPlans.CodeActions(view,"My code is AB12-CD34-EF56.");if(actions.Length!=2||actions[0].value!="AB12-CD34-EF56")throw new Exception("Alphanumeric caller code was ignored");
  if(AgentPlans.CodeActions(view,"I do not have a code.").Length!=0||AgentPlans.CodeActions(view,"1234 or 9876").Length!=0)throw new Exception("Ambiguous or absent code was guessed");
  var state=JObject.Parse("{\"scams\":[{\"id\":\"gift_card\",\"completed\":true},{\"id\":\"credit_card\",\"completed\":false}],\"conversationScams\":[{\"id\":\"lottery\",\"completed\":true}]}");if(Bot.CompletedScams(state).Length!=2)throw new Exception("Native multi-scam progress was not recognized");
  if(!WorkerSettings.Valid(new WorkerSettings{Floor=2},3)||WorkerSettings.Valid(new WorkerSettings{Floor=4},3)||WorkerSettings.Valid(new WorkerSettings{Voice="invalid"},3))throw new Exception("Worker configuration validation failed");
  var gift=ScamTypeLibrary.LoadAll().First(s=>s.id=="gift_card");if(AgentPlans.ResolveScam(new[]{gift},"gift-card")!=gift||AgentPlans.ResolveScam(new[]{gift},"unowned-scam")!=null)throw new Exception("Owned scam alias validation failed");
  string tagged="<transcript sentinel=\"FICTIONAL_SSN\" text=\"645-\">six four five</transcript><transcript sentinel=\"FICTIONAL_SSN\" text=\"34-1538\">three four one five three eight</transcript>";
  if(AgentPlans.PublicText(tagged)!="645-34-1538"||!AgentPlans.SpokenValue("645341538",tagged))throw new Exception("Visible tagged code fragments were not reconstructed");
  view["appId"]="identity";view["fields"][0]["label"]="Social Security number";view["buttons"][0]["label"]="File correction";actions=AgentPlans.CodeActions(view,tagged);if(actions.Length!=2||actions[0].value!="645-34-1538")throw new Exception("Native SSN controls did not accept public tagged code");
  if(!EmployeeCallerRequests.Retryable(new TimeoutException())||EmployeeCallerRequests.Retryable(new InvalidOperationException()))throw new Exception("Caller retry policy invalid");
  view["fields"][0]["label"]="Card number. Enter exactly 12 digits. Delimiters are optional.";
  if(AgentPlans.CodeActions(view,tagged).Length!=0)throw new Exception("An old nine-digit SSN was entered into a twelve-digit card field");
  actions=AgentPlans.CodeActions(view,"My card number is 1234-5678-9012.");if(actions.Length!=2||actions[0].value!="1234-5678-9012")throw new Exception("Correct card code was rejected by visible field format");
  DebugMessage();return 17;
 }
 static void DebugMessage(){UnityEngine.Debug.Log("[HireScammers] PLAN PASS: direct/fenced/nested official plans, metadata stripping and public value checks.");}
}
}
