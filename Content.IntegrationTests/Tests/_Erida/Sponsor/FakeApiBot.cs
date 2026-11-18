// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Content.IntegrationTests.Tests._Erida.Sponsor;

/// <summary>
/// Minimal HTTP stand-in for the link and Boosty API of the Discord bot. It follows the same paths and status codes,
/// so the real <c>EridaServerApi</c> client is what gets tested.
/// </summary>
public sealed class FakeApiBot : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _stop = new();

    public readonly string Url;

    /// <summary>
    /// Rank returned by /api/boosty for every user, null means no subscription.
    /// </summary>
    public int? SponsorRank;

    /// <summary>
    /// Whether /api/link reports every user as linked and verified.
    /// </summary>
    public bool Linked;

    /// <summary>
    /// How long /api/boosty takes to answer.
    /// </summary>
    public TimeSpan SponsorDelay;

    /// <summary>
    /// How long /api/link takes to answer.
    /// </summary>
    public TimeSpan LinkDelay;

    public int LinkRequestsStarted;
    public int LinkRequestsAnswered;

    public int SponsorRequestsStarted;
    public int SponsorRequestsAnswered;

    /// <param name="start">False leaves the port closed until <see cref="Start"/>, as if the bot was down.</param>
    public FakeApiBot(bool start = true)
    {
        var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        var port = ((IPEndPoint) socket.LocalEndpoint).Port;
        socket.Stop();

        Url = $"http://127.0.0.1:{port}";
        _listener.Prefixes.Add($"{Url}/");

        if (start)
            Start();
    }

    public void Start()
    {
        _listener.Start();
        _ = Task.Run(Loop);
    }

    private async Task Loop()
    {
        while (!_stop.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception) when (_stop.IsCancellationRequested)
            {
                return;
            }

            var (status, body) = await Handle(context.Request);
            context.Response.StatusCode = status;
            if (body != null)
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                context.Response.ContentType = "application/json";
                await context.Response.OutputStream.WriteAsync(bytes);
            }

            context.Response.Close();
        }
    }

    private async Task<(int Status, string? Body)> Handle(HttpListenerRequest request)
    {
        var parts = request.Url!.AbsolutePath.Trim('/').Split('/');

        // Health check of EridaServerApi.
        if (parts is ["api", "link", _])
        {
            Interlocked.Increment(ref LinkRequestsStarted);
            await Task.Delay(LinkDelay);
            Interlocked.Increment(ref LinkRequestsAnswered);

            return Linked
                ? (200, "{\"linked\":true,\"verified\":true}")
                : (200, "{\"linked\":false,\"verified\":false}");
        }

        if (parts is ["api", "boosty", _])
        {
            Interlocked.Increment(ref SponsorRequestsStarted);
            await Task.Delay(SponsorDelay);
            Interlocked.Increment(ref SponsorRequestsAnswered);

            return SponsorRank is { } rank
                ? (200, $"{{\"rank\":{rank},\"startedAt\":\"2026-01-01T00:00:00Z\"}}")
                : (404, null);
        }

        return (500, null);
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Close();
        _stop.Dispose();
    }
}
