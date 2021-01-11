using Impostor.Api.Events;
using Impostor.Api.Events.Player;
using Microsoft.Extensions.Logging;
using System.Data.SQLite;
using System.Data;

namespace Impostor.Plugins.Example.Handlers
{
    /// <summary>
    ///     A class that listens for two events.
    ///     It may be more but this is just an example.
    ///
    ///     Make sure your class implements <see cref="IEventListener"/>.
    /// </summary>

    // [DeploymentItem(@"x64\SQLite.Interop.dll", "x64")]
    public class GameEventListener : IEventListener
    {
        private readonly ILogger<ExamplePlugin> _logger;
        private readonly string db = @"URI=file:StatImp.db";
        private readonly bool debug= true;
        private SQLiteConnection con = new SQLiteConnection(@"URI=file:StatImp.db");

        public GameEventListener(ILogger<ExamplePlugin> logger)
        {
            _logger = logger;
        }

        [EventListener]
        public void OnGameStarted(IGameStartedEvent e)
        {
            ResetGameScore(e);
            _logger.LogInformation($"Game is starting.\nThe game's score has been reseted.");
        }

        [EventListener]
        public void OnGameEnded(IGameEndedEvent e)
        {
            EndGame(e);
            _logger.LogInformation($"Game has ended.");
        }

        [EventListener]
        public void OnPlayerDeath(IPlayerMurderEvent e)
        {
            AddKill(e);
        }

        [EventListener]
        public void OnEject(IPlayerExileEvent e)
        {
            if (e.PlayerControl.PlayerInfo.IsImpostor)
            {
                foreach(var player in e.Game.Players)
                {
                    if(!player.Character.PlayerInfo.IsImpostor && !player.Character.PlayerInfo.IsDead) AddScore(player.Character.PlayerInfo.PlayerName, 1);
                }
            }
        }

