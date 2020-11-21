-- commandes destruction de table
drop table if exists Players;

-- commandes de creation de tables
create table    Players(    PlayerName text primary key not null,
                            Score number,
                            NbGames number,
                            NbGamesImpostor number,
                            NbGamesCrewmate number,
                            NbGamesWon number,
                            NbGamesWonImpostor number,
                            NbGamesWonCrewmate number,
                            NbKills number
                        );

--insertion de donnees dans les tables

-- creation de la table Players avec Keleonix
insert into     Players (PlayerName, Score, NbGames, NbGamesImpostor, NbGamesCrewmate, NbGamesWon, NbGamesWonImpostor, NbGamesWonCrewmate, NbKills) values
                    ("Keleonix", 0, 0, 0, 0, 0, 0, 0, 0);