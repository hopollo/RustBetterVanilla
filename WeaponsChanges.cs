using System;
using Rust;
using System.Collections.Generic;
using ConVar;
using UnityEngine;

// TODO : ADD possibility to remove Campfire as same as Lantern new system with Hammer
	// TODO : Put back F1 grenade damage on buildings

namespace Oxide.Plugins
{
	[Info("Weaponchanges", "HoPollo", 2.0)]
	public class WeaponsChanges : RustPlugin
	{
		public int Chance;
		public int Random;
		public bool DebugMode = false;
		public bool NightAllowed = true;
		public bool InstaCraft = false;
		public bool HalfCraft = true;
		public bool HalfCraftMode;
		public bool AnimalsIa = true;
		public bool Radiations = false;
		public bool CustomDamages = true;
		public bool Initialized;

		public void Debug(string debug)
		{
			if (!DebugMode) return;
			Puts(debug);
		}

		List<ItemBlueprint> _blueprintsList = ItemManager.bpList;
		//List<ItemDefinition> _definitionsList = ItemManager.itemList;

		private readonly Dictionary<int, int> _whatToTransform = new Dictionary<int, int>()
		{
			{-1059362949, 688032252}, // Metal Ore -> Metal Fragments
			{2133577942, 374890416}, // High Quality Metal Ore -> High Quality Metal
			{889398893, -891243783}, // Sulfur Ore -> Sulfur
		};

		private readonly  List<string> _itemWithoutConditionLoss = new List<string>()
		{
			"rifle.ak",
			"pistol.semiauto",
			"pistol.revolver",
			"flamethrower",
			"rifle.semiauto",
			"pistol.python",
			"rifle.bolt",
		};

		private readonly Dictionary<BasePlayer, Timer> _woundedTimers = new Dictionary<BasePlayer, Timer>();

		private void OnServerInitialized()
		{
			if (Initialized) return;
			SetServerEnvironementStatus();
			DisableRadiations();
			DisableAnimals();
			SetServerDamageScale();
			PlantAccelerator(30f, 5f);
			Initialized = true;
		}

		private void SkipNight()
		{
			// TODO : Put 55Min days, 5 Min Night (Best is the read vars to change it faster and stuff
			// ISSUE : SkippedNight but don't make heli + airdrop spawn as well

			if (NightAllowed) return;
			const int sunsetHour = 16;
			const int sunriseHour = 10;

			try
			{
				if (TOD_Sky.Instance.Cycle.Hour <= sunsetHour && TOD_Sky.Instance.Cycle.Hour >= sunriseHour)
				{
				}
				else
				{
					TOD_Sky.Instance.Cycle.Hour = sunriseHour;
					Debug("Night skipped !");
				}
			}
			catch (Exception ex)
			{
				PrintError("OnTick failed: {0}", ex.Message);
			}
		}

		private static void SetServerEnvironementStatus()
		{
			ConsoleSystem.Run(ConsoleSystem.Option.Server, "weather.clouds ", 0);
			ConsoleSystem.Run(ConsoleSystem.Option.Server, "weather.rain", 0);
			ConsoleSystem.Run(ConsoleSystem.Option.Server, "weather.wind", 0);
			ConsoleSystem.Run(ConsoleSystem.Option.Server, "weather.fog", 0);
		}

		private static void DisableRadiations()
		{
			ConVar.Server.radiation = false;
		}

		private void DisableAnimals()
		{
			if (AnimalsIa) return;
			AI.think = false;
			AI.move = false;
		}

		private void SetServerDamageScale()
		{
			if (!CustomDamages) return;
			ConVar.Server.arrowdamage = 0f + 1.3f;
			ConVar.Server.bulletdamage = 0f + 1.3f;
		}

		public void PlantAccelerator(float tick, float tickScale)
		{
			ConVar.Server.planttick = tick;
			ConVar.Server.planttickscale = tickScale; 
		}

