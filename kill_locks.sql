-- Script to identify and kill blocking connections on the Invoices table

-- First, check for locks on the Invoices table
SELECT
    pg_stat_activity.pid,
    pg_stat_activity.application_name,
    pg_stat_activity.client_addr,
    pg_stat_activity.state,
    pg_stat_activity.query,
    pg_stat_activity.query_start,
    NOW() - pg_stat_activity.query_start AS duration
FROM pg_stat_activity
WHERE
    pg_stat_activity.datname = current_database()
    AND pg_stat_activity.state != 'idle'
    AND pg_stat_activity.pid != pg_backend_pid()
ORDER BY duration DESC;

-- Terminate all idle connections (except the current one)
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE
    datname = current_database()
    AND state = 'idle'
    AND pid != pg_backend_pid();

-- Terminate all connections that have been running for more than 5 minutes
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE
    datname = current_database()
    AND state != 'idle'
    AND NOW() - query_start > interval '5 minutes'
    AND pid != pg_backend_pid();

-- Show remaining active connections
SELECT
    count(*) as active_connections,
    state
FROM pg_stat_activity
WHERE datname = current_database()
GROUP BY state;
