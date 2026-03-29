#!/bin/bash
# init-db.sh — Wait for SQL Server, then apply schema migrations
# Run from inside the sql-server container or with docker exec

set -e

SA_PASSWORD="${SA_PASSWORD:-YourStrong!Pass2025}"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
SERVER="localhost"

echo "=== Waiting for SQL Server to be ready ==="
for i in $(seq 1 30); do
    $SQLCMD -S $SERVER -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" -b > /dev/null 2>&1 && break
    echo "  Attempt $i/30 — waiting 5s..."
    sleep 5
done

# Verify connection
$SQLCMD -S $SERVER -U sa -P "$SA_PASSWORD" -C -Q "SELECT @@VERSION" -b || {
    echo "ERROR: Cannot connect to SQL Server"
    exit 1
}

echo ""
echo "=== Checking if AdventureWorksDW2020 exists ==="
DB_EXISTS=$($SQLCMD -S $SERVER -U sa -P "$SA_PASSWORD" -C -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = 'AdventureWorksDW2020'" -h -1 -b | tr -d ' ')

if [ "$DB_EXISTS" = "0" ]; then
    echo "Database not found. Checking for backup file..."
    if [ -f /scripts/AdventureWorksDW2020.bak ]; then
        echo "Restoring AdventureWorksDW2020 from backup..."
        # Detect logical file names from the backup (supports DW2020 or DW2022 backups)
        DATA_NAME=$($SQLCMD -S $SERVER -U sa -P "$SA_PASSWORD" -C -Q "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK = '/scripts/AdventureWorksDW2020.bak'" -h -1 -W -b | head -1 | awk '{print $1}')
        LOG_NAME="${DATA_NAME}_log"
        echo "  Logical names: $DATA_NAME / $LOG_NAME"

        $SQLCMD -S $SERVER -U sa -P "$SA_PASSWORD" -C -Q "
            RESTORE DATABASE AdventureWorksDW2020
            FROM DISK = '/scripts/AdventureWorksDW2020.bak'
            WITH MOVE '$DATA_NAME' TO '/var/opt/mssql/data/AdventureWorksDW2020.mdf',
                 MOVE '$LOG_NAME' TO '/var/opt/mssql/data/AdventureWorksDW2020_log.ldf',
                 REPLACE;
        " -b
        echo "Restore complete."
    else
        echo "WARNING: No backup file found at /scripts/AdventureWorksDW2020.bak"
        echo "Please download from: https://github.com/Microsoft/sql-server-samples/releases/tag/adventureworks"
        echo "Place the .bak file in ./infrastructure/scripts/ and re-run this script."
        exit 1
    fi
else
    echo "AdventureWorksDW2020 already exists."
fi

echo ""
echo "=== Applying SQL migrations ==="
for script in /sql/0*.sql; do
    if [ -f "$script" ]; then
        echo "Running: $(basename $script)"
        $SQLCMD -S $SERVER -U sa -P "$SA_PASSWORD" -C -d AdventureWorksDW2020 -i "$script" -b
        echo "  Done."
    fi
done

echo ""
echo "=== Database initialization complete ==="