		private void Loaded()
		{
			Puts("WeaponChanges -> Loaded");
			Debug("DEBUG MODE -> Enabled");

			if (!HalfCraft && HalfCraftMode) return;
			foreach (var bp in _blueprintsList)
			{
				bp.time = bp.time / 2;
			}
			HalfCraftMode = true;
		}

		private void Unloaded()
		{
			if (!HalfCraftMode) return;
			foreach (var bp in _blueprintsList)
			{
				bp.time = bp.time*2;
				HalfCraftMode = false;
			}
		}

		private void OnPlayerAttack(BasePlayer attacker, HitInfo info)
		{
			//Debug("OnPlayerAttack works!");

			if (attacker == null || info == null) return;

			if (info.DidHit && info.HitEntity as BasePlayer)
			{
				Effect.server.Run("assets/bundled/prefabs/fx/player/gutshot_scream.prefab", info.HitEntity.transform.position, Vector3.zero);
			}
		}

		private void OnHammerHit(BasePlayer player, HitInfo info)
		{
			//Admin hammer gonna destroy the entity getting hits

			if (!player.IsAdmin) return;
			SendReply(player, $"Removed : {info.HitEntity.ShortPrefabName}");

			if (info.HitEntity == null) return;
			if (!info.HitEntity.IsDestroyed)
			{
				NextFrame(() =>
				{
					info.HitEntity.Kill();
				});
			}
		}

		private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{	
			// TODO : Add Hit sound when player is hurt ! (not cold)

			Debug($"OnEntityTakeDamage : {info?.Initiator?.ShortPrefabName}");

			//TODO : Assign damage only if block || entity is backward
			if (entity == null) return;
			if (info == null) return;

			var damage = info.damageTypes.GetMajorityDamageType();
			var entityName = entity?.ShortPrefabName;
			var weapon = info?.Weapon?.ShortPrefabName;
			var block = entity?.GetComponent<BuildingBlock>();

			if (info.Initiator != null && info.HitEntity.ToPlayer())
			{
				//Debug("Joueur Hit");
				Effect.server.Run("assets/bundled/prefabs/fx/player/gutshot_scream.prefab", info.HitEntity.transform.position, Vector3.zero);
			}

			if (entityName == null) return;
			if (weapon == null) return;	
			if (block == null) return;

			var arrowRaidAvailable = new List<string>
			{
				"door.hinged.wood",
				"door.double.hinged.wood",
				"shutter.wood.a",
				"wall.window.bars.wood",
			};

			var boneclubRaidAvailable = new List<string>
			{
				"door.hinged.metal",
				"door.double.hinged.metal",
				"floor.ladder.hatch",
			};
			
			if ((block.currentGrade.gradeBase.name.Contains("wood") && weapon.Contains("bow_hunting.entity")) ||
				weapon.Contains("crossbow.entity"))
			{
				info.damageTypes.Add(DamageType.Decay, 1f);
			}

			if (arrowRaidAvailable.Contains(entityName) && weapon.Contains("bow_hunting.entity") ||
				weapon.Contains("crossbow.entity"))
			{
				Debug($"Entity found : {entityName} Hp : {entity.health}");
				info.damageTypes.Add(DamageType.Decay, 1f);
			}

			if (!boneclubRaidAvailable.Contains(entityName) || !weapon.Contains("bone_club.entity")) return;
			Debug($"Entity found : {entityName} Hp : {entity.health}");
			info.damageTypes.Add(DamageType.Decay, 1f);
		}

		private void OnEntitySpawned(BaseNetworkable entity, BaseNetworkable.LoadInfo info, BasePlayer player, BaseEntity.RPCMessage msg)
		{
			//Debug("Entity spawned");

			if (entity as WorldItem)
			{
				var minerai = (WorldItem)entity;
				var itemName = minerai.item.info.displayName.english;
				var itemId = minerai.item.info.itemid;

				Debug($"EntitySpawn as WorldItem : {itemName} ({itemId})");

				if (_whatToTransform.ContainsKey(itemId))
				{
					minerai.item.info.itemid = _whatToTransform[minerai.item.info.itemid];
					minerai.SendNetworkUpdate();
				}
			}

			if (!(entity as SleepingBag)) return;
			var sleepingbag = (SleepingBag)entity;

				Debug($"ENTITY SPAWNED : {sleepingbag.PrefabName}");

				DeployedSleepingBag(sleepingbag);
		}

