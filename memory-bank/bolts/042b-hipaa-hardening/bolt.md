# Bolt 042b - HIPAA review and hardening

Status: complete. The review itself is `memory-bank/intents/002-requirements-gap-closure/hipaa-review.md` (findings, what was fixed, what stays open, what a person has to do). Passwords: PBKDF2 raised from 10,000 to 310,000 rounds; the hash names its strength; old hashes verify and are replaced at the next sign-in.

Full record: [docs/archive/bolts/042b-hipaa-hardening/bolt.md](../../../docs/archive/bolts/042b-hipaa-hardening/bolt.md)
