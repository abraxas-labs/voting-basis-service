// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Grpc.Core;
using Voting.Basis.Core.Services.Read;
using Voting.Lib.Iam.Authorization;
using Permissions = Voting.Basis.Core.Auth.Permissions;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.AdminManagementService.AdminManagementServiceBase;

namespace Voting.Basis.Services;

/// <summary>
/// Provides admin management services specifically used for service-to-service communication.
/// </summary>
public class AdminManagementService : ServiceBase
{
    private readonly DomainOfInfluenceReader _domainOfInfluenceReader;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminManagementService"/> class.
    /// </summary>
    /// <param name="domainOfInfluenceReader">The domain of influence reader.</param>
    /// <param name="mapper">The mapper..</param>
    public AdminManagementService(
        DomainOfInfluenceReader domainOfInfluenceReader,
        IMapper mapper)
    {
        _domainOfInfluenceReader = domainOfInfluenceReader;
        _mapper = mapper;
    }

    /// <summary>
    /// Requests the hierarchical list of all existing domain of influence.
    /// </summary>
    /// <param name="request">The request object.</param>
    /// <param name="context">The request context.</param>
    /// <returns>List of domain of influences.</returns>
    [AuthorizePermission(Permissions.DomainOfInfluenceHierarchy.ReadAll)]
    public override async Task<PoliticalDomainOfInfluenceHierarchies> GetPoliticalDomainOfInfluenceHierarchy(
        GetPoliticalDomainOfInfluenceHierarchyRequest request,
        ServerCallContext context)
    {
        return _mapper.Map<PoliticalDomainOfInfluenceHierarchies>(await _domainOfInfluenceReader.ListAll());
    }
}
