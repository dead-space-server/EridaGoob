// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using Content.Client.Lobby;
using Content.Shared.Preferences;
using Content.Shared.Access.Components;
using Robust.Client.UserInterface;
using Robust.Shared.Log;
using Robust.Shared.Localization;
using System.Globalization;
using Serilog.Events;

namespace Content.IntegrationTests.Tests._Erida;

[TestFixture]
public sealed class ProfilePreviewLocalizationTest
{
    [Test]
    public async Task CharacterPreviewDoesNotLocalizeEmptyMessages()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { InLobby = true });
        await pair.Client.WaitAssertion(() =>
        {
            var client = pair.Client;
            var localization = client.ResolveDependency<ILocalizationManager>();
            localization.SetCulture(CultureInfo.GetCultureInfo("ru-RU"));
            var logger = client.ResolveDependency<ILogManager>().GetSawmill("loc");
            var previousLevel = logger.Level;
            logger.Level = LogLevel.Warning;
            var capture = new EmptyMessageCapture();
            logger.AddHandler(capture);
            try
            {
                var card = new IdCardComponent();
                Assert.That(card.LocalizedJobTitle, Is.Empty);
                card.JobTitle = "";
                Assert.That(card.LocalizedJobTitle, Is.Empty);
                card.JobTitle = " ";
                Assert.That(card.LocalizedJobTitle, Is.Empty);
                card.JobTitle = "job-name-passenger";
                Assert.That(card.LocalizedJobTitle, Is.EqualTo(Loc.GetString("job-name-passenger")));
                card.LocalizedJobTitle = "Custom title";
                Assert.That(card.LocalizedJobTitle, Is.EqualTo("Custom title"));
                var preferences = client.ResolveDependency<IClientPreferencesManager>();
                var jobs = new[] { "Passenger", "Passenger", "HeadOfSecurity", "Bartender" };
                for (var i = 0; i < jobs.Length; i++)
                {
                    var profile = HumanoidCharacterProfile.DefaultWithSpecies("Demon")
                        .WithName($"Preview {i}").WithJobPriority(jobs[i], JobPriority.High);
                    preferences.UpdateCharacter(profile, i);
                }
                var controller = client.ResolveDependency<IUserInterfaceManager>().GetUIController<LobbyUIController>();
                controller.ReloadCharacterSetup();
                controller.ReloadCharacterSetup();
                Assert.That(capture.Stacks, Is.Empty, string.Join("\n", capture.Stacks));
            }
            catch (Exception error)
            {
                TestContext.Out.WriteLine(error);
                throw;
            }
            finally
            {
                logger.RemoveHandler(capture);
                logger.Level = previousLevel;
            }
        });
        await pair.CleanReturnAsync();
    }

    private sealed class EmptyMessageCapture : ILogHandler
    {
        public readonly ConcurrentQueue<string> Stacks = new();

        public void Log(string sawmillName, LogEvent message)
        {
            var rendered = message.RenderMessage();
            if (rendered.Contains("Unknown messageId") && rendered.EndsWith(": \"\""))
                Stacks.Enqueue(Environment.StackTrace);
        }
    }
}
