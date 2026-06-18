using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Music.Models;

namespace Music.Services;

public class MusicDatabase
{
    private readonly string _connectionString = $"Data Source={Values.DbPath}";

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }
    public void Initialize()
    {
        if (File.Exists(Values.DbPath))
            return;
        
        var dir = Path.GetDirectoryName(Values.DbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        
        using var conn = Open();
        CreateSchema(conn);
        SeedDefaultMetadata(conn);
    }

    private static void CreateSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE Genres (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Name        TEXT    NOT NULL UNIQUE,
                Description TEXT    NULL
            );
            CREATE TABLE Styles (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Name        TEXT    NOT NULL UNIQUE,
                Category    TEXT    NULL,
                Description TEXT    NULL
            );
            CREATE TABLE Ratings (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Name      TEXT    NOT NULL UNIQUE,
                SortOrder INTEGER NOT NULL
            );
            CREATE TABLE Tracks (
                Id                 INTEGER PRIMARY KEY AUTOINCREMENT,
                CanonicalUrl       TEXT    NOT NULL UNIQUE,
                Title              TEXT    NOT NULL,
                FileName           TEXT    NOT NULL UNIQUE,
                RatingId           INTEGER NOT NULL,
                DownloadedAt       TEXT    NOT NULL,
                DurationSeconds    INTEGER NULL,
                NeedsReview        INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE TrackStyles (
                TrackId INTEGER NOT NULL,
                StyleId INTEGER NOT NULL,
                PRIMARY KEY (TrackId, StyleId)
            );
            CREATE TABLE TrackGenres (
                TrackId INTEGER NOT NULL,
                GenreId INTEGER NOT NULL,
                PRIMARY KEY (TrackId, GenreId)
            );";
        cmd.ExecuteNonQuery();
    }

    private static void SeedDefaultMetadata(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR IGNORE INTO Ratings (Name, SortOrder) VALUES
                ('Skip', 1), ('Okay', 2), ('Good', 3), ('Great', 4), ('Favorite', 5);

            INSERT OR IGNORE INTO Genres (Name, Description) VALUES
                ('Nightcore', 'Hochgepitchte und beschleunigte Versionen bestehender Songs; oft mit Anime-, Internet- oder Edit-Kultur verbunden.'),
                ('Pop', 'Eingängige, melodische Mainstream-Musik mit Fokus auf Gesang, Hook und Wiedererkennbarkeit.'),
                ('EDM', 'Breiter Sammelbegriff für elektronische Festival-, Club- und Dance-Musik mit moderner Produktion.'),
                ('House', 'Elektronische Musik mit stabilem 4/4-Groove, tanzbarem Rhythmus und meist gleichmäßigem Flow.'),
                ('Techno', 'Treibende, repetitive elektronische Musik mit Fokus auf Rhythmus, Energie, Groove und maschinellem Klang.'),
                ('Trance', 'Melodische elektronische Musik mit langen Aufbauten, Spannung, Euphorie und atmosphärischen Flächen.'),
                ('Hardstyle', 'Harte elektronische Musik mit starkem Kick, klaren Leads und oft euphorischen oder aggressiven Melodien.'),
                ('Hardcore', 'Sehr harte und schnelle elektronische Musik; Oberbegriff für extreme Core-Stile mit hoher Energie.'),
                ('Frenchcore', 'Schneller Core-Stil mit rollender Kickdrum, hohem Tempo und oft überraschend melodischen Elementen.'),
                ('Drum and Bass', 'Schnelle elektronische Musik mit Breakbeats, starkem Bass und komplexerem Rhythmus als House oder Techno.'),
                ('Dubstep', 'Basslastige elektronische Musik mit schweren Drops, Wobble-Bässen und oft halbem Tempo-Gefühl.'),
                ('Future Bass', 'Melodische Bass-Musik mit hellen Synths, Vocal-Chops, emotionalem Klang und modernen Drops.'),
                ('Trap', 'Beat-orientierte Musik mit 808-Bass, schnellen Hi-Hats und Einflüssen aus Hip-Hop und EDM.'),
                ('Phonk', 'Dunkler, basslastiger Stil mit Hip-Hop- und Memphis-Rap-Einfluss; häufig in Drift- und Edit-Musik.'),
                ('Wave', 'Atmosphärische, dunkle Bass-Musik zwischen Trap, Ambient und Synth-Flächen; oft emotional und nächtlich.'),
                ('Synthwave', 'Retro-elektronische Musik mit 80er-Synths, nostalgischer Atmosphäre und oft cineastischem Klang.'),
                ('Ambient', 'Atmosphärische Musik ohne starken Beat; Fokus liegt auf Raum, Stimmung, Fläche und Klangtextur.'),
                ('Downtempo', 'Langsamere elektronische Musik mit Beat, aber ruhiger, entspannter und weniger cluborientiert.'),
                ('Lo-Fi', 'Musik mit absichtlich warmem, rauem oder unperfektem Klang; oft entspannte Beats und weiche Atmosphäre.'),
                ('Hip-Hop', 'Oberbegriff für Beats, Sampling, Rap-Kultur und rhythmisch geprägte urbane Musik.'),
                ('Rap', 'Musik mit gesprochenem oder gerapptem Vocal im Vordergrund; stärker text- und floworientiert als Hip-Hop allgemein.'),
                ('R&B', 'Gesangsorientierte Musik mit Soul-, Pop- und Hip-Hop-Einflüssen; oft weich, emotional und groovebasiert.'),
                ('Rock', 'Gitarrenbasierte Musik mit Band-Sound, Drums und meist stärkerer Live- oder Energie-Wirkung.'),
                ('Metal', 'Härtere Gitarrenmusik mit aggressiveren Riffs, hoher Intensität und oft dunklerem oder wuchtigerem Klang.'),
                ('Classical', 'Klassische Musik mit traditionellen Kompositionsformen, z. B. Klavier, Streicher oder Orchesterwerke.'),
                ('Orchestral', 'Musik mit Orchesterklang; kann klassisch, filmisch, modern, dramatisch oder soundtrackartig sein.'),
                ('Piano', 'Klavierzentrierte Musik, unabhängig davon ob sie klassisch, emotional, ruhig oder modern produziert ist.'),
                ('Epic', 'Große, mächtige und cineastische Musik mit Trailer-, Battle-, Bossfight- oder Heldengefühl.'),
                ('Chiptune', '8-Bit- oder Retro-Game-Sound mit alten Konsolen-, Arcade- oder Chip-Sound-Ästhetiken.'),
                ('J-Pop', 'Japanische Popmusik mit eingängigen Melodien; oft relevant für Anime-, Idol- oder japanische Medienkultur.'),
                ('K-Pop', 'Koreanische Popmusik mit stark produzierten Vocals, Hooks, Choreografie- und Dance-orientiertem Sound.');

            INSERT OR IGNORE INTO Styles (Name, Category, Description) VALUES
                ('Fast', 'Energy', 'Schneller Track mit hohem Tempo oder deutlich beschleunigtem Gefühl.'),
                ('Slow', 'Energy', 'Langsamer Track mit ruhigem Tempo oder getragenem Ablauf.'),
                ('Energetic', 'Energy', 'Wirkt aktiv, antreibend und energiegeladen, ohne zwingend hart zu sein.'),
                ('Calm', 'Energy', 'Ruhiger, entspannter Track mit wenig Druck, Aufregung oder Bewegung.'),

                ('Bouncy', 'Groove', 'Federnder, springender Rhythmus, der leicht und beweglich wirkt.'),
                ('Groovy', 'Groove', 'Starker Rhythmus oder Flow, der besonders gut mitwippt oder tanzbar wirkt.'),

                ('Aggressive', 'Pressure', 'Klingt offensiv, hart, druckvoll oder attackierend.'),
                ('Hard', 'Pressure', 'Allgemein harter Sound mit kräftigem Druck, starken Kicks oder intensiver Energie.'),
                ('Raw', 'Pressure', 'Roh, ungeglättet, dreckig oder wenig poliert; oft härter und direkter als melodische Varianten.'),
                ('Bass', 'Pressure', 'Bass ist auffällig dominant und prägt den Track deutlich.'),

                ('Soft', 'Softness', 'Weicher, sanfter Klang ohne starke Härte oder Aggression.'),
                ('Smooth', 'Softness', 'Sehr flüssiger, angenehmer und sauberer Klangfluss.'),
                ('Deep', 'Softness', 'Tiefer, ruhiger oder räumlicher Klang mit weniger direkter Oberfläche.'),
                ('Chill', 'Softness', 'Entspannt, locker und gut zum Nebenbei- oder Runterkommen-Hören.'),

                ('Dark', 'Mood', 'Dunkle, ernste oder bedrohliche Stimmung.'),
                ('Melodic', 'Mood', 'Melodie ist besonders wichtig und bleibt klar im Vordergrund.'),
                ('Emotional', 'Mood', 'Starker emotionaler Ausdruck, unabhängig davon ob traurig, euphorisch oder dramatisch.'),
                ('Melancholic', 'Mood', 'Traurig, nachdenklich oder bittersüß, aber nicht zwingend komplett negativ.'),
                ('Romantic', 'Mood', 'Romantische, warme oder gefühlvolle Stimmung.'),
                ('Dramatic', 'Mood', 'Wirkt groß, ernst, spannungsvoll oder filmisch zugespitzt.'),
                ('Euphoric', 'Mood', 'Sehr positives, erhebendes Hochgefühl; typisch bei Trance oder Hardstyle.'),
                ('Dreamy', 'Mood', 'Verträumt, weich, schwebend oder leicht unreal wirkend.'),
                ('Nostalgic', 'Mood', 'Erzeugt ein Vergangenheits-, Erinnerungs- oder Retro-Gefühl.'),
                ('Happy', 'Mood', 'Fröhlich, positiv und leicht.'),
                ('Hopeful', 'Mood', 'Hoffnungsvoll, aufbauend und positiv nach vorne gerichtet.'),
                ('Mysterious', 'Mood', 'Geheimnisvoll, unklar oder rätselhaft wirkend.'),
                ('Tense', 'Mood', 'Angespannt, unruhig oder spannungsvoll.'),
                ('Atmospheric', 'Mood', 'Starke Atmosphäre, viel Raum, Fläche oder Stimmung statt nur Beat und Melodie.'),

                ('Vocal', 'Vocals', 'Gesang, Rap oder Stimme ist ein wichtiger Bestandteil des Tracks.'),
                ('Instrumental', 'Vocals', 'Keine deutlichen Vocals im Vordergrund; Fokus liegt auf Instrumenten, Synths oder Beats.'),

                ('Anime', 'Context', 'Hat klaren Anime-Bezug oder klingt stark nach Anime-, AMV- oder Opening-Kontext.'),
                ('Sleep', 'Context', 'Ruhig genug zum Einschlafen oder sehr passivem Hören.'),

                ('Speed Up', 'Version', 'Beschleunigte Version eines Tracks, aber nicht zwingend so stark verändert wie Nightcore.'),
                ('Slowed', 'Version', 'Verlangsamte Version eines Tracks.'),
                ('Reverb', 'Version', 'Hall wurde deutlich hinzugefügt und prägt den Klang.'),
                ('Mashup', 'Version', 'Mehrere Songs oder Elemente wurden zu einem neuen Track kombiniert.'),
                ('Live Version', 'Version', 'Live-Aufnahme oder Live-Version statt normaler Studiofassung.'),

                ('Retro', 'Aesthetic', 'Klingt bewusst nach älteren Musik-, Synth- oder Game-Ästhetiken.'),
                ('Cyberpunk', 'Aesthetic', 'Düstere, futuristische High-Tech-Atmosphäre, oft neonartig oder urban.'),
                ('Glitchy', 'Aesthetic', 'Bewusst digitale Fehler, Stottern, Artefakte oder zerhackte Sounds.'),
                ('Immense', 'Aesthetic', 'Sehr groß, wuchtig oder überwältigend im Klangbild.'),
                ('Minimal', 'Aesthetic', 'Reduzierter Track mit wenigen Elementen, viel Wiederholung und wenig Überladung.');";
        cmd.ExecuteNonQuery();
    }

    public bool TrackExists(string canonicalUrl)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Tracks WHERE CanonicalUrl = $url";
        cmd.Parameters.AddWithValue("$url", canonicalUrl);
        return (long)cmd.ExecuteScalar()! > 0;
    }

    public void InsertTrack(string canonicalUrl, string title, string fileName,
                            List<int> genreIds, int ratingId, List<int> styleIds, int? durationSeconds)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        using var insertCmd = conn.CreateCommand();
        insertCmd.Transaction = tx;
        insertCmd.CommandText = @"
            INSERT INTO Tracks (CanonicalUrl, Title, FileName, RatingId, DownloadedAt, DurationSeconds)
            VALUES ($url, $title, $fileName, $ratingId, $downloadedAt, $duration)";
        insertCmd.Parameters.AddWithValue("$url", canonicalUrl);
        insertCmd.Parameters.AddWithValue("$title", title);
        insertCmd.Parameters.AddWithValue("$fileName", fileName);
        insertCmd.Parameters.AddWithValue("$ratingId", ratingId);
        insertCmd.Parameters.AddWithValue("$downloadedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        insertCmd.Parameters.AddWithValue("$duration", durationSeconds.HasValue ? (object)durationSeconds.Value : DBNull.Value);
        insertCmd.ExecuteNonQuery();

        using var idCmd = conn.CreateCommand();
        idCmd.Transaction = tx;
        idCmd.CommandText = "SELECT last_insert_rowid()";
        var trackId = (long)idCmd.ExecuteScalar()!;

        InsertJunctionRows(conn, tx, "TrackGenres", "GenreId", trackId, genreIds);
        InsertJunctionRows(conn, tx, "TrackStyles", "StyleId", trackId, styleIds);

        tx.Commit();
    }

    public List<MusicTrack> GetAllTracks()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT Id, CanonicalUrl, Title, FileName, RatingId, DownloadedAt,
                                   DurationSeconds, NeedsReview
                            FROM Tracks ORDER BY DownloadedAt DESC";
        using var r = cmd.ExecuteReader();
        var list = new List<MusicTrack>();
        while (r.Read())
            list.Add(new MusicTrack(
                r.GetInt32(0), 
                r.GetString(1), 
                r.GetString(2), 
                r.GetString(3),
                r.GetInt32(4), 
                r.GetString(5),
                r.IsDBNull(6) ? null : r.GetInt32(6),
                r.GetInt32(7) != 0));
        return list;
    }
    public Dictionary<int, List<int>> GetAllTrackStyleIds() => GetAllJunctionIds("TrackStyles", "StyleId");
    public List<int> GetTrackStyleIds(int trackId) => GetJunctionIds("TrackStyles", "StyleId", trackId);

    public Dictionary<int, List<int>> GetAllTrackGenreIds() => GetAllJunctionIds("TrackGenres", "GenreId");
    public List<int> GetTrackGenreIds(int trackId) => GetJunctionIds("TrackGenres", "GenreId", trackId);

    public void UpdateTrack(int id, string title, List<int> genreIds, int ratingId,
                            List<int> styleIds)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE Tracks SET Title = $title, RatingId = $ratingId WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$title", title);
            cmd.Parameters.AddWithValue("$ratingId", ratingId);
            cmd.ExecuteNonQuery();
        }

        ReplaceJunctionRows(conn, tx, "TrackGenres", "GenreId", id, genreIds);
        ReplaceJunctionRows(conn, tx, "TrackStyles", "StyleId", id, styleIds);

        tx.Commit();
    }

    public void SetTrackNeedsReview(int id, bool needsReview)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Tracks SET NeedsReview = $needsReview WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$needsReview", needsReview ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    // --- Lookups ---
    public List<Genre> GetGenres() => GetLookupList("Genres", (id, name) => new Genre(id, name));
    public List<Style> GetStyles() => GetLookupList("Styles", (id, name) => new Style(id, name));
    public List<Rating> GetRatings()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, SortOrder FROM Ratings ORDER BY SortOrder";
        using var r = cmd.ExecuteReader();
        var list = new List<Rating>();
        while (r.Read()) list.Add(new Rating(r.GetInt32(0), r.GetString(1), r.GetInt32(2)));
        return list;
    }

    // --- Private helpers ---

    private List<T> GetLookupList<T>(string table, Func<int, string, T> ctor)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT Id, Name FROM {table} ORDER BY Name";
        using var r = cmd.ExecuteReader();
        var list = new List<T>();
        while (r.Read()) list.Add(ctor(r.GetInt32(0), r.GetString(1)));
        return list;
    }

    private Dictionary<int, List<int>> GetAllJunctionIds(string table, string idColumn)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT TrackId, {idColumn} FROM {table}";
        using var r = cmd.ExecuteReader();
        var dict = new Dictionary<int, List<int>>();
        while (r.Read())
        {
            var trackId = r.GetInt32(0);
            var tagId = r.GetInt32(1);
            if (!dict.TryGetValue(trackId, out var ids))
            {
                ids = [];
                dict[trackId] = ids;
            }
            ids.Add(tagId);
        }
        return dict;
    }

    private List<int> GetJunctionIds(string table, string idColumn, int trackId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT {idColumn} FROM {table} WHERE TrackId = $id";
        cmd.Parameters.AddWithValue("$id", trackId);
        using var r = cmd.ExecuteReader();
        var ids = new List<int>();
        while (r.Read()) ids.Add(r.GetInt32(0));
        return ids;
    }

    private static void InsertJunctionRows(SqliteConnection conn, SqliteTransaction tx,
        string table, string idColumn, long trackId, List<int> ids)
    {
        if (ids.Count == 0) return;
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"INSERT INTO {table} (TrackId, {idColumn}) VALUES ($tid, $val)";
        cmd.Parameters.Add("$tid", SqliteType.Integer).Value = trackId;
        var valParam = cmd.Parameters.Add("$val", SqliteType.Integer);
        foreach (var id in ids)
        {
            valParam.Value = id;
            cmd.ExecuteNonQuery();
        }
    }

    private static void ReplaceJunctionRows(SqliteConnection conn, SqliteTransaction tx,
        string table, string idColumn, int trackId, List<int> ids)
    {
        using (var del = conn.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = $"DELETE FROM {table} WHERE TrackId = $id";
            del.Parameters.AddWithValue("$id", trackId);
            del.ExecuteNonQuery();
        }
        InsertJunctionRows(conn, tx, table, idColumn, trackId, ids);
    }
}
