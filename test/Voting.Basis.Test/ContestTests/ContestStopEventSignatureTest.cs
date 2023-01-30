// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using FluentAssertions;
using Google.Protobuf;
using Voting.Basis.Core.Jobs;
using Voting.Basis.EventSignature;
using Voting.Basis.Test.MockedData;
using Voting.Basis.Test.Mocks;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Basis.Test.ContestTests;

public class ContestStopEventSignatureTest : BaseTest
{
    private static readonly Guid ContestId = Guid.Parse(ContestMockedData.IdGossau);
    private readonly ContestCache _contestCache;
    private readonly IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey> _asymmetricAlgorithmAdapter;

    public ContestStopEventSignatureTest(TestApplicationFactory factory)
        : base(factory)
    {
        _contestCache = GetService<ContestCache>();
        _asymmetricAlgorithmAdapter = GetService<IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey>>();
    }

    public override Task InitializeAsync()
    {
        return ContestMockedData.Seed(RunScoped);
    }

    public override Task DisposeAsync()
    {
        AdjustableMockedClock.OverrideUtcNow = null;
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ShouldStopSignatureWhenKeyExpired()
    {
        var key = _asymmetricAlgorithmAdapter.CreateRandomPrivateKey();
        var entry = _contestCache.Get(ContestId)!;

        AdjustableMockedClock.OverrideUtcNow = MockedClock.GetDate(-10);
        entry.KeyData = new ContestCacheEntryKeyData(key, MockedClock.GetDate(-10, -9), MockedClock.GetDate(-10, -1));

        var stopContestEventSignatureJob = GetService<StopContestEventSignatureJob>();
        await stopContestEventSignatureJob.Run(CancellationToken.None);

        var ev = EventPublisherMock.GetSinglePublishedEvent<EventSignaturePublicKeyDeleted, EventSignaturePublicKeyMetadata>();
        ev.Data.KeyId.Should().Be(key.Id);
        ev.Data.ContestId.Should().Be(ContestId.ToString());
        ev.Data.AuthenticationTag.Should().NotBeEmpty();
        ev.Metadata!.HsmSignature.Should().NotBeEmpty();

        ev.Data.KeyId = string.Empty;
        ev.Data.AuthenticationTag = ByteString.Empty;
        ev.Metadata.HsmSignature = ByteString.Empty;
        ev.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldNotStopSignatureWhenKeyNotExpired()
    {
        var key = _asymmetricAlgorithmAdapter.CreateRandomPrivateKey();
        var entry = _contestCache.Get(ContestId)!;

        AdjustableMockedClock.OverrideUtcNow = MockedClock.GetDate(-10);
        entry.KeyData = new ContestCacheEntryKeyData(key, MockedClock.GetDate(-10, -9), MockedClock.GetDate(-10));

        var stopContestEventSignatureJob = GetService<StopContestEventSignatureJob>();
        await stopContestEventSignatureJob.Run(CancellationToken.None);

        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeyDeleted>().Should().BeEmpty();
    }
}
