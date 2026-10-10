public static  class PlayerLevelRewards 
{
    public static void Apply(Player player, int level)
    {
        switch (level)
        {
            case 2:
                player.AddInventorySlots(1);
                break;

            case 3:
                player.IncreaseSpeed(0.05f);
                break;

            case 4:
                player.AddInventorySlots(1);
                break;
            case 5:
                player.IncreaseSpeed(0.05f);
                player.AddInventorySlots(1);
                break;
            case 10:
                player.IncreaseSpeed(0.05f);
                player.AddInventorySlots(1);
                break;
            case 15:
                player.IncreaseSpeed(0.05f);
                player.AddInventorySlots(1);
                break;
        }
    }
    // Posiible values: Dash duration , Dash speed, Health, maxStackSize, etc.
}
