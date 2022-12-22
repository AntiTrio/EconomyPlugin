using Microsoft.Data.Sqlite;
using Microsoft.Xna.Framework;
using System.Data;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace EconomyPlugin
{
    [ApiVersion(2, 1)]
    public class Economy : TerrariaPlugin
    {
        #region PluginInfo
        public override string Author => "Trio";

        public override string Description => "";

        public override string Name => "Economy Plugin";

        public override Version Version => new Version(1, 0, 0, 0);

        public Economy(Main game) : base(game) { }
        #endregion

        #region Properties
        public static List<EconomyPlayer> economyPlayers = new List<EconomyPlayer>();
        private IDbConnection database;
        public static Database dbManager;
        public static Config config = new Config();
        #endregion

        #region Initialize

        public override void Initialize()
        {
            database = new SqliteConnection(("Data Source=" + Path.Combine(TShock.SavePath, "Economy.sqlite")));

            dbManager = new Database(database);
            PlayerHooks.PlayerPostLogin += PlayerJoin;
            GetDataHandlers.KillMe += PlayerDead;
            ServerApi.Hooks.ServerLeave.Register(this, PlayerLeave);
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.NetSendData.Register(this, MobKilled);
            GeneralHooks.ReloadEvent += Reloaded;
        }

        private void OnInitialize(EventArgs args)
        {
            config = Config.Read();
            Commands.ChatCommands.Add(new Command("economy.user", Balance, "balance", "bal", "eco"));
            Commands.ChatCommands.Add(new Command("economy.user", PayUser, "pay"));
            Commands.ChatCommands.Add(new Command("economy.admin", GiveBalance, "givebalance", "givebal"));
            Commands.ChatCommands.Add(new Command("economy.admin", TakeBalance, "takebalance", "takebal"));
            Commands.ChatCommands.Add(new Command("economy.admin", ResetBalance, "resetbalance", "resetbal"));
            Commands.ChatCommands.Add(new Command("economy.admin", SetBalance, "setbalance", "setbal"));

        }

        #endregion

        public void PlayerDead(object sender, GetDataHandlers.KillMeEventArgs args)
        {
            if (args.Player.IsLoggedIn && config.DropOnDeath > 0)
            {

                EconomyPlayer p = PlayerManager.GetPlayer(args.Player.Name);
                var toLose = (int)(p.balance * config.DropOnDeath);
                p.balance -= toLose;
                if (config.announceMobDrops)
                {
                    args.Player.SendMessage($"Вы потеряли {toLose} {config.currencyName} за смерть!", Color.Orange);
                    return;
                }
            }
            return;
        }

        public void MobKilled(SendDataEventArgs args)
        {
            if (args.MsgId != PacketTypes.NpcStrike) return;

            if (config.enableMobDrops == false) return;
            var npc = Main.npc[args.number];

            if (args.ignoreClient == -1) return;

            var player = TSPlayer.FindByNameOrID(args.ignoreClient.ToString())[0];

            if (!(npc.life <= 0)) return;

            if (npc.type != NPCID.TargetDummy && !npc.SpawnedFromStatue)
            {
                int totalGiven = npc.lifeMax;

                if (config.excludedMobs.Count > 0)
                {
                    foreach (var mob in config.excludedMobs)
                    {
                        if (npc.netID == mob) return;
                    }
                }

                PlayerManager.GetPlayer(player.Name).balance += totalGiven;

                if (config.announceMobDrops == false) return;

                player.SendMessage($"+ " + totalGiven + " " + config.currencyName + " вы убили " + npc.FullName, Color.LightGoldenrodYellow);
            }
        }

        #region Commands

        private void Balance(CommandArgs args)
        {
            EconomyPlayer player = PlayerManager.GetPlayer(args.Player.Name);
            int balance = dbManager.getUserBalance(player.accountName);
            args.Player.SendMessage($"Ваш баланс: {balance} {config.currencyName}", Color.LightGoldenrodYellow);
            return;
        }

        private void GiveBalance(CommandArgs args)
        {
            switch (args.Parameters.Count)
            {
                case 0:
                    args.Player.SendErrorMessage("Введите ник игрока и сумму! /givebal <игрок> <сумма>");
                    break;

                case 1:
                    args.Player.SendErrorMessage($"Введите сумму! /givebal {args.Player} <сумма>");
                    break;
            }

            TSPlayer player = args.Player;
            if (player == null)
            {
                args.Player.SendErrorMessage("Несуществующий игрок!");
                return;
            }
            int amount = int.Parse(args.Parameters[1]);
            PlayerManager.GetPlayer(player.Name).balance += amount;
            dbManager.SaveAllPlayers();
            player.SendSuccessMessage($"Администратор {args.Player.Name} выдал вам {amount} {config.currencyName}!\n " +
                $"Ваш баланс: {PlayerManager.GetPlayer(player.Name).balance} {config.currencyName}");
            return;
        }
        private void SetBalance(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Введите ник игрока и сумму! /setbal <игрок> <сумма>");
                return;
            }
            if (args.Parameters.Count == 1)
            {
                args.Player.SendErrorMessage($"Введите сумму! /setbal {args.Player} <сумма>");
                return;
            }

            TSPlayer player = args.Player;
            if (player == null)
            {
                args.Player.SendErrorMessage("Несуществующий игрок!");
                return;
            }
            int amount = int.Parse(args.Parameters[1]);
            PlayerManager.GetPlayer(player.Name).balance = amount;
            dbManager.SaveAllPlayers();

            player.SendSuccessMessage($"Администратор {args.Player.Name} изменил ваш баланс!\n " +
                $"Ваш баланс: {PlayerManager.GetPlayer(player.Name).balance} {config.currencyName}");
            return;

        }
        private void ResetBalance(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Введите ник игрока! /resetbal <игрок>");
                return;
            }

            TSPlayer player = args.Player;
            if (player == null)
            {
                args.Player.SendErrorMessage("Несуществующий игрок!");
                return;
            }
            int amount = int.Parse(args.Parameters[1]);
            PlayerManager.GetPlayer(player.Name).balance = 0;
            dbManager.SaveAllPlayers();
            player.SendErrorMessage($"Администратор {args.Player.Name} сбросил ваш баланс!\n" +
                $"Ваш баланс: {PlayerManager.GetPlayer(player.Name).balance} {config.currencyName}");
            return;
        }

        private void TakeBalance(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Введите ник игрока и сумму! /takebal <игрок> <сумма>");
                return;
            }
            if (args.Parameters.Count == 1)
            {
                args.Player.SendErrorMessage($"Введите сумму! /takebal {args.Player} <сумма>");
                return;
            }

            TSPlayer player = args.Player;
            if (player == null)
            {
                args.Player.SendErrorMessage("Несуществующий игрок!");
                return;
            }
            int amount = int.Parse(args.Parameters[1]);
            PlayerManager.GetPlayer(player.Name).balance -= amount;
            dbManager.SaveAllPlayers();
            player.SendErrorMessage($"Администратор {args.Player.Name} забрал с вашего баланса: {amount} {config.currencyName}! " +
                $"Ваш баланс: {PlayerManager.GetPlayer(player.Name).balance} {config.currencyName}");
            return;
        }
        private void PayUser(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Введите ник игрока и сумму! /pay <игрок> <сумма>");
                return;
            }
            if (args.Parameters.Count == 1)
            {
                args.Player.SendErrorMessage($"Введите сумму! /pay {args.Player} <сумма>");
                return;
            }
            TSPlayer player = args.Player;
            if (player == null)
            {
                args.Player.SendErrorMessage("Несуществующий игрок!");
                return;
            }
            int amount = int.Parse(args.Parameters[1]);
            if (amount < 1)
            {
                args.Player.SendErrorMessage("Отправьте действительную сумму!");
                return;
            }

            if (amount <= PlayerManager.GetPlayer(args.Player.Name).balance)
            {
                PlayerManager.GetPlayer(args.Player.Name).balance -= amount;
                PlayerManager.GetPlayer(player.Name).balance += amount;
                dbManager.SaveAllPlayers();
                args.Player.SendSuccessMessage($"Вы успешно отправили {amount} {config.currencyName} игроку {player.Name}!\n" +
                    $"Ваш баланс: {PlayerManager.GetPlayer(args.Player.Name).balance} {config.currencyName}");

                player.SendSuccessMessage($"Вам перевели {amount} {config.currencyName} от {args.Player.Name}!\n" +
                    $"Ваш баланс: {PlayerManager.GetPlayer(player.Name).balance} {config.currencyName}");
                return;
            }
            args.Player.SendErrorMessage("У вас недостаточно денег!");
            return;
        }

        private void Reloaded(ReloadEventArgs e)
        {
            dbManager.SaveAllPlayers();
            config = Config.Read();
        }


        private void PlayerJoin(PlayerPostLoginEventArgs args)
        {
            if (args.Player == null)
            {
                return;
            }
            if (args.Player.IsLoggedIn == false)
            {
                return;
            }

            TSPlayer player = args.Player;

            if (dbManager.userExists(player.Account.Name) == false)
            {
                EconomyPlayer p = new EconomyPlayer(player.Name, player);
                economyPlayers.Add(p);
                dbManager.InsertPlayer(p);
                return;
            }

            var bal = dbManager.getUserBalance(player.Account.Name);
            EconomyPlayer o = new EconomyPlayer(player.Name, player, bal);
            economyPlayers.Add(o);

            return;
        }


        public void UpdatePlayer(EconomyPlayer p)
        {
            p.balance++;
            dbManager.SavePlayer(p);
        }

        private void PlayerLeave(LeaveEventArgs args)
        {
            if (TShock.Players[args.Who] == null) return;

            TSPlayer player = TShock.Players[args.Who];

            if (PlayerManager.GetPlayer(player.Name) == null) return;

            dbManager.SavePlayer(PlayerManager.GetPlayer(player.Name));
            economyPlayers.Remove(new EconomyPlayer(player.Name, player));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dbManager.SaveAllPlayers();
                PlayerHooks.PlayerPostLogin -= PlayerJoin;
                ServerApi.Hooks.ServerLeave.Deregister(this, PlayerLeave);
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.NetSendData.Deregister(this, MobKilled);
                GeneralHooks.ReloadEvent -= Reloaded;
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
