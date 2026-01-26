SystemBase
Template base full-stack para sistemas administrativos modernos, con backend en .NET 8 y frontend en Vue 3 +
Vuetify.

Arquitectura General
El proyecto está dividido claramente en backend y frontend, siguiendo buenas prácticas de separación de
responsabilidades.
backend/
- API REST en .NET 8
- Controllers, Gestores y Entidades
- Autenticación JWT
- Menú dinámico jerárquico
frontend/
- Vue 3 + Vite
- Vuetify
- Layout persistente
- Sidebar dinámico desde backend

Funcionalidades
Backend:
- Login y registro
- JWT
- Roles y usuarios
- Menú jerárquico por rol
- Orden configurable
Frontend:
- Login protegido
- Sidebar dinámico
- Menús padre e hijos
- ABM de menú

Variables de Entorno
El proyecto utiliza variables de entorno. Los archivos .env no se suben al repositorio.
Se debe crear un archivo .env a partir de .env.example.
DB_CONNECTION
JWT_SECRET
JWT_ISSUER
JWT_AUDIENCE
VITE_API_URL

Backend (.NET 8)
Requisitos:
- .NET SDK 8
- SQL Server
Comandos:
Windows / macOS / Linux
cd backend
dotnet restore
dotnet run

Frontend (Vue 3 + Vite)
Requisitos:
- Node.js 18+
Comandos:
Windows / macOS / Linux
cd frontend
npm install
npm run dev

Endpoints Principales
POST /api/v1/auth/login
POST /api/v1/auth/registrar
GET /api/v1/menu
GET /api/v1/menu/tree
POST /api/v1/menu
PUT /api/v1/menu/{id}
DELETE /api/v1/menu/{id}

Sidebar Dinámico
El sidebar se construye completamente desde el backend.
Respeta roles, jerarquía y orden configurado en base de datos.

Git Ignore
El proyecto ignora:
- node_modules
- bin / obj
- archivos .env
- logs
- configuraciones locales