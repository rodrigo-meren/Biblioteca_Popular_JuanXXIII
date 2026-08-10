using Biblioteca_Popular_JuanXXIII.Models;
using Biblioteca_Popular_JuanXXIII.Data;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;
using System.Text;

namespace Biblioteca_Popular_JuanXXIII.Pages.Libros;

public class AltaModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public AltaModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Libro Libro { get; set; } = default!;


    // Agregamos el parámetro int? idDuplicar
    public async Task<IActionResult> OnGetAsync(int? idDuplicar)
    {
        // 1. Calculamos el próximo número de inventario siempre
        var codigos = await _context.Libros
            .Select(l => l.CodigoInventario)
            .ToListAsync();

        int maxInventario = codigos
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => int.TryParse(c, out int numero) ? numero : 0)
            .DefaultIfEmpty(0)
            .Max();

        int proximoNumero = Math.Max(maxInventario, 12067) + 1;

        // 2. Verificamos si el bibliotecario quiere duplicar un libro existente
        if (idDuplicar.HasValue)
        {
            var libroOriginal = await _context.Libros.FindAsync(idDuplicar.Value);

            if (libroOriginal != null)
            {
                // Copiamos los datos del viejo, pero le clavamos el inventario nuevo
                Libro = new Libro
                {
                    CodigoInventario = proximoNumero.ToString(),
                    Titulo = libroOriginal.Titulo,
                    Autor = libroOriginal.Autor,
                    Editorial = libroOriginal.Editorial,
                    AnioPublicacion = libroOriginal.AnioPublicacion,
                    ISBN = libroOriginal.ISBN,
                    GeneroLiterario = libroOriginal.GeneroLiterario,
                    UbicacionFisica = libroOriginal.UbicacionFisica,
                    Sinopsis = libroOriginal.Sinopsis,
                    UrlPortada = libroOriginal.UrlPortada
                };
                return Page();
            }
        }

        // 3. Si no viene ningún ID para duplicar, creamos un formulario en blanco
        Libro = new Libro
        {
            CodigoInventario = proximoNumero.ToString()
        };

        return Page();
    }

    // ==========================================
    // MÉTODO 1: IMPORTACIÓN MASIVA DESDE CSV
    // ==========================================
    public async Task<IActionResult> OnPostImportarAsync(IFormFile archivoCsv)
    {
        if (archivoCsv == null || archivoCsv.Length == 0)
        {
            TempData["MensajeError"] = "Por favor, seleccioná un archivo CSV válido.";
            return RedirectToPage();
        }

        var config = new CsvConfiguration(new CultureInfo("es-AR"))
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        try
        {
            using (var stream = new StreamReader(archivoCsv.OpenReadStream(), Encoding.UTF8))
            using (var csv = new CsvReader(stream, config))
            {
                csv.Context.RegisterClassMap<LibroMap>();

                var records = csv.GetRecords<Libro>().ToList();
                int librosAgregados = 0;
                int librosActualizados = 0;

                foreach (var libro in records)
                {
                    // Limpieza rápida de caracteres de Excel
                    if (!string.IsNullOrEmpty(libro.Titulo))
                    {
                        libro.Titulo = libro.Titulo.Replace("Ã³", "ó").Replace("Ã-", "í").Replace("Ã¡", "á").Replace("Ã©", "é").Replace("Ã±", "ñ").Replace("Ã", "í");
                    }
                    if (!string.IsNullOrEmpty(libro.Autor))
                    {
                        libro.Autor = libro.Autor.Replace("Ã³", "ó").Replace("Ã-", "í").Replace("Ã¡", "á").Replace("Ã©", "é").Replace("Ã±", "ñ").Replace("Ã", "í");
                    }

                    // Validamos que venga con código
                    if (!string.IsNullOrWhiteSpace(libro.CodigoInventario))
                    {
                        var libroExistente = await _context.Libros.FirstOrDefaultAsync(l => l.CodigoInventario == libro.CodigoInventario);

                        // Calculamos el género usando nuestro nuevo método
                        string generoCalculado = ObtenerGeneroPorCDU(libro.UbicacionFisica);

                        if (libroExistente == null)
                        {
                            // 1. SI ES UN LIBRO NUEVO
                            libro.Disponible = true;
                            libro.EsDestacado = false;
                            libro.GeneroLiterario = generoCalculado; // Asignamos el género
                            var ultimoLibro = await _context.Libros
                             .OrderByDescending(l => l.Id)
                             .FirstOrDefaultAsync();
                           
                            int proximoNumero = ultimoLibro != null ? ultimoLibro.Id + 1 : 1;
                                                       
                            Libro.CodigoInventario = proximoNumero.ToString();
                            
                            _context.Libros.Add(Libro);
                            await _context.SaveChangesAsync();
                            _context.Libros.Add(libro);
                            librosAgregados++;
                        }
                        else
                        {
                            // 2. SI EL LIBRO YA EXISTÍA (Se actualiza)
                            libroExistente.UbicacionFisica = libro.UbicacionFisica;

                            // Si el libro viejo no tenía género, o queremos sobreescribirlo con el nuevo cálculo:
                            libroExistente.GeneroLiterario = generoCalculado;

                            _context.Libros.Update(libroExistente);
                            librosActualizados++;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = $"¡Importación completada! Se agregaron {librosAgregados} libros nuevos y se actualizaron {librosActualizados} existentes.";
            }
        }
        catch (Exception ex)
        {
            TempData["MensajeError"] = $"Error al procesar el archivo: {ex.Message}";
        }

        return RedirectToPage();
    }

    // ==========================================
    // MÉTODO 2: ALTA MANUAL DE UN SOLO LIBRO
    // ==========================================
    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Libro.Autor");

        if (string.IsNullOrWhiteSpace(Libro.Autor))
        {
            Libro.Autor = "Anónimo";
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Chequeamos que en el tiempo que tardó en llenar el formulario, 
        // otra persona no haya usado el mismo número.
        var existeLibro = await _context.Libros.AnyAsync(l => l.CodigoInventario == Libro.CodigoInventario);
        if (existeLibro)
        {
            ModelState.AddModelError("Libro.CodigoInventario", "Este código acaba de ser usado. Actualizá la página para obtener el siguiente.");
            return Page();
        }

        var maxId = await _context.Libros.MaxAsync(l => (int?)l.Id) ?? 0;
        int nextId = maxId + 1;

        try
        {
            await _context.Database.ExecuteSqlRawAsync($"ALTER TABLE \"Libros\" ALTER COLUMN \"Id\" RESTART WITH {nextId};");
        }
        catch
        {
            try { await _context.Database.ExecuteSqlRawAsync($"SELECT setval('\"Libros_Id_seq\"', {maxId});"); } catch { }
            try { await _context.Database.ExecuteSqlRawAsync($"SELECT setval('\"Libro_Id_seq\"', {maxId});"); } catch { }
        }

        // Ya NO calculamos el número acá. Respetamos el que vino del formulario (Libro.CodigoInventario)
        _context.Libros.Add(Libro);
        await _context.SaveChangesAsync();

        TempData["MensajeExito"] = $"Libro registrado con éxito en el inventario N° {Libro.CodigoInventario}.";
        return RedirectToPage("./Alta");
    }
    // ==========================================
    // TRADUCTOR DE CDU A GÉNERO LITERARIO (VERSIÓN DETALLADA + TESAURO LOCAL)
    // ==========================================
    private string ObtenerGeneroPorCDU(string signatura)
    {
        if (string.IsNullOrWhiteSpace(signatura)) return "Sin clasificar";

        // Limpiamos espacios y tomamos solo la primera parte
        var partes = signatura.Trim().Split(' ');
        var codigo = partes[0].ToUpper();

        // 0. MAPEO PERSONALIZADO (El Tesauro de la Biblioteca)
        if (codigo.StartsWith("*L")) return "Autores locales";
        if (codigo.StartsWith("02")) return "Bibliotecologia";
        if (codigo.StartsWith("001.92")) return "Divulgacion cientifica";
        if (codigo.StartsWith("31")) return "Sociologia";
        if (codigo.StartsWith("36")) return "Trabajo social";
        if (codigo.StartsWith("572")) return "Antropologia";
        if (codigo.StartsWith("57.017.5")) return "Reproduccion. Maternidad";
        if (codigo.StartsWith("794.1")) return "Ajedrez";
        if (codigo.StartsWith("791.43")) return "Cine";
        if (codigo.StartsWith("78")) return "Musica. Teoria musical. Danza";
        if (codigo.StartsWith("77")) return "Fotografia";
        if (codigo == "7") return "Arte. Dibujo. Pintura";

        // 1. Mapeo detallado de Literatura (La rama del 800)
        if (codigo.StartsWith("82-1")) return "Poesía";
        if (codigo.StartsWith("82-2")) return "Teatro / Drama";
        if (codigo.StartsWith("82-311.2")) return "Novela Histórica";
        if (codigo.StartsWith("82-311.9")) return "Ciencia Ficción";
        if (codigo.StartsWith("82-31")) return "Novela";
        if (codigo.StartsWith("82-32")) return "Novela Corta";
        if (codigo.StartsWith("82-34")) return "Cuento / Relato";
        if (codigo.StartsWith("82-36")) return "Mitos y Leyendas";
        if (codigo.StartsWith("82-3")) return "Narrativa";
        if (codigo.StartsWith("82-4")) return "Ensayo";
        if (codigo.StartsWith("82-9") || codigo.StartsWith("82-93")) return "Literatura Infantil / Juvenil";
        if (codigo.StartsWith("8")) return "Literatura / Lingüística";

        // 2. Mapeo del resto de las ciencias y áreas del conocimiento
        if (codigo.StartsWith("0")) return "Obras Generales / Referencia";
        if (codigo.StartsWith("159.9")) return "Psicología";
        if (codigo.StartsWith("1")) return "Filosofía";
        if (codigo.StartsWith("2")) return "Religión / Teología";
        if (codigo.StartsWith("3")) return "Ciencias Sociales (Derecho, Educación)";
        if (codigo.StartsWith("5")) return "Ciencias Exactas y Naturales";
        if (codigo.StartsWith("6")) return "Ciencias Aplicadas / Tecnología / Medicina";
        if (codigo.StartsWith("7")) return "Arte / Deportes / Recreación";

        // 3. Mapeo de Historia y Geografía (La rama del 900)
        if (codigo.StartsWith("929")) return "Biografías";
        if (codigo.StartsWith("982")) return "Historia Argentina";
        if (codigo.StartsWith("9")) return "Historia / Geografía";

        return "General";
    }

    // ==========================================
    // ENDPOINT PARA EL AUTOCOMPLETADO EN VIVO (AJAX)
    // ==========================================
    public IActionResult OnGetObtenerGenero(string ubicacion)
    {
        string generoCalculado = ObtenerGeneroPorCDU(ubicacion);
        return new JsonResult(generoCalculado);
    }
}

// ==========================================================
// CLASE MAPA: LE ENSEÑA AL CÓDIGO A LEER TU EXCEL
// ==========================================================
public sealed class LibroMap : ClassMap<Libro>
{
    public LibroMap()
    {
        Map(m => m.CodigoInventario).Name("Codigo_Inventario", "CodigoInventario");
        Map(m => m.Titulo).Name("Titulo");
        Map(m => m.Autor).Name("Autor").Optional();
        Map(m => m.ISBN).Name("ISBN").Optional();
        Map(m => m.Editorial).Name("Editorial").Optional();
        Map(m => m.AnioPublicacion).Name("Anio_Publicacion", "AnioPublicacion").Optional();

        // NUEVO MAPEO: Relaciona la Signatura_Dewey con la UbicacionFisica
        Map(m => m.UbicacionFisica).Name("Signatura_Dewey").Optional();
    }
}