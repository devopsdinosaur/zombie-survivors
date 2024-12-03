using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;

public class Settings {
    private static Settings m_instance = null;
    public static Settings Instance {
        get {
            if (m_instance == null) {
                m_instance = new Settings();
            }
            return m_instance;
        }
    }
    private DDPlugin m_plugin = null;

    // General
    public static ConfigEntry<bool> m_enabled;
    public static ConfigEntry<string> m_log_level;

    // Cheats
    public static ConfigEntry<float> m_fire_rate_multiplier;
    public static ConfigEntry<bool> m_infinite_ammo;
    public static ConfigEntry<bool> m_infinite_health;
    public static ConfigEntry<bool> m_perpetual_magnet;

    // Stats
    public static Dictionary<PlayerStatistic.EType, ConfigEntry<float>> m_stat_multipliers = new Dictionary<PlayerStatistic.EType, ConfigEntry<float>>();

    // Hotkeys
    public static ConfigEntry<string> m_hotkey_modifier;
    public static ConfigEntry<string> m_hotkey_toggle_exp;

    private string hotkey_description(string unique) {
        const bool IS_MODIFIER_AVAILABLE = true;
        return $"Comma-separated list of Unity Keycodes any of which will {(unique == null ? "act as the special modifier key (i.e. alt/ctrl/shift) required to be pressed along with other hotkeys" : (IS_MODIFIER_AVAILABLE ? "(when combined with the Modifier key) " : " ") + unique)}.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)";
    }

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_log_level = this.m_plugin.Config.Bind<string>("General", "Log Level", "info", "[Advanced] Logging level, one of: 'none' (no logging), 'error' (only errors), 'warn' (errors and warnings), 'info' (normal logging), 'debug' (extra log messages for debugging issues).  Not case sensitive [string, default info].  Debug level not recommended unless you're noticing issues with the mod.  Changes to this setting require an application restart.");

        // Cheats
        m_fire_rate_multiplier = this.m_plugin.Config.Bind<float>("Cheats", "Fire Rate Multiplier", 1.0f, "Multiplier applied to weapon fire rate (float, default 1 [no change]).");
        m_infinite_ammo = this.m_plugin.Config.Bind<bool>("Cheats", "Infinite Ammo", false, "Set to true for infinite ammo, i.e. no reload delays.");
        m_infinite_health = this.m_plugin.Config.Bind<bool>("Cheats", "Invincibility", false, "Set to true for player invincibility.");
        m_perpetual_magnet = this.m_plugin.Config.Bind<bool>("Cheats", "Perpetual Magnet", false, "Set to true to have all collectible items (exp, cash, etc.) come immediately to player at all times.  This will override the 'TeamMagnetRange' stat multiplier.");
        
        // Stats
        foreach (PlayerStatistic.EType stat in Enum.GetValues(typeof(PlayerStatistic.EType))) {
            if (stat >= PlayerStatistic.EType.TeamHashtagFire) {
                continue;
            }
            string name = Enum.GetName(typeof(PlayerStatistic.EType), stat);
            m_stat_multipliers[stat] = this.m_plugin.Config.Bind<float>("Stats", "Stat Multiplier - " + name, 1f, $"Multiplier applied to the '{name}' statistic (float, default 1 [no change]).");
        }

        // Hotkeys
        m_hotkey_modifier = this.m_plugin.Config.Bind<string>("Hotkeys", "Hotkey - Modifier", "LeftControl,RightControl", hotkey_description(null));
        m_hotkey_toggle_exp = this.m_plugin.Config.Bind<string>("Hotkeys", "Hotkey - Toggle EXP Gain", "E", hotkey_description("toggle exp gain (to disable annoying level-ups and just finish the level)"));
    }
}