		private void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
		{
			// TODO : Display back default "Building blocked" gui info

			//Debug("OnEntityEnter works!");

			//var player = entity as BasePlayer;
			//if (player)
			//{
			//	//player.SetPlayerFlag(BasePlayer.PlayerFlags.HasBuildingPrivilege, false);
			//	Debug($"Layer : {trigger.interestLayers}");
			//} 
		}

		private void OnPlayerWound(BasePlayer player)
		{
			//Debug($"Player {player.displayName} is DOWN!");

			// TODO : Improve detection, (intant interup sound if revieved or dead)

			Effect.server.Run("assets/bundled/prefabs/fx/player/beartrap_scream.prefab", player.transform.position);

			_woundedTimers.Add(player, timer.Repeat(7f, 0, () =>
			{
				if (!player.IsWounded() && !_woundedTimers[player].Destroyed)
				{
					_woundedTimers[player].Destroy();
					_woundedTimers.Remove(player);

					return;
				}
				Effect.server.Run("assets/bundled/prefabs/fx/player/beartrap_scream.prefab", player.transform.position);
			}));
		}

		private void OnCropGather(PlantEntity plant, Item item, BasePlayer player)
		{
			HempCropQuatityChanger(item);
		}

		private static void HempCropQuatityChanger(Item item)
		{
			if (item?.info.itemid != 94756378) return;
			if (item.amount < 10) { item.amount = 10; return; }
			if (item.amount < 10) return;
			item.amount = 20;
		}

		private void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
		{
			//Debug("Fuel used : " + oven.ShortPrefabName.ToString());

			var allowedToRefull = new List<string>
			{
				"tunalight", "ceilinglight", "lantern",
			};

			if (!allowedToRefull.Contains(oven.panelName)) return;
			//Debug("Refuilling");
			fuel.amount += 1;
		}

		private void OnItemUse(Item item, int amount)
		{
			Debug("Consumable used : " + item.info.shortname + " (" + item.info.itemid + ")");

			var player = item.GetOwnerPlayer();

			var badVegetables = new int[7, 2]{
				{-991829475,    60},	// Cooked Human Meat
				{-1565095136,   100},	// Rotten Apple
				{-533484654,    100},	// Raw Fish
				{-1658459025,   100},	// Raw Chiken
 				{1711323399,    100},	// Burned Chicken
				{179448791,     100},	// Raw WolfMeat
				{-642008142,    100},	// Raw HumanMeat
			};

			for (var x = 0; x < badVegetables.Length / 2; x++)
			{
				if (badVegetables[x, 0] != item.info.itemid) continue;
				Chance = badVegetables[x, 1];
				Random = UnityEngine.Random.Range(1, 100);
				//Debug("Bad vegetables eated : " + Random + " of " + Chance);
				if (Chance >= Random)
				{
					player.Hurt(7f);

					// TODO : ADD animation when player head look down when vomiting
					Effect.server.Run("assets/bundled/prefabs/fx/gestures/drink_vomit.prefab", player.transform.position, Vector3.zero);
				}

				break;
			}

			if (item.info.itemid == -789202811)
			{
				OnLargeMedkitUse(item);
			}
		}