        [EventListener]
        public void OnPlayerChat(IPlayerChatEvent e)
        {
            string name = e.PlayerControl.PlayerInfo.PlayerName;
            if (debug) _logger.LogInformation($"Ouverture de {db}");
            try
            {
                con.Open();
            }
            catch (System.IO.FileNotFoundException exception)
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
            switch (e.Message) {
                case "/score":
                    int score = GetScore(name, rdr);
                    if (debug) _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} score is {score}");
                    SendMessage(e, "My score is :" + score.ToString());
                    break;
                case "/StatImp":
                    SendMessage(e, "Commands are : /score (Game), /stats (Impostor, Crewmate), /leaderboard, /kills");
                    break;
                case "/stats":
                    float stats = GetStats(name, rdr);
                    if (debug) _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} win ratio is {stats}");
                    SendMessage(e, "My win ratio is : " + stats.ToString());
                    break;
                case "/stats Impostor":
                    float statsImpostor = GetStatsImpostor(name, rdr);
                    if (debug) _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} win ratio as Impostor is {statsImpostor}");
                    SendMessage(e, "My win ratio as Impostor is : " + statsImpostor.ToString());
                    break;
                case "/stats Crewmate":
                    float statsCrewmate = GetStatsCrewmate(name, rdr);
                    if (debug) _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} win ratio as Crewmate is {statsCrewmate}");
                    SendMessage(e, "My win ratio as Crewmate is : " + statsCrewmate.ToString());
                    break;
                case "/leaderboard":
                    PrintLeaderboard(e, rdr);
                    break;
                case "/kills":
                    int nbKills = GetNbKills(name, rdr);
                    if (debug) _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} kill count is {nbKills}");
                    SendMessage(e, "My kill count is : " + nbKills.ToString());
                    break;
                case "/score Game":
                    int scoreGame = GetScoreGame(name, rdr);
                    if (debug) _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} kill count is {scoreGame}");
                    SendMessage(e, "My score last game was : " + scoreGame.ToString());
                    break;
                case "/debug AddScore":
                    if (debug)
                    {
                        AddScore(name, 1);
                        _logger.LogInformation($"Added 1 to {e.PlayerControl.PlayerInfo.PlayerName}'s score");
                        SendMessage(e, "Added 1 to your score");
                    }
                    break;
                case "/debug AddGame":
                    if (debug)
                    {
                        AddGame(name);
                        _logger.LogInformation($"Added 1 to {e.PlayerControl.PlayerInfo.PlayerName}'s game count");
                        SendMessage(e, "Added 1 to your game count");
                    }
                    break;
                case "/debug EndGame":
                    if (debug)
                    {
                        EndGameDebug(e);
                        _logger.LogInformation($"Debug : Ending Game");
                        SendMessage(e, "Ending Game");
                    }
                    break;
                default:
                    break;
            }

            rdr.Close();
            if (con.State == ConnectionState.Open)
            {
                con.Close();
            }
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
            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "insert into Players (PlayerName, Score, NbGames, NbGamesImpostor, NbGamesCrewmate, NbGamesWon, NbGamesWonImpostor, NbGamesWonCrewmate, NbKills, ScoreGame) values (@name, @valInit, @valInit, @valInit, @valInit, @valInit, @valInit, @valInit, @valInit, @valInit); ";

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
                    if (debug) _logger.LogInformation($"{rdr.GetString(0)} has stats {rdr.GetInt32(5)}/{rdr.GetInt32(2)}");
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
                    if (debug) _logger.LogInformation($"{rdr.GetString(0)} has stats Impostor {rdr.GetInt32(6)}/{rdr.GetInt32(3)}");
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
                    if (debug) _logger.LogInformation($"{rdr.GetString(0)} has stats Impostor {rdr.GetInt32(7)}/{rdr.GetInt32(4)}");
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
            SQLiteCommand cmd = new SQLiteCommand(con);

            cmd.CommandText = "UPDATE Players SET NbGames = NbGames + 1 WHERE PlayerName = @name; ";
            cmd.Parameters.AddWithValue("name", name);

            if (debug) _logger.LogInformation(cmd.CommandText);
            int nbLinesAffected = cmd.ExecuteNonQuery();
            if (debug) _logger.LogInformation($"AddGame affected {nbLinesAffected} lines");

            return;
        }

        private void AddScore(string name, int score)                   // Add to total score and game score
        {
            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "update Players set Score = Score + @score where PlayerName = @name;";    // Total Score

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@score", score);
            cmd.Prepare();
            if (debug) _logger.LogInformation($"{cmd.ToString()}");

            cmd.ExecuteNonQuery();

            //------------------------------------------------------------------//

            cmd.CommandText = "update Players set ScoreGame = ScoreGame + @score where PlayerName = @name;";    // Game Score

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@score", score);
            cmd.Prepare();
            if (debug) _logger.LogInformation($"{cmd.ToString()}");

            cmd.ExecuteNonQuery();
            return;
        }

        private void AddGameWin(string name)
        {
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
            string name = e.PlayerControl.PlayerInfo.PlayerName;
            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "update Players set NbKills = NbKills + 1 where PlayerName = @name;";

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Prepare();
            if (debug) _logger.LogInformation($"{cmd.ToString()}");

            cmd.ExecuteNonQuery();
            AddScore(name, 1);
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

        private int GetScoreGame(string name, SQLiteDataReader rdr)
        {
            while (rdr.Read())
            {
                string rdrName = rdr.GetString(0);
                int rdrScoreGame = rdr.GetInt32(9);
                if (debug) _logger.LogInformation($"in GetScore: {rdrName}");
                if (rdrName.Equals(name))
                {
                    if (debug) _logger.LogInformation($"{rdrName} has score {rdrScoreGame}");
                    return rdrScoreGame;
                }
            }
            return -1;
        }

        private void ResetGameScore( IGameStartedEvent e)
        {
            foreach (var player in e.Game.Players)
            {
                using var cmd = new SQLiteCommand(con);
                cmd.CommandText = "update Players set ScoreGame = 0 where PlayerName = @name;";

                cmd.Parameters.AddWithValue("@name", player.Character.PlayerInfo.PlayerName);
                cmd.Prepare();
                if (debug) _logger.LogInformation($"{cmd.ToString()}");


                cmd.ExecuteNonQuery();
            }
            return;
        }

        private void EndGame(IGameEndedEvent e)
        {
            if (debug) _logger.LogInformation($"Starting Endgame");

            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "SELECT * FROM Players";
            using SQLiteDataReader rdr = cmd.ExecuteReader();

            foreach (var player in e.Game.Players)
            {
                var info = player.Character.PlayerInfo;
                string name = info.PlayerName;

                if (debug) _logger.LogInformation($"Checking player {name} (if exists in db)");

                if (!PlayerExists(name, rdr))
                {
                    if (debug) _logger.LogInformation($"Adding player {name} to db");
                    AddPlayerToDB(name);
                }
            }

            if (debug) _logger.LogInformation($"Closing rdr");
            rdr.Close();

            bool winners = CheckWinners(e);                         // true = Crewmates, false = Impostor
            int cmpImp = 0;
            string[] nameImpostors = new string[e.Game.Options.NumImpostors];

            foreach (var player in e.Game.Players)
            {
                var info = player.Character.PlayerInfo;
                string name = info.PlayerName;

                if (info.IsImpostor)
                {
                    if (debug) _logger.LogInformation($"Adding impostor game to {name}");
                    AddGameImpostor(name);
                }
                else
                {
                    if (debug) _logger.LogInformation($"Adding crewmate game to {name}");
                    AddGameCrewmate(name);
                }

                if (debug) _logger.LogInformation($"Adding game to {name}");
                AddGame(name);
                
                if (info.IsImpostor && !winners)
                {
                    if (info.IsDead)
                    {
                        if (debug) _logger.LogInformation($"Adding score 2 to {name}");
                        AddScore(name, 2);
                    }
                    else
                    {
                        if (debug) _logger.LogInformation($"Adding score 3 to {name}");
                        AddScore(name, 3);
                        nameImpostors[cmpImp] = name;
                        cmpImp++;
                        if (cmpImp == e.Game.Options.NumImpostors)
                        {
                            for (int i = 0; i < cmpImp; i++)
                            {
                                if (debug) _logger.LogInformation($"Adding score 1 to {name}");
                                AddScore(nameImpostors[i], 1);
                            }
                        }
                    }
                    if (debug) _logger.LogInformation($"Adding win to {name}");
                    AddGameWin(name);
                    if (debug) _logger.LogInformation($"Adding impostor win to {name}");
                    AddGameWinImpostor(name);
                }
                else if (!info.IsImpostor && winners)
                {
                    if (info.IsDead)
                    {
                        if (debug) _logger.LogInformation($"Adding score 1 to {name}");
                        AddScore(name, 1);
                    }
                    else
                    {
                        if (debug) _logger.LogInformation($"Adding score to {name}");
                        AddScore(name, 2);
                    }
                    if (debug) _logger.LogInformation($"Adding win to {name}");
                    AddGameWin(name);
                    if (debug) _logger.LogInformation($"Adding crewmate win to {name}");
                    AddGameWinCrewmate(name);
                }
            }
        }

        private void EndGameDebug(IPlayerChatEvent e)
        {
            if (debug) _logger.LogInformation($"Starting Endgame");
            
            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "SELECT * FROM Players";
            using SQLiteDataReader rdr = cmd.ExecuteReader();

            foreach (var player in e.Game.Players)
            {
                var info = player.Character.PlayerInfo;
                string name = info.PlayerName;

                if (debug) _logger.LogInformation($"Checking player {name} (if exists in db)");

                if (!PlayerExists(name, rdr))
                {
                    if (debug) _logger.LogInformation($"Adding player {name} to db");
                    AddPlayerToDB(name);
                }
            }

            if (debug) _logger.LogInformation($"Closing rdr");
            rdr.Close();

            bool winners = CheckWinnersDebug(e);                         // true = Crewmates, false = Impostor
            int cmpImp = 0;
            string[] nameImpostors = new string[e.Game.Options.NumImpostors];

            foreach (var player in e.Game.Players)
            {
                var info = player.Character.PlayerInfo;
                string name = info.PlayerName;

                if (info.IsImpostor)
                {
                    if (debug) _logger.LogInformation($"Adding impostor game to {name}");
                    AddGameImpostor(name);
                }
                else
                {
                    if (debug) _logger.LogInformation($"Adding crewmate game to {name}");
                    AddGameCrewmate(name);
                }

                if (debug) _logger.LogInformation($"Adding game to {name}");
                AddGame(name);

                if (info.IsImpostor && !winners)
                {
                    if (info.IsDead)
                    {
                        if (debug) _logger.LogInformation($"Adding score 2 to {name}");
                        AddScore(name, 2);
                    }
                    else
                    {
                        if (debug) _logger.LogInformation($"Adding score 3 to {name}");
                        AddScore(name, 3);
                        nameImpostors[cmpImp] = name;
                        cmpImp++;
                        if (cmpImp == e.Game.Options.NumImpostors)
                        {
                            for (int i = 0; i < cmpImp; i++)
                            {
                                if (debug) _logger.LogInformation($"Adding score 1 to {name}");
                                AddScore(nameImpostors[i], 1);
                            }
                        }
                    }
                    if (debug) _logger.LogInformation($"Adding win to {name}");
                    AddGameWin(name);
                    if (debug) _logger.LogInformation($"Adding impostor win to {name}");
                    AddGameWinImpostor(name);
                }
                else if (!info.IsImpostor && winners)
                {
                    if (info.IsDead)
                    {
                        if (debug) _logger.LogInformation($"Adding score 1 to {name}");
                        AddScore(name, 1);
                    }
                    else
                    {
                        if (debug) _logger.LogInformation($"Adding score to {name}");
                        AddScore(name, 2);
                    }
                    if (debug) _logger.LogInformation($"Adding win to {name}");
                    AddGameWin(name);
                    if (debug) _logger.LogInformation($"Adding crewmate win to {name}");
                    AddGameWinCrewmate(name);
                }
            }
        }

        private bool CheckWinnersDebug(IPlayerChatEvent e)
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
    }
}