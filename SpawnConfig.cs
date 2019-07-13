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
		public bool ForceStaffKit = false;
		public bool DebugMode = true;

		private readonly List<int> _compo = new List<int>()
		{
				479143914,    // Gears
				95950017,     // Metal Pipe
				176787552,    // Riffle body
				573926264,    // Semi autobody
				1230323789,   // SMG Body
				-1021495308,  // Metal spring
				1414245522,   // Rope
				1234880403,   // sewing kits
				1199391518,   // Roadsigns
				2019042823,	  // Tarps
		};

		public void Debug(string debug)
		{
			if (DebugMode) Puts(debug);
		}

		private void Loaded()
		{
			Puts("SpawnConfig -> Loaded");
			Debug("DEBUG MODE -> Enabled");
		}

		private void OnPlayerInit(BasePlayer player)
		{
			GiveDefaultMetabolismValues(player);

			if (ForceStaffKit && player.IsAdmin) {
				GiveDefaultStaffKit(player);
			} else {
				GiveInfiniteComponents(player);
			}
		}

		private void OnPlayerRespawned(BasePlayer player)
		{
			GiveDefaultMetabolismValues(player);

			if (ForceStaffKit && player.IsAdmin) {
				GiveDefaultStaffKit(player);
			} else {
				GiveAutoKit(player);
			}
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
		}

		private static void GiveDefaultMetabolismValues(BasePlayer player)
		{
			player.health = Starthealth;
			player.metabolism.calories.value = Startcalories;
			player.metabolism.hydration.value = Starthydration;
		}

		private void OnEntitySpawned(BaseNetworkable entity)
		{
			Debug($"Entity Spawned : {entity}");
			//if (!(entity as LootableCorpse)) return;
			//var corpse = (LootableCorpse) entity;
			//RemoveInfiniteComponentsFromCorpse(corpse);
		}

		private void GiveInfiniteComponents(BasePlayer player)
		{
			foreach (var compoToCreate in _compo)
			{
				var compoCreated = ItemManager.CreateByItemID(compoToCreate, 1000);
				compoCreated.MoveToContainer(player.inventory.containerMain, player.inventory.containerMain.capacity++);
					//player.inventory.containerMain.GetSlot(slotToUse).IsLocked();
			}
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
			foreach (var componentToRemove in _compo)
			{
				player.inventory.Take(null, componentToRemove, player.inventory.GetAmount(componentToRemove));
			}
		}

		private void OnItemCraftCancelled(ItemCraftTask task)
		{
			//Debug("OnItemCraftCancelled works!");
			
			var player = task.owner;
			/*
			var container = player.inventory.containerMain;
			var containerList = new List<Item>();

			task.cancelled = true;
			*/

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

			var ItemstoAddAutokitBeltInventory = new List<string>
			{
				"bow.hunting",
				"spear.wooden",
				"stonehatchet",
				"stone.pickaxe",
				"torch",
				"bandage",
			};

			foreach (var item in ItemstoAddAutokitBeltInventory) {
				player.inventory.GiveItem(ItemManager.CreateByName(item), player.inventory.containerBelt);
			}

			var ItemstoAddAutokitMainInventory = new List<Item> {
				ItemManager.CreateByName("arrow.wooden", 26),
			};
			
			ItemstoAddAutokitMainInventory[0].MoveToContainer(player.inventory.containerMain);
			
			GiveInfiniteComponents(player);
		}
	}
}
