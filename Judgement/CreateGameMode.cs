using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using RoR2.ContentManagement;

namespace Judgement
{
    public class CreateGameMode
    {
        public static GameObject judgementRunPrefab;
        public static GameObject extraGameModeMenu;
        private GameObject simClone = AssetAsyncReferenceManager<GameObject>.LoadAsset(new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_GameModes_InfiniteTowerRun.InfiniteTowerRun_prefab)).WaitForCompletion();

        public CreateGameMode()
        {
            CreateJudgementRun();
        }

        private void CreateJudgementRun()
        {
            judgementRunPrefab = PrefabAPI.InstantiateClone(new GameObject("JudgementRun"), "JudgementRun");
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

            ContentAddition.AddGameMode(judgementRunPrefab, "Defeat all that stand before you to reach the final throne.");
        }
    }
}