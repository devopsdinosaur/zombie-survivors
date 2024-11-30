using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public static class PluginInfo {

	public const string TITLE = "Easy Mode";
	public const string NAME = "easy_mode";
	public const string SHORT_DESCRIPTION = "Lots of configurable QoL tweaks and cheats to make the game easier (or even harder)!";

	public const string VERSION = "0.0.1";

	public const string AUTHOR = "devopsdinosaur";
	public const string GAME_TITLE = "Yet Another Zombie Survivors";
	public const string GAME = "zombie-survivors";
	public const string GUID = AUTHOR + "." + GAME + "." + NAME;
	public const string REPO = "zombie-survivors-mods";

	public static Dictionary<string, string> to_dict() {
		Dictionary<string, string> info = new Dictionary<string, string>();
		foreach (FieldInfo field in typeof(PluginInfo).GetFields((BindingFlags) 0xFFFFFFF)) {
			info[field.Name.ToLower()] = (string) field.GetValue(null);
		}
		return info;
	}
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.TITLE, PluginInfo.VERSION)]
public class EasyModePlugin : DDPlugin {
	private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	public override void Load() {
		logger = base.Log;
		try {
			this.m_plugin_info = PluginInfo.to_dict();
			Settings.Instance.load(this);
			DDPlugin.set_log_level(Settings.m_log_level.Value);
			this.create_nexus_page();
			this.m_harmony.PatchAll(); ;
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(Health), "Update")]
	class HarmonyPatch_Health_Update {
		private static bool Prefix(Health __instance) {
			try {
				if (Settings.m_enabled.Value && Settings.m_infinite_health.Value) {
					__instance.SetHealth(99999f);
				}
				return true;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Health_Update.Prefix ERROR - " + e);
			}
			return true;
		}
	}

}