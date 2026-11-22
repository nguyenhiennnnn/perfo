namespace Cinema.Infrastructure.Persistence;

public static class AuditLogTriggers
{
    public const string Up = """
        CREATE OR REPLACE FUNCTION audit_logs_block_mutation() RETURNS trigger AS $$
        BEGIN
            RAISE EXCEPTION 'AuditLogs is append-only' USING ERRCODE = 'restrict_violation';
        END;
        $$ LANGUAGE plpgsql;

        DROP TRIGGER IF EXISTS trg_audit_logs_block_row ON "AuditLogs";
        CREATE TRIGGER trg_audit_logs_block_row
            BEFORE UPDATE OR DELETE ON "AuditLogs"
            FOR EACH ROW EXECUTE FUNCTION audit_logs_block_mutation();

        DROP TRIGGER IF EXISTS trg_audit_logs_block_truncate ON "AuditLogs";
        CREATE TRIGGER trg_audit_logs_block_truncate
            BEFORE TRUNCATE ON "AuditLogs"
            FOR EACH STATEMENT EXECUTE FUNCTION audit_logs_block_mutation();
        """;

    public const string Down = """
        DROP TRIGGER IF EXISTS trg_audit_logs_block_row ON "AuditLogs";
        DROP TRIGGER IF EXISTS trg_audit_logs_block_truncate ON "AuditLogs";
        DROP FUNCTION IF EXISTS audit_logs_block_mutation();
        """;
}
