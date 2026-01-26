#!/bin/bash

echo "🔄 Ejecutando scaffold completo de systemBase..."

export $(grep -v '^#' .env | xargs)

dotnet ef dbcontext scaffold \
"Server=$DB_SERVER;Database=$DB_NAME;User Id=$DB_USER;Password=$DB_PASSWORD;TrustServerCertificate=True;" \
Microsoft.EntityFrameworkCore.SqlServer \
--context SystemBaseContext \
--context-dir Data \
--output-dir Models/Entidades \
--use-database-names \
--no-pluralize \
--force


echo "✅ Scaffold finalizado"
