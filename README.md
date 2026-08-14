# 📚 Biblioteca Popular Juan XXIII — Sistema de Gestión

> 🟢 **En producción:** [bibliojuan23.com.ar](https://bibliojuan23.com.ar/)

Sistema web integral desarrollado para la **Biblioteca Popular Juan XXIII**, orientado a centralizar y automatizar la gestión del catálogo bibliográfico, socios, préstamos, reservas y cuotas sociales.

Actualmente administra un catálogo de **más de 12.000 libros**.

### 🛠️ Stack principal

`C#` · `.NET 8` · `ASP.NET Core` · `PostgreSQL` · `Entity Framework Core` · `Docker` · `GitHub Actions`

---

## 🎯 Problema y solución

### El problema
La gestión de la biblioteca requería centralizar información y procesos que involucran un catálogo histórico de más de **12.000 libros**, socios, préstamos, reservas y cuotas sociales. 

La necesidad principal era disponer de una única plataforma que permitiera organizar esta información, reducir tareas manuales y mejorar el seguimiento de las operaciones diarias.

### La solución
Se desarrolló una **aplicación web integral** para centralizar y automatizar la gestión de la biblioteca. 

La plataforma permite administrar el catálogo bibliográfico, gestionar socios, registrar préstamos y devoluciones, controlar reservas y listas de espera, gestionar cuotas y morosidad, e integrar información bibliográfica mediante APIs externas.

El sistema se encuentra **actualmente desplegado en producción**.

---

## ✨ Funcionalidades principales

### 📚 Catálogo bibliográfico
- Gestión completa de libros y ejemplares.
- Importación y actualización masiva mediante archivos CSV.
- Búsqueda y consulta del catálogo.
- Integración con **Google Books API** y **Open Library API** (obtención automática de información, sinopsis y portadas mediante ISBN).
- Clasificación y mapeo de signaturas topográficas CDU.

### 👥 Gestión de socios
- Alta, modificación y consulta de socios activos e inactivos.
- Clasificación por categoría (Estudiante, Jubilado, Honorario, Vitalicio).
- Consulta del estado de cuenta y morosidad.
- Historial de préstamos y seguimiento de situación del socio.

### 🔄 Préstamos y reservas
- Registro de préstamos y devoluciones con control de disponibilidad.
- Renovación de préstamos y seguimiento de vencimientos.
- Gestión de reservas y automatización de listas de espera.

### 💰 Cuotas y administración
- Generación masiva de cuotas mensuales.
- Cálculo de deuda activa e histórico de pagos.
- Paneles y estadísticas de recaudación en el Dashboard.

---

## 🏗️ Arquitectura

La aplicación está desarrollada sobre **ASP.NET Core .NET 8** utilizando **Razor Pages** como modelo de interfaz y **Entity Framework Core** como capa de persistencia. La solución separa la presentación, el acceso a datos, los modelos de dominio y los mecanismos de autenticación/autorización.

### Arquitectura general

```text
┌────────────────────────────────────────────┐
│                ASP.NET Core                │
│                   .NET 8                   │
├────────────────────────────────────────────┤
│                                            │
│             Razor Pages / Areas            │
│         Interfaz y lógica de página        │
│                                            │
├──────────────────────┬─────────────────────┤
│                      │                     │
│      ASP.NET Core    │     Application     │
│        Identity      │    Data / Domain    │
│                      │                     │
│  • Autenticación     │  • Models           │
│  • Roles             │  • ApplicationDb    │
│  • Autorización      │    Context          │
│                      │  • Migraciones      │
├──────────────────────┴─────────────────────┤
│            Entity Framework Core           │
│                   Npgsql                   │
├────────────────────────────────────────────┤
│                 PostgreSQL                 │
└────────────────────────────────────────────┘

```
### Organización del proyecto
```text
Biblioteca_Popular_JuanXXIII/
│
├── Areas/
│   └── Identity/
│       └── Autenticación y gestión de identidad
│
├── Data/
│   └── ApplicationDbContext
│
├── Migrations/
│   └── Migraciones de Entity Framework Core
│
├── Models/
│   └── Entidades del dominio
│
├── Pages/
│   └── Razor Pages de la aplicación
│
├── Program.cs
│   └── Configuración y composición de la aplicación
│
├── Dockerfile
│   └── Contenerización de la aplicación
│
└── deploy.yml
    └── Automatización del despliegue
```
### 💾 Persistencia
ApplicationDbContext utiliza Entity Framework Core y hereda de IdentityDbContext, permitiendo centralizar en el mismo contexto tanto las entidades de negocio como las estructuras necesarias para Identity.

Principales entidades gestionadas:

Socio

Libro

Prestamo

Reserva

Cuota

Recordatorio

La persistencia se realiza sobre un motor PostgreSQL mediante el proveedor Npgsql.EntityFrameworkCore.PostgreSQL.

### 🔐 Autenticación y seguridad
La aplicación utiliza ASP.NET Core Identity para gestionar la autenticación y el control de acceso basado en roles (RBAC). El pipeline de la aplicación evalúa estos permisos de forma estricta antes de renderizar las vistas o procesar operaciones en la base de datos.

Roles implementados:

Administrador: Acceso total al sistema, gestión de usuarios y configuraciones críticas.

Bibliotecario: Gestión del catálogo, préstamos, reservas y ABM de socios.

Socio: Acceso de solo lectura a su historial de préstamos, estado de deuda y catálogo.

Se implementaron prácticas de seguridad para evitar la exposición de datos sensibles, aislando credenciales, tokens de APIs externas y cadenas de conexión fuera del control de versiones mediante variables de entorno (appsettings.json en desarrollo).

### 🚀 Migraciones y despliegue
El sistema cuenta con un flujo de despliegue automatizado y contenerizado:

Contenerización (Docker): La aplicación y el motor de base de datos están orquestados mediante docker-compose, garantizando la paridad entre el entorno de desarrollo y producción.

CI/CD (GitHub Actions): Se configuró un pipeline en deploy.yml que intercepta los cambios en la rama principal, construye la imagen y despliega automáticamente la nueva versión en el servidor.

Hosting en VPS: El sistema se ejecuta en un servidor en la nube (Cloud Server) operando sobre Linux.

Gestión de Entorno: Las credenciales de base de datos y la creación segura del usuario administrador inicial se inyectan directamente al contenedor de producción a través de variables de entorno protegidas, evitando credenciales hardcodeadas.

Migraciones Automáticas: Entity Framework Core aplica las migraciones estructurales (dbContext.Database.MigrateAsync()) durante la inicialización del contenedor, asegurando que la base de datos en producción siempre coincida con el modelo de código desplegado.
Las migraciones de Entity Framework Core forman parte del repositorio y
la aplicación ejecuta la migración de la base de datos durante el proceso
de inicialización.

La aplicación también incluye configuración para ejecución mediante
Docker y un flujo de despliegue automatizado mediante deploy.yml.
