# Bolt 039 - Storage limit enforced

Status: complete (scoped down, see below). Starting an upload is refused with 413 (`code: storage_limit`, used and limit bytes) when the file would pass the plan's `MaxStorageGB`. Uploads in progress count at once with their declared size, so several parallel...

Full record: [docs/archive/bolts/039-storage-limit/bolt.md](../../../docs/archive/bolts/039-storage-limit/bolt.md)
