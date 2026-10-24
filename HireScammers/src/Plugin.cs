using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Mirror;
using Newtonsoft.Json;
using Kolkata.UI;
using ScamWYF.Modding.Core;
using ScamWYF.Modding.Core.Content;
using ScamWYF.Modding.Core.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace HireScammers {
[BepInPlugin(Id,"Hire Scammers","0.3.8")]
[BepInProcess("Scam With Your Friends.exe")]
public sealed class Plugin : ScamMod {
 public const string Id="com.local.scamwyf.hirescammers", App="hire-scammers", Product="hire-scammers-license";
 internal const string ViewId="hire-scammers-hiring-view";
 public const int UnlockDay=6; // Five workdays per Scamazon week; Week 2 begins on day 6.
 internal static Plugin Instance;
 internal static ConfigEntry<int> Required, LicensePrice, HirePrice, MaxWorkers;
 internal static ConfigEntry<float> Grace;
 Harmony harmony;
 protected override void OnModLoad() {
  NativeScamPayout.Validate();
  Instance=this;
  Required=Config.Bind("Hiring","PersonalScamsRequired",3,new ConfigDescription("Different callers the player must scam personally.",new AcceptableValueRange<int>(1,100)));
  LicensePrice=Config.Bind("Hiring","AppPrice",500,new ConfigDescription("Personal money price for the app.",new AcceptableValueRange<int>(1,1000000)));
  HirePrice=Config.Bind("Hiring","RecruitPrice",750,new ConfigDescription("Personal money per recruit.",new AcceptableValueRange<int>(1,1000000)));
  MaxWorkers=Config.Bind("Hiring","MaximumPerPlayer",8,new ConfigDescription("Maximum recruits per player.",new AcceptableValueRange<int>(1,16)));
  Grace=Config.Bind("Behaviour","PlayerAnswerGraceSeconds",6f,new ConfigDescription("Wait before taking an unoccupied ringing desk.",new AcceptableValueRange<float>(0f,30f)));
  ContentRegistry.RegisterApp(this,new OsAppSpec { Id=App,Title="Hire Scammers",IconResource="KolkataOS/SVG/phone-in-talk",ContentType="placeholder",ShowOnDesktop=true,DesktopOrder=98,WindowLeft=160,WindowTop=80,WindowWidth=680,WindowHeight=560,MinWindowWidth=480,MinWindowHeight=320,AccentColor="#27734A",InitialState="closed",Access=new OsAppAccessSpec {Mode="catalog",ProductId=Product,UnlockDay=UnlockDay},Content=new OsAppContentSpec {Title="Hire Scammers"}});
  ContentRegistry.RegisterProduct(this,new StoreProductSpec {Id=Product,AppId=App,Title="Hire Scammers",Description="Available Week 2 (Day 6). Complete "+Required.Value+" personal scams, then hire autonomous coworkers. Recruitment is paid from your personal wallet.",IconResource="KolkataOS/SVG/phone-in-talk",Price=LicensePrice.Value,Ownership="run",Category="functional",AppCategory="fun",AccessMode="purchase",UnlockDay=UnlockDay,AccentColor="#27734A"});
  ScammerScreens.Register(this);
  harmony=new Harmony(Id);CallerRouting.Install(harmony);GuestCompatibility.Install(harmony);EmployeeReview.Install(harmony);EmployeeBoard.Install(harmony);EmployeeDesktop.Install(harmony);
  Patch(typeof(KolkataOsController).Assembly.GetType("Kolkata.UI.KolkataOsApplicationStoreCatalog"),"SetManifest",nameof(Store));
  Patch(typeof(KolkataOsController),"BuildDesktop",nameof(Definitions));
  Patch(typeof(KolkataOsController),"IsAppAvailableOnDesktop",nameof(AvailableOnLaptop));
  Patch(typeof(KolkataOsController),"EnsureAppContentBuilt",nameof(Content));
  Patch(typeof(KolkataOsController).Assembly.GetType("Kolkata.UI.KolkataOsContentBuilder"),"BuildAppContent",nameof(Content));
  Patch(typeof(KolkataOsPlayer),"UserCode_CmdPurchaseApplication__String",nameof(Purchase));
  Patch(typeof(NetworkedCall),"ResolveTalkerDisplayName",nameof(BotDisplayName));
  Patch(typeof(KolkataOsController).Assembly.GetType("Kolkata.UI.KolkataOsContentBuilder"),"ResolveCallTalkerDisplayName",nameof(BotDisplayName));
  Patch(typeof(NetworkedCall),"TargetGameplayPrompts",nameof(PromptOwner));
  Patch(typeof(ScammerWorkspace),"ResolveWorkerPlayerVoice",nameof(ResolveWorker));

  Patch(typeof(KolkataPlayerVoice),"ServerWarmUpTts",nameof(Warmup));
  Patch(typeof(KolkataOsPlayer),"ServerCreditScamPayout",nameof(Credit));
  Patch(typeof(KolkataOsPlayer),"GetVisibleTaskAppsForWorkspace",nameof(EmployeeVisibleApps));
  var runner=new GameObject("HireScammers.Runtime"); DontDestroyOnLoad(runner); runner.AddComponent<OfficeCharacters>(); runner.AddComponent<AgentAi>(); runner.AddComponent<Runtime>();
  ModMenu.AddPage(this,"Hire Scammers",BuildPanel);
  if(Environment.GetCommandLineArgs().Contains("--hire-scammers-smoke"))runner.AddComponent<Diagnostics>();
  ModLog.LogInfo("Hire Scammers loaded. Buy the app in Scamazon after personal scams, or open F1 > Hire Scammers.");
 }
 void Patch(Type t,string name,string hook,bool optional=false) {
  var m=AccessTools.Method(t,name); if(m==null){if(optional)return;throw new MissingMethodException(t.Name,name);}
  harmony.Patch(m,prefix:new HarmonyMethod(typeof(Plugin),hook));
 }
 static bool AvailableOnLaptop(object __0,ref bool __result){if((string)Get(__0,"id")!=App&&(string)Get(__0,"id")!=ScammerScreens.App)return true;__result=true;return false;}
 static bool EmployeeVisibleApps(KolkataOsPlayer __instance,ScammerWorkspace __0,ref string[] __result){
  if(!NetworkServer.active||!__0||!__0.workerPlayerVoice)return true;var bot=__0.workerPlayerVoice.GetComponent<Bot>();if(!bot||!bot.Owner||bot.Owner.GetComponent<KolkataOsPlayer>()!=__instance)return true;
  __result=string.IsNullOrEmpty(bot.DesktopApp)?new string[0]:new[]{bot.DesktopApp};return false;
 }
 protected override void OnModUnload(){if(Runtime.Instance)Destroy(Runtime.Instance.gameObject);harmony?.UnpatchSelf();Instance=null;}
 internal static object Get(object o,string n){if(o==null)return null;var f=o.GetType().GetField(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);if(f!=null)return f.GetValue(o);return AccessTools.Property(o.GetType(),n)?.GetValue(o,null);}
 internal static void Set(object o,string n,object v){var f=AccessTools.Field(o.GetType(),n);if(f!=null)f.SetValue(o,v);else AccessTools.Property(o.GetType(),n).SetValue(o,v,null);}
 static void Definitions(object __instance) {
  var defs=Get(__instance,"m_appDefinitions") as IList;if(defs==null)return;
  foreach(var spec in ContentRegistry.RegisteredApps.Where(a=>a.Id==App||a.Id==ScammerScreens.App)){
   if(defs.Cast<object>().Any(d=>(string)Get(d,"id")==spec.Id))continue;
   var type=typeof(KolkataOsController).Assembly.GetType("Kolkata.UI.KolkataOsAppDefinition");object app=JsonConvert.DeserializeObject(JsonConvert.SerializeObject(spec.Spec),type,null);AccessTools.Method(__instance.GetType(),"NormalizeDefinition").Invoke(__instance,new[]{app});defs.Add(app);
  }
 }
 static void Store(object __0){
  if(__0==null)return;var original=(Array)Get(__0,"products");
  if(original!=null)foreach(object p in original)if((string)Get(p,"id")==Product)return;
  var entry=ContentRegistry.RegisteredProducts.FirstOrDefault(p=>p.Id==Product);if(entry==null)return;
  var field=AccessTools.Field(__0.GetType(),"products");var type=field.FieldType.GetElementType();
  object definition=JsonConvert.DeserializeObject(JsonConvert.SerializeObject(entry.Spec),type,null);
  int count=original==null?0:original.Length;var products=Array.CreateInstance(type,count+1);if(count>0)Array.Copy(original,products,count);products.SetValue(definition,count);field.SetValue(__0,products);
 } static bool Content(object __0) {
  if((string)Get(Get(__0,"Definition"),"id")==ScammerScreens.App){var panel=(VisualElement)Get(__0,"WindowContent");if(panel!=null&&(panel.childCount==0||panel[0].name!="scammer-screens-view"))ScammerScreens.Build(panel);Set(__0,"ContentBuilt",true);return false;}
  if((string)Get(Get(__0,"Definition"),"id")!=App)return true;

  var root=(VisualElement)Get(__0,"WindowContent");if(root==null)return false;
  Set(__0,"NeedsAutoSize",false);
  if(root.childCount==0||root[0].name!=ViewId)BuildPanel(root);
  Set(__0,"ContentBuilt",true);return false;
 }
 static bool Purchase(KolkataOsPlayer __instance,string __0) {
  if(__0!=Product)return true;
  string reason=Runtime.PurchaseBlockReason(__instance);
  if(reason==null)return true; // Native Scamazon performs pricing and the transaction.
  if(__instance){
   AccessTools.Method(typeof(KolkataOsPlayer),"SendApplicationTransaction").Invoke(__instance,new object[]{Product,false,false,true,reason,new string[0]});
   Debug.Log("[HireScammers] Purchase declined: "+reason);
  }
  return false;
 }
 static bool BotDisplayName(KolkataPlayerVoice __0,ref string __result){var bot=__0?__0.GetComponent<Bot>():null;if(!bot)return true;__result=bot.DisplayName;return false;}
 internal static KolkataPlayerVoice OwnerVoice(PlayerName owner){return owner?owner.GetComponentsInChildren<KolkataPlayerVoice>(true).FirstOrDefault(v=>!v.GetComponent<Bot>()):null;}
 static void PromptOwner(ref KolkataPlayerVoice __1){var bot=__1?__1.GetComponent<Bot>():null;if(bot&&bot.Owner)__1=OwnerVoice(bot.Owner);}
 static bool ResolveWorker(ScammerWorkspace __instance,ref KolkataPlayerVoice __result){
  var call=__instance.activeCall;var voice=call?call.talker:null;
  if(voice&&voice.GetComponent<Bot>()&&(!__instance.chairInteractable||!__instance.chairInteractable.IsOccupied)){__result=voice;return false;}return true;
 }
 static bool Warmup(KolkataPlayerVoice __instance){return !__instance.GetComponent<Bot>();}
 static bool Credit(KolkataOsPlayer __instance,Guid __0,int __1,object[] __args){
  var bot=Runtime.ForCall(__0);if(!bot||__1<=0){if(__1>0)Runtime.RecordPersonalScam(__instance,__0);return true;}
  // Native rewards are credited without inflating the owner's personal unlock counter.
  NativeScamPayout.CreditMoney(__instance,__1,__args.Length>2?__args[2] as string:null);bot.Earned+=__1;bot.DayEarned+=__1;
  Debug.Log("[HireScammers] Native payout: "+bot.DisplayName+" +$"+__1);return false;
 }
 public static void BuildPanel(VisualElement root) {
  root.Clear();root.style.flexGrow=1;root.style.minHeight=0;
  var scroll=new ScrollView(){name=ViewId};scroll.style.flexGrow=1;scroll.style.minHeight=0;root.Add(scroll);
  root=scroll.contentContainer;root.style.paddingLeft=root.style.paddingRight=16;root.style.paddingTop=12;root.style.paddingBottom=16;
  var title=new Label("Hire Scammers");title.style.fontSize=24;root.Add(title);
  var status=new Label(){name="hire-status"};status.style.whiteSpace=WhiteSpace.Normal;root.Add(status);
  var buy=new Button(()=>Runtime.Request(0)){text="Buy app",name="hire-buy-app"};root.Add(buy);
  var characters=new DropdownField("Character",new List<string>{"Loading office characters…"},0);characters.name="hire-character";root.Add(characters);
  var name=new TextField("Recruit name"){value="",maxLength=24,name="hire-name"};root.Add(name);
  characters.RegisterValueChangedCallback(e=>{var model=OfficeCharacters.ModelForName(e.newValue);if(model!=null)name.value=e.newValue;});
  var hire=new Button(()=>Runtime.Request(1,name.value,OfficeCharacters.ModelForName(characters.value))){text="Hire recruit",name="hire-recruit"};root.Add(hire);
  WorkerSettings.Build(root);
  var roster=new Label();roster.style.whiteSpace=WhiteSpace.Normal;root.Add(roster);
  var explanation=new Label("Each recruit gets a distinct outfit and an unused character look when available. The default office cast appears only when hired. Recruits use the built-in call-center characters, like Pooja, and share your purchased scam tools and credit successful calls to you. They use your game AI settings and API key for dialogue, roam the office, and run to unoccupied ringing desks. Calls can fail. No income is generated while idle. Names appear as Owner's Recruit.\nAll players need this mod. Native run saves retain your app and recruits. New runs start locked.");explanation.style.whiteSpace=WhiteSpace.Normal;root.Add(explanation);
  Action refresh=()=> {
   var choices=OfficeCharacters.Choices.Select(c=>c.Name).Distinct().ToList();
   if(choices.Count>0&&!characters.choices.SequenceEqual(choices)){
    string old=characters.value;characters.choices=choices;if(!choices.Contains(old))characters.value=choices.Contains("Pooja")?"Pooja":choices[0];
    if(string.IsNullOrWhiteSpace(name.value))name.value=characters.value;
   }
   var p=KolkataOsPlayer.LocalPlayerInstance; bool owned=Runtime.Owns(p,Product);
   status.text=p?"Unlocks Week 2 (Day 6). Personal wallet: $"+p.SpendableMoney+" | Personal callers scammed: "+Runtime.PersonalCount(p)+" / "+Required.Value+"\n"+(owned?"Open the app to hire a recruit.":(Runtime.PurchaseBlockReason(p)??"Ready to buy."))+"\n"+Runtime.Notice:"Join a modded host and start a run to hire scammers.";
   buy.text="Buy Hire Scammers — $"+LicensePrice.Value;buy.SetEnabled(p&&Runtime.CanBuy(p)&&p.SpendableMoney>=LicensePrice.Value);
   buy.style.display=owned?DisplayStyle.None:DisplayStyle.Flex;
   hire.text="Hire recruit — $"+HirePrice.Value;hire.SetEnabled(p&&owned&&OfficeCharacters.Ready&&p.SpendableMoney>=HirePrice.Value);
   roster.text=Runtime.RosterText();
  };
  refresh();root.schedule.Execute(refresh).Every(500);
  Style(root);title.style.fontSize=24;title.style.marginBottom=10;
 }
 static void Style(VisualElement v){
  v.style.color=new Color(.08f,.12f,.16f);
  if(v is TextElement)v.style.fontSize=14;
  if(v is Button){v.style.minHeight=34;v.style.flexShrink=0;v.style.marginTop=8;v.style.marginBottom=8;v.style.backgroundColor=new Color(.88f,.94f,.9f);}
  if(v is TextField||v is DropdownField){v.style.minHeight=34;v.style.flexShrink=0;v.style.marginTop=8;}
  if(v.ClassListContains("unity-text-field__input")||v.ClassListContains("unity-base-popup-field__input")){
   v.style.backgroundColor=new Color(.95f,.96f,.98f);v.style.color=new Color(.08f,.12f,.16f);v.style.paddingLeft=v.style.paddingRight=8;
  }
  for(int i=0;i<v.childCount;i++)Style(v[i]);
 }
}
}

























