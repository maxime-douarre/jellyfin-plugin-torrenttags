using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.TorrentTags.Repositories;

public class TorrentTagRepository : IDisposable
{
    private readonly SqliteConnection _connection;

    private int _isDisposed;

    public TorrentTagRepository([FromKeyedServices(Plugin.DatabaseFileName)] SqliteConnection connection)
    {
        _connection = connection;
    }

    public void DeleteOrphanTorrentTags(IEnumerable<Guid> baseItemIds)
    {
        ArgumentNullException.ThrowIfNull(baseItemIds);

        _connection.Open();

        using SqliteTransaction deleteOrphanTorrentTagsTransaction = _connection.BeginTransaction();

        using (SqliteCommand createBaseItemTempTableCommand = _connection.CreateCommand())
        {
            createBaseItemTempTableCommand.CommandText = """
                CREATE TEMP TABLE base_item(
                    id BLOB PRIMARY KEY
                );
                """;
            createBaseItemTempTableCommand.ExecuteNonQuery();
        }

        using (SqliteCommand insertBaseItemCommand = _connection.CreateCommand())
        {
            insertBaseItemCommand.CommandText = """
                INSERT INTO base_item(id)
                VALUES($id);
                """;

            SqliteParameter baseItemIdParameter = insertBaseItemCommand.CreateParameter();
            baseItemIdParameter.ParameterName = "$id";
            insertBaseItemCommand.Parameters.Add(baseItemIdParameter);

            foreach (Guid baseItemId in baseItemIds)
            {
                baseItemIdParameter.Value = baseItemId;
                insertBaseItemCommand.ExecuteNonQuery();
            }
        }

        using (SqliteCommand deleteOrphanTorrentTagsCommand = _connection.CreateCommand())
        {
            deleteOrphanTorrentTagsCommand.CommandText = """
                DELETE FROM torrent_tag AS tt
                WHERE NOT EXISTS (
                    SELECT 1 FROM base_item AS bi
                    WHERE bi.id = tt.item_id
                );
                DROP TABLE base_item;
                """;
            deleteOrphanTorrentTagsCommand.ExecuteNonQuery();
        }

        deleteOrphanTorrentTagsTransaction.Commit();
    }

    public IEnumerable<string> GetBaseItemTorrentTags(Guid baseItemId)
    {
        _connection.Open();

        using SqliteCommand getBaseItemTorrentTagsCommand = _connection.CreateCommand();
        getBaseItemTorrentTagsCommand.CommandText = """
            SELECT tag
            FROM torrent_tag
            WHERE item_id = $item_id;
            """;
        getBaseItemTorrentTagsCommand.Parameters.AddWithValue("$item_id", baseItemId);

        using SqliteDataReader getBaseItemTorrentTagsReader = getBaseItemTorrentTagsCommand.ExecuteReader();

        while (getBaseItemTorrentTagsReader.Read())
            yield return getBaseItemTorrentTagsReader.GetString(0);
    }

    public void UpdateBaseItemTorrentTags(Guid baseItemId, IReadOnlyCollection<string> tagsToAdd, IReadOnlyCollection<string> tagsToDelete)
    {
        ArgumentNullException.ThrowIfNull(tagsToAdd);
        ArgumentNullException.ThrowIfNull(tagsToDelete);

        if (tagsToAdd.Count == 0 && tagsToDelete.Count == 0)
            return;

        _connection.Open();

        using SqliteTransaction updateBaseItemTorrentTagsTransaction = _connection.BeginTransaction();

        if (tagsToAdd.Count > 0)
        {
            using SqliteCommand addBaseItemTorrentTagsCommand = _connection.CreateCommand();
            addBaseItemTorrentTagsCommand.CommandText = """
                INSERT INTO torrent_tag(item_id, tag)
                VALUES($item_id, $tag);
                """;
            addBaseItemTorrentTagsCommand.Parameters.AddWithValue("$item_id", baseItemId);

            SqliteParameter tagParameter = addBaseItemTorrentTagsCommand.CreateParameter();
            tagParameter.ParameterName = "$tag";
            addBaseItemTorrentTagsCommand.Parameters.Add(tagParameter);

            foreach (string tagToAdd in tagsToAdd)
            {
                tagParameter.Value = tagToAdd;
                addBaseItemTorrentTagsCommand.ExecuteNonQuery();
            }
        }

        if (tagsToDelete.Count > 0)
        {
            using (SqliteCommand createTagTempTableCommand = _connection.CreateCommand())
            {
                createTagTempTableCommand.CommandText = """
                    CREATE TEMP TABLE tag(
                        tag TEXT PRIMARY KEY
                    );
                    """;
                createTagTempTableCommand.ExecuteNonQuery();
            }

            using (SqliteCommand insertTagCommand = _connection.CreateCommand())
            {
                insertTagCommand.CommandText = """
                    INSERT INTO tag(tag)
                    VALUES($tag);
                    """;

                SqliteParameter tagParameter = insertTagCommand.CreateParameter();
                tagParameter.ParameterName = "$tag";
                insertTagCommand.Parameters.Add(tagParameter);

                foreach (string tagToDelete in tagsToDelete)
                {
                    tagParameter.Value = tagToDelete;
                    insertTagCommand.ExecuteNonQuery();
                }
            }

            using SqliteCommand deleteBaseItemTorrentTagsCommand = _connection.CreateCommand();

            deleteBaseItemTorrentTagsCommand.CommandText = """
                DELETE FROM torrent_tag AS tt
                WHERE tt.item_id = $item_id AND EXISTS(
                    SELECT 1 FROM tag AS t
                    WHERE t.tag = tt.tag
                );
                DROP TABLE tag;
                """;
            deleteBaseItemTorrentTagsCommand.Parameters.AddWithValue("$item_id", baseItemId);
            deleteBaseItemTorrentTagsCommand.ExecuteNonQuery();
        }

        updateBaseItemTorrentTagsTransaction.Commit();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
        {
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }
}
