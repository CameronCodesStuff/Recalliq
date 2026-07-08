# Project Notes

## Architecture Decisions

The system uses event-driven architecture with message queues for async processing.

## Database Schema

- Users table with UUID primary keys
- Documents table with full-text search indexes
- Audit log for compliance tracking

## Deployment

Production runs on three availability zones with automatic failover.
The CI/CD pipeline triggers on main branch merges.
