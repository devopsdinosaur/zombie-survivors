using BepInEx.Configuration;

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

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_log_level = this.m_plugin.Config.Bind<string>("General", "Log Level", "info", "[Advanced] Logging level, one of: 'none' (no logging), 'error' (only errors), 'warn' (errors and warnings), 'info' (normal logging), 'debug' (extra log messages for debugging issues).  Not case sensitive [string, default info].  Debug level not recommended unless you're noticing issues with the mod.  Changes to this setting require an application restart.");

        // Cheats
        m_fire_rate_multiplier = this.m_plugin.Config.Bind<float>("Cheats", "Fire Rate Multiplier", 1.0f, "Multiplier applied to weapon fire rate (float, default 1 [no change]).");
        m_infinite_ammo = this.m_plugin.Config.Bind<bool>("Cheats", "Infinite Ammo", false, "Set to true for infinite ammo.");
        m_infinite_health = this.m_plugin.Config.Bind<bool>("Cheats", "Invincibility", false, "Set to true for player invincibility.");
    }
}