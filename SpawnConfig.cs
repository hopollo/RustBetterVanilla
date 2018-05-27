using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
	[Info("SpawnConfig", "HoPollo", 2.0)]

	public class SpawnConfig : RustPlugin
	{
		private readonly Dictionary<string, int> _itemsBelt = new Dictionary<string, int>();
		private readonly Dictionary<string, int> _items = new Dictionary<string, int>();
		private const float Starthealth = 100f, Startcalories = 250f, Starthydration = 125f;
		private const int BasicInventorySlots = 24;
		private const int BasicBeltSlots = 7;
		public bool ForceStaffKit = true;
		public bool DebugMode = false;

		private readonly List<int> _compo = new List<int>()
		{
				98228420,     // Gears
				-1057402571,  // Metal Pipe
				1939428458,   // Riffle body
				1223860752,   // Semi autobody
				-2092529553,  // SMG Body
				1835797460,   // Metal spring
				3506418,      // Rope
				-419069863,   // sewing kits
				-847065290,   // Roadsigns
				3552619,	  // Tarps
		};

		public void Debug(string debug)
		{
			if (DebugMode)
			{
				Puts(debug);
			}
		}

		private void Loaded()
		{
			Puts("SpawnConfig -> Loaded");
			Debug("DEBUG MODE -> Enabled");
		}

		private void OnPlayerInit(BasePlayer player)
		{
			GiveDefaultMetabolismValues(player);

			if (ForceStaffKit)
			{
				if (player.IsAdmin)
				{
					GiveDefaultStaffKit(player);
					return;
				}
			}

			GiveInfiniteComponents(player);
		}

		private void OnPlayerRespawned(BasePlayer player)
		{
			GiveDefaultMetabolismValues(player);

			if (ForceStaffKit)
			{
				if (player.IsAdmin)
				{
					GiveDefaultStaffKit(player);
					return;
				}
			}

			GiveAutoKit(player);
		}

		private void OnPlayerDie(BasePlayer player, HitInfo info)
		{
			player.inventory.crafting.CancelAll(true);
			//timer.Once(.1f, ()=> { RemoveInfiniteComponents(player); });
			RemoveInfiniteComponents(player);
		}

		private void OnPlayerDisconnected(BasePlayer player, string reason)
		{
			RemoveInfiniteComponents(player);
			BasePlayer.activePlayerList.Remove(player);
		}

		private static void GiveDefaultMetabolismValues(BasePlayer player)
		{
			player.health = Starthealth;
			player.metabolism.calories.value = Startcalories;
			player.metabolism.hydration.value = Starthydration;
		}

		private void OnEntitySpawned(BaseNetworkable entity)
		{
			//if (!(entity as LootableCorpse)) return;
			//var corpse = (LootableCorpse) entity;
			//RemoveInfiniteComponentsFromCorpse(corpse);
		}

		private void GiveInfiniteComponents(BasePlayer player)
		{
			Debug($"GiveInfiniteComponents : {player.inventory.containerMain.capacity} Main slots");

			player.inventory.containerMain.capacity = BasicInventorySlots + _compo.Count;

			Debug($"New Inv : {player.inventory.containerMain.capacity} ({BasicInventorySlots} + {_compo.Count}) added");

			foreach (var compoToCreate in _compo)
			{
				var compoCreated = ItemManager.CreateByItemID(compoToCreate, 1000);
				var slotToUse = BasicInventorySlots;

				for (var i = 0; i < compoCreated.amount; i++)
				{
					compoCreated.MoveToContainer(player.inventory.containerMain, slotToUse++);
					//player.inventory.containerMain.GetSlot(slotToUse).IsLocked();
				}
			}

			player.inventory.SendSnapshot();
		}

		private void RemoveInfiniteComponentsFromCorpse(LootableCorpse corpse)
		{

			//TODO : WTF how compo + map are removed if nothing is available here ????????????????

			Debug($"Corpse");
			//var container = corpse.containers[0];
			////var containerList = new List<Item>();

			//Debug($"Remove comp : Corpse {container.capacity} slots");
			////containerList.AddRange(container.itemList.Where(item => !_compo.Contains(item.info.itemid)));
			//container.itemList.AddRange(container.itemList.Where(item => _compo.Contains(item.info.itemid)));
			////container.itemList.Clear();
			////container.capacity = BasicInventorySlots;

			//foreach (var item in container.itemList)
			//{
			//	item.position = item.position - _compo.Count;
			//	//container.Take(null, componentToRemove, container.GetAmount(componentToRemove, false));
			//}
			////container.itemList = containerList;
		}

		private void RemoveInfiniteComponents(BasePlayer player)
		{
			const int mapId = 107868;

			player.inventory.containerMain.capacity = BasicInventorySlots;

			foreach (var componentToRemove in _compo)
			{
				player.inventory.Take(null, componentToRemove, player.inventory.GetAmount(componentToRemove));
			}

			player.inventory.containerBelt.Take(null, mapId, player.inventory.GetAmount(mapId));

			player.inventory.SendSnapshot();
		}

		private void OnItemCraftCancelled(ItemCraftTask task)
		{
			//Debug("OnItemCraftCancelled works!");

			var player = task.owner;
			var container = player.inventory.containerMain;
			var containerList = new List<Item>();

			task.cancelled = true;

			timer.In(.1f, () => { RemoveInfiniteComponents(player); });
			timer.Once(.3f, () => { GiveInfiniteComponents(player); }); // Don't work if under .1f
		}

		private void GiveDefaultStaffKit(BasePlayer player)
		{
			player.inventory.Strip();

			var toAddAutokitBelt = new List<string>
			{
				"hammer",
				"map",
			};

			foreach (var itemsCreated in toAddAutokitBelt.Select(beltToCreate => ItemManager.CreateByName(beltToCreate)))
			{
				player.inventory.GiveItem(itemsCreated, player.inventory.containerBelt);
			}
		}

		private void GiveAutoKit(BasePlayer player)
		{
			//TODO : Give already reloaded bow

			player.inventory.Strip();

			player.inventory.containerBelt.capacity = BasicBeltSlots;

			Debug($"GiveAutoKit belt : {BasicBeltSlots}");

			var toAddAutokitBelt = new List<string>
			{
				"bow.hunting",
				"spear.wooden",
				"stonehatchet",
				"stone.pickaxe",
				"torch",
				"bandage",
				 // NOT GIVING THE LAST ITEM
			};

			foreach (var beltToCreate in toAddAutokitBelt)
			{
				var itemsCreated = ItemManager.CreateByName(beltToCreate);

				player.inventory.GiveItem(itemsCreated, player.inventory.containerBelt);
			}

			var toAddHidenItem = new List<Item>
			{
				ItemManager.CreateByName("map"),
			};

			player.inventory.containerBelt.capacity += toAddHidenItem.Count;
				toAddHidenItem[0].MoveToContainer(player.inventory.containerBelt, -1, false);
					player.inventory.containerBelt.capacity -= toAddHidenItem.Count;
			//player.inventory.containerBelt.GetSlot(7).IsLocked();

			var toAddAutokitMain = new List<Item>
			{
				ItemManager.CreateByName("arrow.wooden", 26),
			};

			toAddAutokitMain[0].MoveToContainer(player.inventory.containerMain);

			//player.inventory.containerWear.SetFlag(ItemContainer.Flag.IsLocked, false);
			//player.inventory.containerBelt.SetFlag(ItemContainer.Flag.IsLocked, false);

			//player.inventory.containerBelt.capacity -= 1;

			player.inventory.SendSnapshot();

			GiveInfiniteComponents(player);
		}
	}
}