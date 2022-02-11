using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.InputSystem;

namespace QOLfixes
{
    public class CustomMapHotkeyCategory : GameKeyContext
    {
		public const string clearWaypointKeyName = "ClearWaypoints";
		public const string startWaypointTravelKeyName = "StartWaypointTravel";
		public const string dequeueWaypointKeyName = "DequeueWaypoint";
		public const string IncreaseFFSpeedKeyName = "IncreaseFastForwardSpeed";
		public const string DecreaseFFSpeedKeyName = "DecreaseFastForwardSpeed";
		public CustomMapHotkeyCategory() : base("CustomMapHotkeyCategory", 0, GameKeyContext.GameKeyContextType.AuxiliarySerialized)
		{
			this.RegisterHotKeys();
		}
		public void RegisterHotKeys()
		{	
			List<Key> keys = new List<Key>
			{
				new Key(InputKey.Z),
			};
			base.RegisterHotKey(new HotKey("DecreaseFastForwardSpeed", "MapHotKeyCategory", keys, HotKey.Modifiers.Shift, HotKey.Modifiers.None), true);

			keys = new List<Key>
			{
				new Key(InputKey.X),
			};
			base.RegisterHotKey(new HotKey("IncreaseFastForwardSpeed", "MapHotKeyCategory", keys, HotKey.Modifiers.Shift, HotKey.Modifiers.None), true);

			if (!ConfigFileManager.EnableWaypoints)
				return;

			//Defining hotkey for adding waypoint is redundant since coding logic requires this to be hardcoded inside callback of clicking on a settlementNameplate.
			keys = new List<Key>
			{
				new Key(InputKey.LeftMouseButton),
			};
			base.RegisterHotKey(new HotKey("AddWaypoint", "MapHotKeyCategory", keys, HotKey.Modifiers.Shift, HotKey.Modifiers.None), true);

			keys = new List<Key>
			{
				new Key(InputKey.RightMouseButton),
			};
			base.RegisterHotKey(new HotKey("ClearWaypoints", "MapHotKeyCategory", keys, HotKey.Modifiers.Shift, HotKey.Modifiers.None), true);

			keys = new List<Key>
			{
				new Key(InputKey.C),
			};
			base.RegisterHotKey(new HotKey("StartWaypointTravel", "MapHotKeyCategory", keys, HotKey.Modifiers.Shift, HotKey.Modifiers.None), true);

			keys = new List<Key>
			{
				new Key(InputKey.V),
			};
			base.RegisterHotKey(new HotKey("DequeueWaypoint", "MapHotKeyCategory", keys, HotKey.Modifiers.Shift, HotKey.Modifiers.None), true);
		}
	}
}
