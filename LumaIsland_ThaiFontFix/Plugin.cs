using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.TextCore.Text;

namespace LumaIsland_ThaiFontFix
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            ThaiFontFix.Setup(Logger);
        }
    }

    [HarmonyPatch]
    static class ThaiFontFix
    {
        // config here
        public static class Config
        {
            public const string MainFontName = "RSU";
            public const string PostfixRegularName = "_Regular";
        }

        public static ManualLogSource g_logger;
        public static Harmony g_harmony;
        public readonly static Dictionary<string, TMP_FontAsset> g_thaiFontAssetMap = [];
        public readonly static HashSet<TMP_FontAsset> g_thaiFontAssetSet = [];

        public static void Setup(ManualLogSource logger)
        {
            g_logger = logger;

            g_harmony = new(nameof(ThaiFontFix));
            g_harmony.PatchAll();
        }

        public static void LogInfo(object msg)
        {
#if !RELEASE
            g_logger.LogInfo(msg);
#endif
        }

        public static TMP_FontAsset? GetThaiFontFix(TMP_FontAsset currentFontAsset)
        {
            // already fixed
            if (g_thaiFontAssetSet.Contains(currentFontAsset))
                return currentFontAsset;

            // load font name with regular or bold
            // only support regular for now
            var fontNameKey = Config.MainFontName + Config.PostfixRegularName;

            // get from cache
            if (g_thaiFontAssetMap.TryGetValue(fontNameKey, out var fontAssetCache))
                return fontAssetCache;

            // load new font
            try
            {

                string fontFileName = fontNameKey + ".ttf";
                var currentAssembly = Assembly.GetExecutingAssembly();
                var currentDir = Path.GetDirectoryName(currentAssembly.Location);
                string fontFilePath = Path.Combine(currentDir, fontFileName);

                fontAssetCache = TMP_FontAsset.CreateFontAsset(new Font(fontFilePath));
                fontAssetCache.name = fontNameKey;
                g_thaiFontAssetMap.Add(fontNameKey, fontAssetCache);
                g_thaiFontAssetSet.Add(fontAssetCache);
            }
            catch (System.Exception ex)
            {
                LogInfo($"Failed to load font name: {fontNameKey}, exception: {ex}");
            }

            return fontAssetCache;
        }

        static Dictionary<string, string> g_fixStringCacheMap = new();
        public static void FixThaiString(TMP_Text tmp, ref string refTextValue)
        {
            if (ThaiFontAdjuster.IsThaiString(refTextValue) == false)
                return;

            // create string thai fix cache
            if (g_fixStringCacheMap.ContainsKey(refTextValue) == false)
            {
                var newStringFix = ThaiFontAdjuster.Adjust(refTextValue);
                g_fixStringCacheMap.Add(refTextValue, newStringFix);
            }

            refTextValue = g_fixStringCacheMap[refTextValue];
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TMP_Text), "set_text")]
        static void Prefix_TMPText_SetText(TMP_Text __instance, ref string value)
        {
            FixThaiString(__instance, ref value);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TMP_Text), "set_font")]
        static void Prefix_TMPText_SetFont(TMP_Text __instance, ref TMP_FontAsset value)
        {
            // already fixed
            if (g_thaiFontAssetSet.Contains(value))
                return;

            // don't match font
            if (value.name.Contains("NotoSansThai") == false)
                return;

            // fixed it
            var newFont = GetThaiFontFix(value);
            if (newFont)
                value = newFont;
        }
    }
}
