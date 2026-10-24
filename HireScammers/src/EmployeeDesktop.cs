using System;
using System.Collections;
using System.Linq;
using System.Threading;
using HarmonyLib;
using Cysharp.Threading.Tasks;
using Kolkata.UI;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;
namespace HireScammers {
// Each viewer renders the native OS locally. Only the server submits gameplay actions.
public sealed partial class EmployeeDesktop : MonoBehaviour {
 readonly System.Collections.Generic.Dictionary<VisualElement,Action> nativeClicks=new System.Collections.Generic.Dictionary<VisualElement,Action>();
 static EmployeeDesktop initializing;float nextCursorUpdate,nextCallRefresh;volatile bool callRefreshPending;Vector2 lastCursor=new Vector2(-1,-1);
 KolkataOsController controller;UIDocument document;PanelSettings panel;RenderTexture texture;GameObject surface;Material material;Bot bot;ScammerWorkspace workspace;Renderer[] covered;bool[] wasEnabled;CancellationTokenSource lifetime;bool ready;string opened;Label cursor;
 internal static void Install(Harmony harmony){
  var registration=typeof(KolkataOsController).Assembly.GetType("Kolkata.UI.KolkataOsEventRegistry").GetMethods(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).First(m=>m.Name=="RegisterCallback"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==2&&m.GetParameters()[1].ParameterType==typeof(Action)).MakeGenericMethod(typeof(ClickEvent));harmony.Patch(registration,postfix:new HarmonyMethod(typeof(EmployeeDesktop),"CaptureClick"));
  harmony.Patch(AccessTools.Method(typeof(KolkataOsController),"BuildDesktop"),prefix:new HarmonyMethod(typeof(EmployeeDesktop),"TrimEmployeeApps"){priority=Priority.Last});
  harmony.Patch(AccessTools.Method(typeof(Focusable),"Focus"),prefix:new HarmonyMethod(typeof(EmployeeDesktop),"ProtectPlayerFocus"));
  foreach(string name in new[]{"OnEnable","OnDisable","Update","EnsureBrowserHost","OpenCameraOnOsOpen","InitializeVirusRuntime","ScheduleAffordableAppNotifications","PublishVisibleTaskApps"}){var method=AccessTools.Method(typeof(KolkataOsController),name);if(method!=null)harmony.Patch(method,prefix:new HarmonyMethod(typeof(EmployeeDesktop),"Skip"));}
  harmony.Patch(AccessTools.Method(typeof(KolkataOsController),"OnVictimCallUiStateChanged"),prefix:new HarmonyMethod(typeof(EmployeeDesktop),"DeferCallRefresh"));
  harmony.Patch(AccessTools.Method(typeof(KolkataOsController),"VerifyScamField"),prefix:new HarmonyMethod(typeof(EmployeeDesktop),"VerifyEmployee"));
  harmony.Patch(AccessTools.Method(typeof(KolkataOsController),"BuildDesktop"),postfix:new HarmonyMethod(typeof(EmployeeDesktop),"MemoryStore"));
 }
 static void CaptureClick(object __instance,VisualElement __0,Action __1){if(__0==null||__1==null)return;foreach(var display in Resources.FindObjectsOfTypeAll<EmployeeDesktop>())if(display&&display.controller&&Plugin.Get(display.controller,"m_eventRegistry")==__instance){display.nativeClicks[__0]=__1;break;}}
 static void TrimEmployeeApps(KolkataOsController __instance){if(!__instance.GetComponent<EmployeeDesktop>())return;var definitions=Plugin.Get(__instance,"m_appDefinitions") as IList;if(definitions==null)return;var allowed=new System.Collections.Generic.HashSet<string>(ScamTypeLibrary.LoadAll().Where(s=>s!=null).Select(s=>s.appId));allowed.Add("phone");for(int i=definitions.Count-1;i>=0;i--)if(!allowed.Contains((string)Plugin.Get(definitions[i],"id")))definitions.RemoveAt(i);}
 static bool ProtectPlayerFocus(Focusable __instance){for(var element=__instance as VisualElement;element!=null;element=element.parent)if(element.ClassListContains("hire-scammers-passive-panel"))return false;return true;}
 static bool Skip(KolkataOsController __instance){return !__instance.GetComponent<EmployeeDesktop>();}
 static bool DeferCallRefresh(KolkataOsController __instance){var display=__instance.GetComponent<EmployeeDesktop>();if(!display)return true;display.callRefreshPending=true;return false;}
 static void MemoryStore(KolkataOsController __instance){
  if(!__instance.GetComponent<EmployeeDesktop>())return;var interaction=Plugin.Get(__instance,"m_desktopInteraction");if(interaction==null)return;
  var asm=typeof(KolkataOsController).Assembly;var state=Activator.CreateInstance(asm.GetType("Kolkata.UI.KolkataOsDesktopSessionState"),true);
  var store=Activator.CreateInstance(asm.GetType("Kolkata.UI.KolkataOsMemoryDesktopStateStore"),new[]{state});Plugin.Set(interaction,"m_store",store);
 }
 internal static EmployeeDesktop Create(Bot bot,ScammerWorkspace desk){
  var go=new GameObject("Employee native desktop");go.SetActive(false);var display=go.AddComponent<EmployeeDesktop>();display.bot=bot;display.workspace=desk;display.lifetime=new CancellationTokenSource();
  try{
   var source=KolkataOsSingleton.Instance?Plugin.Get(KolkataOsSingleton.Instance,"kolkataOsDocument") as UIDocument:null;if(!source||!source.visualTreeAsset||!source.panelSettings)throw new InvalidOperationException("Native desktop template unavailable");
   display.texture=new RenderTexture(1920,1080,24);display.texture.filterMode=FilterMode.Trilinear;display.texture.anisoLevel=8;display.texture.Create();display.panel=UnityEngine.Object.Instantiate(source.panelSettings);display.panel.targetTexture=display.texture;display.panel.scale=1;display.panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;display.panel.referenceResolution=new Vector2Int(1920,1080);
   display.document=go.AddComponent<UIDocument>();display.document.panelSettings=display.panel;display.document.visualTreeAsset=source.visualTreeAsset;
   display.panel.SetScreenToPanelSpaceFunction(point=>new Vector2(-100000,-100000));
   display.controller=go.AddComponent<KolkataOsController>();display.controller.enabled=true;go.SetActive(true);display.StartCoroutine(display.Initialize());return display;
  }catch{Destroy(go);throw;}
 }
 internal bool Ready{get{return ready;}}
 internal UIDocument Document{get{return document;}}
 internal void Capture(string path){Debug.Log("[HireScammers] DESKTOP app="+opened+" defs="+((IList)Plugin.Get(controller,"m_appDefinitions")).Count+" content="+document.rootVisualElement.ToString()+" window="+Plugin.Get(controller,"m_windowLayer"));foreach(var element in document.rootVisualElement.Query<VisualElement>().ToList().Where(e=>e.name=="window-layer"||e.name=="desktop-icons"||e.name=="kolkata-os-root"))Debug.Log("[HireScammers] UI "+element.name+" children="+element.childCount+" size="+element.layout+" display="+element.resolvedStyle.display+" opacity="+element.resolvedStyle.opacity);var previous=RenderTexture.active;var image=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);try{RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);System.IO.File.WriteAllBytes(path,ImageConversion.EncodeToPNG(image));}finally{RenderTexture.active=previous;Destroy(image);}}
 IEnumerator Initialize(){var routine=InitializeCore();while(true){bool more;try{more=routine.MoveNext();}catch(Exception error){Debug.LogWarning("[HireScammers] Desktop initialization failed: "+error);Destroy(gameObject);yield break;}if(!more)yield break;yield return routine.Current;}}
 IEnumerator InitializeCore(){
  while(initializing&&initializing!=this)yield return null;initializing=this;
  yield return null;document.rootVisualElement.AddToClassList("hire-scammers-passive-panel");var asm=typeof(KolkataOsController).Assembly;
  Plugin.Set(controller,"m_root",document.rootVisualElement);AccessTools.Method(typeof(KolkataOsController),"SetVisualElements").Invoke(controller,null);Plugin.Set(controller,"m_eventRegistry",Activator.CreateInstance(asm.GetType("Kolkata.UI.KolkataOsEventRegistry"),true));
  Plugin.Set(controller,"m_assets",Activator.CreateInstance(asm.GetType("Kolkata.UI.KolkataOsAssetLoader"),new object[]{Plugin.Get(controller,"StreamingRoot")}));
  Plugin.Set(controller,"m_applicationAccess",Activator.CreateInstance(asm.GetType("Kolkata.UI.KolkataOsApplicationAccess"),true));Plugin.Set(controller,"m_uiAudio",null);
  var source=KolkataOsSingleton.Instance.Controller;var portraitFields=new[]{"m_callerHeadTexture","m_callerNormalEyesTexture","m_callerAngryEyesTexture","m_callerScaredEyesTexture","m_callerConfusedEyesTexture","m_callerBlinkEyesTexture","m_callerClosedMouthTexture","m_callerOpenMouthTexture"};var portraitTextures=portraitFields.Select(f=>Plugin.Get(source,f)).ToArray();for(int i=0;i<portraitFields.Length;i++)Plugin.Set(controller,portraitFields[i],portraitTextures[i]);
  Plugin.Set(controller,"m_callerWebcamPresenter",Activator.CreateInstance(asm.GetType("Kolkata.UI.KolkataOsCallerWebcamPresenter"),portraitTextures));
  var operation=(UniTask)AccessTools.Method(typeof(KolkataOsController),"InitializeAsync").Invoke(controller,new object[]{lifetime.Token});var awaiter=operation.GetAwaiter();
  while(!awaiter.IsCompleted)yield return null;
  try{awaiter.GetResult();BuildSurface();cursor=new Label("↖");cursor.style.position=Position.Absolute;cursor.style.fontSize=28;cursor.style.color=Color.white;cursor.pickingMode=PickingMode.Ignore;document.rootVisualElement.Add(cursor);document.rootVisualElement.RegisterCallback<PointerDownEvent>(e=>e.StopImmediatePropagation(),TrickleDown.TrickleDown);document.rootVisualElement.RegisterCallback<KeyDownEvent>(e=>e.StopImmediatePropagation(),TrickleDown.TrickleDown);ready=true;initializing=null;Debug.Log("[HireScammers] Native employee desktop ready.");}catch(Exception error){Debug.LogWarning("[HireScammers] Desktop initialization failed: "+error.GetType().Name);Destroy(gameObject);}
 }
 void BuildSurface(){
  var screen=workspace.callStateScreen;var original=Plugin.Get(screen,"loadingScreenRenderer") as MeshRenderer;if(!original)throw new InvalidOperationException("No physical screen mesh");var mesh=original.GetComponent<MeshFilter>();if(!mesh||!mesh.sharedMesh)throw new InvalidOperationException("No screen mesh");
  surface=new GameObject("Employee desktop screen");surface.transform.SetParent(original.transform,false);surface.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
  material=new Material(original.sharedMaterial);material.mainTexture=texture;if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap",texture);if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",Color.white);if(material.HasProperty("_Color"))material.SetColor("_Color",Color.white);
  surface.AddComponent<MeshRenderer>().sharedMaterial=material;
  // Reparent out of the native state objects so phone icons cannot hide this screen.
  surface.transform.SetParent(screen.MonitorInteractable.transform,true);
  covered=(Plugin.Get(screen,"callStateObjects") as GameObject[]??new GameObject[0]).Where(g=>g).SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Distinct().ToArray();wasEnabled=covered.Select(r=>r.enabled).ToArray();
 }
 void Update(){
  if(!bot||!workspace||bot.Workspace!=workspace||!bot.IsSeated){Destroy(gameObject);return;}if(!ready)return;
  if(callRefreshPending&&Time.unscaledTime>=nextCallRefresh){callRefreshPending=false;nextCallRefresh=Time.unscaledTime+.25f;try{AccessTools.Method(typeof(KolkataOsController),"RefreshBoundVictimCallState").Invoke(controller,null);}catch(Exception error){Debug.LogWarning("[HireScammers] Deferred call UI: "+error.GetType().Name);}}
  try{MaintainPhoneWindow();}catch(Exception error){Debug.LogWarning("[HireScammers] Phone window: "+error.GetType().Name);}
  if(surface)surface.SetActive(true);foreach(var renderer in covered)if(renderer)renderer.enabled=false;
  if(cursor!=null&&Time.unscaledTime>=nextCursorUpdate){nextCursorUpdate=Time.unscaledTime+.1f;var point=(Vector2)Plugin.Get(workspace.callStateScreen.MonitorInteractable,"syncedMousePointerPosition");if((point-lastCursor).sqrMagnitude>.000001f){lastCursor=point;cursor.style.left=point.x*document.rootVisualElement.layout.width;cursor.style.top=point.y*document.rootVisualElement.layout.height;cursor.BringToFront();}}
  try{if(NetworkServer.active){if(!runningAction){var command=bot.NextDesktopAction();if(command!=null)StartCoroutine(Execute(command));}}else PassiveRefresh();}catch(Exception error){Debug.LogWarning("[HireScammers] Desktop action failed: "+error.GetType().Name);if(NetworkServer.active)bot.CompleteDesktopAction("Native desktop action failed");}
 }
 void OnDestroy(){if(initializing==this)initializing=null;if(controller){var bound=Plugin.Get(controller,"m_boundVictimCall") as VictimCall;if(bound)bound.OnPublicUiStateChanged-=(Action<VictimCall>)Delegate.CreateDelegate(typeof(Action<VictimCall>),controller,"OnVictimCallUiStateChanged");}if(lifetime!=null){lifetime.Cancel();lifetime.Dispose();}if(covered!=null)for(int i=0;i<covered.Length;i++)if(covered[i])covered[i].enabled=wasEnabled[i];if(surface)Destroy(surface);if(material)Destroy(material);if(panel)Destroy(panel);if(texture){texture.Release();Destroy(texture);}}
}
}









