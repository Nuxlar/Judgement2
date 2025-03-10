using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace Judgement
{
    public class CreateGameMode
    {
        public static GameObject judgementRunPrefab;
        public static GameObject extraGameModeMenu;
        private GameObject simClone = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerRun.prefab").WaitForCompletion();

        public CreateGameMode()
        {
            CreateJudgementRun();

            IL.RoR2.UI.MainMenu.MultiplayerMenuController.BuildGameModeChoices += AddGameModeToMultiplayer;
            On.RoR2.GameModeCatalog.SetGameModes += SortGameModes;
            On.RoR2.UI.LanguageTextMeshController.Start += AddGameModeButton;
        }

        private static void AddGameModeToMultiplayer(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, 
                x => x.MatchCallvirt(typeof(System.Collections.Generic.List<string>).GetMethod("Contains"))
                ))
            {
                c.Emit(OpCodes.Pop); // Remove the original result
                c.Emit(OpCodes.Ldc_I4_1); // Return true
            }
            else Log.Error("Judgement: Failed to apply AddGameModeToMultiplayer IL Hook");
        }

        private void SortGameModes(
          On.RoR2.GameModeCatalog.orig_SetGameModes orig,
          Run[] newGameModePrefabComponents)
        {
            Array.Sort(newGameModePrefabComponents, (a, b) => string.CompareOrdinal(a.name, b.name));
            orig(newGameModePrefabComponents);
        }

        private void AddGameModeButton(
          On.RoR2.UI.LanguageTextMeshController.orig_Start orig,
          LanguageTextMeshController self)
        {
            orig(self);
            if (!(self.token == "TITLE_ECLIPSE") || !(bool)self.GetComponent<HGButton>())
                return;
            self.transform.parent.gameObject.AddComponent<JudgementRunButtonAdder>();
        }

        private void CreateJudgementRun()
        {
            judgementRunPrefab = PrefabAPI.InstantiateClone(new GameObject("xJudgementRun"), "xJudgementRun");
            judgementRunPrefab.AddComponent<NetworkIdentity>();
            PrefabAPI.RegisterNetworkPrefab(judgementRunPrefab);

            InfiniteTowerRun component2 = simClone.GetComponent<InfiniteTowerRun>();

            JudgementRun judgementRun = judgementRunPrefab.AddComponent<JudgementRun>();
            judgementRun.nameToken = "Judgement";
            judgementRun.userPickable = true;
            judgementRun.startingSceneGroup = component2.startingSceneGroup;
            judgementRun.gameOverPrefab = component2.gameOverPrefab;
            judgementRun.lobbyBackgroundPrefab = component2.lobbyBackgroundPrefab;
            judgementRun.uiPrefab = component2.uiPrefab;

            judgementRun.defaultWavePrefab = component2.defaultWavePrefab;

            List<InfiniteTowerWaveCategory.WeightedWave> weightedWaves = new List<InfiniteTowerWaveCategory.WeightedWave>();
            List<InfiniteTowerWaveCategory.WeightedWave> weightedBossWaves = new List<InfiniteTowerWaveCategory.WeightedWave>();

            foreach (InfiniteTowerWaveCategory cat in component2.waveCategories)
            {
                if (cat.name == "BossWaveCategory")
                {
                    foreach (InfiniteTowerWaveCategory.WeightedWave item in cat.wavePrefabs)
                    {
                        if (!item.wavePrefab.name.Contains("Brother") && !item.wavePrefab.name.Contains("Lunar") && !item.wavePrefab.name.Contains("Void"))
                            weightedBossWaves.Add(item);
                    }
                }
                else
                    foreach (InfiniteTowerWaveCategory.WeightedWave item in cat.wavePrefabs)
                    {
                        if (!item.wavePrefab.name.Contains("Command"))
                            weightedWaves.Add(item);
                    }
            }

            InfiniteTowerWaveCategory commonCategory = ScriptableObject.CreateInstance<InfiniteTowerWaveCategory>();
            commonCategory.name = "CommonWaveCategoryNux";
            commonCategory.wavePrefabs = weightedWaves.ToArray();
            commonCategory.availabilityPeriod = 1;
            commonCategory.minWaveIndex = 0;
            InfiniteTowerWaveCategory bossCategory = ScriptableObject.CreateInstance<InfiniteTowerWaveCategory>();
            bossCategory.name = "BossWaveCategoryNux";
            bossCategory.wavePrefabs = weightedBossWaves.ToArray();
            bossCategory.availabilityPeriod = 2;
            bossCategory.minWaveIndex = 0;

            InfiniteTowerWaveCategory[] categories = new InfiniteTowerWaveCategory[] { bossCategory, commonCategory };
            judgementRun.waveCategories = categories;

            judgementRun.defaultWaveEnemyIndicatorPrefab = component2.defaultWaveEnemyIndicatorPrefab;
            judgementRun.enemyItemPattern = component2.enemyItemPattern;
            judgementRun.enemyItemPeriod = 100;
            judgementRun.enemyInventory = judgementRunPrefab.AddComponent<Inventory>();
            judgementRun.stageTransitionPeriod = 2;
            judgementRun.stageTransitionPortalCard = component2.stageTransitionPortalCard;
            judgementRun.stageTransitionPortalMaxDistance = component2.stageTransitionPortalMaxDistance;
            judgementRun.stageTransitionChatToken = component2.stageTransitionChatToken;
            judgementRun.fogDamagePrefab = component2.fogDamagePrefab;
            judgementRun.spawnMaxRadius = component2.spawnMaxRadius;
            judgementRun.initialSafeWardCard = component2.initialSafeWardCard;
            judgementRun.safeWardCard = component2.safeWardCard;
            judgementRun.playerRespawnEffectPrefab = component2.playerRespawnEffectPrefab;
            judgementRun.interactableCredits = 0;
            judgementRun.blacklistedTags = component2.blacklistedTags;
            judgementRun.blacklistedItems = component2.blacklistedItems;

            judgementRunPrefab.AddComponent<TeamManager>();
            judgementRunPrefab.AddComponent<NetworkRuleBook>();
            judgementRunPrefab.AddComponent<TeamFilter>();
            judgementRunPrefab.AddComponent<EnemyInfoPanelInventoryProvider>();
            judgementRunPrefab.AddComponent<DirectorCore>();
            judgementRunPrefab.AddComponent<ExpansionRequirementComponent>();
            judgementRunPrefab.AddComponent<RunCameraManager>();

            ContentAddition.AddGameMode(judgementRunPrefab);
        }

        public class JudgementRunButton : MonoBehaviour
        {
            public HGButton hgButton;

            public void Start()
            {
                this.hgButton = this.GetComponent<HGButton>();
                this.hgButton.onClick = new Button.ButtonClickedEvent();
                this.hgButton.onClick.AddListener(() =>
                {
                    int num = (int)Util.PlaySound("Play_UI_menuClick", RoR2Application.instance.gameObject);
                    RoR2.Console.instance.SubmitCmd(null, "transition_command \"gamemode xJudgementRun; host 0; \"");
                });
            }
        }

        public class JudgementRunButtonAdder : MonoBehaviour
        {
            public void Start()
            {
                GameObject gameObject = Instantiate(this.transform.Find("GenericMenuButton (Eclipse)").gameObject, this.transform);
                gameObject.AddComponent<JudgementRunButton>();
                gameObject.GetComponent<LanguageTextMeshController>().token = "Judgement";
                gameObject.GetComponent<HGButton>().hoverToken = "Defeat all that stand before you to reach the final throne.";
            }
        }
    }
}