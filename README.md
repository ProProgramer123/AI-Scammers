# AI Scammers

Hire Scammers 0.3.8 — a BepInEx mod tested with Scam With Your Friends 0.1.89-swyf / Unity 6000.3.26f1.

Install through [Switchboard](https://switchboard.ras-rap.click/#/), or download the release ZIP and follow [Windows and Linux setup](INSTALL.md). Switchboard catalogue submission is subject to maintainer review; the GitHub ZIP is available directly.

## Play

The host must install the mod: recruitment, movement, desk assignments and earnings are server-authoritative. Players who hire employees also need the same mod version to use the app and their own AI settings. Unmodded guests can join; a compatibility NPC uses the native Scammer NPC prefab, appearance/name SyncVars and networked bone transforms. Custom entities and messages are sent only to identified modded peers. This guest compatibility path is experimental until verified with a separate vanilla client. Vanilla guests do not get the hiring app, employee desktop rendering or extra live agent-board rows. Native review slides and owner wallet changes use the game's existing networking. Joining an unmodded host does not enable employees: the mod waits for the host's Steam lobby advertisement before sending its handshake.

1. Finish the tutorial and personally scam 3 different callers.
2. Reach Week 2 (Day 6), then buy Hire Scammers in Scamazon for $500 of personal money.
3. Open Hire Scammers on an office computer or the laptop, choose an office character and name, and hire them for $750 each. Default limit: 8 employees per player (configurable up to 16; 32 workers per server). F1 > Hire Scammers also opens the controls.

Native sandbox purchase rules still apply. Prices, required personal scams, recruit limit and human-answer grace period are configurable in BepInEx/config/com.local.scamwyf.hirescammers.cfg.

## Employees

Employees use the game's office cast, distinct saved appearances and outfit colors. Default Scammer NPC placements are hidden and their chairs released. Their source assets remain intact; other NPC families are unaffected.

Names use a head-relative anchor, bounded text size and the game's native PlayerNameText tag and identify the hiring player and recruit, for example unknown's Pooja. Detailed status and errors appear in the hiring roster instead of covering the screen above a character.

Employees roam the work floor, allow humans six seconds to answer, then claim an unattended ringing desk, run to it and pick up through the native call system. Humans sitting at their desk take priority. Navigation includes floor projection, body collision checks and alternate computer approaches.

Version 0.2.0 uses the native body animator/controller, native step-arm poses, seating and monitor hand IK. The old replacement bone animation has been removed. Version 0.2.1 rebuilds the native leg chains, binds the native animation helpers to the body animator, and supplies alternating grounded footstep targets to the game's native leg IK solver after animation. The stride is driven by distance travelled, with separate walk/run reach and foot lift. Seated keyboard IK suppresses both native arm-swing helpers.

An occupied employee monitor renders a separate instance of the native OS at 1920 x 1080. Version 0.3.1 automatically opens its native Phone window beside the scam app during normal calls. Watch the employee's physical monitor; taking their seat interrupts them because human players have priority. The AI chooses an owned, usable scam from a supplied menu, opens its native app, reads the available form controls and requests actual typing and button clicks. The desktop cursor follows those controls; native input change and click callbacks perform the actions. Disabled controls are respected. Payment values must already appear in the public caller transcript. Native verification is dispatched only by an employee desktop button, with the selected scam's source and sentinel IDs; there is no automatic code submission from dialogue. The selected app is also reported to the native caller prompt context. Native mouse positions, the employee's typing state and submitted public caller codes are synchronized. The monitor is an observer view: gameplay submissions occur once on the server. It has an independent in-memory desktop store. It does not clone player input, open another browser process, or give an employee a separate wallet.

The live agent/quota board includes employee rows and earnings, with owner earnings adjusted for display attribution. Employees join the presentation room and are included in native review snapshots. A Hired Scammers slide lists today's and session earnings. Income is credited to the owner's real wallet; employee earnings are separated for review attribution without issuing another payout.

## AI settings

Version 0.3.1 fixes the native victim-request check that rejected employees because their voice proxy is not a local player. Only the employee owner's real local client may forward the request to the native client runner; native provider settings, payload limits, cancellation and server response validation remain in effect.

Dialogue generation runs on the employee owner's client using that player's existing game settings. Bring Your Own API Key / OpenRouter uses their saved key and model; Official Backend uses their native game authentication. Keys are not stored in this mod's config or sent to the host. Configure settings before joining if the game locks them in a lobby.

Version 0.2.0 supplies the native official backend's required prompt boundary. The previous missing boundary caused the request to fail before it could reach the service. Structured AI responses are parsed into dialogue, scam choice and bounded desktop commands, including plans nested inside the official backend's dialogue envelope. Trust, emotion and objective metadata are never sent as employee speech. Provider errors are shown in the roster and retries are delayed. Requests use the provider's normal credits/usage.

The game AI caller and native scam adjudication determine success. Only real successful native payouts earn money; there is no timed income or guaranteed success. Employee payouts do not advance the owner's personal-call unlock counter. Both caller and worker speech now route through the hiring player's native TTS client and the desk's native network speaker. Worker voice can be muted or changed in the hiring app. Actual speech generation and separate-client audio remain live-test items. Specialized scams requiring complex world interactions may not complete autonomously.

## Saves and limits

The native run entitlements save the app, personal unlock progress, hired names and appearances. Active employees are recreated when an entitled owner rejoins. Session earnings reset when an employee respawns; today's earnings reset at the next workday. Quitting removes active mod objects. A fresh run begins locked.

In-game diagnostics verify native animation bone movement, visibility, floors, wall blocking, travel, pickup, desk binding, desktop rendering, nametags, review roster/slide insertion, purchases and official request preparation. No external AI request or user save write is performed by diagnostics. Full successful AI conversations/payouts and two-client modded/vanilla multiplayer rendering remain live-test items. Desktop tests use explicit synthetic plans and temporary controls to verify actual UI events; they do not demonstrate an AI-generated successful payment.

## Changes in 0.3.2

The native Phone window now initializes the victim portrait renderer and shows both sides of the transcript. The Phone uses a wider portrait-and-transcript layout. Diagnostic captures contain synthetic dialogue and a built-in caller face; they are not a successful live AI call.

Desktop plans include the native app's visible instructions, including labels drawn separately from an input field. A single unambiguous public caller code in a visible code field is typed and submitted through the real UI controls. Multiple fields or ambiguous values remain AI planned. Native objective and payment validation still determine success. The worker tracks native completed scams and can choose another owned, allowed scam while the same call remains active. Calls are bounded to ten minutes / 48 spoken turns; native hang-ups still end them.

The hiring app contains per-worker floor, allowed-scam, pause and voice controls. Only the hiring owner can change their worker. Floors must already be unlocked. Automatic follows the current work floor; older floors may have no incoming calls outside Sandbox. Assignment retains the selected floor and moves the worker to its native floor start. Configuration lasts for the lobby session; app/recruit ownership continues to use native run saves.

Native animation helpers no longer bind to the owner's controller or ragdoll. Each worker has an independent native nametag, with retries if its template was unavailable at spawn. The employee review uses the game's supported ranking layout so earnings rows render even when earnings are zero.

Worker panels reject physical pointer input and cannot take keyboard focus from player panels. Desktop initialization is serialized across employees, and unchanged cursors no longer invalidate the UI every frame. These changes reduce unnecessary work; performance during a real simultaneous player/worker session still needs observation.

## Install / remove

Install BepInEx 5.4.23.5 and mod-lib 1.1.0, then place BepInEx/plugins/ScamWYF.HireScammers.dll in the game directory. The package includes mod-lib's core DLL and license. Close the game before replacing a loaded plugin. Remove only ScamWYF.HireScammers.dll to disable this mod. HostBoss is separate and is not replaced.

## Build / diagnostics

Run HireScammers/build.ps1 with a .NET SDK installed and the game's managed assemblies present. Run the game with --hire-scammers-smoke for an isolated diagnostic session that exits afterward. The optional local fixtures/current-run.json contains sanitized day/app ownership only and is not distributed. Source is included; game assemblies and game assets are not distributed.


## Changes in 0.3.3

Cache public call JSON and completion scans until their contents change. Limit public-code UI scans to once per second. Employee desktops render at native 1080p and create only phone/scam app windows, omitting unrelated desktop apps.

The AI menu no longer includes the native list of unowned scam IDs. Exact IDs, app IDs, conversation IDs and display-name aliases resolve only within the owner's currently usable scams. Missing first-choice IDs recover by opening an allowed owned app; explicit unowned choices remain rejected.

Public code entry runs independently of the speech gate, searches the last eight caller lines, recognizes gift-card apps even when fields lack code labels, and invalidates stale in-flight plans before typing. Native submission validation remains in effect. Failed desktop actions permit another attempt.
## Changes in 0.3.4

Scammer Screens is included with the Hire Scammers license on office computers and the laptop. Select an owned worker to view their live desktop. Take control for 60 seconds, choose an owned scam app, enter field values, click app buttons, or send a spoken caller response. Renew the lease or return control explicitly. Autonomous planning pauses during control; native call hang-ups and human seat priority still apply. Controls are explicit fields/buttons beneath the live image; clicking arbitrary pixels in the image is not supported. Only the worker's owner may issue commands through the host.

Public code parsing reconstructs the game's visible transcript fragments into the number shown on screen. Numeric fields such as SSN and registration are recognized alongside gift-card fields. Native event callbacks, field format checks and server verification remain authoritative.

Caller provider errors are logged with their error code, without credentials or prompt bodies. A transient unavailable/timeout/invalid-output error gets one delayed retry. Quota, authentication and rate-limit failures do not. Backend service notices stay visible as errors rather than being voiced or treated as victim dialogue; automated dialogue backs off for 30 seconds. Actual backend availability still depends on the selected provider.
## Changes in 0.3.5

Verified desktop submissions now credit the newly awarded native payout through the owner's real money path. This updates the owner's spendable balance, team quota total, worker earnings, native agent board and review snapshots. Repeated verification pays zero under the native verifier. Employee payouts still do not count toward personally scamming callers to unlock hiring.

Completed conversation scam IDs resolve to the same canonical IDs used by the employee planner. A completed scam advances to another available owned scam in the same call, invalidating any pending plan for the finished scam. Existing missed payouts are not reconstructed from old screenshots or transcripts.
## Changes in 0.3.8

Updates compatibility for game 0.1.89. Restores the human-call speech priority, employee UI refresh and payout fixes previously available only in local builds. Native payout APIs are resolved with their current scam-ID and income-flag arguments; human Verify actions pass through immediately to the game. Employee keyboard targets accept the game's new optional animation/time arguments and retain independent timing.

Automatic code entry now checks explicit digit/character lengths shown by the selected native field. A nine-digit Social Security number is not entered into a card field requiring twelve digits. Correct public caller codes still use the actual native input and submit controls.

The package contains the plugin, required shared mod library, Windows and Linux/Proton scripts, source and checksums. Credentials, game assemblies, saves, recordings and diagnostic logs are excluded. Native provider service availability, actual microphone/audio playback, Linux gameplay and separate-client compatibility remain outside the automated tests; this release does not guarantee those paths. See verification.txt for the exact checks run.

Build from source with a .NET SDK: run 

    .\HireScammers\build.ps1 -GameDir 'C:\path\to\Scam With Your Friends'

The release ZIP places ScamWYF.HireScammers.dll and ScamWYF.Modding.Core.dll directly at its root for Switchboard. The included setup scripts verify these files and install them into BepInEx/plugins and BepInEx/core respectively. The source repository keeps its BepInEx directory layout; the scripts support either layout.
