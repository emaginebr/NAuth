#!/bin/bash
set -e

# Creates multiple tenant databases from the nauth.sql schema.
# Environment variables NAUTH_DB and VIRALT_DB define the database names.

DATABASES=("${NAUTH_DB}" "${VIRALT_DB}")

for DB_NAME in "${DATABASES[@]}"; do
    if [ -z "$DB_NAME" ]; then
        continue
    fi

    echo "Creating database: $DB_NAME"
    psql -v ON_ERROR_STOP=0 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-SQL
        SELECT 'CREATE DATABASE $DB_NAME'
        WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$DB_NAME')\gexec
SQL

    echo "Applying schema to: $DB_NAME"
    # Skip the CREATE DATABASE lines (handled by this script) and apply only the schema
    sed '1,/^$/d' /schemas/nauth.sql | psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$DB_NAME"
    echo "Database $DB_NAME ready."
done
