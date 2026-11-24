// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Shared._Erida.Roles;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Erida.Roles;

/// <summary>
/// Checks the alternate job titles and how the character profile stores them.
/// </summary>
[TestFixture]
[TestOf(typeof(JobAlternateTitleSystem))]
public sealed class JobAlternateTitleTest
{
    private static readonly Gender[] Genders = [Gender.Male, Gender.Female, Gender.Epicene, Gender.Neuter];

    /// <summary>
    /// Every title points at a selectable job, has a short unique name for every gender and is cached by the system.
    /// </summary>
    [Test]
    public async Task AllTitlesValid()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var loc = server.ResolveDependency<ILocalizationManager>();
        var maxLength = server.ResolveDependency<IConfigurationManager>().GetCVar(CCVars.MaxIdJobLength);
        var system = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<JobAlternateTitleSystem>();

        SortedSet<string> errors = [];
        Dictionary<(string Job, Gender Gender), Dictionary<string, string>> namesByJob = [];

        await server.WaitAssertion(() =>
        {
            foreach (var title in protoMan.EnumeratePrototypes<JobAlternateTitlePrototype>())
            {
                if (!protoMan.TryIndex(title.Job, out var job))
                {
                    errors.Add($"{title.ID}: job {title.Job} does not exist");
                    continue;
                }

                if (!job.SetPreference)
                    errors.Add($"{title.ID}: job {job.ID} can't be picked in the character editor");

                if (!system.GetTitles(job.ID).Contains(title))
                    errors.Add($"{title.ID}: missing from the cached titles of {job.ID}");

                foreach (var gender in Genders)
                {
                    if (!loc.TryGetString(title.Name, out var name, ("gender", gender)) || string.IsNullOrWhiteSpace(name))
                    {
                        errors.Add($"{title.ID}: no localized name for {gender}");
                        continue;
                    }

                    if (name.Length > maxLength)
                        errors.Add($"{title.ID}: '{name}' is longer than the ID card limit of {maxLength}");

                    if (name == job.LocalizedName)
                        errors.Add($"{title.ID}: '{name}' repeats the job name");

                    if (!namesByJob.TryGetValue((job.ID, gender), out var names))
                        namesByJob[(job.ID, gender)] = names = [];

                    if (names.TryGetValue(name, out var other))
                        errors.Add($"{title.ID}: '{name}' duplicates {other} for {gender}");
                    else
                        names[name] = title.ID;
                }
            }
        });

        await pair.CleanReturnAsync();
        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
    }

    /// <summary>
    /// A picked title survives a profile copy, takes part in equality and is dropped when it belongs to another job.
    /// </summary>
    [Test]
    public async Task ProfileKeepsValidTitles()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoMan = server.ResolveDependency<IPrototypeManager>();
        List<string> errors = [];

        await server.WaitAssertion(() =>
        {
            var title = protoMan.EnumeratePrototypes<JobAlternateTitlePrototype>().OrderBy(t => t.ID).First();
            var otherJob = protoMan.EnumeratePrototypes<JobPrototype>()
                .First(job => job.ID != title.Job.Id && job.SetPreference);

            var profile = new HumanoidCharacterProfile().WithJobAltTitle(title.Job, title.ID);
            var copy = new HumanoidCharacterProfile(profile);

            if (!copy.TryGetJobAltTitle(title.Job, protoMan, out var copied) || copied.ID != title.ID)
                errors.Add("the copy lost the title");

            if (!profile.MemberwiseEquals(copy))
                errors.Add("the copy is not equal to the original");

            if (profile.MemberwiseEquals(new HumanoidCharacterProfile()))
                errors.Add("a profile with a title is equal to one without");

            var wrong = new HumanoidCharacterProfile { JobAlternateTitles = { [otherJob.ID] = title.ID } };
            if (wrong.TryGetJobAltTitle(otherJob.ID, protoMan, out _))
                errors.Add($"title {title.ID} was accepted for {otherJob.ID}");
        });

        await pair.CleanReturnAsync();
        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
    }
}
