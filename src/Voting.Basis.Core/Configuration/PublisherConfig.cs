// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Cryptography.Configuration;
using Voting.Lib.Ech.Configuration;
using Voting.Lib.ObjectStorage.Config;
using Voting.Lib.Scheduler;

namespace Voting.Basis.Core.Configuration;

public class PublisherConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether detailed errors are enabled. Should not be enabled in production environments,
    /// as this could expose information about the internals of this service.
    /// </summary>
    public bool EnableDetailedErrors { get; set; }

    public bool EnableGrpcWeb { get; set; } // this should only be enabled for testing purposes

    public bool EnablePkcs11Mock { get; set; }

    public ContestConfig Contest { get; set; } = new();

    public VoteConfig Vote { get; set; } = new();

    public EventSignatureConfig EventSignature { get; set; } = new();

    public Pkcs11Config Pkcs11 { get; set; } = new();

    public MachineConfig Machine { get; set; } = new();

    public JobConfig ContestStateEndTestingPhaseJob { get; set; } = new()
    {
        Interval = TimeSpan.FromMinutes(5),
    };

    public JobConfig ContestStateSetPastJob { get; set; } = new()
    {
        Interval = TimeSpan.FromMinutes(5),
    };

    public JobConfig ContestStateArchiveJob { get; set; } = new()
    {
        Interval = TimeSpan.FromMinutes(15),
    };

    public JobConfig ActivateCountingCirclesMergeJob { get; set; } = new()
    {
        Interval = TimeSpan.FromHours(1),
    };

    public JobConfig ActivateCountingCircleEVotingJob { get; set; } = new()
    {
        Interval = TimeSpan.FromHours(1),
    };

    public JobConfig StopContestEventSignatureJob { get; set; } = new()
    {
        Interval = TimeSpan.FromMinutes(15),
    };

    public EchConfig Ech { get; set; } = new(typeof(AppConfig).Assembly);

    public ObjectStorageConfig ObjectStorage { get; set; } = new();

    public ObjectStorageBucketConfig DomainOfInfluenceLogos { get; set; } = new()
    {
        BucketName = "voting",
        ObjectPrefix = "domain-of-influence-logos/",
        DefaultPublicDownloadLinkTtl = TimeSpan.FromMinutes(1),
    };

    public HashSet<string> AllowedLogoFileExtensions { get; set; } = new();
}
