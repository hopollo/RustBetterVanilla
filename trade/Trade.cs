using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Oxide.Core.Plugins;
using System.Threading;

namespace Carbon.Plugins;

[Info("Trade", "HoPollo", "1.0.0")]
[Description("Trade plugin")]
public class Trade : CarbonPlugin
{
  private readonly float _successTradeCooldownInMinutes = 10f;
  private readonly float _shopFrontLifetimeInSeconds = 30f;
  private Dictionary<ulong, float> _nextTradeTime = new();

  [ChatCommand("t")]
  void TradeCommand(BasePlayer player, string command, string[] args)
  {
    if (player == null || !player.IsAdmin) return;

    var customer = BasePlayer.activePlayerList.FirstOrDefault(p => p != player && p.IsConnected);
    if (customer == null)
    {
      SendReply(player, "Trade: No other connected players found to trade with.");
      //return;
    }

    var deployable = ItemManager.FindDefinitionByPartialName("wall.frame.shopfront.metal")?.GetComponent<ItemModDeployable>();
    var shopFront = GameManager.server.CreateEntity(
        deployable?.entityPrefab.resourcePath,
        player.transform.position + new Vector3(0, -200f, 0) // spawns far under the map to avoid any other interactions
    ) as ShopFront;

    if (shopFront == null)
    {
      Puts(player, "Failed to create basic shop front.");
      SendReply(player, "Trade: Failed to initialize, try again later.");
      return;
    }

    // Force server ownership to allow only on them (not players owned) further features
    shopFront.OwnerID = 0uL;

    /*  
      shopFront.limitNetworking = true; // makes it invisible
      foreach (var collider in shopFront.GetComponentsInChildren<Collider>())
      {
        collider.enabled = false;
      }
    */

    shopFront.Spawn();

    // Note (hopollo): Force player roles to avoid position checks/roles assignations issues that are harmony patched anyway.
    shopFront.vendorPlayer = player;
    shopFront.customerPlayer = customer ?? null;

    /*
    bool canVendorOpen = shopFront.CanOpenLootPanel(player, shopFront.panelName);
    bool canCustomerOpen = shopFront.CanOpenLootPanel(customer, shopFront.panelName) | false;
    bool vendorEligable = shopFront.LootEligable(player);
    bool customerEligable = shopFront.LootEligable(customer) || false;
    bool canBeLootedByVendor = shopFront.CanBeLooted(player);
    bool canBeLootedByCustomer = shopFront.CanBeLooted(customer) || false;

    Puts("===============TRADE INFO===============");
    Puts($"CanBeLooted {canBeLootedByVendor} <-> {canBeLootedByCustomer}");
    Puts($"Vendor: {player.displayName} <-> Customer: X");
    Puts($"CanOpenLootPanel Vendor: {canVendorOpen}");
    Puts($"Vendor Eligable ? {vendorEligable}");
    Puts($"isVendor: {shopFront.IsPlayerVendor(player)} <-> isCustomer: {shopFront.IsPlayerCustomer(player)}");
    Puts($"isInVendorPos: {shopFront.PlayerInVendorPos(player)} <-> isInCustomerPos: {shopFront.PlayerInCustomerPos(player)}");
    */

    // IMPORTANT (hopollo): Slight timer other/higher than NextFrame/NextTick
    // are need to avoid opening loot panel too early so it makes nothing player side.
    timer.In(0.5f, () =>
    {
      Puts("===============TRADE STARTED===============");
      if (shopFront.PlayerOpenLoot(player, shopFront.panelName, doPositionChecks: false))
      {
        Puts($"Trade w/ {shopFront.vendorPlayer.displayName}");
      }

      /*
      if (customer != null)
      {
        if (shopFront.PlayerOpenLoot(customer, shopFront.panelName))
        {
          Puts($"Trade w/ {shopFront.vendorPlayer.displayName}");
        }
      }
      */
      Puts("===============TRADE ENDED===============");
    });


    /*
      Puts("===============BEFORE===============");
      Puts($"Vendor: {shopFront.vendorPlayer.displayName} <-> Customer: {shopFront.customerPlayer?.displayName}");
      Puts($"CanOpenLootPanel Vendor: {shopFront.CanOpenLootPanel(player, shopFront.panelName)}");
      Puts($"CanOpenLootPanel Customer: {shopFront.CanOpenLootPanel(customer, shopFront.panelName)}");
      Puts($"Vendor Eligable ? {shopFront.LootEligable(player)}");
      Puts($"Customer Eligable ? {shopFront.LootEligable(customer)}");
      Puts($"IsPlayerVendor ? {shopFront.IsPlayerVendor(player)}");
      Puts($"IsPlayerCustomer ? {shopFront.IsPlayerCustomer(player)}");
      Puts($"IsPlayerInVendorPos ? {shopFront.PlayerInVendorPos(player)}");
      Puts($"IsPlayerInCustomerPos ? {shopFront.PlayerInCustomerPos(player)}");
      Puts($"IsTradingPlayer ? {shopFront.IsTradingPlayer(player)}");

        if (player.inventory.loot.StartLootingEntity(shopFront, doPositionChecks: false))
        {
          player.inventory.loot.AddContainer(shopFront.vendorInventory);
          player.inventory.loot.AddContainer(shopFront.customerInventory);
          player.inventory.loot.SendImmediate();

          player.ClientRPC(RpcTarget.Player("RPC_OpenLootPanel", player), shopFront.panelName);
        }

        if (customer.inventory.loot.StartLootingEntity(shopFront, doPositionChecks: false))
        {
          customer.inventory.loot.AddContainer(shopFront.vendorInventory);
          customer.inventory.loot.AddContainer(shopFront.customerInventory);
          customer.inventory.loot.SendImmediate();

          customer.ClientRPC(RpcTarget.Player("RPC_OpenLootPanel", customer), shopFront.panelName);
        }

        shopFront.UpdatePlayers();
        shopFront.ResetTrade();
      });
    });
   */

    timer.In(_shopFrontLifetimeInSeconds, () => shopFront.Kill());
  }
}
