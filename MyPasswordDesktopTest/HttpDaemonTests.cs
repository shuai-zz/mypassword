using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MyPasswordDesktop.Core;

namespace MyPasswordDesktopTest;

[NonParallelizable]
public class HttpDaemonTests
{
    private HttpListener _listener = null!;
    private HttpClient _client = null!;
    private HttpDaemon _daemon = null!;
    private string _dbFile = null!;
    private readonly ManualResetEventSlim _activated = new();

    [SetUp]
    public void SetUp()
    {
        _dbFile = Path.Combine(Path.GetTempPath(), "test-mypassword-http-" + Guid.NewGuid() + ".db");
        new VaultManager(new DbManager(_dbFile)).InitVault("test-password");
        // Use a separate loopback port, so tests cannot hit the installed app.
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Start();
        HttpDaemon.SharedListener = _listener;
        _activated.Reset();
        UiBridge.ActivateApp = () => _activated.Set();
        _daemon = new HttpDaemon();
        _daemon.InitDispatcher();
        _daemon.Start();
        _client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/"), Timeout = TimeSpan.FromSeconds(5) };
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _daemon.Stop();
        _listener.Close();
        HttpDaemon.SharedListener = null;
        UiBridge.ActivateApp = null;
        foreach (string file in Directory.GetFiles(Path.GetDirectoryName(_dbFile)!, Path.GetFileName(_dbFile) + "*"))
            File.Delete(file);
    }

    [Test]
    public async Task ActivateWithoutExtensionHeadersRestoresWindow()
    {
        using var response = await _client.PostAsync("activate", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null, Is.False);
        Assert.That(_activated.Wait(TimeSpan.FromSeconds(1)), Is.True);
    }

    [TestCase("POST", "vault/lock")]
    [TestCase("GET", "items/list")]
    [TestCase("GET", "activate")]
    public async Task OtherRequestsStillRequireExtensionAuthentication(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        using var response = await _client.SendAsync(request);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("error").GetString(), Is.EqualTo("UNKNOWN_EXTENSION"));
        Assert.That(_activated.IsSet, Is.False);
    }
}
