using Backend.Models.Sistemas;
using Backend.Negocio.Generadores;
using Backend.Negocio.Gestores;
using Backend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;

namespace Backend.Controllers
{
    [ApiController]
    [Authorize]
    public class SistemasController : AppController
    {
        private static readonly ConcurrentDictionary<int, Process> BackendProcesses = new();
        private static readonly HttpClient Http = new();
        private readonly IWebHostEnvironment _env;

        public SistemasController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet(Routes.v1.Sistemas.Obtener)]
        public IActionResult Obtener()
        {
            var sistemas = SistemasGestor.ObtenerTodos();
            return Ok(sistemas);
        }

        [HttpGet(Routes.v1.Sistemas.ObtenerPorId)]
        public IActionResult ObtenerPorId(int id)
        {
            var sistema = SistemasGestor.ObtenerPorId(id);
            return sistema == null ? NotFound() : Ok(sistema);
        }

        [HttpGet(Routes.v1.Sistemas.ObtenerPorSlug)]
        public IActionResult ObtenerPorSlug(string slug)
        {
            var sistema = SistemasGestor.ObtenerPorSlug(slug);
            return sistema == null ? NotFound() : Ok(sistema);
        }

        [HttpPost(Routes.v1.Sistemas.Crear)]
        public IActionResult Crear([FromBody] SistemaCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var id = SistemasGestor.Crear(request);
            if (id == null)
                return Conflict("Slug invalido o ya existe.");

            return Ok(new { id });
        }

        [HttpPut(Routes.v1.Sistemas.Editar)]
        public IActionResult Editar(int id, [FromBody] SistemaUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var ok = SistemasGestor.Editar(id, request);
            return ok ? Ok() : NotFound();
        }

        [HttpPost(Routes.v1.Sistemas.Publicar)]
        public IActionResult Publicar(int id)
        {
            var result = SistemasPublicador.Publicar(id);
            return result.Ok ? Ok(result) : BadRequest(result);
        }

        [HttpPost(Routes.v1.Sistemas.Exportar)]
        public IActionResult Exportar(int id, [FromQuery] bool full = false, [FromQuery] string mode = "zip", [FromQuery] bool overwrite = false)
        {
            var normalizedMode = (mode ?? "zip").Trim().ToLowerInvariant();
            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;

            string exportRoot;
            if (normalizedMode == "workspace")
            {
                exportRoot = Environment.GetEnvironmentVariable("SYSTEMBASE_SYSTEMS_ROOT");
                if (string.IsNullOrWhiteSpace(exportRoot))
                    exportRoot = Path.Combine(repoRoot, "systems");
            }
            else
            {
                exportRoot = Environment.GetEnvironmentVariable("SYSTEMBASE_EXPORT_ROOT");
                if (string.IsNullOrWhiteSpace(exportRoot))
                    exportRoot = Path.Combine(repoRoot, "exports");
            }

            var result = SistemasExportador.Exportar(
                id,
                exportRoot,
                _env.ContentRootPath,
                full,
                normalizedMode == "workspace",
                overwrite
            );

            if (!result.Ok)
                return BadRequest(result);

            if (normalizedMode == "workspace")
                return Ok(result);

            if (string.IsNullOrWhiteSpace(result.ZipPath) || string.IsNullOrWhiteSpace(result.ZipFileName))
                return BadRequest(result);

            return PhysicalFile(result.ZipPath, "application/zip", result.ZipFileName);
        }

        [HttpPost(Routes.v1.Sistemas.GenerarBackend)]
        public IActionResult GenerarBackend(int id, [FromQuery] bool overwrite = false)
        {
            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
            var sistema = SistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var outputRoot = Path.Combine(repoRoot, "systems", sistema.Slug, "backend");
            var result = SistemasBackendGenerator.Generar(id, outputRoot, overwrite);

            if (!result.Ok || string.IsNullOrWhiteSpace(result.OutputPath))
                return BadRequest(result);

            var restoreResult = RunDotnetRestore(outputRoot);
            result.RestoreOk = restoreResult.Ok;
            result.RestoreOutput = restoreResult.Output;
            result.RestoreError = restoreResult.Error;

            return Ok(result);
        }

        [HttpPost(Routes.v1.Sistemas.IniciarBackend)]
        public async Task<IActionResult> IniciarBackend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var repoRoot = Directory.GetParent(_env.ContentRootPath)?.FullName ?? _env.ContentRootPath;
            var sistema = SistemasGestor.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            var backendPath = Path.Combine(repoRoot, "systems", sistema.Slug, "backend");
            if (!Directory.Exists(backendPath))
                return BadRequest(new { message = "El backend no esta generado. Genera backend primero." });

