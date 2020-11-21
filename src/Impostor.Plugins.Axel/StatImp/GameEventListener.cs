using Impostor.Api.Events;
using Impostor.Api.Events.Player;
using Microsoft.Extensions.Logging;
using System.Data.SQLite;

namespace Impostor.Plugins.Example.Handlers
{
    /// <summary>
    ///     A class that listens for two events.
    ///     It may be more but this is just an example.
    ///
    ///     Make sure your class implements <see cref="IEventListener"/>.
    /// </summary>
    public class GameEventListener : IEventListener
    {
        private readonly ILogger<ExamplePlugin> _logger;
        private readonly string db = @"URI=file:.\Data\StatImp.db";
        private readonly bool debug= false;

        public GameEventListener(ILogger<ExamplePlugin> logger)
        {
            _logger = logger;
        }

        /// <summary>
        ///     An example event listener.
        /// </summary>
        /// <param name="e">
        ///     The event you want to listen for.
        /// </param>
        [EventListener]
        public void OnGameStarted(IGameStartedEvent e)
        {
            _logger.LogInformation($"Game is starting.");
        }

        [EventListener]
        public void OnGameEnded(IGameEndedEvent e)
        {
            _logger.LogInformation($"Game has ended.");

            using var con = new SQLiteConnection(db);
            con.Open();

            string stm = "SELECT * FROM Players";
            using var cmd = new SQLiteCommand(stm, con);
            using SQLiteDataReader rdr = cmd.ExecuteReader();
            
            bool winners = CheckWinners(e);                         // true = Crewmates, false = Impostor
            foreach (var player in e.Game.Players)
            {
                var info = player.Character.PlayerInfo;
                string name = info.PlayerName;

                if (!PlayerExists(name, rdr))
                {
                    AddPlayerToDB(name);
                }

                if (info.IsImpostor) AddGameImpostor(name);
                else AddGameCrewmate(name);

                AddGame(name);
                if (info.IsImpostor && !winners)
                {
                    if (info.IsDead) AddScore(name, 4);
                    else AddScore(name, 5);
                    AddGameWin(name);
                    AddGameWinImpostor(name);
                }
                else if (!info.IsImpostor && winners)
                {
                    if (info.IsDead) AddScore(name, 2);
                    else AddScore(name, 1);
                    AddGameWin(name);
                    AddGameWinCrewmate(name);
                }
            }
        }

