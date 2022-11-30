// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Voting.Basis.Core.Auth;
using Voting.Basis.Core.Export.Generators;
using Voting.Basis.Core.Export.Models;
using Voting.Lib.Iam.Store;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository;

namespace Voting.Basis.Core.Export;

public class ExportService
{
    private readonly IAuth _auth;
    private readonly Dictionary<string, IExportGenerator> _exportGenerators;
    private readonly Dictionary<string, IExportsGenerator> _multipleExportsGenerators;

    public ExportService(IAuth auth, IEnumerable<IExportGenerator> exportGenerators, IEnumerable<IExportsGenerator> multipleExportsGenerators)
    {
        _auth = auth;
        _exportGenerators = exportGenerators.ToDictionary(x => x.Template.Key);
        _multipleExportsGenerators = multipleExportsGenerators.ToDictionary(x => x.Template.Key);
    }

    public IReadOnlyCollection<TemplateModel> GetExportTemplates(VotingApp generatedBy, EntityType? entityType)
    {
        _auth.EnsureAdminOrElectionAdmin();
        return entityType.HasValue
            ? TemplateRepository.GetByGeneratorAndEntityType(generatedBy, entityType.Value)
            : TemplateRepository.GetByGenerator(generatedBy);
    }

    public bool IsMultipleFileExport(string key)
        => _multipleExportsGenerators.ContainsKey(key);

    public async IAsyncEnumerable<ExportFile> GenerateExports(
        string templateKey,
        Guid id)
    {
        _auth.EnsureAdminOrElectionAdmin();

        if (_exportGenerators.TryGetValue(templateKey, out var generator))
        {
            yield return await generator.GenerateExport(id);
            yield break;
        }

        if (_multipleExportsGenerators.TryGetValue(templateKey, out var multiGenerator))
        {
            await foreach (var file in multiGenerator.GenerateExports(id))
            {
                yield return file;
            }

            yield break;
        }

        throw new ValidationException($"Export template for key {templateKey} does not exist");
    }
}
