using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class PluginInfo {

	public const string TITLE = "Easy Mode";
	public const string NAME = "easy_mode";
	public const string SHORT_DESCRIPTION = "Lots of configurable QoL tweaks and cheats to make the game easier (or even harder)!  And more coming soon.";

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
			m_instance = this;
			this.m_plugin_info = PluginInfo.to_dict();
			Settings.Instance.load(this);
			DDPlugin.set_log_level(Settings.m_log_level.Value);
			this.create_nexus_page();
			this.m_harmony.PatchAll();
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			_error_log("** Load FATAL - " + e);
		}
	}

	public class InfiniteHealth : MonoBehaviour {
		private const float UPDATE_FREQUENCY = 1.0f;
		private float m_elapsed = UPDATE_FREQUENCY;
		private Health m_health = null;

		public static void create(DDPlugin plugin, Health health) {
			InfiniteHealth instance = plugin.AddComponent<InfiniteHealth>();
			instance.m_health = health;
		}

		[HarmonyPatch(typeof(GamePlayer), "Awake")]
		class HarmonyPatch_GamePlayer_Awake {
			private static void Postfix(GamePlayer __instance) {
				try {
					create(DDPlugin.Instance, __instance.health);
				} catch (Exception e) {
					_error_log("** InfiniteHealth.HarmonyPatch_GamePlayer_Awake.Postfix ERROR - " + e);
				}
			}
		}

		private void Update() {
			try {
				if (!(Settings.m_enabled.Value && Settings.m_infinite_health.Value && (this.m_elapsed += Time.deltaTime) >= UPDATE_FREQUENCY)) {
					return;
				}
				this.m_elapsed = 0;
				this.m_health.SetUnpauseImmunity(99999f);
			} catch (Exception e) {
				_error_log("** InfiniteHealth.update ERROR - " + e);
			}
		}
	}

	public class InfiniteAmmo {
		private const float UPDATE_FREQUENCY = 0.1f;
		private static float m_elapsed = UPDATE_FREQUENCY;
		
		[HarmonyPatch(typeof(WeaponController), "Update")]
		class HarmonyPatch_WeaponController_Update {
			private static void Postfix(WeaponController __instance) {
				try {
					if (!(Settings.m_enabled.Value && Settings.m_infinite_ammo.Value && (m_elapsed += Time.deltaTime) >= UPDATE_FREQUENCY)) {
						return;
					}
					m_elapsed = 0;
					__instance.SetFullClipAllWeapons();
				} catch (Exception e) {
					_error_log("** InfiniteAmmo.HarmonyPatch_WeaponController_Update.Postfix ERROR - " + e);
				}
			}
		}
	}

	public class WeaponModifier : MonoBehaviour {
		private const float UPDATE_FREQUENCY = 1.0f;
		private float m_elapsed = UPDATE_FREQUENCY;
		private List<int> m_modified = new List<int>();

		public static void create(DDPlugin plugin) {
			plugin.AddComponent<WeaponModifier>();
		}

		[HarmonyPatch(typeof(GamePlayer), "Awake")]
		class HarmonyPatch_GamePlayer_Awake {
			private static void Postfix() {
				try {
					create(DDPlugin.Instance);
				} catch (Exception e) {
					_error_log("** WeaponModifier.HarmonyPatch_GamePlayer_Awake.Postfix ERROR - " + e);
				}
			}
		}

		private void ensure_modified(WeaponProperties props) {
			if (m_modified.Contains(props.GetHashCode())) {
				return;
			}
			m_modified.Add(props.GetHashCode());
			props.FiresPerSecond *= Settings.m_fire_rate_multiplier.Value;
		}

		private void Update() {
			try {
				if (!(Settings.m_enabled.Value && (this.m_elapsed += Time.deltaTime) >= UPDATE_FREQUENCY)) {
					return;
				}
				this.m_elapsed = 0;
				foreach (WeaponProperties props in Resources.FindObjectsOfTypeAll<WeaponProperties>()) {
					this.ensure_modified(props);
				}
			} catch (Exception e) {
				_error_log("** WeaponModifier.update ERROR - " + e);
			}
		}
	}

	public class __Testing__ {

		[HarmonyPatch(typeof(GamePlayer), "Awake")]
		class HarmonyPatch_GamePlayer_Awake {
			private static void Postfix(GamePlayer __instance) {
				try {
					foreach (PlayerStatisticsRuntime stats in Resources.FindObjectsOfTypeAll<PlayerStatisticsRuntime>()) {
						foreach (KeyValuePair<PlayerStatistic.EType, ConfigEntry<float>> kvp in Settings.m_stat_multipliers) {
							try {
								PlayerStatistic stat = stats.GetStatistic(kvp.Key);
								stat.SetValue(stat.GetValue() * (kvp.Key == PlayerStatistic.EType.TeamMagnetRange && Settings.m_perpetual_magnet.Value ? 99999f : kvp.Value.Value));
								stat.SetDirty();
							} catch {}
						}
					}
				} catch (Exception e) {
					_error_log("** HarmonyPatch_GamePlayer_Awake.Postfix ERROR - " + e);
				}
			}
		}

		[HarmonyPatch(typeof(GamePlayer), "Update")]
		class HarmonyPatch_GamePlayer_Update {
			private const float UPDATE_FREQUENCY = 5.0f;
			private static float m_elapsed = UPDATE_FREQUENCY;

			private static void Postfix(GamePlayer __instance) {
				try {
					if ((m_elapsed += Time.deltaTime) < UPDATE_FREQUENCY) {
						return;
					}
					m_elapsed = 0;
					_debug_log(".");
					//foreach (LevelDefinition level_def in Resources.FindObjectsOfTypeAll<LevelDefinition>()) {
					//	_debug_log("-");
					//	foreach (GameModeDefinition game_mode_def in level_def.gameModeDefinitions) {
					//		_debug_log($"duration: {game_mode_def.gameModeDuration}");
					//	}
					//}
					
				} catch (Exception e) {
					_error_log("** __Testing__.HarmonyPatch_GamePlayer_Update.Postfix ERROR - " + e);
				}
			}
		}

		[HarmonyPatch(typeof(UIMainMenu), "Awake")]
		class HarmonyPatch_UIMainMenu_Awake {
			private static void Postfix(UIMainMenu __instance) {
				try {
					_debug_log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
					foreach (LevelDefinition level_def in Resources.FindObjectsOfTypeAll<LevelDefinition>()) {
						_debug_log("-");
						foreach (GameModeDefinition game_mode_def in level_def.gameModeDefinitions) {
							_debug_log($"duration: {game_mode_def.gameModeDuration}");
						}
					}
				} catch (Exception e) {
					_error_log("** HarmonyPatch_UIMainMenu_Awake.Postfix ERROR - " + e);
				}
			}
		}
	}
}