		private void OnLargeMedkitUse(Item item)
		{
			/* TODO : 
			/	Fix healing over time ? 
			/	Debug if use from container and not from player inventory itself
			/	Block others Medkits while healing (cooldown)
			/	Add heal stop if player gets any hurts (especially wounded)(without cold)
			/	Add sound while gets healing for notice others players
			*/

			if (item == null) return;
			var player = item.GetOwnerPlayer();
			//var hitinfo = new HitInfo();
			//var medkitId = -789202811;
			//var container = player.inventory.containerMain;
			//var containerList = new List<Item>();

			//player.Hurt(95f);
			Debug($"MedKit");

			//containerList.AddRange(container.itemList.Where(medkits => item.info.itemid.Equals(medkitId)));
			//foreach (var medkits in containerList)
			//{
			//	medkits.SetFlag(global::Item.Flag.IsLocked, true);  // ISSUE : LOCKS ALL ITEMS
			//}

			var healOverTime = timer.Repeat(1f, 10, () => player.Heal(5f));

			//timer.Destroy(ref HealOverTime);

			//player.metabolism.ApplyChange(MetabolismAttribute.Type.HealthOverTime, 10f, 10f); // ISSUE : NOT WORKING
		}

		private void OnHealingItemUse(HeldEntity item, BasePlayer player)
		{
			Debug($"Heal : {item.ShortPrefabName}");

			if (item is MedicalTool && item.ShortPrefabName == "syringe_medical.entity")
			{
				player.Heal(6f);
			}

			if (item is MedicalTool && item.ShortPrefabName == "bandage.entity")
			{
				player.Heal(5f);
			}
		}

		private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
		{
			// TODO : Detect when player is hit + remove explosive damage to players 
			Debug("OnRocketLaunched works!");

			if (entity as BasePlayer)
			{
				Debug("Hitplayer");
			}
		}

		private void OnItemCraft(ItemCraftTask task)
		{
			var item = task.blueprint.targetItem.itemid;
			var skinId = task.skinID;
			var amountToCreate = task.amount * task.blueprint.amountToCreate;
			var player = task.owner;

			Debug($"Craft : {task.blueprint.name} {task.blueprint.time} ");

			if (task.blueprint.name == "map.item")
			{
				Debug("MAP crafted");
				task.cancelled = true;
				player.GiveItem(ItemManager.CreateByPartialName("paper"));
				SendReply(player, "Use G (Default) to look the map");
				return;
			}

			if (InstaCraft)
			{
				task.cancelled = true;

				Debug("Insta");
				if (task.blueprint.targetItem.itemid == 107868) return; // MAP
				
				var whatToCreate = ItemManager.CreateByItemID(item, amountToCreate, (ulong)skinId);

				if (player.inventory.GiveItem(whatToCreate))
				{
					player.Command("note.inv", item, amountToCreate);
				}
				else
				{
					whatToCreate.Drop(player.GetDropPosition(), player.GetDropVelocity(), new Quaternion());
				}
			}
		}

		private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
		{
			//Debug("OnDispenserGather works!");

			var player = entity.ToPlayer();

			if (dispenser.gatherType != ResourceDispenser.GatherType.Tree) return;
				OnPlayerGatherTree(player, item);
		}

		private void OnCollectiblePickup(Item item, BasePlayer player)
		{
			Debug($"OnCollectiblePickup works! ({item.info.displayName.english} - {item.info.itemid})");

			if (item.info.itemid == 3655341)
			{
				const int charcoalId = 1436001773;
				var amountCharcoal = (item.amount/10);

				var whatToCreate = ItemManager.CreateByItemID(charcoalId, amountCharcoal);

				if (player.inventory.GiveItem(whatToCreate))
				{
					player.Command("note.inv", charcoalId, amountCharcoal);
				}
				else
				{
					whatToCreate.Drop(player.GetDropPosition(), player.GetDropVelocity(), new Quaternion());
				}
			}

			if (item.info.itemid != 94756378) return;
			item.amount = item.amount * 2;
		}

		private void OnItemAddedToContainer(ItemContainer container, Item item)
		{
			var itemId = item.info.itemid;
			var itemAmount = item.amount;

			if (_whatToTransform.ContainsKey(item.info.itemid))
			{
				container.Take(null, itemId, itemAmount);
				item.Drop(container.dropPosition, container.dropVelocity, new Quaternion());
				item.MoveToContainer(container);
			}
		}

