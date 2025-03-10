using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace Judgement
{
    public class BazaarHooks
    {
        private BasicPickupDropTable dtEquip = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtEquipment.asset").WaitForCompletion();
        private BasicPickupDropTable dtWhite = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier1Item.asset").WaitForCompletion();
        private BasicPickupDropTable dtGreen = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier2Item.asset").WaitForCompletion();
        private BasicPickupDropTable dtRed = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier3Item.asset").WaitForCompletion();
        private BasicPickupDropTable dtYellow = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/DuplicatorWild/dtDuplicatorWild.asset").WaitForCompletion();

        private GameObject potentialPickup = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion();
        private GameObject portalPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalArena/PortalArena.prefab").WaitForCompletion();

        private SpawnCard chest1 = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Chest1/iscChest1.asset").WaitForCompletion();
        private SpawnCard chest2 = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Chest2/iscChest2.asset").WaitForCompletion();
        private SpawnCard goldChest = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/GoldChest/iscGoldChest.asset").WaitForCompletion();
        private SpawnCard equipChest = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/EquipmentBarrel/iscEquipmentBarrel.asset").WaitForCompletion();
        private SpawnCard yellowChest = Addressables.LoadAssetAsync<SpawnCard>("RoR2/CommandChest/iscCommandChest.asset").WaitForCompletion();
        private SpawnCard lockBox = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Junk/TreasureCache/iscLockbox.asset").WaitForCompletion();
        private SpawnCard lockBoxVoid = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/TreasureCacheVoid/iscLockboxVoid.asset").WaitForCompletion();
        private SpawnCard freeChest = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/FreeChest/iscFreeChest.asset").WaitForCompletion();
        private SpawnCard greenPrinter = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/DuplicatorLarge/iscDuplicatorLarge.asset").WaitForCompletion();
        private SpawnCard voidChest = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/VoidChest/iscVoidChest.asset").WaitForCompletion();

        private PickupIndex tier1PickupIdx = PickupIndex.none;
        private PickupIndex tier2PickupIdx = PickupIndex.none;
        private PickupIndex tier3PickupIdx = PickupIndex.none;
        private PickupIndex yellowPickupIdx = PickupIndex.none;
        private PickupIndex equipPickupIdx = PickupIndex.none;

        public BazaarHooks()
        {
            On.RoR2.ChestBehavior.BaseItemDrop += DropPotential;
            On.RoR2.BazaarController.Start += SetupJudgementBazaar;
            On.RoR2.PurchaseInteraction.OnInteractionBegin += PreventPrinterCheese;
        }

        private void DropPotential(On.RoR2.ChestBehavior.orig_BaseItemDrop orig, ChestBehavior self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                if (self.dropPickup == PickupIndex.none || self.dropCount < 1)
                    return;

                JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
                double angle = 360.0 / self.dropCount;
                Vector3 velocity = Vector3.up * self.dropUpVelocityStrength + self.dropTransform.forward * self.dropForwardVelocityStrength;
                Vector3 up = Vector3.up;
                Quaternion quaternion = Quaternion.AngleAxis((float)angle, up);

                switch (self.name)
                {
                    case "Chest1(Clone)":
                        CreateDrop(tier1PickupIdx, dtWhite, judgementRun.bazaarRng);
                        break;
                    case "Chest2(Clone)":
                        CreateDrop(tier2PickupIdx, dtGreen, judgementRun.bazaarRng);
                        break;
                    case "GoldChest(Clone)":
                        CreateDrop(tier3PickupIdx, dtRed, judgementRun.bazaarRng);
                        break;
                    case "EquipmentBarrel(Clone)":
                        CreateDrop(equipPickupIdx, dtEquip, judgementRun.bazaarRng);
                        break;
                    case "CommandChest(Clone)":
                        CreateDrop(yellowPickupIdx, dtYellow, judgementRun.bazaarRng);
                        break;
                    default:
                        orig(self);
                        break;
                }

                void CreateDrop(PickupIndex pickupIndex, BasicPickupDropTable dropTable, Xoroshiro128Plus rng)
                {
                    for (int index = 0; index < self.dropCount; ++index)
                    {
                        GenericPickupController.CreatePickupInfo pickupInfo = new GenericPickupController.CreatePickupInfo()
                        {
                            pickupIndex = pickupIndex,
                            position = self.dropTransform.position + Vector3.up * 1.5f,
                            pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dropTable, rng),
                            chest = self,
                            prefabOverride = potentialPickup,
                            artifactFlag = self.isCommandChest ? GenericPickupController.PickupArtifactFlag.COMMAND : GenericPickupController.PickupArtifactFlag.NONE
                        };
                        PickupDropletController.CreatePickupDroplet(pickupInfo, pickupInfo.position, velocity);
                        velocity = quaternion * velocity;
                    }
                    self.dropPickup = PickupIndex.none;
                    self.NetworkisChestOpened = true;
                }
            }
            else orig(self);
        }

        private void SpawnInteractable(SpawnCard spawnCard, Vector3 position, Xoroshiro128Plus rng, bool isFree = true)
        {
            DirectorCore instance = DirectorCore.instance;
            DirectorPlacementRule placementRule = new DirectorPlacementRule();
            placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
            placementRule.position = position;
            DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);
            GameObject interactable = instance.TrySpawnObject(directorSpawnRequest);
            if (isFree)
            {
                interactable.GetComponent<PurchaseInteraction>().Networkcost = 0;
            }
        }

        private void SetupJudgementBazaar(On.RoR2.BazaarController.orig_Start orig, BazaarController self)
        {
            orig(self);
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                tier1PickupIdx = PickupCatalog.FindPickupIndex(ItemTier.Tier1);
                tier2PickupIdx = PickupCatalog.FindPickupIndex(ItemTier.Tier2);
                tier3PickupIdx = PickupCatalog.FindPickupIndex(ItemTier.Tier3);
                yellowPickupIdx = PickupCatalog.FindPickupIndex(ItemTier.Boss);
                equipPickupIdx = PickupCatalog.equipmentIndexToPickupIndex[0];

                Debug.LogWarning(equipPickupIdx);
                Debug.LogWarning(EquipmentIndex.None);

                Run.instance.stageClearCount += 1;
                JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
                GameObject holder = GameObject.Find("HOLDER: Store");
                if (holder)
                {
                    judgementRun.shouldGoBazaar = false;
                    GameObject portal = GameObject.Instantiate(portalPrefab, new Vector3(-128.6f, -25.4f, -14.4f), Quaternion.Euler(0, 90, 0));
                    NetworkServer.Spawn(portal);

                    holder.transform.GetChild(1).gameObject.SetActive(false); // disable seers
                    holder.transform.GetChild(2).gameObject.SetActive(false); // disable cauldrons

                    // Disable lunar shop but keep props
                    foreach (Transform child in holder.transform.GetChild(0))
                    {
                        if (child.gameObject.name.Contains("Lunar"))
                        {
                            child.gameObject.SetActive(false);
                        }
                    }

                    // Spawn specific chests based on the wave
                    for (int i = 0; i < Run.instance.participatingPlayerCount; i++)
                    {
                        SpawnInteractable(chest1, new Vector3(-70.4f, -24.5f, -39.1f), judgementRun.bazaarRng);
                        SpawnInteractable(chest1, new Vector3(-73.7f, -24.7f, -42.9f), judgementRun.bazaarRng);
                        SpawnInteractable(chest1, new Vector3(-75.0f, -25.1f, -39.6f), judgementRun.bazaarRng);

                        switch (judgementRun.currentWave)
                        {
                            case 0:
                            case 4:
                                SpawnInteractable(goldChest, new Vector3(-81.4f, -23.7f, -45.5f), judgementRun.bazaarRng);
                                SpawnInteractable(equipChest, new Vector3(-77.1f, -24.4f, -45.4f), judgementRun.bazaarRng);
                                break;
                            case 6:
                                SpawnInteractable(chest2, new Vector3(-81.4f, -25.1f, -39.2f), judgementRun.bazaarRng);
                                SpawnInteractable(yellowChest, new Vector3(-77.1f, -24.4f, -45.4f), judgementRun.bazaarRng);
                                break;
                            case 8:
                                SpawnInteractable(goldChest, new Vector3(-81.4f, -23.7f, -45.5f), judgementRun.bazaarRng);
                                SpawnInteractable(chest2, new Vector3(-77.1f, -24.4f, -45.4f), judgementRun.bazaarRng);
                                break;
                            default:
                                SpawnInteractable(chest2, new Vector3(-81.4f, -25.1f, -39.2f), judgementRun.bazaarRng);
                                SpawnInteractable(chest2, new Vector3(-77.1f, -24.4f, -45.4f), judgementRun.bazaarRng);
                                break;
                        }
                    }

                    // Prevent Newt kick out and close the bazaar
                    GameObject kickout = SceneInfo.instance.transform.Find("KickOutOfShop").gameObject;
                    if ((bool)kickout)
                    {
                        kickout.gameObject.SetActive(true);
                        kickout.transform.GetChild(8).gameObject.SetActive(false);
                    }

                    // Spawn Void Cradles on specific waves
                    if (judgementRun.currentWave == 4 || judgementRun.currentWave == 8)
                    {
                        for (int i = 0; i < Run.instance.participatingPlayerCount; i++)
                        {
                            SpawnInteractable(voidChest, new Vector3(-90f, -25f, -11.5f), judgementRun.bazaarRng, false);
                        }
                    }   

                    // Spawn a Green Printer if someone has Regen Scrap
                    foreach (CharacterMaster readOnlyInstances in CharacterMaster.readOnlyInstancesList)
                    {
                        if (readOnlyInstances.inventory.GetItemCount(DLC1Content.Items.RegeneratingScrap) > 0)
                        {
                            SpawnInteractable(greenPrinter, new Vector3(-108.7849f, -27f, -46.7452f), judgementRun.bazaarRng, false);
                            break;
                        }
                    }

                    // Spawn Lockboxes based on players with Rusted Keys (limited to 1 per player)
                    Vector3[] lockboxPositions = new Vector3[] { new Vector3(-103.7627f, -24.5f, -4.7243f), new Vector3(-101.7627f, -24.5f, -4.7243f), new Vector3(-105.7627f, -24.5f, -4.7243f), new Vector3(-107.7627f, -24.5f, -4.7243f) };
                    for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
                    {
                        CharacterMaster master = CharacterMaster.readOnlyInstancesList[i];
                        if (master.inventory.GetItemCount(RoR2Content.Items.TreasureCache) > 0)
                            SpawnInteractable(lockBox, lockboxPositions[i], judgementRun.bazaarRng, false);
                    }

                     // Spawn Void Lockboxes based on players with Encrusted Keys (limited to 1 per player)
                    Vector3[] lockboxVoidPositions = new Vector3[] { new Vector3(-89.5709f, -23.5f, -6.589f), new Vector3(-87.5709f, -23.5f, -6f), new Vector3(-85.5709f, -23.5f, -5.589f), new Vector3(-83.5709f, -23.5f, -5f) };
                    for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
                    {
                        CharacterMaster master = CharacterMaster.readOnlyInstancesList[i];
                        if (master.inventory.GetItemCount(DLC1Content.Items.TreasureCacheVoid) > 0)
                            SpawnInteractable(lockBoxVoid, lockboxVoidPositions[i], judgementRun.bazaarRng, false);
                    }

                    // Spawn Free Chests based on players with Shipping Request Form (limited to 1 per player)
                    Vector3[] freeChestPositions = new Vector3[] { new Vector3(-122.9354f, -26f, -29.2073f), new Vector3(-123.8197f, -25.1055f, -22.2822f), new Vector3(-117.0709f, -24.2098f, -32.8076f), new Vector3(-110.2542f, -24.979f, -37.4319f) };
                          for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
                    {
                        CharacterMaster master = CharacterMaster.readOnlyInstancesList[i];
                        if (master.inventory.GetItemCount(DLC1Content.Items.FreeChest) > 0)
                            SpawnInteractable(freeChest, freeChestPositions[i], judgementRun.bazaarRng, false);
                    }
                }
            }
        }

        private void PreventPrinterCheese(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
            if (judgementRun && self.name == "DuplicatorLarge(Clone)")
            {
                int count = activator.GetComponent<CharacterBody>().inventory.GetItemCount(DLC1Content.Items.RegeneratingScrap);
                if (count == 0)
                    return;
            }
            else
                orig(self, activator);
        }
    }
}