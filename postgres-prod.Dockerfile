FROM postgres:16-alpine

# Copy the schema SQL to a reference location (NOT to initdb.d, to avoid auto-execution)
COPY nauth.sql /schemas/nauth.sql

# Copy the multi-tenant init script
COPY init-multi-tenant.sh /docker-entrypoint-initdb.d/init-multi-tenant.sh
RUN chmod +x /docker-entrypoint-initdb.d/init-multi-tenant.sh
