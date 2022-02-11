# Bannerlord-QOLfixes

This mod provides various QOL fixes and a waypoint mechanic explained below.  
  
1) TW has a total of 12 loading screens with beautiful artwork. Sadly they load those in a deterministic way. This means an average person won't see much more than 4 or 5. The loading screens will follow the same old pattern every time you re-open the game. And change only after you've seen them once.  **I randomized them. As a test I've uploaded all 12 of the screens on my [mod](https://www.nexusmods.com/mountandblade2bannerlord/mods/3731) page on nexusmods. See how many you have actually seen**  

2) Left-clicking on map while in Fast Forward Mode used to switch to normal speed or play mode. Now you get to keep your Fast Forward Speed.
  
3) You can change Campaign Fast Forward Speed through hotkeys. A much shorter version of console command. Just Press **Shift+Z** or **Shift+X** to change speed.
  
4) Now you can set `StopGameOnFocusLost` to false in the game options but still stop the game automatically while during battles. The game option uses the setting for both campaign map and during battles/mission. Added an extra option for battles. See the config file.
  
5) This also includes my  [Skip Intro And Character Creation mod](https://www.nexusmods.com/mountandblade2bannerlord/mods/3696)﻿ which basically skips all the intro videos and gives a quick start by skipping character creation for modders or people testing something 
  
6) Also includes my  [No Pause On Entering Settlement Mod](https://www.nexusmods.com/mountandblade2bannerlord/mods/3704).﻿ Pretty self-explanatory. Useful when you join armies. Now you can just Alt-Tab out and let things play out.

5) New Waypoint mechanic.

### WAYPOINTS EXPLAINED  

Now you can `Shift + left-click` on Settlement nameplates (the widget hovering above every settlement) to add them as a waypoint and create a route you want to follow. Then use `Shift+C` to start traveling on this route.  You can detour while traveling on this route. Just press `Shift+C` again to start following the route again. **Every time you enter a settlement that waypoint is removed and upon leaving you automatically start travelling towards the next one.** This saves much time especially if you are doing trade runs. No more zooming out clicking to settlements after every time you leave one  

The waypoint system uses the existing settlement tracking system and UI as I'm really not good with UI stuff. However, it's made to co-exist with the original settlement tracking feature. What this means is, you can now track settlements twice. Once through the button manually and once by adding waypoints. 

So for e.g,  You track the settlement manually, you add it to the waypoint. When you reach the settlement the waypoint is removed but the visual tracker marker doesn't disappear since it's also being tracked manually. Hence manual tracking goes side-by-side with waypoint tracking. Neither interferes each other. 
However you won't be able to differentiate between the two.
  
In short, you can have certain settlements like your homeland tracked manually AND still use them as waypoints. Clearing waypoints through hotkeys or by reaching that settlement WON'T CLEAR YOUR MANUAL TRACKING.  
  
### Hotkeys Avaiable  
  
`Shift + LMB` = Add waypoints (Click on Settlement Nameplate widgets, those half transparent bars that have circular buttons)  
`Shift + RMB` = Remove All waypoints (Click anywhere)  
`Shift + C` = Start Traveling towards the waypoints in order. You can detour and press this again to go back on track  
`Shift + V` = Remove the very first waypoint from the "queue". Useful if the current waypoint is being raided or you can't get in.  
`Shift + Z` = Decrease Fast forward Speed Multiplier by 1 - capped to 4.  
`Shift + X` = Increase Fast forward Speed Multiplier by 1 - uncapped.
