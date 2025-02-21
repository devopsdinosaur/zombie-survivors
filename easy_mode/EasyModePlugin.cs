using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Diagnostics.Eventing.Reader;
using UnityEngine.UIElements;

public static class PluginInfo {

	public const string TITLE = "Easy Mode";
	public const string NAME = "easy_mode";
	public const string SHORT_DESCRIPTION = "Lots of configurable QoL tweaks and cheats to make the game easier (or even harder)!  And more coming soon.";

	public const string VERSION = "0.0.3";

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
			Hotkeys.load();
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			_error_log("** Load FATAL - " + e);
		}
	}

	public class __Global__ {
		[HarmonyPatch(typeof(DeveloperTools), "Awake")]
		class HarmonyPatch_DeveloperTools_Awake {
			private static void Postfix() {
				try {
					DeveloperTools.HelperBool = Settings.m_enabled.Value;
				} catch (Exception e) {
					_error_log("** __Global__.HarmonyPatch_DeveloperTools_Awake.Postfix ERROR - " + e);
				}
			}
		}

		public static void popup_message(string main_text, Color color, float display_time = 3f, float scale = 25f, string lower_text = null) {
            foreach (GameplayEventPopup popup in Resources.FindObjectsOfTypeAll<GameplayEventPopup>()) {
                _debug_log($"popup message: {main_text}{(!string.IsNullOrEmpty(lower_text) ? $" (lower_text: {lower_text})" : "")}");
				popup.AddPopup(new GameplayEventPopup.PopupData() {
                    mainText = main_text,
                    lowerText = lower_text,
                    lowerPartActive = lower_text != null,
                    color = color,
                    visibilityTime = display_time,
                    scale = scale
                });
				break;
            }
        }
	}

    public class AutoLevel {
		public enum Mode {
			Disabled,
			Random,
			Skip
		}
		private static readonly Dictionary<Mode, string> MODE_STRINGS = new Dictionary<Mode, string>() {
			{Mode.Disabled, "Disabled"},
			{Mode.Random, "Random"},
			{Mode.Skip, "Skip"}
		};
		private static Mode m_current_mode = Mode.Disabled;

        [HarmonyPatch(typeof(UIGameplayUpgradeSelection), "OnEnable")]
        class HarmonyPatch_UIGameplayUpgradeSelection_OnEnable {
            private static void Postfix(UIGameplayUpgradeSelection __instance) {
                try {
                    if (m_current_mode == Mode.Disabled || 
						(!string.IsNullOrEmpty(__instance.powerupButtons[0].attachedPowerup?.nameLocalizationKey) && __instance.powerupButtons[0].attachedPowerup.nameLocalizationKey.StartsWith("UI/Hero_")) ||
						__instance.powerupButtons[0].attachedItem != null
					) {
						return;
					}
					switch (m_current_mode) {
						case Mode.Random:
							UIPowerupButton button = __instance.powerupButtons[UnityEngine.Random.Range(0, __instance.powerupButtons.Count - 1)];
							GameplayMaster.Get.AddPowerup(button.attachedPowerup);
							try {
								button.attachedPowerup.OnApply();
							} catch {}
							__instance.Hide(button);
							break;
						case Mode.Skip:
							__instance.ApplySkipBonus();
							__instance.Hide(null);
							break;
					}
                } catch (Exception e) {
                    _error_log("** AutoLevel.HarmonyPatch_UIGameplayUpgradeSelection_OnEnable.Prefix ERROR - " + e);
                }
            }
        }

		public static void set_mode(Mode mode) {
			try {
				m_current_mode = mode;
				__Global__.popup_message($"Auto-level Mode = {MODE_STRINGS[mode]}", Color.white);
            } catch (Exception e) {
                _error_log("** AutoLevel.set_mode ERROR - " + e);
            }
        }
    }

    public class AutoMagnet : MonoBehaviour {
		private static GamePlayer m_player;
        private static float m_update_frequency;
		private static CollectibleMagnet m_magnet;
        private float m_elapsed = 0;

        public static void create(DDPlugin plugin, GamePlayer player) {
			m_player = player;
			m_update_frequency = Mathf.Max(1f, Settings.m_automatic_magnet_frequency.Value);
			m_magnet = null;
			foreach (CollectibleMagnet magnet in Resources.FindObjectsOfTypeAll<CollectibleMagnet>()) {
				m_magnet = magnet;
				break;
			}
			if (m_magnet == null) {
				_warn_log("* AutoMagnet.create WARNING - unable to locate CollectibleMagnet instance; disabling AutoMagnet functionality.");
				return;
			}
			plugin.AddComponent<AutoMagnet>();
        }

        [HarmonyPatch(typeof(GamePlayer), "Awake")]
        class HarmonyPatch_GamePlayer_Awake {
            private static void Postfix(GamePlayer __instance) {
                try {
					if (Settings.m_enabled.Value && Settings.m_automatic_magnet_frequency.Value > 0) {
						create(DDPlugin.Instance, __instance);
					}
                } catch (Exception e) {
                    _error_log("** AutoMagnet.HarmonyPatch_GamePlayer_Awake.Postfix ERROR - " + e);
                }
            }
        }

        private void Update() {
            try {
                if ((this.m_elapsed += Time.deltaTime) < m_update_frequency) {
                    return;
                }
                this.m_elapsed = 0;
                m_magnet.onCollectSoundEvent = null;
				m_magnet.OnCollect(m_player, true);
            } catch (Exception e) {
                _error_log("** InfiniteHealth.update ERROR - " + e);
            }
        }
    }

	public class DevTools {
		public static void toggle_dev_tools() {
            DeveloperTools.s_instance.ToggleVisibility();
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

		[HarmonyPatch(typeof(CollectibleHealth), "ShouldBeMagnetCollected")]
		class HarmonyPatch_CollectibleHealth_ShouldBeMagnetCollected {
			private static bool Prefix(ref bool __result) {
				if (!Settings.m_enabled.Value || !Settings.m_infinite_health.Value) {
					return true;
				}
				__result = true;
				return false;
			}
		}

		[HarmonyPatch(typeof(CollectibleHealth), "AdditionalCollectCondition")]
		class HarmonyPatch_CollectibleHealth_AdditionalCollectCondition {
			private static bool Prefix(ref bool __result) {
				if (!Settings.m_enabled.Value || !Settings.m_infinite_health.Value) {
					return true;
				}
				__result = true;
				return false;
			}
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

	public class Spawner {
		public static void spawn_chest() {
			_debug_log("spawn chest");
			GameplayMaster.Get.currentGameMode.SpawnItemChest();
		}

		public static void spawn_sos() {
            GameplayMaster.Get.currentGameMode.SpawnCharacterRescue();
		}
	}

	public class StatsModifier : MonoBehaviour {
        private const float UPDATE_FREQUENCY = 1.0f;
        private float m_elapsed = UPDATE_FREQUENCY;
        private static bool m_is_exp_enabled = true;
		private static Dictionary<PlayerStatistic, float> m_base_values;

        [HarmonyPatch(typeof(GamePlayer), "Awake")]
		class HarmonyPatch_GamePlayer_Awake {
			private static void Postfix(GamePlayer __instance) {
				try {
					if (!Settings.m_enabled.Value) {
						return;
					}
					m_base_values = new Dictionary<PlayerStatistic, float>();
					foreach (PlayerStatisticsRuntime stats in Resources.FindObjectsOfTypeAll<PlayerStatisticsRuntime>()) {
                        foreach (KeyValuePair<PlayerStatistic.EType, ConfigEntry<float>> kvp in Settings.m_stat_multipliers) {
							try {
								PlayerStatistic stat = stats.GetStatistic(kvp.Key);
                                m_base_values[stat] = stat.GetValue() * kvp.Value.Value;
								stat.SetValue(m_base_values[stat]);
								stat.SetDirty();
							} catch { }
						}
					}
					DDPlugin.Instance.AddComponent<StatsModifier>();
				} catch (Exception e) {
					_error_log("** StatsModifier.HarmonyPatch_GamePlayer_Awake.Postfix ERROR - " + e);
				}
			}
		}

		private void Update() {
            try {
				if (!(Settings.m_enabled.Value && (this.m_elapsed += Time.deltaTime) >= UPDATE_FREQUENCY)) {
					return;
				}
				m_elapsed = 0;
				foreach (KeyValuePair<PlayerStatistic, float> kvp in m_base_values) {
                    float delta = kvp.Value - kvp.Key.GetValue();
					if (delta > 0) {
						kvp.Key.SetValue(kvp.Value);
						kvp.Key.SetDirty();
					} else if (delta < 0) {
						m_base_values[kvp.Key] = kvp.Key.GetValue();
					}
                }
            } catch (Exception e) {
                _error_log("** StatsModifier.Update ERROR - " + e);
            }
        }

		public static void toggle_exp_gain() {
            m_is_exp_enabled = !m_is_exp_enabled;
		}

        [HarmonyPatch(typeof(ExperienceProgress), "CollectXP")]
        class HarmonyPatch_ExperienceProgress_CollectXP {
            private static bool Prefix(ref long value) {
				if (Settings.m_enabled.Value && !m_is_exp_enabled) {
					value = 0;
				}
				return true;
            }
        }

        [HarmonyPatch(typeof(PlayerStatistic), "GetBaseValue")]
        class HarmonyPatch_PlayerStatistic_GetBaseValue {
            private static bool Prefix(PlayerStatistic __instance, ref float __result) {
                try {
					if (!Settings.m_enabled.Value) {
						return true;
					}
					__result = m_base_values[__instance];
					return false;
                } catch (Exception e) {
                    _error_log("** StatsModifier.HarmonyPatch_PlayerStatistic_GetBaseValue.Prefix ERROR - " + e);
                }
                return true;
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
					//_debug_log(".");
					
				} catch (Exception e) {
					_error_log("** __Testing__.HarmonyPatch_GamePlayer_Update.Postfix ERROR - " + e);
				}
			}
		}
	}
}