            if (BackendProcesses.TryGetValue(id, out var existing) && !existing.HasExited)
            {
                BackendProcessLogStore.Add(id, "info", "Inicio solicitado: backend ya en ejecucion.");
                return Ok(new { status = "running", message = "El backend ya esta en ejecucion." });
            }

            if (await IsBackendOnline(id))
            {
                BackendProcessLogStore.Add(id, "info", "Inicio solicitado: backend ya estaba online.");
                return Ok(new { status = "running", message = "El backend ya esta en ejecucion." });
            }

            var logBuffer = BackendProcessLogStore.Reset(id);
            logBuffer.Add("info", "Iniciando backend (dotnet watch run)...");

            var restore = RunDotnetRestore(backendPath);
            if (!restore.Ok)
            {
                logBuffer.Add("error", $"dotnet restore fallo: {restore.Error}");
                return BadRequest(new { message = "dotnet restore fallo. Revisa la consola del backend." });
            }
            logBuffer.Add("info", "dotnet restore ok.");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "watch run",
                WorkingDirectory = backendPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            startInfo.Environment["DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER"] = "1";

            var process = Process.Start(startInfo);
            if (process == null)
            {
                logBuffer.Add("error", "No se pudo iniciar el proceso dotnet.");
                return BadRequest(new { message = "No se pudo iniciar el backend." });
            }

            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    logBuffer.Add("stdout", args.Data);
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    logBuffer.Add("stderr", args.Data);
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.Exited += (_, __) =>
            {
                logBuffer.Add("info", $"dotnet watch terminó (exit code {process.ExitCode}).");
                BackendProcesses.TryRemove(id, out Process removedProcess);
            };

            BackendProcesses[id] = process;
            return Ok(new { status = "started", message = "Backend iniciando..." });
        }

        [HttpPost(Routes.v1.Sistemas.DetenerBackend)]
        public IActionResult DetenerBackend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            if (!BackendProcesses.TryGetValue(id, out var process) || process.HasExited)
            {
                BackendProcessLogStore.Add(id, "info", "Detener solicitado: backend no estaba en ejecucion.");
                return Ok(new { status = "not_running", message = "El backend no esta en ejecucion." });
            }

            try
            {
                BackendProcessLogStore.Add(id, "info", "Deteniendo backend...");
                process.Kill(true);
                process.WaitForExit(10000);
            }
            catch
            {
                // ignore
            }

            BackendProcesses.TryRemove(id, out Process removedProcess);
            BackendProcessLogStore.Add(id, "info", "Backend detenido.");
            return Ok(new { status = "stopped", message = "Backend detenido." });
        }

        [HttpGet(Routes.v1.Sistemas.PingBackend)]
        public async Task<IActionResult> PingBackend(int id)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var online = await IsBackendOnline(id);
            return Ok(new { online });
        }

        [HttpGet(Routes.v1.Sistemas.LogsBackend)]
        public IActionResult LogsBackend(int id, [FromQuery] long after = 0, [FromQuery] int take = 200)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            take = Math.Clamp(take, 1, 500);
            var buffer = BackendProcessLogStore.Get(id);
            var items = buffer.Read(after, take, out var lastId);
            return Ok(new { items, lastId });
        }

        private async Task<bool> IsBackendOnline(int systemId)
        {
            try
            {
                var basePath = NormalizeApiBase(BackendConfigGestor.ObtenerPorSistema(systemId)?.System?.ApiBase);
                var port = 5032 + systemId;
                var url = $"http://localhost:{port}{basePath}/dev/ping";
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using var response = await Http.GetAsync(url, cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeApiBase(string? apiBase)
        {
            var value = string.IsNullOrWhiteSpace(apiBase) ? "api/v1" : apiBase.Trim();
            value = value.Trim('/');
            return "/" + value;
        }

        private (bool Ok, string Output, string Error) RunDotnetRestore(string workingDirectory)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "restore",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null)
                    return (false, string.Empty, "No se pudo iniciar dotnet restore.");

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(120000))
                {
                    try { process.Kill(true); } catch { }
                    return (false, output, "Timeout ejecutando dotnet restore.");
                }

                var ok = process.ExitCode == 0;
                return (ok, output, error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, ex.Message);
            }
        }
    }
}
