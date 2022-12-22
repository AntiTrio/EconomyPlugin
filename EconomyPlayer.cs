using TShockAPI;

namespace EconomyPlugin
{
    public class EconomyPlayer
    {
        public string name { get; set; }

        public string accountName { get; set; }

        public TSPlayer player { get; set; }

        public int balance { get; set; }

        public int RetrieveBalance()
        {
            return this.balance;
        }

        public EconomyPlayer(string playerName, TSPlayer player)
        {
            this.name = playerName;
            this.player = player;
            this.accountName = player.Account.Name;
        }

        public EconomyPlayer(string playerName, TSPlayer player, int bal)
        {
            this.name = playerName;
            this.player = player;
            this.balance = bal;
            this.accountName = player.Account.Name;
        }

    }

    public static class PlayerManager
    {
        public static EconomyPlayer GetPlayer(int playerId)
        {
            if (playerId == null)
            {
                return null;
            }

            if (TShock.Players[playerId] == null)
            {
                return null;
            }

            var name = TShock.Players[playerId].Name;


            return Economy.economyPlayers.Find(p => p.name == name);
        }
        public static EconomyPlayer GetPlayer(string name)
        {
            return Economy.economyPlayers.Find(p => p.name == name);
        }

        public static EconomyPlayer GetPlayerFromAccount(string name)
        {
            return Economy.economyPlayers.Find(p => p.accountName == name);
        }


        public static void UpdatePlayerBalance(string name, int am)
        {
            var p = Economy.economyPlayers.Find(p => p.name == name);
            p.balance = am;
            Economy.dbManager.SavePlayer(p);
            return;
        }


        public static void UpdatePlayerBalance(EconomyPlayer player, int am)
        {
            var p = Economy.economyPlayers.Find(p => p.name == player.name);
            p.balance = am;
            Economy.dbManager.SavePlayer(p);
            return;
        }

        public static void SubtractPlayerBalance(EconomyPlayer player, int toRemove)
        {
            var p = Economy.economyPlayers.Find(p => p.name == player.name);
            p.balance -= toRemove;
            if (p.balance <= 0)
            {
                p.balance = 0;
            }
            Economy.dbManager.SavePlayer(p);
            return;
        }

        public static void AddPlayerBalance(EconomyPlayer player, int toAdd)
        {
            var p = Economy.economyPlayers.Find(p => p.name == player.name);
            p.balance += toAdd;
            if (p.balance <= 0)
            {
                p.balance = 0;
            }
            Economy.dbManager.SavePlayer(p);
            return;
        }

        public static void ResetPlayerBalance(EconomyPlayer player)
        {
            var p = Economy.economyPlayers.Find(p => p.name == player.name);
            p.balance = 0;
            Economy.dbManager.SavePlayer(p);
            return;
        }
    }
}
