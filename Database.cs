using MySql.Data.MySqlClient;
using System.Data;
using TShockAPI.DB;

namespace EconomyPlugin
{
    public class Database
    {
        private readonly IDbConnection database;

        public Database(IDbConnection database)
        {
            this.database = database;

            var sqlCreator = new SqlTableCreator(database, new SqliteQueryCreator());
            var table = new SqlTable("Economy",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("Name", MySqlDbType.VarChar, 50) { Unique = true },
                new SqlColumn("Balance", MySqlDbType.Int32)
                );
            sqlCreator.EnsureTableStructure(table);
        }

        public bool InsertPlayer(EconomyPlayer player)
        {
            return database.Query("INSERT INTO Economy (Name, Balance)" + "VALUES (@0, @1)", player.accountName, 0) != 0;
        }

        public bool DeletePlayer(string accountName)
        {
            return database.Query("DELETE FROM Economy WHERE Name = @0", accountName) != 0;
        }

        public bool SavePlayer(EconomyPlayer p)
        {
            EconomyPlayer player = PlayerManager.GetPlayer(p.name);

            return database.Query("UPDATE Economy SET Balance = @0 WHERE Name = @1",
                player.balance, player.accountName) != 0;
        }

        public void SaveAllPlayers()
        {
            foreach (var player in Economy.economyPlayers)
            {
                SavePlayer(PlayerManager.GetPlayer(player.name));
            }
        }

        public bool userExists(string name)
        {
            using (var reader = database.QueryReader("SELECT * FROM Economy WHERE Name = @0", name))
            {
                while (reader.Read())
                {
                    var user = reader.Get<string>("Name");
                    var bal = reader.Get<int>("Balance");

                    return true;
                }
                Console.WriteLine("Пользователь не существует! Создание баланса для: " + name);
                return false;
            }
        }

        public int getUserBalance(string player)
        {
            SaveAllPlayers();
            List<Tuple<string, int>> p = new List<Tuple<string, int>>();

            using (var reader = database.QueryReader("SELECT * FROM Economy WHERE Name = @0", player))
            {
                while (reader.Read())
                {
                    var name = reader.Get<string>("Name");
                    var bal = reader.Get<int>("Balance");

                    return bal;

                }
                return 0;
            }
        }
    }
}
