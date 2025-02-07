// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Voting.Basis.Core.Auth;
using Voting.Basis.Test.ImportTests.TestFiles;
using Voting.Lib.Testing.Utils;
using Xunit;
using ContestImport = Abraxas.Voting.Basis.Services.V1.Models.ContestImport;
using ResolveImportFileRequest = Voting.Basis.Controllers.Models.ResolveImportFileRequest;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Basis.Test.ImportTests;

public class ResolveImportFileTest : BaseRestTest
{
    private const string Url = "api/imports";

    public ResolveImportFileTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestEch0157ReturnOk()
    {
        var request = new ResolveImportFileRequest
        {
            ImportType = SharedProto.ImportType.Ech157,
        };

        var httpResponse = await HttpUtil.RequestWithFileAndData(
            EchTestFiles.GetTestFilePath(EchTestFiles.Ech0157FileName),
            request,
            async content => await CantonAdminClient.PostAsync(Url, content));

        var response = await DeserializeHttpResponse(httpResponse);
        IgnoreGeneratedFields(response);
        response.MatchSnapshot(x => x.Contest.Id, x => x.Contest.EndOfTestingPhase);
    }

    [Fact]
    public async Task TestEch0159ReturnOk()
    {
        var request = new ResolveImportFileRequest
        {
            ImportType = SharedProto.ImportType.Ech159,
        };

        var httpResponse = await HttpUtil.RequestWithFileAndData(
            EchTestFiles.GetTestFilePath(EchTestFiles.Ech0159FileName),
            request,
            async content => await CantonAdminClient.PostAsync(Url, content));

        var response = await DeserializeHttpResponse(httpResponse);
        IgnoreGeneratedFields(response);
        response.MatchSnapshot(x => x.Contest.Id, x => x.Contest.EndOfTestingPhase);
    }

    [Fact]
    public async Task TestEch0159WithAllTypesReturnOk()
    {
        var request = new ResolveImportFileRequest
        {
            ImportType = SharedProto.ImportType.Ech159,
        };

        var httpResponse = await HttpUtil.RequestWithFileAndData(
            EchTestFiles.GetTestFilePath(EchTestFiles.Ech0159AllTypesFileName),
            request,
            async content => await CantonAdminClient.PostAsync(Url, content));

        var response = await DeserializeHttpResponse(httpResponse);
        IgnoreGeneratedFields(response);
        response.MatchSnapshot(x => x.Contest.Id, x => x.Contest.EndOfTestingPhase);
    }

    [Fact]
    public async Task TestInvalidEch0159ShouldThrow()
    {
        var request = new ResolveImportFileRequest
        {
            ImportType = SharedProto.ImportType.Ech159,
        };

        var httpResponse = await HttpUtil.RequestWithFileAndData(
            EchTestFiles.GetTestFilePath(EchTestFiles.Ech0159InvalidFileName),
            request,
            async content => await CantonAdminClient.PostAsync(Url, content));

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    protected override async Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        HttpResponseMessage? httpResponse = null;

        var request = new ResolveImportFileRequest
        {
            ImportType = SharedProto.ImportType.Ech159,
        };

        return await HttpUtil.RequestWithFileAndData(
            EchTestFiles.GetTestFilePath(EchTestFiles.Ech0159AllTypesFileName),
            request,
            async content => httpResponse = await httpClient.PostAsync(Url, content));
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return Roles.CantonAdmin;
        yield return Roles.ElectionAdmin;
        yield return Roles.ElectionSupporter;
    }

    private void IgnoreGeneratedFields(ContestImport contestImport)
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

    private async Task<ContestImport> DeserializeHttpResponse(HttpResponseMessage response)
    {
        var responseByteArray = await response.Content.ReadAsByteArrayAsync();
        return ContestImport.Parser.ParseFrom(responseByteArray);
    }
}
