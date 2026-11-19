# UniTracks aus Strava-Sicht: Lückenliste

Stand: nach dem Ausbau der Diagramme (0.5.3). Diese Liste hält fest, was einem Sportler
beim Vergleich mit Strava in UniTracks fehlt, was davon inzwischen gelöst ist und was
bewusst nicht kommt. Sie ist bewusst keine Roadmap mit Zusagen, sondern eine Einordnung.

Legende: **behoben** = in 0.5.3 umgesetzt · **offen** = braucht neue Datenquelle oder
größeren Umbau · **nicht geplant** = passt nicht zu einer Einzelgeräte-App ohne Social Graph.

## Behoben in 0.5.3

| # | Lücke | Lösung |
|---|-------|--------|
| 1 | Kein Höhenprofil im Vergleich: nur die Summe der Höhenmeter stand in der Kennzahlen-Tabelle. | Abschnitt **Höhenprofil** auf der Vergleichsseite. Alle Kurven laufen über die Distanz, die *alle* Trips gemeinsam zurückgelegt haben (120 Stützstellen, linear interpoliert), damit sie deckungsgleich übereinander liegen. Farben wie auf der Karte. |
| 2 | Splits waren nur Zahlen. Ob ein Kilometer schneller oder langsamer war, musste man im Kopf rechnen. | Abschnitt **Split-Unterschied**: Balken um eine Nulllinie, nach oben langsamer, nach unten schneller, in Sekunden pro Kilometer gegen den Basis-Trip. |
| 3 | Kein Verlauf über mehrere Versuche derselben Strecke: Trend und Median standen als Text da. | Abschnitt **Tempo je Lauf** auf der Vergleichsseite: ein Punkt pro Versuch desselben Typs, ältester links, Achse in der Einheit der Sportart. |
| 4 | Keine Bestzeiten: „Wie schnell war mein bester 5-km-Abschnitt?" war nicht beantwortbar. | Abschnitt **Bestzeiten** auf der Analyse-Seite (1 km, 5 km, 10 km, soweit die Distanz reicht) und als Kennzahlen-Zeile im A/B-Vergleich. Gleitendes Fenster über die Distanz, Standzeit zählt nicht, ein GPS-Loch wird nie überbrückt. |

## Offen

| # | Lücke | Warum es noch fehlt |
|---|-------|---------------------|
| 5 | **Puls** ist im Vergleich vorbereitet (Ø-Puls-Zeile), wird aber nie aufgezeichnet. Es gibt weder HealthKit-Anbindung noch einen Brustgurt-Empfänger; nur Altdaten aus dem Import haben Werte. | Braucht eine echte Datenquelle (HealthKit-Lesen/Schreiben oder BLE-Herzfrequenz), nicht nur eine Anzeige. |
| 6 | **Wetter** hängt am Trip, wird aber nirgends geschrieben. | Braucht eine Wetter-API beim Aufzeichnen und eine Entscheidung, ob rückwirkend ergänzt wird. |
| 7 | **Analyse-Seite nutzt Zeit auf der X-Achse**, nicht Distanz. Zwei Trips mit gleicher Strecke sind dadurch optisch nicht vergleichbar. | Der Umbau berührt alle drei Profil-Charts und die Achsenbeschriftung; im Vergleich ist es bereits gelöst. |
| 8 | **Gleichmäßigkeit** (wie stark schwankt das Tempo?) wird nicht ausgewiesen. | Klein: Streuung der Kilometer-Pace als Kennzahl. Noch nicht umgesetzt, weil erst die Splits verlässlich sein mussten. |
| 9 | **Teilstrecken-Segmente** („Runden", Zwischenzeiten an festen Punkten) fehlen. | Braucht eine Segmentverwaltung mit eigenen Start-/Zielpunkten. |
| 10 | **Trainingsbelastung** (Load, Erholung) fehlt vollständig. | Ohne Puls und ohne Sportart-Modell nicht seriös berechenbar; hängt an Punkt 5. |

## Bewusst nicht geplant

Diese Strava-Funktionen sind absichtlich nicht auf der Liste. Sie brauchen einen Social
Graph, ein Abo oder eine Serverflotte, und sie würden UniTracks nicht besser machen:

- Segmente mit Bestenlisten anderer Nutzer, Kudos, Kommentare, Follower.
- Vereine, Challenges, Live-Standortfreigabe.
- Relativer Effort und Suffer Score als „Trainingspunkte" ohne belastbare Herzfrequenz.
- Routenplanung mit automatischer Streckensuche.
