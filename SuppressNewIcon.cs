using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;
using CommonModNS;
using System.Reflection;

namespace SuppressNewIconModNS
{
    [HarmonyPatch]
    public class SuppressNewIconMod : Mod
    {
        public static SuppressNewIconMod instance;
        public static void Log(string msg) => instance.Logger.Log(msg);
        public static void LogError(string msg) => instance.Logger.LogError(msg);

        private ConfigEntryBool configSuppressCardIcon;
        private ConfigEntryBool configSuppressCardopedia;
        private ConfigEntryBool configSuppressIdeas;
        private ConfigEntryBool configSuppressQuests;
        private ConfigEntryBool configSuppressBoosterPacks;

        public static bool SuppressCardIcon => instance.configSuppressCardIcon.Value;
        public static bool SuppressCardopedia => instance.configSuppressCardopedia.Value;
        public static bool SuppressIdeas => instance.configSuppressIdeas.Value;
        public static bool SuppressQuests => instance.configSuppressQuests.Value;
        public static bool SuppressBoosterPacks => instance.configSuppressBoosterPacks.Value;

        private void Awake()
        {
            instance = this;

            _ = new ConfigFreeText("suppressmod_config", Config, "suppressmod_config");

            configSuppressCardIcon = CreateBool("suppressmod_cardicon");
            configSuppressCardopedia = CreateBool("suppressmod_cardopedia");
            configSuppressIdeas = CreateBool("suppressmod_ideas");
            configSuppressQuests = CreateBool("suppressmod_quests");
            configSuppressBoosterPacks = CreateBool("suppressmod_packs");

            //WorldManagerPatches.LoadSaveRound += WM_OnLoad;
            //WorldManagerPatches.GetSaveRound += WM_OnSave;
            //WorldManagerPatches.StartNewRound += WM_OnNewGame;
            //WorldManagerPatches.Play += WM_OnPlay;
            //WorldManagerPatches.Update += WM_OnUpdate;
            //WorldManagerPatches.ApplyPatches(Harmony);
            Harmony.PatchAll();
        }

        private ConfigEntryBool CreateBool(string name)
        {
            return new(name, Config, false, new ConfigUI
            {
                NameTerm = name,
                TooltipTerm = name + "_tooltip"
            })
            {
                currentValueColor = Color.blue
            };
        }

        public override void Ready()
        {
            Log($"Card Icons: {SuppressCardIcon}, Ideas: {SuppressIdeas}, Cardopedia: {SuppressCardopedia}, Quests: {SuppressQuests}, BoosterPacks: {SuppressBoosterPacks}");
            MethodInfo mi = AccessTools.Method(typeof(SaveManager), "Load");
            mi.Invoke(SaveManager.instance, []);
            Log("Ready!");
        }

        [HarmonyPatch(typeof(SaveManager),"Load")]
        [HarmonyPostfix]
        public static void SaveManager_Load(SaveManager __instance)
        {
            if (SuppressCardopedia)
            {
                __instance.CurrentSave.NewCardopediaIds.Clear();
            }
            if (SuppressIdeas)
            {
                __instance.CurrentSave.NewKnowledgeIds.Clear();
            }
        }

        [HarmonyPatch(typeof(AchievementElement), "SetQuest")]
        [HarmonyPrefix]
        public static void AchievementElement_SetQuest(AchievementElement __instance, Quest ach)
        {
            if (SuppressQuests && !I.WM.CurrentSave.SeenQuestIds.Contains(ach.Id))
            {
                I.WM.CurrentSave.SeenQuestIds.Add(ach.Id);
            }
        }

        [HarmonyPatch(typeof(WorldManager), "FoundCard")]
        [HarmonyPostfix]
        public static void WorldManager_FoundCard(WorldManager __instance, CardData card)
        {
            if (card.MyGameCard.IsNew)
            {
                if (SuppressCardIcon)
                {
                    card.MyGameCard.IsNew = false;
                }
                if (SuppressCardopedia)
                {
                    __instance.CurrentSave.NewCardopediaIds.Remove(card.Id);
                }
                if (SuppressIdeas && (card.MyCardType == CardType.Ideas || card.MyCardType == CardType.Rumors))
                {
                    __instance.CurrentSave.NewKnowledgeIds.Remove(card.Id);
                }
            }
        }

        [HarmonyPatch(typeof(WorldManager), "QuestCompleted")]
        [HarmonyPostfix]
        public static void WorldManager_QuestCompleted(WorldManager __instance)
        {
            if (SuppressBoosterPacks)
            {
                BoosterpackData boosterpackData = QuestManager.instance.JustUnlockedPack();
                if (boosterpackData != null)
                {
                    if (!I.WM.CurrentSave.FoundBoosterIds.Contains(boosterpackData.BoosterId))
                    {
                        I.WM.CurrentSave.FoundBoosterIds.Add(boosterpackData.BoosterId);
                    }
                }
            }
        }
    }
}

#if false
        private static bool In_RestartGame = false;
        [HarmonyPatch(typeof(WorldManager), "RestartGame")]
        [HarmonyPrefix]
        public static void WorldManager_RestartGame_start()
        {
            I.Log($"RestartGame called");
            In_RestartGame = true;
        }

        [HarmonyPatch(typeof(WorldManager), "RestartGame")]
        [HarmonyPostfix]
        public static void WorldManager_RestartGame_stop()
        {
            In_RestartGame = false;
        }
#endif