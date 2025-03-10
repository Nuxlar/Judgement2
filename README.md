# Judgement

Compatible with Mithrix mods (Umbral, Inferno, AotK). Your HP persists between stages. You have no base regen.

There are a total of 5 rounds then you fight Mithrix at the end. Each round consists of a regular enemy wave and a boss wave then you move onto another stage for the next round (10 total waves). At the start of every round you'll start in the bazaar where you choose items and some items that were previously useless now have a function at the bazaar (keys, form, regen scrap). At the end of the 10th wave you'll get 1 last round of items then you'll be teleported to fight Mithrix.

         // 10 total waves, happens in pairs
                    // Wave 0 white x3, red, equip
                    // Wave 1-2 white x3, green x2
                    // Wave 2-4 white x3, green x2
                    // Wave 5-6 white x3, red, equip VOID
                    // Wave 7-8 white x3, green, yellow
                    // Wave 9-10 white x3, green, red VOID

                    // Chest Positions
                    // Gold Chest -81.4544 -23.7595 -45.4965
                    // Equip 
                    // Chest2/Yellow/Equip
                    // -81.4306 -25.1229 -39.2297
                    // -77.1273 -24.3599 -45.3712
                    // Chest1 
                    // -70.3718 -24.4882 -39.1348
                    // -73.705 -24.6836 -42.9798
                    // -75.0178 -25.0719 -39.5813
                    // chest.gameObject.GetComponent<PurchaseInteraction>().networkCost = 0;
                    // Spawn chests