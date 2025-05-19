// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Services.Permission;
using Voting.Basis.Data;
using Voting.Basis.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Basis.Core.Services.Read;

public class MajorityElectionReader : PoliticalBusinessReader<MajorityElection>
{
    private readonly IDbRepository<DataContext, MajorityElectionCandidate> _candidateRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElection> _secondaryMajorityElectionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionBallotGroupEntry> _ballotGroupEntryRepo;

    public MajorityElectionReader(
        IDbRepository<DataContext, MajorityElection> repo,
        IDbRepository<DataContext, MajorityElectionCandidate> candidateRepo,
        IDbRepository<DataContext, SecondaryMajorityElection> secondaryMajorityElectionRepo,
        IDbRepository<DataContext, MajorityElectionBallotGroupEntry> ballotGroupEntryRepo,
        IAuth auth,
        PermissionService permissionService)
        : base(auth, permissionService, repo)
    {
        _candidateRepo = candidateRepo;
        _secondaryMajorityElectionRepo = secondaryMajorityElectionRepo;
        _ballotGroupEntryRepo = ballotGroupEntryRepo;
    }

    public async Task<IEnumerable<MajorityElectionCandidate>> GetCandidates(Guid electionId)
    {
        var majorityElection = await Repo.Query()
            .Include(me => me.MajorityElectionCandidates)
            .FirstOrDefaultAsync(me => me.Id == electionId)
            ?? throw new EntityNotFoundException(nameof(MajorityElection), electionId);

        await EnsureAllowedToRead(majorityElection.DomainOfInfluenceId);
        return majorityElection.MajorityElectionCandidates.OrderBy(c => c.Position);
    }

    public async Task<MajorityElectionCandidate> GetCandidate(Guid candidateId)
    {
        var candidate = await _candidateRepo.Query()
            .Include(c => c.MajorityElection)
            .FirstOrDefaultAsync(c => c.Id == candidateId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionCandidate), candidateId);

        await EnsureAllowedToRead(candidate.MajorityElection.DomainOfInfluenceId);
        return candidate;
    }

    public async Task<IEnumerable<SecondaryMajorityElection>> GetSecondaryMajorityElections(Guid electionId)
    {
        var majorityElection = await Repo.Query()
            .Include(me => me.SecondaryMajorityElections)
            .FirstOrDefaultAsync(me => me.Id == electionId)
            ?? throw new EntityNotFoundException(nameof(SecondaryMajorityElection), electionId);

        await EnsureAllowedToRead(majorityElection.DomainOfInfluenceId);
        return majorityElection.SecondaryMajorityElections.OrderBy(c => c.PoliticalBusinessNumber);
    }

    public async Task<SecondaryMajorityElection> GetSecondaryMajorityElection(Guid secondaryMajorityElectionId)
    {
        var secondaryMajorityElection = await _secondaryMajorityElectionRepo.Query()
            .Include(c => c.PrimaryMajorityElection)
            .FirstOrDefaultAsync(c => c.Id == secondaryMajorityElectionId)
            ?? throw new EntityNotFoundException(nameof(SecondaryMajorityElection), secondaryMajorityElectionId);

        await EnsureAllowedToRead(secondaryMajorityElection.PrimaryMajorityElection.DomainOfInfluenceId);
        return secondaryMajorityElection;
    }

    public async Task<IEnumerable<SecondaryMajorityElectionCandidate>> GetSecondaryMajorityElectionCandidates(
        Guid secondaryMajorityElectionId)
    {
        var secondaryMajorityElection = await _secondaryMajorityElectionRepo.Query()
            .Include(sme => sme.Candidates)
            .Include(sme => sme.PrimaryMajorityElection)
            .FirstOrDefaultAsync(me => me.Id == secondaryMajorityElectionId)
            ?? throw new EntityNotFoundException(nameof(SecondaryMajorityElectionCandidate), secondaryMajorityElectionId);

        await EnsureAllowedToRead(secondaryMajorityElection.PrimaryMajorityElection.DomainOfInfluenceId);
        return secondaryMajorityElection.Candidates.OrderBy(c => c.Position).ThenBy(c => c.LastName).ThenBy(c => c.FirstName);
    }

    public async Task<IEnumerable<MajorityElectionBallotGroup>> GetBallotGroups(Guid electionId)
    {
        var majorityElection = await Repo.Query()
            .AsSplitQuery()
            .Include(me => me.BallotGroups)
            .ThenInclude(bg => bg.Entries)
            .FirstOrDefaultAsync(me => me.Id == electionId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionBallotGroup), electionId);

        await EnsureAllowedToRead(majorityElection.DomainOfInfluenceId);

        // with ef 5 these sorts can be inlined
        foreach (var ballotGroup in majorityElection.BallotGroups)
        {
            ballotGroup.Entries = ballotGroup.Entries.OrderByDescending(x => x.PrimaryMajorityElectionId.HasValue).ToList();
        }

        return majorityElection.BallotGroups.OrderBy(bg => bg.Position);
    }

    public async Task<IEnumerable<MajorityElectionBallotGroupEntry>> GetBallotGroupEntriesWithCandidates(Guid ballotGroupId)
    {
        var majorityElection = await Repo.Query()
            .FirstOrDefaultAsync(me => me.BallotGroups.Any(bg => bg.Id == ballotGroupId))
            ?? throw new EntityNotFoundException(ballotGroupId);
        await EnsureAllowedToRead(majorityElection.DomainOfInfluenceId);

        return await _ballotGroupEntryRepo.Query()
            .Include(e => e.Candidates)
            .Where(e => e.BallotGroupId == ballotGroupId)
            .OrderByDescending(x => x.PrimaryMajorityElectionId.HasValue)
            .ToListAsync();
    }

    protected override async Task<MajorityElection> QueryById(Guid id)
    {
        return await Repo.Query()
            .IgnoreQueryFilters() // Deleted DOI should still work
            .Include(v => v.DomainOfInfluence)
            .FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new EntityNotFoundException(id);
    }

    private async Task EnsureAllowedToRead(Guid majorityElectionDomainOfInfluenceId)
    {
        await PermissionService.EnsureIsOwnerOfDomainOfInfluenceOrHasAdminPermissions(majorityElectionDomainOfInfluenceId, true);
    }
}
