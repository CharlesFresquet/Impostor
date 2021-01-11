import sqlite3, random
from datetime import datetime

# ouverture/initialisation de la base de donnee 
conn = sqlite3.connect('StatImp.db')
# conn.row_factory = sqlite3.Row
c = conn.cursor()

# A completer...
# On affiche Mesures pour verifier les valeurs de base
c.execute("SELECT * FROM Players;")
results = c.fetchall()
print("\nPlayers:\n")
for r in results:
    print(r)

# fermeture
conn.commit()
conn.close()
