// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Grpc.Net.Client;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ExportTests;

public class ExportGetTemplatesTest : BaseGrpcTest<ExportService.ExportServiceClient>
{
    public ExportGetTemplatesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestAsAdminShouldReturnOk()
    {
        var response = await AdminClient.GetTemplatesAsync(new GetExportTemplatesRequest
        {
            EntityType = SharedProto.ExportEntityType.Contest,
            Generator = SharedProto.ExportGenerator.VotingBasis,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnOk()
    {
        var response = await AdminClient.GetTemplatesAsync(new GetExportTemplatesRequest
        {
            EntityType = SharedProto.ExportEntityType.Contest,
            Generator = SharedProto.ExportGenerator.VotingBasis,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestVoteTemplates()
    {
        var response = await AdminClient.GetTemplatesAsync(new GetExportTemplatesRequest
        {
            EntityType = SharedProto.ExportEntityType.Vote,
            Generator = SharedProto.ExportGenerator.VotingBasis,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestMajorityElectionTemplates()
    {
        var response = await AdminClient.GetTemplatesAsync(new GetExportTemplatesRequest
        {
            EntityType = SharedProto.ExportEntityType.MajorityElection,
            Generator = SharedProto.ExportGenerator.VotingBasis,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestProportionalElectionTemplates()
    {
        var response = await AdminClient.GetTemplatesAsync(new GetExportTemplatesRequest
        {
            EntityType = SharedProto.ExportEntityType.ProportionalElection,
            Generator = SharedProto.ExportGenerator.VotingBasis,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestTemplates()
    {
        var response = await AdminClient.GetTemplatesAsync(new GetExportTemplatesRequest
        {
            Generator = SharedProto.ExportGenerator.VotingBasis,
        });
        response.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel)
            .GetTemplatesAsync(new GetExportTemplatesRequest
            {
                EntityType = SharedProto.ExportEntityType.Contest,
                Generator = SharedProto.ExportGenerator.VotingBasis,
            });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
