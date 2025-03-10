using RoR2;
using System;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using EntityStates.Missions.BrotherEncounter;
using MonoMod.Cil;
using System.Collections;

namespace Judgement
{
    public class RunHooks
    {
       // private PostProcessProfile ppProfile = Addressables.LoadAssetAsync<PostProcessProfile>("RoR2/Base/title/PostProcessing/ppSceneEclipseStandard.asset").WaitForCompletion();
        private Material spaceStarsMat = Addressables.LoadAssetAsync<Material>("RoR2/Base/eclipseworld/matEclipseStarsSpheres.mat").WaitForCompletion();
        private Material altSkyboxMat = Addressables.LoadAssetAsync<Material>("RoR2/Base/artifactworld/matAWSkySphere.mat").WaitForCompletion();

        private SceneDef voidPlains = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itgolemplains/itgolemplains.asset").WaitForCompletion();
        private SceneDef voidAqueduct = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itgoolake/itgoolake.asset").WaitForCompletion();
        private SceneDef voidAphelian = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itancientloft/itancientloft.asset").WaitForCompletion();
        private SceneDef voidRPD = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itfrozenwall/itfrozenwall.asset").WaitForCompletion();
        private SceneDef voidAbyssal = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itdampcave/itdampcave.asset").WaitForCompletion();
        private SceneDef voidMeadow = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itskymeadow/itskymeadow.asset").WaitForCompletion();

        private GameEndingDef judgementRunEnding = Addressables.LoadAssetAsync<GameEndingDef>("RoR2/Base/WeeklyRun/PrismaticTrialEnding.asset").WaitForCompletion();

        private GameObject tpOutController = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/TeleportOutController.prefab").WaitForCompletion();

        public RunHooks()
        {
            IL.RoR2.SceneDirector.PopulateScene += RemoveExtraLoot;
            On.RoR2.Run.PickNextStageScene += SetInitialStageBazaar;
            On.RoR2.Run.Start += SeedRun;
            On.RoR2.CharacterMaster.SpawnBody += MoveSurvivorOnSpawn;
            On.RoR2.MusicController.PickCurrentTrack += SetJudgementMusic;
            On.EntityStates.Missions.BrotherEncounter.BossDeath.OnEnter += EndRun;
            On.RoR2.CharacterBody.Start += ManageSurvivorStats;
            On.RoR2.SceneExitController.Begin += ManageStageSelection;
        }


        public void SavePersistentHP()
        {
            JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
            foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
            {
                if (judgementRun.persistentHP.TryGetValue(instance.master.netId, out float hp))
                    judgementRun.persistentHP[instance.master.netId] = instance.master.GetBody().healthComponent.health;
                else
                    judgementRun.persistentHP.Add(instance.master.netId, instance.master.GetBody().healthComponent.health);

            }
        }

        public void LoadPersistentHP(CharacterBody body)
        {
            JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
            if (body.master && body.healthComponent && judgementRun.persistentHP.TryGetValue(body.master.netId, out float hp))
                body.healthComponent.health = hp;
        }

        private void RemoveExtraLoot(ILContext il)
        {
            ILCursor ilCursor = new ILCursor(il);

            static int ItemFunction(int itemCount)
            {
                if (Run.instance && Run.instance.name.Contains("Judgement") && SceneManager.GetActiveScene().name != "bazaar")
                    return 0;
                return itemCount;
            }

            if (ilCursor.TryGotoNext(0, x => ILPatternMatchingExt.MatchLdsfld(x, typeof(RoR2Content.Items), "TreasureCache")))
            {
                ilCursor.Index += 2;
                ilCursor.EmitDelegate<Func<int, int>>((count) => ItemFunction(count));
            }
            else
                Debug.LogWarning("Judgement: TreasureCache IL hook failed");

            ilCursor.Index = 0;

            if (ilCursor.TryGotoNext(0, x => ILPatternMatchingExt.MatchLdsfld(x, typeof(DLC1Content.Items), "TreasureCacheVoid")))
            {
                ilCursor.Index += 2;
                ilCursor.EmitDelegate<Func<int, int>>((count) => ItemFunction(count));
            }
            else
                Debug.LogWarning("Judgement: TreasureCacheVoid IL hook failed");

            ilCursor.Index = 0;

            if (ilCursor.TryGotoNext(0, x => ILPatternMatchingExt.MatchLdsfld(x, typeof(DLC1Content.Items), "FreeChest")))
            {
                ilCursor.Index += 2;
                ilCursor.EmitDelegate<Func<int, int>>((count) => ItemFunction(count));
            }
            else
                Debug.LogWarning("Judgement: FreeChest IL hook failed");
        }


        // bazaar spawnpos -81.5 -24.8 -16.6
        // portal spawnpos -128.6 -25.4 -14.4
        // key/shorm1 -112.0027 -23.7788 -4.5843
        // key/shorm2 -103.7627 -23.8988 -4.7243
        // vradle -90.5743 -24.3739 -11.5119

        private void SeedRun(On.RoR2.Run.orig_Start orig, Run self)
        {
            orig(self);
            if (self.name.Contains("Judgement"))
            {
                JudgementRun judgementRun = self.gameObject.GetComponent<JudgementRun>();
                judgementRun.bazaarRng = new Xoroshiro128Plus(self.seed ^ 1635UL);
            }
        }

