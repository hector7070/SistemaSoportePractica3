using Abstracciones.Clientes;
using Abstracciones.Comentarios;
using Abstracciones.Etiquetas;
using Abstracciones.Seguridad;
using Abstracciones.Tickets;
using DA.Contexto;
using Microsoft.EntityFrameworkCore;

namespace DA.Semilla;

/// <summary>
/// Datos de demostración. SOLO se ejecuta cuando se arranca con:  dotnet run --project API -- --seed
/// Los DATOS de demostración siguen siendo un comando explícito (no HasData): con User
/// Secrets activos la conexión puede apuntar a AZURE sin que nadie lo note. Por eso muestra el servidor y la
/// base destino y pide confirmación antes de escribir nada. (Desde la Semana 9 las MIGRACIONES sí corren al
/// arrancar, con interruptor y mostrando el destino: ver RegistroDA.AplicarMigracionesAsync.)
/// </summary>
public class SemillaDatos
{
    private readonly SistemaSoporteContexto _ctx;

    public SemillaDatos(SistemaSoporteContexto ctx) => _ctx = ctx;

    /// <summary>Crea Admin, Supervisor y Agente de demostración. Idempotente por correo: solo agrega los que falten.</summary>
    private async Task SembrarUsuariosAsync(string? clave, CancellationToken ct)
    {
        var usuariosBase = new[]
        {
            (Correo: "admin@soporte.local", Nombre: "Administrador", Rol: RolUsuario.Admin),
            (Correo: "supervisor@soporte.local", Nombre: "Supervisor de soporte", Rol: RolUsuario.Supervisor),
            (Correo: "agente@soporte.local", Nombre: "Agente de soporte", Rol: RolUsuario.Agente)
        };

        var correosExistentes = await _ctx.Usuarios
            .Select(u => u.Correo.ToLower())
            .ToListAsync(ct);

        var faltantes = usuariosBase
            .Where(u => !correosExistentes.Contains(u.Correo))
            .ToList();

        if (faltantes.Count == 0)
        {
            Console.WriteLine("Todos los usuarios base ya existen. No se agregó ninguno.");
            return;
        }

        if (string.IsNullOrWhiteSpace(clave) || clave.Length < 8)
        {
            Console.WriteLine("No se crearon usuarios faltantes: falta Semilla:ClaveInicial (mínimo 8 caracteres). " +
                              "Defínala con: dotnet user-secrets set \"Semilla:ClaveInicial\" \"<clave>\" --project API");
            return;
        }

        // Correos en minúscula: AutenticacionFlujo normaliza el correo del login con ToLowerInvariant.
        foreach (var u in faltantes)
        {
            _ctx.Usuarios.Add(new Usuario
            {
                Correo = u.Correo,
                Nombre = u.Nombre,
                ClaveHash = HasherPassword.Hashear(clave),
                Rol = u.Rol
            });
        }

        await _ctx.SaveChangesAsync(ct);
        var creados = string.Join(", ", faltantes.Select(u => $"{u.Correo} ({u.Rol})"));
        Console.WriteLine($"Usuarios creados: {creados}.");
    }

