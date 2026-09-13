using System.Net;
using System.Text;
using System.Text.Json;

namespace AISEG.DigitalTwin.Core;

public sealed class DashboardServer : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpListener _listener = new();
    private readonly SimulationManager _simulation;
    private readonly string _dashboardRoot;
    private readonly CancellationTokenSource _stop = new();

    public DashboardServer(SimulationManager simulation, string dashboardRoot, int port = 5080)
    {
        _simulation = simulation;
        _dashboardRoot = dashboardRoot;
        _listener.Prefixes.Add($"http://localhost:{port}/");
    }

    public void Start()
    {
        _listener.Start();
        _ = Task.Run(ListenAsync);
        Console.WriteLine("Web dashboard: http://localhost:5080/");
    }

    private async Task ListenAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            HttpListenerContext context;
            try { context = await _listener.GetContextAsync(); }
            catch when (_stop.IsCancellationRequested) { break; }
            catch (HttpListenerException) { break; }
            _ = Task.Run(() => HandleAsync(context));
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        context.Response.Headers["Cache-Control"] = "no-store";
        var path = context.Request.Url?.AbsolutePath ?? "/";
        if (path == "/api/state")
        {
            await WriteJsonAsync(context, new
            {
                connected = true,
                running = _simulation.IsRunning,
                processed = _simulation.ItemsProcessed,
                safe = _simulation.SafeItems,
                hazard = _simulation.HazardItems,
                active = _simulation.ActiveItemCount,
                confidence = _simulation.AIConfidence,
                length = _simulation.ConveyorLength,
                width = _simulation.ConveyorWidth,
                speed = _simulation.ConveyorSpeed,
                decisions = _simulation.Decisions,
                sensors = new { depth = _simulation.SensorDepth, nir = _simulation.SensorNir, loadCell = _simulation.SensorLoadCell, inductive = _simulation.SensorInductive, capacitive = _simulation.SensorCapacitive }
            });
            return;
        }

        if (context.Request.HttpMethod == "POST")
        {
            switch (path)
            {
                case "/api/start": _simulation.SetRunning(true); break;
                case "/api/stop": _simulation.SetRunning(false); break;
                case "/api/reset": _simulation.Reset(); break;
                case "/api/inject": _simulation.InjectWaste(); break;
                default: context.Response.StatusCode = 404; break;
            }
            await WriteJsonAsync(context, new { ok = context.Response.StatusCode == 200 });
            return;
        }

        var relative = path == "/" ? "index.html" : path.TrimStart('/');
        var fullPath = Path.GetFullPath(Path.Combine(_dashboardRoot, relative));
        if (!fullPath.StartsWith(Path.GetFullPath(_dashboardRoot), StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
            return;
        }
        context.Response.ContentType = Path.GetExtension(fullPath).ToLowerInvariant() switch { ".html" => "text/html", ".css" => "text/css", ".js" => "text/javascript", _ => "application/octet-stream" };
        var bytes = await File.ReadAllBytesAsync(fullPath);
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }

    private static async Task WriteJsonAsync(HttpListenerContext context, object value)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions));
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }

    public void Dispose()
    {
        _stop.Cancel();
        if (_listener.IsListening) _listener.Stop();
        _listener.Close();
        _stop.Dispose();
    }
}
