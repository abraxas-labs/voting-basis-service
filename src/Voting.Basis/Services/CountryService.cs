// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Services.V1.Requests;
using AutoMapper;
using Grpc.Core;
using Voting.Basis.Ech.Utils;
using Voting.Lib.Iam.Authorization;
using Permissions = Voting.Basis.Core.Auth.Permissions;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.CountryService.CountryServiceBase;

namespace Voting.Basis.Services;

public class CountryService : ServiceBase
{
    private readonly IMapper _mapper;

    public CountryService(IMapper mapper)
    {
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.Country.Read)]
    public override Task<Countries> List(ListCountriesRequest request, ServerCallContext context)
    {
        var countries = CountryUtils.GetAll();
        return Task.FromResult(_mapper.Map<Countries>(countries));
    }
}