    /// <param name="confirmado">true si se pasó --yes (no pregunta).</param>
    /// <param name="claveUsuarios">Clave inicial de los usuarios de la semilla (Semilla:ClaveInicial). Si es null, no se crean usuarios.</param>
    public async Task EjecutarAsync(bool confirmado, string? claveUsuarios = null, CancellationToken ct = default)
    {
        var conexion = _ctx.Database.GetDbConnection();
        Console.WriteLine($"Destino de la semilla -> Servidor: {conexion.DataSource} | Base: {conexion.Database}");

        if (!confirmado)
        {
            Console.Write("¿Continuar? (s/n): ");
            if (!string.Equals(Console.ReadLine()?.Trim(), "s", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Cancelado. No se modificó nada.");
                return;
            }
        }

        await _ctx.Database.MigrateAsync(ct);

        await SembrarUsuariosAsync(claveUsuarios, ct);

        if (await _ctx.Clientes.AnyAsync(ct))
        {
            Console.WriteLine("La base ya tiene clientes. No se agregó nada (la semilla es idempotente).");
            return;
        }

        var ahora = DateTime.UtcNow;

        var clientes = new[]
        {
            Nuevo("Ana Pérez", "ana@ejemplo.com", "88880001", "Café Aurora"),
            Nuevo("Luis Mora", "luis@ejemplo.com", "88880002", "Taller Mora"),
            Nuevo("Sofía Rojas", "sofia@ejemplo.com", "88880003", null),
            Nuevo("Carlos Vega", "carlos@ejemplo.com", "88880004", "Vega y Hnos"),
            Nuevo("Marta Solano", "marta@ejemplo.com", "88880005", "Clínica Sol"),
            Nuevo("Diego Chaves", "diego@ejemplo.com", "88880006", null),
            Nuevo("Laura Campos", "laura@ejemplo.com", "88880007", "Librería Campos"),
            Nuevo("Pablo Núñez", "pablo@ejemplo.com", "88880008", "Núñez Logística")
        };
        _ctx.Clientes.AddRange(clientes);

        var etiquetas = new[]
        {
            new Etiqueta { Nombre = "Facturación", Color = "#0d6efd" },
            new Etiqueta { Nombre = "Acceso", Color = "#dc3545" },
            new Etiqueta { Nombre = "Hardware", Color = "#198754" },
            new Etiqueta { Nombre = "Correo", Color = "#fd7e14" },
            new Etiqueta { Nombre = "Rendimiento", Color = "#6f42c1" },
            new Etiqueta { Nombre = "Consulta", Color = "#6c757d" }
        };
        _ctx.Etiquetas.AddRange(etiquetas);
        await _ctx.SaveChangesAsync(ct); // aquí se generan los Id

        var titulos = new[]
        {
            "No puedo iniciar sesión", "La impresora no responde", "Error al generar factura",
            "Solicitud de nuevo usuario", "Correo no sincroniza", "Lentitud en el sistema"
        };

        // Random con semilla fija: la misma base de datos de demostración en todas las máquinas.
        var azar = new Random(42);
        var tickets = new List<Ticket>();

        for (var i = 1; i <= 36; i++)
        {
            var creado = ahora.AddDays(-azar.Next(1, 60)).AddMinutes(-azar.Next(0, 1440));
            var estado = (EstadoTicket)azar.Next(0, 4);
            var prioridad = (PrioridadTicket)azar.Next(0, 4);

            var ticket = new Ticket
            {
                Codigo = FormatoCodigoTicket.Generar(creado.Year, i),
                Titulo = $"{titulos[azar.Next(titulos.Length)]} (caso {i})",
                Descripcion = "Ticket de demostración creado por la semilla de datos del curso.",
                Prioridad = prioridad,
                Estado = estado,
                ClienteId = clientes[azar.Next(clientes.Length)].Id,
                AgenteId = estado == EstadoTicket.Abierto ? null : azar.Next(1, 4),
                FechaCreacion = creado,
                FechaActualizacion = creado
            };

            ticket.TicketEtiquetas.Add(new TicketEtiqueta
            {
                Etiqueta = etiquetas[azar.Next(etiquetas.Length)],
                FechaAsignacion = creado
            });

            if (i % 3 == 0)
            {
                ticket.Comentarios.Add(new Comentario
                {
                    Autor = "Soporte",
                    Texto = "Recibimos su caso, lo estamos revisando.",
                    EsInterno = false,
                    FechaCreacion = creado.AddMinutes(30)
                });
            }

            tickets.Add(ticket);
        }

        // Los códigos deben ser únicos aunque dos tickets caigan en años distintos con la misma secuencia: se numera por año.
        foreach (var grupo in tickets.GroupBy(t => t.FechaCreacion.Year))
        {
            var n = 1;
            foreach (var t in grupo.OrderBy(t => t.FechaCreacion))
                t.Codigo = FormatoCodigoTicket.Generar(grupo.Key, n++);
        }

        _ctx.Tickets.AddRange(tickets);
        await _ctx.SaveChangesAsync(ct);

        Console.WriteLine($"Semilla lista: {clientes.Length} clientes, {etiquetas.Length} etiquetas, {tickets.Count} tickets.");

        Cliente Nuevo(string nombre, string correo, string telefono, string? empresa) => new()
        {
            Nombre = nombre,
            Correo = correo,
            Telefono = telefono,
            Empresa = empresa,
            FechaRegistro = ahora
        };
    }
}
