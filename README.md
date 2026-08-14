
# 📚 Sistema de Gestión — Biblioteca Popular Juan XXIII

![C#](https://img.shields.io/badge/C%23-512BD4?style=for-the-badge&logo=csharp&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/Entity%20Framework%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![GitHub Actions](https://img.shields.io/badge/GitHub%20Actions-2088FF?style=for-the-badge&logo=githubactions&logoColor=white)

Sistema web integral desarrollado para la **Biblioteca Popular Juan XXIII**, actualmente desplegado en producción.

La plataforma centraliza y automatiza la gestión del catálogo bibliográfico, socios, préstamos, reservas y cuotas sociales, reemplazando procesos manuales y permitiendo administrar un catálogo histórico de **más de 12.000 libros**.

---

## 🚀 Sobre el proyecto

El sistema fue desarrollado como una solución orientada a las necesidades operativas reales de la institución.

El objetivo principal fue centralizar la información y automatizar procesos que anteriormente requerían gestión manual, incorporando reglas de negocio, control de acceso, integración con APIs externas y herramientas para facilitar la administración diaria de la biblioteca.

### Estado

🟢 **En producción**

### Alcance

- Catálogo de más de **12.000 libros**
- Gestión de socios
- Préstamos y devoluciones
- Reservas y listas de espera
- Control de morosidad
- Gestión de cuotas sociales
- Administración de usuarios y roles
- Integración con APIs externas de libros
- Importación masiva mediante CSV

---

## ✨ Funcionalidades principales

### 📖 Catálogo bibliográfico

- ABML completo de libros.
- Importación y actualización masiva mediante archivos CSV.
- Búsqueda y consulta del catálogo.
- Integración con **Open Library** y **Google Books API**.
- Obtención automática de información bibliográfica mediante ISBN.
- Recuperación de títulos, autores, sinopsis y portadas.
- Traducción y mapeo automático de signaturas topográficas CDU a categorías literarias.

### 👥 Gestión de socios

- Alta, modificación y consulta de socios.
- Categorización de socios:
  - Estudiante
  - Jubilado
  - Honorario
  - Vitalicio
- Ficha unificada del socio.
- Consulta del estado de cuenta.
- Historial de préstamos.

### 🔄 Préstamos y reservas

- Registro de préstamos y devoluciones.
- Control de disponibilidad.
- Gestión de renovaciones.
- Seguimiento de vencimientos.
- Cálculo de morosidad.
- Reservas de ejemplares.
- Gestión automática de listas de espera.

### 💰 Gestión de cuotas

- Generación masiva de cuotas mensuales.
- Cálculo de deuda activa.
- Gestión según categoría de socio.
- Panel estadístico de recaudación.

### 🔐 Seguridad y autorización

- Autenticación de usuarios.
- Autorización basada en roles.
- Roles diferenciados para administración y biblioteca.
- Restricción de acceso a funcionalidades sensibles.
- Protección de operaciones administrativas y contables.
- Validaciones de datos en backend.
- Control de integridad referencial.

---

## 🏗️ Arquitectura

La aplicación utiliza una **arquitectura en capas (N-Tier)** con separación de responsabilidades.

```text
┌──────────────────────────────────────┐
│           Presentation               │
│      ASP.NET Core / Razor Pages      │
└──────────────────┬───────────────────┘
                   │
┌──────────────────▼───────────────────┐
│         Application / Business       │
│          Reglas de negocio           │
└──────────────────┬───────────────────┘
                   │
┌──────────────────▼───────────────────┐
│          Data Access / Infra         │
│       Entity Framework Core          │
└──────────────────┬───────────────────┘
                   │
┌──────────────────▼───────────────────┐
│             PostgreSQL               │
└──────────────────────────────────────┘
Principios y patrones utilizados
Arquitectura N-Tier
Inyección de Dependencias
Repository Pattern
Entity Framework Core
Code First
Separación de responsabilidades
Validaciones en backend
Manejo transaccional
🛠️ Stack tecnológico
Backend
C#
ASP.NET Core
Razor Pages
Entity Framework Core
Base de datos
PostgreSQL
Entity Framework Core Code First
Frontend
HTML5
CSS3
JavaScript
AJAX / Fetch API
Bootstrap 5
Integraciones
Google Books API
Open Library API
DevOps
Docker
GitHub Actions
CI/CD
🔄 Flujo de integración de libros

Una de las funcionalidades principales del sistema es la incorporación y actualización de información bibliográfica.

ISBN
 │
 ▼
Google Books API / Open Library API
 │
 ▼
Obtención de metadatos
 │
 ├── Título
 ├── Autor
 ├── Sinopsis
 ├── Portada
 └── Información bibliográfica
 │
 ▼
Procesamiento y validación
 │
 ▼
PostgreSQL

Esto permite enriquecer el catálogo existente y reducir la carga manual de información.

📦 Importación masiva

El sistema incorpora herramientas para procesar grandes volúmenes de información mediante archivos CSV.

El flujo general es:

Archivo CSV
     ↓
Lectura y validación
     ↓
Procesamiento
     ↓
Reglas de negocio
     ↓
Entity Framework Core
     ↓
PostgreSQL

Esta funcionalidad fue especialmente importante para trabajar con el catálogo histórico de más de 12.000 libros.

🐳 Docker

La aplicación puede ejecutarse mediante contenedores Docker, facilitando la configuración y consistencia entre entornos.

docker build -t biblioteca-popular .

La configuración sensible no se incluye en el repositorio.

Las claves de API y cadenas de conexión deben configurarse mediante variables/configuración del entorno correspondiente.

⚙️ Ejecución local
Requisitos
.NET SDK
PostgreSQL
Docker (opcional)
Git
Clonar el repositorio
git clone https://github.com/rodrigo-meren/Biblioteca_Popular_JuanXXIII.git

cd Biblioteca_Popular_JuanXXIII
Configuración

Crear la configuración local a partir de la plantilla proporcionada por el proyecto y configurar:

Cadena de conexión a PostgreSQL
Google Books API Key
Open Library configuration

⚠️ No utilizar credenciales reales dentro del repositorio.

Migraciones
dotnet ef database update
Ejecutar
dotnet run

La aplicación quedará disponible en el endpoint local configurado.

🔒 Seguridad

El proyecto implementa diferentes mecanismos para proteger la información y controlar el acceso:

Configuración sensible fuera del código fuente.
Uso de IConfiguration.
.gitignore para archivos de configuración locales.
Variables de entorno para secretos.
Autorización basada en roles.
Validaciones de entrada.
Integridad referencial mediante Entity Framework Core.
Control de operaciones sensibles.

Las credenciales utilizadas por el entorno de producción no forman parte del repositorio.

🚀 CI/CD

El proyecto utiliza GitHub Actions para automatizar tareas relacionadas con integración y despliegue.

El objetivo es reducir tareas manuales y mantener un flujo reproducible para la publicación de nuevas versiones.

Git Push
   ↓
GitHub Actions
   ↓
Build
   ↓
Validaciones
   ↓
Docker
   ↓
Deploy
📸 Capturas
Dashboard
<img width="1872" height="964" alt="image" src="https://github.com/user-attachments/assets/8f455f7c-0f9a-4274-a4ee-823a59e14d0d" />

Gestión del catálogo
   <img width="1872" height="964" alt="image" src="https://github.com/user-attachments/assets/1d4c3929-c0c7-4052-8450-862206de0634" />

Gestión de socios / préstamos
<img width="1877" height="967" alt="image" src="https://github.com/user-attachments/assets/1b1bc576-982e-4c16-af83-44703a32f68a" />

🎯 Resultados

El sistema permite centralizar en una única plataforma:

Más de 12.000 registros bibliográficos.
Información de socios.
Historial de préstamos.
Reservas.
Cuotas sociales.
Control de morosidad.
Información bibliográfica proveniente de APIs externas.

La aplicación se encuentra actualmente en producción.

👨‍💻 Autor

Rodrigo Meren

Junior Backend .NET Developer

Tecnologías principales:

C# · .NET · ASP.NET Core · PostgreSQL · Entity Framework Core · Docker · GitHub Actions