        private CharacterBody MoveSurvivorOnSpawn(On.RoR2.CharacterMaster.orig_SpawnBody orig, CharacterMaster self, Vector3 position, Quaternion rotation)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement") && self.teamIndex == TeamIndex.Player)
            {
                string sceneName = SceneManager.GetActiveScene().name;
                JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();

                if (sceneName == "bazaar" && (judgementRun.isFirstStage || judgementRun.persistentHP.TryGetValue(self.netId, out float _)))
                    return orig(self, new Vector3(-81.5f, -24.8f, -16.6f), Quaternion.Euler(358, 210, 0));
                else if (sceneName == "moon2" && judgementRun.persistentHP.TryGetValue(self.netId, out float _) && !self.IsExtraLifePendingServer())
                    return orig(self, new Vector3(127, 500, 101), Quaternion.Euler(358, 210, 0));
                else
                    return orig(self, position, rotation);
            }
            else
                return orig(self, position, rotation);
        }

        private static void SetJudgementMusic(On.RoR2.MusicController.orig_PickCurrentTrack orig, MusicController self, ref MusicTrackDef newTrack)
        {
            orig(self, ref newTrack);
            if (Run.instance && Run.instance.name.Contains("Judgement") && SceneManager.GetActiveScene().name != "moon2")
                newTrack = MusicTrackCatalog.FindMusicTrackDef("muSong23");
        }

        private void SetInitialStageBazaar(On.RoR2.Run.orig_PickNextStageScene orig, Run self, WeightedSelection<SceneDef> choices)
        {
            if (Run.instance.stageClearCount == 0 && self.name.Contains("Judgement"))
            {
                JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
                if (judgementRun.isFirstStage)
                {
                    SceneDef sceneDef = SceneCatalog.FindSceneDef("bazaar");
                    self.nextStageScene = sceneDef;
                }
            }
            else
                orig(self, choices);
        }

        private void ManageStageSelection(On.RoR2.SceneExitController.orig_Begin orig, SceneExitController self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
                SavePersistentHP();
                if (judgementRun.currentWave == 0)
                    Run.instance.nextStageScene = voidPlains;
                if (judgementRun.currentWave == 2)
                {
                    int[] array = Array.Empty<int>();
                    WeightedSelection<SceneDef> weightedSelection = new WeightedSelection<SceneDef>();
                    weightedSelection.AddChoice(voidAqueduct, 1f);
                    weightedSelection.AddChoice(voidAphelian, 1f);
                    int toChoiceIndex = weightedSelection.EvaluateToChoiceIndex(Run.instance.runRNG.nextNormalizedFloat, array);
                    WeightedSelection<SceneDef>.ChoiceInfo choice = weightedSelection.GetChoice(toChoiceIndex);
                    Run.instance.nextStageScene = choice.value;
                }
                if (judgementRun.currentWave == 4)
                    Run.instance.nextStageScene = voidRPD;
                if (judgementRun.currentWave == 6)
                    Run.instance.nextStageScene = voidAbyssal;
                if (judgementRun.currentWave == 8)
                    Run.instance.nextStageScene = voidMeadow;
                if (judgementRun.currentWave == 10)
                {
                    SceneDef sceneDef = SceneCatalog.FindSceneDef("moon2");
                    Run.instance.nextStageScene = sceneDef;
                }
                if (judgementRun.shouldGoBazaar)
                {
                    SceneDef sceneDef = SceneCatalog.FindSceneDef("bazaar");
                    Run.instance.nextStageScene = sceneDef;
                }
                ReadOnlyCollection<CharacterMaster> onlyInstancesList = CharacterMaster.readOnlyInstancesList;
                for (int index = 0; index < onlyInstancesList.Count; ++index)
                {
                    CharacterMaster component = onlyInstancesList[index].GetComponent<CharacterMaster>();
                    if ((bool)component.GetComponent<SetDontDestroyOnLoad>())
                    {
                        GameObject bodyObject = component.GetBodyObject();
                        if ((bool)bodyObject)
                        {
                            GameObject gameObject = UnityEngine.Object.Instantiate(tpOutController, bodyObject.transform.position, Quaternion.identity);
                            gameObject.GetComponent<TeleportOutController>().Networktarget = bodyObject;
                            NetworkServer.Spawn(gameObject);
                        }
                    }
                }
                self.SetState(SceneExitController.ExitState.Finished);
            }
            else
                orig(self);
        }

        private void ManageSurvivorStats(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            if (Run.instance && Run.instance.name.Contains("Judgement") && self.isPlayerControlled && !self.HasBuff(RoR2Content.Buffs.Immune))
            {
                LoadPersistentHP(self);
                JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
                if (Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse1 && judgementRun.currentWave == 0)
                    self.healthComponent.health = self.healthComponent.fullHealth;
                self.baseDamage *= 1.25f;
            }
        }

        private void EndRun(On.EntityStates.Missions.BrotherEncounter.BossDeath.orig_OnEnter orig, BossDeath self)
        {
            orig(self);
            if (Run.instance && Run.instance.name.Contains("Judgement"))
                Run.instance.BeginGameOver(judgementRunEnding);
        }

    }
}