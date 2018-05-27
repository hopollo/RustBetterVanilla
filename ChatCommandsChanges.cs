using System.Collections.Generic;

namespace Oxide.Plugins
{
	[Info("ChatCommandChanges", "HoPollo", 1.0)]
	public class ChatCommandsChanges : RustPlugin
	{
		public bool DebugMode = false;
		public bool HideAdmin = false;
		public string GameModName = $"<size=15><color=#ffcc00>Fast & Rustious</color></size>";

		public bool Purge = false;
		public bool Help = false;

		public void Debug(string debug)
		{
			if (!DebugMode) return;
			Puts(debug);
		}

		private void Loaded()
		{
			Puts("ChatCommandsChanges" + " -> Charge");
			Debug("DEBUG MODE -> Enabled");
		}
		
		private void OnPlayerInit(BasePlayer player)
		{
			if (Purge)
			{
				SendReply(player, "Purge time is available ! Use /purge to enjoy !");
			}
		}

		private void OnPlayerRespawned(BasePlayer player)
		{
			if (Purge)
			{
				SendReply(player, "Purge time is available ! Use /purge to enjoy !");
			}
		}
		
		// Chat command to detect cheaters

		[ChatCommand("jump")] // For fall down damages cheats, throw the player on air
		private void JumpChatCommand(BasePlayer player)
		{
			//TODO : Makes the selected player jump high in the air
			SendReply(player, $"Throwing");
		}

		[ChatCommand("purge")] // Fun explosives to wipe map by players
		private void PurgeChatCommand(BasePlayer player)
		{
			if (Purge)
			{
				var items = new int[12, 2]{
					{1578894260, 1000},
					{1295154089, 1000},
					{815896488, 500},
					{586484018, 6},
					{649603450, 2},
					{-1461508848, 1},
					{-46848560, 1},
					{-1595790889, 1},
					{707427396, 1},
					{707432758, 1},
					{1767561705, 1},
					{1265861812, 1},
				};

				for (var x = 0; x < items.Length / 2; x++)
				{
					player.inventory.GiveItem(ItemManager.CreateByItemID(items[x, 0], items[x, 1]));
				}
			}
			else
			{
				SendReply(player, "Purge time not available yet");
			}
		}

		//[ChatCommand("test")]
		//private void TestChatCommand(BasePlayer player)
		//{
		//	//CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, null, new Facepunch.ObjectList("https://www.youtube.com/watch?v=THLeuQshOz8"));
		//	//SendReply(player, player.GetActiveItem().info.shortname);
		//}

		[ChatCommand("help")]
		private void HelpChatCommand(BasePlayer player)
		{
			if (Help)
			{
				SendReply(player, $"<color=#5091ff>Available commands</color> :" +
								  "\n /wipe - Wipe info" +
								  "\n /players - Players info" +
								  "\n /info - Server goal" +
								  "\n /who - Credits" +
								  "\n /changelog - News" +
								  "\n /why - Motivations about that");
			}
		}

		[ChatCommand("wipe")]
		private void WipeChatCommand(BasePlayer player)
		{
			SendReply(player, $"<color=#5091ff>Server Wipe </color> :" +
							  "\n Every weeks @ Facepuch updates or less than 10 players (afternoon/night), more info in the description.");
		}

		[ChatCommand("info")]
		private void InfoChatCommand(BasePlayer player)
		{
			SendReply(player, $"This gamemode is called " + GameModName + " by " + $"<size=14><color=#5091ff>HoPollo</color></size>" +
							"\n No Components are required" +
							"\n Old loot on barrels/crates system" +
							"\n Just farm to craft & Pvp !" +
							"\n Use /help to display commands");
		}

		[ChatCommand("who")]
		private void WhoChatCommand(BasePlayer player)
		{
			SendReply(player, $"<color=#5091ff>Credits</color> : " +
							  "\n Staff : HoPollo(Dev), Razoks(Dev)" +
							  "\n Donors : Casawi, Rabs, Grim, Rapace, Parigo, Diwan_One");
		}

		[ChatCommand("why")]
		private void WhyChatCommand(BasePlayer player)
		{
			SendReply(player, $"<color=#5091ff>Why ?</color>" +
							  "\n 1°) Tired of weird modded setups" +
							  "\n 2°) Keep vanilla aspects (intense fights, back to home alive & loaded feeling, etc...)" +
							  "\n 3°) Escape from forced roleplay by Facepunch to Pvp fuccbois " +
							  "\n 4°) Focus on skill to win not farm to roofcamp" +
							  "\n 5°) Create someting cool, learn programming, mess around the game");
		}

		[ChatCommand("players")]
		private void PlayersChatCommand(BasePlayer player)
		{
			SendReply(player, $"<color=#5091ff>Online players</color> : " + BasePlayer.activePlayerList.Count.ToString());
		}

		[ChatCommand("changelog")]
		private void ChangelogChatCommand(BasePlayer player)
		{
			SendReply(player, $"<color=#5091ff>09/04</color> :" +
							  "\n-Instacraft now give choosen skin" +
							  "\n-Syringes + bandages heal buffed" +
							  "\n-player belt glitch fixed" +

							  $"\n<color=#5091ff>08/04</color> :" +
							  "\n-Optimized Half/InstaCrafting features implemented"+
							  
							  $"\n\n Find all updates on our ({GameModName}) steam group.");
        }
	}
}