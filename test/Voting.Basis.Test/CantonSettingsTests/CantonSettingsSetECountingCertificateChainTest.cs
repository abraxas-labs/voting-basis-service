// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Snapper;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.MockedData.TestFiles;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.CantonSettingsTests;

public class CantonSettingsSetECountingCertificateChainTest : BaseGrpcTest<CantonSettingsService.CantonSettingsServiceClient>
{
    public CantonSettingsSetECountingCertificateChainTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await DomainOfInfluenceMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldWork()
    {
        var response = await AdminClient.SetECountingCertificateChainAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<SetCantonSettingsECountingCertificateChain>();
        response.ShouldMatchChildSnapshot("response");
        eventData.ShouldMatchChildSnapshot("eventData");
    }

    [Fact]
    public async Task InvalidPemStringShouldThrow()
    {
        await AssertStatus(
           async () => await AdminClient.SetECountingCertificateChainAsync(NewValidRequest(x => x.RawPem = "random")),
           StatusCode.InvalidArgument,
           "Exactly one leaf certificate is expected, but 0 are provided");
    }

    [Fact]
    public async Task TestProcessor()
    {
        var id = CantonSettingsMockedData.GuidStGallen;

        await TestEventPublisher.Publish(new SetCantonSettingsECountingCertificateChain
        {
            EventInfo = GetMockedEventInfo(),
            Id = id.ToString(),
            RawPem = CertificateTestFiles.FourLevelChainRawPem,
        });

        var cantonSettings = await RunOnDb(db => db.CantonSettings
            .Include(x => x.ECountingCertificateChain!.Infos!.OrderBy(i => i.Subject))
            .SingleAsync(x => x.Id == id));

        var certChain = cantonSettings.ECountingCertificateChain!;

        foreach (var info in certChain.Infos!)
        {
            info.Id = Guid.Empty;
            info.CertificateChainId = Guid.Empty;
            info.CertificateChain = null!;
        }

        certChain.Id = Guid.Empty;
        certChain.CantonSettings = null!;
        certChain.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
        => await new CantonSettingsService.CantonSettingsServiceClient(channel)
            .SetECountingCertificateChainAsync(NewValidRequest());

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.Admin;
    }

    private SetCantonSettingsECountingCertificateChainRequest NewValidRequest(
        Action<SetCantonSettingsECountingCertificateChainRequest>? customizer = null)
    {
        var request = new SetCantonSettingsECountingCertificateChainRequest
        {
            Id = CantonSettingsMockedData.IdStGallen,
            RawPem = CertificateTestFiles.FourLevelChainRawPem,
        };
        customizer?.Invoke(request);
        return request;
    }
}
