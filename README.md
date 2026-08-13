
# 📚 Sistema de Gestión - Biblioteca Popular Juan XXIII

![.NET Core](https://img.shields.io/badge/.NET%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2CA5E0?style=for-the-badge&logo=docker&logoColor=white)

Un sistema backend integral y escalable desarrollado para la automatización, administración y modernización de una biblioteca popular. Diseñado para procesar de manera eficiente un catálogo histórico de más de 12.000 libros, gestionar el padrón de socios, controlar la trazabilidad de los préstamos y llevar la contabilidad financiera de las cuotas sociales.

## ✨ Características Principales

*   **📖 Catálogo Inteligente:** 
    *   Módulo ABML completo con importación y actualización masiva de libros mediante archivos `.csv`.
    *   Integración de APIs REST (Open Library y Google Books) para el autocompletado automático de metadatos (títulos, autores, sinopsis y portadas) mediante ISBN.
    *   Algoritmo personalizado para la traducción y mapeo automático de signaturas topográficas (CDU) a géneros literarios legibles.
*   **👥 Gestión de Socios:** 
    *   Administración del padrón con perfiles detallados y categorización dinámica (Estudiante, Jubilado, Honorario, Vitalicio).
    *   Ficha unificada que centraliza el estado de cuenta y el historial completo de préstamos de cada usuario.
*   **🔄 Control de Préstamos y Reservas:** 
    *   Sistema transaccional para la asignación y devolución rápida de libros.
    *   Tablero de control semaforizado para el seguimiento de vencimientos, renovaciones y cálculo de morosidad.
    *   Lógica de disponibilidad y lista de espera automatizada para material reservado.
*   **💰 Módulo Contable:** 
    *   Generación masiva y automática de lotes de cuotas mensuales según categoría de socio.
    *   Tablero estadístico de recaudación en tiempo real y cálculo de deuda activa.

## 🛠️ Stack Tecnológico

*   **Backend & Framework:** C#, ASP.NET Core, Razor Pages.
*   **Base de Datos:** PostgreSQL, Entity Framework Core (Code-First).
*   **Arquitectura:** Arquitectura en capas (N-Tier), Inyección de Dependencias, Patrones Repository/CRUD.
*   **Frontend:** HTML5, CSS3, JavaScript (AJAX/Fetch API), Bootstrap 5.
*   **DevOps & Despliegue:** Docker, GitHub Actions (CI/CD Pipeline).

## 🚀 Instalación y Despliegue Local

Para correr este proyecto en un entorno de desarrollo local, necesitás tener instalado [.NET SDK](https://dotnet.microsoft.com/download) y un motor de base de datos **PostgreSQL**.

1. **Clonar el repositorio:**
   ```bash
   git clone [https://github.com/tu-usuario/Biblioteca_Popular_JuanXXIII.git](https://github.com/tu-usuario/Biblioteca_Popular_JuanXXIII.git)
   cd Biblioteca_Popular_JuanXXIII
Configurar Variables de Entorno (Secretos):
El proyecto utiliza variables de entorno para proteger claves sensibles.

Copiá el archivo de plantilla appsettings.example.json y renombralo a appsettings.json.

Abrí appsettings.json y configurá tu cadena de conexión local a PostgreSQL y tus API Keys (Google Books).

Ejecutar las Migraciones (Base de Datos):

Bash
dotnet ef database update
Compilar y Ejecutar:

Bash
dotnet run
El sistema estará disponible por defecto en https://localhost:7xxx.

🔒 Seguridad y Buenas Prácticas
Protección de Credenciales: Uso estricto de IConfiguration y .gitignore para evitar la exposición de tokens y cadenas de conexión.

Validaciones en Backend: Control de integridad referencial, sanitización de entradas en formularios y manejo seguro de transacciones mediante EF Core.

Autorización: Implementación de roles diferenciados (Administrador / Bibliotecario) para restringir el acceso a módulos sensibles (ej. contabilidad y borrado de registros).

👨‍💻 Autor
Rodrigo Meren

Desarrollador Backend .NET
