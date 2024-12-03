using System.Collections.Generic;
using UnityEngine;

class Hotkeys : MonoBehaviour {
    private static Hotkeys m_instance = null;
    public static Hotkeys Instance {
        get {
            if (m_instance == null) {
                m_instance = new Hotkeys();
            }
            return m_instance;
        }
    }
    private const int HOTKEY_MODIFIER = 0;
    private const int HOTKEY_TOGGLE_EXP = 1;
    private const int HOTKEY_TOGGLE_DEV_TOOLS = 2;
    private const int HOTKEY_AUTOLEVEL_NONE = 3;
    private const int HOTKEY_AUTOLEVEL_RANDOM = 4;
    private const int HOTKEY_AUTOLEVEL_SKIP = 5;
    private const int HOTKEY_SPAWN_SOS = 6;
    private static Dictionary<int, List<KeyCode>> m_hotkeys = null;

    public static void load() {
        m_hotkeys = new Dictionary<int, List<KeyCode>>();
        set_hotkey(Settings.m_hotkey_modifier.Value, HOTKEY_MODIFIER);
        set_hotkey(Settings.m_hotkey_toggle_exp.Value, HOTKEY_TOGGLE_EXP);
        set_hotkey(Settings.m_hotkey_toggle_dev_tools.Value, HOTKEY_TOGGLE_DEV_TOOLS);
        set_hotkey(Settings.m_hotkey_auto_level_none.Value, HOTKEY_AUTOLEVEL_NONE);
        set_hotkey(Settings.m_hotkey_auto_level_random.Value, HOTKEY_AUTOLEVEL_RANDOM);
        set_hotkey(Settings.m_hotkey_auto_level_skip.Value, HOTKEY_AUTOLEVEL_SKIP);
        set_hotkey(Settings.m_hotkey_spawn_sos.Value, HOTKEY_SPAWN_SOS);
        DDPlugin.Instance.AddComponent<Hotkeys>();
    }

    private static void set_hotkey(string keys_string, int key_index) {
        m_hotkeys[key_index] = new List<KeyCode>();
        foreach (string key in keys_string.Split(',')) {
            string trimmed_key = key.Trim();
            if (trimmed_key != "") {
                m_hotkeys[key_index].Add((KeyCode) System.Enum.Parse(typeof(KeyCode), trimmed_key));
            }
        }
    }

    private static bool is_modifier_hotkey_down() {
        if (m_hotkeys[HOTKEY_MODIFIER].Count == 0) {
            return true;
        }
        foreach (KeyCode key in m_hotkeys[HOTKEY_MODIFIER]) {
            if (Input.GetKey(key)) {
                return true;
            }
        }
        return false;
    }

    public static bool is_hotkey_down(int key_index) {
        foreach (KeyCode key in m_hotkeys[key_index]) {
            if (Input.GetKeyDown(key)) {
                return true;
            }
        }
        return false;
    }

    private void Update() {
        if (!is_modifier_hotkey_down()) {
            return;
        }
        if (is_hotkey_down(HOTKEY_TOGGLE_EXP)) {
            EasyModePlugin.StatsModifier.toggle_exp_gain();
        } else if (is_hotkey_down(HOTKEY_TOGGLE_DEV_TOOLS)) {
            EasyModePlugin.DevTools.toggle_dev_tools();
        } else if (is_hotkey_down(HOTKEY_AUTOLEVEL_NONE)) {
            EasyModePlugin.AutoLevel.set_mode(EasyModePlugin.AutoLevel.Mode.Disabled);
        } else if (is_hotkey_down(HOTKEY_AUTOLEVEL_RANDOM)) {
            EasyModePlugin.AutoLevel.set_mode(EasyModePlugin.AutoLevel.Mode.Random);
        } else if (is_hotkey_down(HOTKEY_AUTOLEVEL_SKIP)) {
            EasyModePlugin.AutoLevel.set_mode(EasyModePlugin.AutoLevel.Mode.Skip);
        } else if (is_hotkey_down(HOTKEY_SPAWN_SOS)) {
            EasyModePlugin.Spawner.spawn_sos();
        }
    }
}