        [EventListener]
        public void OnPlayerChat(IPlayerChatEvent e)
        {
            if (e.Message.Equals("/score") || e.Message.Equals("/stats") || e.Message.Equals("/stats Impostor") || e.Message.Equals("/stats Crewmate") || e.Message.Equals("/leaderboard") || e.Message.Equals("/kills")) {
                string name = e.PlayerControl.PlayerInfo.PlayerName;
                using var con = new SQLiteConnection(db);
                if (debug) _logger.LogInformation($"Ouverture de {db}");
                try
                {
                    con.Open();
                }
                catch(System.IO.FileNotFoundException exception)
                {
                    _logger.LogInformation($"Could not open {exception.FileName}");
                }

                string stm = "SELECT * FROM Players";
                using var cmd = new SQLiteCommand(stm, con);
                SQLiteDataReader rdr = cmd.ExecuteReader();
                if (!PlayerExists(name, rdr)) {
                    _logger.LogInformation($"Adding {name} to Data Base");
                    AddPlayerToDB(name);
                }
                rdr.Close();

                rdr = cmd.ExecuteReader();
                if (e.Message.Equals("/score"))
                {
                    int score = GetScore(name, rdr);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} score is {score}");
                    SendMessage(e, "My score is :" + score.ToString());
                }
                else if (e.Message.Equals("/stats"))
                {
                    float stats = GetStats(name, rdr);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} win ratio is {stats}");
                    SendMessage(e, "My win ratio is :" + stats.ToString());
                }
                else if (e.Message.Equals("/stats Impostor"))
                {
                    float statsImpostor = GetStatsImpostor(name, rdr);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} win ratio as Impostor is {statsImpostor}");
                    SendMessage(e, "My win ratio as Impostor is :" + statsImpostor.ToString());
                }
                else if (e.Message.Equals("/stats Crewmate"))
                {
                    float statsCrewmate = GetStatsCrewmate(name, rdr);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} win ratio as Crewmate is {statsCrewmate}");
                    SendMessage(e, "My win ratio as Crewmate is :" + statsCrewmate.ToString());
                }
                else if (e.Message.Equals("/leaderboard"))
                {
                    PrintLeaderboard(e, rdr);
                }
                else if (e.Message.Equals("/kills"))
                {
                    int nbKills = GetNbKills(name, rdr);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} kill count is {nbKills}");
                    SendMessage(e, "My kill count is :" + nbKills.ToString());
                }
            }
        }

        [EventListener]
        public void OnPlayerDeath(IPlayerMurderEvent e)
        {
            AddKill(e);
        }

        private bool PlayerExists(string name, SQLiteDataReader rdr)
        {
            while (rdr.Read())
            {
                if(debug) _logger.LogInformation($"{rdr.GetString(0)}");
                if(rdr.GetString(0).Equals(name))
                {
                    if (debug) _logger.LogInformation($"true");
                    return true;
                }
            }
            return false;
        }

        private void AddPlayerToDB(string name)
        {
            using var con = new SQLiteConnection(db);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "insert into Players (PlayerName, Score, NbGames, NbGamesImpostor, NbGamesCrewmate, NbGamesWon, NbGamesWonImpostor, NbGamesWonCrewmate) values (@name, @valInit, @valInit, @valInit, @valInit, @valInit, @valInit, @valInit); ";

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@valInit", 0);
            cmd.Prepare();
            if (debug) _logger.LogInformation($"{cmd.ToString()}");

            cmd.ExecuteNonQuery();
        }

        private int GetScore(string name, SQLiteDataReader rdr)
        {
            while (rdr.Read())
            {
                string rdrName = rdr.GetString(0);
                int rdrScore = rdr.GetInt32(1);
                if (debug) _logger.LogInformation($"in GetScore: {rdrName}");
                if (rdrName.Equals(name))
                {
                    if (debug) _logger.LogInformation($"{rdrName} has score {rdrScore}");
                    return rdrScore;
                }
            }
            return -1;
        }

        private float GetStats(string name, SQLiteDataReader rdr)
        {
            while (rdr.Read())
            {
                if (rdr.GetString(0).Equals(name))
                {
                    if (debug) _logger.LogInformation($"{rdr.GetString(0)} has stats {rdr.GetInt32(5) / rdr.GetInt32(2)}");
                    if (rdr.GetInt32(2) != 0) return rdr.GetInt32(5) / rdr.GetInt32(2);
                    else return 0;
                }
            }
            return -1;
        }

        private float GetStatsImpostor(string name, SQLiteDataReader rdr)
        {
            while (rdr.Read())
            {
                if (rdr.GetString(0).Equals(name))
                {
                    if (debug) _logger.LogInformation($"{rdr.GetString(0)} has stats Impostor {rdr.GetInt32(6) / rdr.GetInt32(3)}");
                    if(rdr.GetInt32(3) != 0) return rdr.GetInt32(6) / rdr.GetInt32(3);
                    else return 0;
                }
            }
            return -1;
        }

        private float GetStatsCrewmate(string name, SQLiteDataReader rdr)
        {
            while (rdr.Read())
            {
                if (rdr.GetString(0).Equals(name))
                {
                    if (debug) _logger.LogInformation($"{rdr.GetString(0)} has stats Impostor {rdr.GetInt32(7) / rdr.GetInt32(4)}");
                    if (rdr.GetInt32(4) != 0) return rdr.GetInt32(7) / rdr.GetInt32(4);
                    else return 0;
                }
            }
            return -1;
        }

        private void PrintLeaderboard(IPlayerChatEvent e, SQLiteDataReader rdr)
        {
            string Player1 = "MISSING";
            string Player2 = "MISSING";
            string Player3 = "MISSING";
            int score1 = -1;
            int score2 = -1;
            int score3 = -1;

            while (rdr.Read())
            {
                if (debug) _logger.LogInformation($"{rdr.GetString(0)}");
                if (rdr.GetInt32(1) > score1)
                {
                    if (debug) _logger.LogInformation($"{rdr.GetString(0)} has score {rdr.GetInt32(1)}, 1st in leaderboard");
                    Player3 = Player2;
                    score3 = score2;
                    Player2 = Player1;
                    score2 = score1;
                    Player1 = rdr.GetString(0);
                    score1 = rdr.GetInt32(1);
                }
                else if (rdr.GetInt32(1) > score2)
                {
                    if (debug) _logger.LogInformation($"{rdr.GetString(0)} has score {rdr.GetInt32(1)}, 2nd in leaderboard");
                    Player3 = Player2;
                    score3 = score2;
                    Player2 = rdr.GetString(0);
                    score2 = rdr.GetInt32(1);
                }
                else if (rdr.GetInt32(1) > score3)
                {
                    if (debug) _logger.LogInformation($"{rdr.GetString(0)} has score {rdr.GetInt32(1)}, 3rd in leaderboard");
                    Player3 = rdr.GetString(0);
                    score3 = rdr.GetInt32(1);
                }
            }
            _logger.LogInformation($"{Player1} is 1st with a score of {score1}");
            _logger.LogInformation($"{Player2} is 2nd with a score of {score2}");
            _logger.LogInformation($"{Player3} is 3rd with a score of {score3}");

            SendMessage(e, "Leaderboard : 1st is " + Player1 + " (" + score1 + ") | 2nd is " + Player2 + " (" + score2 + ") | 3rd is " + Player3 + " (" + score3 + ")");

            return;
        }

        private bool CheckWinners(IGameEndedEvent e)
        {
            int cntImp = 0;
            int cntCre = 0;
            foreach (var player in e.Game.Players)
            {
                var info = player.Character.PlayerInfo;
                if (info.IsImpostor && !info.IsDead)
                {
                    if (debug) _logger.LogInformation($"{info.PlayerName} is Impostor");
                    cntImp++;
                }
                else if (!info.IsImpostor && !info.IsDead)
                {
                    if (debug) _logger.LogInformation($"{info.PlayerName} is Crewmate");
                    cntCre++;
                }
            }
            if (debug) _logger.LogInformation($"Victory is {(cntCre > cntImp)}");
            return (cntCre > cntImp);
        }

        private void AddGame(string name)
        {
            using var con = new SQLiteConnection(db);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "update Players set NbGames = NbGames + 1 where PlayerName = @name;";

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Prepare();
            if (debug) _logger.LogInformation($"{cmd.ToString()}");

            cmd.ExecuteNonQuery();
            return;
        }

        private void AddScore(string name, int score)
        {
            using var con = new SQLiteConnection(db);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "update Players set Score = Score + @score where PlayerName = @name;";

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@score", score);
            cmd.Prepare();
            if (debug) _logger.LogInformation($"{cmd.ToString()}");

            cmd.ExecuteNonQuery();
            return;
        }

        private void AddGameWin(string name)
        {
            using var con = new SQLiteConnection(db);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "update Players set NbGamesWon = NbGamesWon + 1 where PlayerName = @name;";

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Prepare();
            if (debug) _logger.LogInformation($"{cmd.ToString()}");

            cmd.ExecuteNonQuery();
            return;
        }

        private void AddGameWinImpostor(string name)
        {
            using var con = new SQLiteConnection(db);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "update Players set NbGamesWonImpostor = NbGamesWonImpostor + 1 where PlayerName = @name;";

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Prepare();
            if (debug) _logger.LogInformation($"{cmd.ToString()}");

            cmd.ExecuteNonQuery();
            return;
        }

        private void AddGameWinCrewmate(string name)
        {
            using var con = new SQLiteConnection(db);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "update Players set NbGamesWonCrewmate = NbGamesWonCrewmate + 1 where PlayerName = @name;";

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Prepare();
            if (debug) _logger.LogInformation($"{cmd.ToString()}");

            cmd.ExecuteNonQuery();
            return;
        }
        
        private void AddGameCrewmate(string name)
        {
            using var con = new SQLiteConnection(db);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "update Players set NbGamesCrewmate = NbGamesCrewmate + 1 where PlayerName = @name;";

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Prepare();
            if (debug) _logger.LogInformation($"{cmd.ToString()}");

            cmd.ExecuteNonQuery();
            return;
        }

        private void AddGameImpostor(string name)
        {
            using var con = new SQLiteConnection(db);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "update Players set NbGamesImpostor = NbGamesImpostor + 1 where PlayerName = @name;";

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Prepare();
            if (debug) _logger.LogInformation($"{cmd.ToString()}");

            cmd.ExecuteNonQuery();
            return;
        }

        private void SendMessage(IPlayerChatEvent e, string message)
        {
            e.Game.Host.Client.Player.Character.SendChatAsync(message);
            return;
        }

        private void AddKill(IPlayerMurderEvent e)
        {
            using var con = new SQLiteConnection(db);
            con.Open();
            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "update Players set NbKills = NbKills + 1 where PlayerName = @name;";

            cmd.Parameters.AddWithValue("@name", e.PlayerControl.PlayerInfo.PlayerName);
            cmd.Prepare();
            if (debug) _logger.LogInformation($"{cmd.ToString()}");

            cmd.ExecuteNonQuery();
            return;
        }

        private int GetNbKills(string name, SQLiteDataReader rdr)
        {
            while (rdr.Read())
            {
                string rdrName = rdr.GetString(0);
                int rdrNbKills = rdr.GetInt32(8);
                if (debug) _logger.LogInformation($"in GetNbKills: {rdrName}");
                if (rdrName.Equals(name))
                {
                    if (debug) _logger.LogInformation($"{rdrName} kill count is {rdrNbKills}");
                    return rdrNbKills;
                }
            }
            return -1;
        }
    }
}