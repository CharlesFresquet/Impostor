-- commandes destruction de table
drop table if exists Players;

-- commandes de creation de tables
create table Players(       PlayerName text primary key not null,
                            Score number,
                            NbGames number,
                            NbGamesImpostor number,
                            NbGamesCrewmate number,
                            NbGamesWon number,
                            NbGamesWonImpostor number,
                            NbGamesWonCrewmate number,
                            NbKills number,
                            ScoreGame number
                        );