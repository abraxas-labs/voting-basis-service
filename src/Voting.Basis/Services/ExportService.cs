// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Basis.Services.V1.Models;
using Abraxas.Voting.Basis.Services.V1.Requests;
using Abraxas.Voting.Basis.Shared.V1;
using AutoMapper;
using Grpc.Core;
using Voting.Lib.Iam.Authorization;
using Voting.Lib.VotingExports.Models;
using Permissions = Voting.Basis.Core.Auth.Permissions;
using ServiceBase = Abraxas.Voting.Basis.Services.V1.ExportService.ExportServiceBase;

namespace Voting.Basis.Services;

public class ExportService : ServiceBase
{
    private readonly Core.Export.ExportService _exportService;
    private readonly IMapper _mapper;

    public ExportService(
        Core.Export.ExportService exportService,
        IMapper mapper)
    {
        _exportService = exportService;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.Export.ExportData)]
    public override Task<ExportTemplates> GetTemplates(GetExportTemplatesRequest request, ServerCallContext context)
    {
        EntityType? entityType = request.EntityType == ExportEntityType.Unspecified ? null : _mapper.Map<EntityType>(request.EntityType);
        var templates = _exportService.GetExportTemplates(_mapper.Map<VotingApp>(request.Generator), entityType);
        return Task.FromResult(_mapper.Map<ExportTemplates>(templates));
    }
}
