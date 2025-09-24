private void GiveDefaultMetabolismValues(BasePlayer player)
  {
    player.SetHealth(Configuration.Startcalories);
    player.metabolism.calories.value = Configuration.Startcalories;
    player.metabolism.hydration.value = Configuration.Starthydration;
  }
