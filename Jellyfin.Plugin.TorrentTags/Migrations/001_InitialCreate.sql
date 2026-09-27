CREATE TABLE torrent_tag(
    item_id BLOB NOT NULL,
    tag TEXT NOT NULL,
    PRIMARY KEY(item_id, tag)
);
