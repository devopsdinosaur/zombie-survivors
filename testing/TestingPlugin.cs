using BepInEx;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;

public static class PluginInfo {

	public const string TITLE = "Testing";
	public const string NAME = "testing";
	public const string SHORT_DESCRIPTION = "";

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
public class TestingPlugin : DDPlugin {
	private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	public override void Load() {
		logger = base.Log;
		try {
			this.m_plugin_info = PluginInfo.to_dict();
			Settings.Instance.load(this);
			DDPlugin.set_log_level(Settings.m_log_level.Value);
			this.create_nexus_page();
			this.m_harmony.PatchAll();
			PluginUpdater.create(this, logger);
			PluginUpdater.Instance.register("keypress", 0f, keypress_update);
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	private static void keypress_update() {
		if (Input.GetKeyDown(KeyCode.Backspace)) {
			dump_all_objects();
			Application.Quit();
		} else if (Input.GetKeyDown(KeyCode.F1)) {
			
		}
	}

	public static T get_internal_method<T>(string signature) where T : Delegate {
		IntPtr ptr = IL2CPP.il2cpp_resolve_icall(signature);
		if (ptr == IntPtr.Zero) {
			return null;
		}
		return (T) Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
	}

	delegate int Delegate_GetRootCountInternal(int handle);
	delegate void Delegate_GetRootGameObjects(int handle, IntPtr list);

	public struct DummyScene {
		public int handle;
		public string name;
	}

	private static void dump_all_objects() {
		const string DIRECTORY = "C:/tmp/dump/" + PluginInfo.GAME;
		Dictionary<string, List<GameObject>> scene_roots = new Dictionary<string, List<GameObject>>() {
			{"__DontDestroyOnLoad__", new List<GameObject>()},
			{"__HideAndDontSave__", new List<GameObject>()}
		};
		List<GameObject> all_roots = new List<GameObject>();
		for (int index = 0; index < SceneManager.sceneCount; index++) {
			Scene scene = SceneManager.GetSceneAt(index);
			scene_roots[scene.name] = scene.GetRootGameObjects().ToList();
			all_roots = all_roots.Concat(scene_roots[scene.name]).ToList();
		}
		foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>()) {
			if (obj.transform.parent != null || all_roots.Contains(obj)) {
				continue;
			}
			scene_roots[(obj.transform.hideFlags == HideFlags.None ? "__DontDestroyOnLoad__" : "__HideAndDontSave__")].Add(obj);
			all_roots.Add(obj);
		}
		Directory.CreateDirectory(DIRECTORY);
		foreach (string file in Directory.GetFiles(DIRECTORY, "*.json", SearchOption.AllDirectories)) {
			File.Delete(Path.Combine(DIRECTORY, file));
		}
		foreach (KeyValuePair<string, List<GameObject>> kvp in scene_roots) {
			_debug_log($"scene: {kvp.Key}, count: {kvp.Value.Count}");
			string directory = Path.Combine(DIRECTORY, kvp.Key);
			Directory.CreateDirectory(directory);
			foreach (GameObject obj in kvp.Value) {
				try {
					UnityUtils.json_dump(obj.transform, Path.Combine(directory, obj.name + ".json"));
				} catch {
					_warn_log($"* dump_all_objects WARNING - unable to dump {kvp.Key}.{(string.IsNullOrEmpty(obj?.name) ? "<null/noname>" : obj.name)}");
				}
			}
		}
	}

	/*
	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {
		private static bool Prefix() {
			
			return true;
		}
	}

	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {
		private static void Postfix() {
			
		}
	}

	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {
		private static bool Prefix() {
			try {

			} catch (Exception e) {
				_error_log("** HarmonyPatch_.Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {
		private static void Postfix() {
			try {

			} catch (Exception e) {
				_error_log("** HarmonyPatch_.Postfix ERROR - " + e);
			}
		}
	}
	*/
}