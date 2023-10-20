// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Basis.Services.V1.Models;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ImportTests;

public class ResolveImportFileTest : BaseImportTest
{
    public ResolveImportFileTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestEch0157ReturnOk()
    {
        var xml = await GetTestEch0157File();

        var response = await AdminClient.ResolveImportFileAsync(new ResolveImportFileRequest
        {
            ImportType = SharedProto.ImportType.Ech157,
            FileContent = xml,
        });

        IgnoreGeneratedFields(response);
        response.MatchSnapshot(x => x.Contest.Id, x => x.Contest.EndOfTestingPhase);
    }

    [Fact]
    public async Task TestEch0159ReturnOk()
    {
        var xml = await GetTestEch0159File();

        var response = await AdminClient.ResolveImportFileAsync(new ResolveImportFileRequest
        {
            ImportType = SharedProto.ImportType.Ech159,
            FileContent = xml,
        });

        IgnoreGeneratedFields(response);
        response.MatchSnapshot(x => x.Contest.Id, x => x.Contest.EndOfTestingPhase);
    }

    [Fact]
    public async Task TestInvalidEch0159ShouldThrow()
    {
        var xml = await GetTestInvalidEch0159File();

        await AssertStatus(
            async () => await AdminClient.ResolveImportFileAsync(new ResolveImportFileRequest
            {
                ImportType = SharedProto.ImportType.Ech159,
                FileContent = xml,
            }),
            StatusCode.InvalidArgument);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ImportService.ImportServiceClient(channel)
            .ResolveImportFileAsync(new ResolveImportFileRequest
            {
                ImportType = SharedProto.ImportType.Ech157,
            });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    private void IgnoreGeneratedFields(ProtoModels.ContestImport contestImport)
    {
        foreach (var majorityElection in contestImport.MajorityElections)
        {
            majorityElection.Election.Id = string.Empty;

            foreach (var candidate in majorityElection.Candidates)
            {
                candidate.Id = string.Empty;
                candidate.MajorityElectionId = string.Empty;
            }
        }

        foreach (var proportionalElection in contestImport.ProportionalElections)
        {
            proportionalElection.Election.Id = string.Empty;

            foreach (var list in proportionalElection.Lists)
            {
                list.List.Id = string.Empty;
                list.List.ProportionalElectionId = string.Empty;

                foreach (var candidate in list.Candidates)
                {
                    candidate.Candidate.Id = string.Empty;
                    candidate.Candidate.ProportionalElectionListId = string.Empty;
                }
            }

            foreach (var listUnion in proportionalElection.ListUnions)
            {
                listUnion.Id = string.Empty;
                listUnion.ProportionalElectionId = string.Empty;
                var referencedListCount = listUnion.ProportionalElectionListIds.Count;
                listUnion.ProportionalElectionListIds.Clear();
                listUnion.ProportionalElectionListIds.AddRange(Enumerable.Range(0, referencedListCount).Select(_ => string.Empty));
                listUnion.ProportionalElectionRootListUnionId = string.Empty;
            }
        }

        foreach (var vote in contestImport.Votes)
        {
            vote.Vote.Id = string.Empty;

            foreach (var ballot in vote.Vote.Ballots)
            {
                ballot.Id = string.Empty;
                ballot.VoteId = string.Empty;
            }
        }
    }
}
