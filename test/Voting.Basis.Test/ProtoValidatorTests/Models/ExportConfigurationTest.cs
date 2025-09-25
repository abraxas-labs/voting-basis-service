// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Shared.V1;
using Voting.Lib.Testing.Utils;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class ExportConfigurationTest : ProtoValidatorBaseTest<ExportConfiguration>
{
    public static ExportConfiguration NewValid(Action<ExportConfiguration>? action = null)
    {
        var exportConfiguration = new ExportConfiguration
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            DomainOfInfluenceId = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Description = "Schnittstelle 5",
            EaiMessageType = "1234567",
            ExportKeys = { "vote_ech_0110", "proportional_election_candidate_results_with_vote_sources" },
            Provider = ExportProvider.Standard,
        };

        action?.Invoke(exportConfiguration);
        return exportConfiguration;
    }

    protected override IEnumerable<ExportConfiguration> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Id = string.Empty);
        yield return NewValid(x => x.DomainOfInfluenceId = string.Empty);
        yield return NewValid(x => x.Description = string.Empty);
        yield return NewValid(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(100));
        yield return NewValid(x => x.EaiMessageType = RandomStringUtil.GenerateNumeric(7));
        yield return NewValid(x => x.ExportKeys.Clear());
        yield return NewValid(x => x.ExportKeys.Add(RandomStringUtil.GenerateComplexSingleLineText(1)));
        yield return NewValid(x => x.ExportKeys.Add(RandomStringUtil.GenerateComplexSingleLineText(200)));
    }

    protected override IEnumerable<ExportConfiguration> NotOkMessages()
    {
        yield return NewValid(x => x.Id = "invalid-guid");
        yield return NewValid(x => x.DomainOfInfluenceId = "invalid-guid");
        yield return NewValid(x => x.Description = RandomStringUtil.GenerateSimpleSingleLineText(101));
        yield return NewValid(x => x.Description = "Schnitt\nstelle 5");
        yield return NewValid(x => x.EaiMessageType = string.Empty);
        yield return NewValid(x => x.EaiMessageType = RandomStringUtil.GenerateNumeric(6));
        yield return NewValid(x => x.EaiMessageType = RandomStringUtil.GenerateNumeric(8));
        yield return NewValid(x => x.EaiMessageType = RandomStringUtil.GenerateAlphabetic(7));
        yield return NewValid(x => x.ExportKeys.Add(string.Empty));
        yield return NewValid(x => x.ExportKeys.Add(RandomStringUtil.GenerateComplexSingleLineText(201)));
        yield return NewValid(x => x.ExportKeys.Add("vote_\nech_0110"));
    }
}
