using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

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
        public class MyFontData
        {
            public TMP_FontAsset fontAsset;
            public string filePath;
            public string fileName;
            public string fontName;
        }

        public static ManualLogSource g_logger;
        public static Harmony g_harmony;
        public static Dictionary<string, MyFontData> g_fontDataMap = new();

        public static void Setup(ManualLogSource logger)
        {
            g_logger = logger;

            g_harmony = new(nameof(ThaiFontFix));
            g_harmony.PatchAll();
        }

        public static MyFontData GetThaiFontFix(string fontName)
        {
            if (g_fontDataMap.TryGetValue(fontName, out var fontData))
                return fontData;

            string fileName = fontName + ".ttf";
            string filePath = "D:\\LumaIslandModder\\" + fileName;
            g_logger.LogInfo($"Try loading font RSU path: {filePath}");

            var font = new Font(filePath);
            var fontAsset = TMP_FontAsset.CreateFontAsset(font);
            fontAsset.name = fontName;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fontAsset.ReadFontAssetDefinition();


            fontData = new MyFontData()
            {
                fontAsset = fontAsset,
                filePath = filePath,
                fileName = fileName,
                fontName = font.name
            };
            g_fontDataMap.Add(fontName, fontData);
            g_logger.LogInfo($"Loaded font name: {font.name}");

            return fontData;
        }

        static Dictionary<string, string> g_fixStringCacheMap = new();
        static TMP_Text lastFixTMP = null;
        public static void FixThaiString(TMP_Text tmp, ref string refTextValue)
        {
            if (ThaiFontAdjuster.IsThaiString(refTextValue) == false)
                return;

            // create string thai fix cache
            //if (g_fixStringCacheMap.ContainsKey(refTextValue) == false)
            //{
            //    var newStringFix = ThaiFontAdjuster.Adjust(refTextValue);
            //    g_fixStringCacheMap.Add(refTextValue, newStringFix);
            //}

            //refTextValue = g_fixStringCacheMap[refTextValue];
            refTextValue = ThaiFontAdjuster.Adjust(refTextValue);


            // fix font
            if (tmp.font.name.Contains("NotoSansThai"))
            {
                var newFontNameToLoad = "RSU_Regular";
                if (tmp.font.name.Contains("Bold"))
                    newFontNameToLoad = "RSU_Bold";

                var oldFont = tmp.font;
                tmp.font = GetThaiFontFix(newFontNameToLoad).fontAsset;
                g_logger.LogInfo($"fix new font: {tmp.font.name}, original: {oldFont.name}");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TMP_Text), "set_text")]
        static void Prefix_TMPText_SetText(TMP_Text __instance, ref string value)
        {
            FixThaiString(__instance, ref value);
        }
    }
}