		private void OnLoseCondition(Item item, ref float amount)
		{
			//Debug($"OnLoseCondition : {item.info.shortname}({item.info.itemid})");

			var player = item.GetOwnerPlayer();

			// TODO : Fix condition loss on Attires, guns is working great

			if (item.info.category == ItemCategory.Attire)
			{
				// TODO : Check if its working (Wears should not lose conditions)
				item.condition = item.condition + amount;
			}

			if (_itemWithoutConditionLoss.Contains(item.info.shortname) && item.contents.itemList.Count < 1 || item.info.shortname.Contains("rocket.launcher"))
			{
				item.condition = item.condition + amount;
			}

			if (item.info.itemid == 110547964 || Math.Abs(amount - 0.166666672f) > 0.000000001f)
			{
				WarmPlayer(player, item);
			}
		}

		private static void WarmPlayer(BasePlayer player, Item item)
		{
			// TODO : Improve this maybe ? + Add warming others players around him 
			player.metabolism.temperature.MoveTowards(18f, 9f);
			//player.transform.position
		}

		private void OnAutoturretFire(AutoTurret turret, float aimCone)
		{
			turret.sightRange = 15f;
		}

		private void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
		{
			/* TODO : 
				- Remove retarded cone of fire/bloom
				- Put back the old crazy recoil of Ak, fuck noobs
			*/

			var item = projectile.GetItem();

			Debug($"SHOOT : {projectile.GetAimCone()} {projectile.aimCone} {projectile.aimSway}");

			var shotgunsNerfed = new int[2, 2]
			{
				{-1009492144, 30},	// Shotgun Pump
				{191795897, 30}	// Double Barrel
			};

			//Debug("Shooting with : " + item.info.shortname + " (" + item.info.itemid + ")");

			for (var x = 0; x < shotgunsNerfed.Length / 2; x++)
			{
				if (shotgunsNerfed[x, 0] != item.info.itemid) continue;
				Chance = shotgunsNerfed[x, 1];
				Random = UnityEngine.Random.Range(0, 100);
				//Debug("Shotgun fail : " + Random + " of " + Chance);
				if (Chance <= Random) return;
				item.LoseCondition(10f);
				break;
			}

			if (item.info.itemid != 2077983581) return;
			OnPipeShotgunUse(player, item);
		}

		private static void OnPlayerGatherTree(BasePlayer player, Item item)
		{
			const int charcoalId = 1436001773;
			var amountCharcoal = item.amount / 10;

			if (amountCharcoal < 1) return;

			var whatToCreate = ItemManager.CreateByItemID(charcoalId, amountCharcoal);

			if (player.inventory.GiveItem(whatToCreate))
			{
				player.Command("note.inv", charcoalId, amountCharcoal);
			}
			else
			{
				whatToCreate.Drop(player.GetDropPosition(), player.GetDropVelocity(), new Quaternion());
			}
		}

		private void OnPipeShotgunUse(BasePlayer player, Item item)
		{
			Chance = 25;
			Random = UnityEngine.Random.Range(0, 100);
			Debug("PipeExplosion : " + Random + " of " + Chance);
			if (Chance <= Random) return;
			item.Drop(player.eyes.position, player.eyes.BodyForward() * 3f * UnityEngine.Random.Range(1f, 1.5f));
			player.Hurt(40f, DamageType.Explosion, player);
			player.metabolism.bleeding.value = 0.20f;
			item.LoseCondition(50f);
			rust.SendChatMessage(player, $"Your {item.info.displayName.english.ToLower()} exploded in your hands !");
		}

		private void DeployedSleepingBag(BaseNetworkable entity)
		{
			// TODO : Remove the cooldown on sleepingbags only (not beds), right after Deployed

			Debug("Sleepingbad deployed");
			var sleep = entity as SleepingBag;
			var sleepId = entity.net.ID;

			Debug($"SleepingbagID : {sleepId}");
			sleep.niceName = "<3";
			sleep.secondsBetweenReuses = 180f;
			sleep.SendNetworkUpdate();
			Debug($"Sleeping : {sleep.unlockSeconds} cooldown");
		}
	}
}