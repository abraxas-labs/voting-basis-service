// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Voting.Basis.Core.Configuration;
using Voting.Lib.ObjectStorage;

namespace Voting.Basis.Core.ObjectStorage;

public class DomainOfInfluenceLogoStorage : BucketObjectStorageClient
{
    public DomainOfInfluenceLogoStorage(PublisherConfig config, IObjectStorageClient client)
        : base(config.DomainOfInfluenceLogos, client)
    {
    }

    public Task Store(
        Guid doiId,
        string objectName,
        Stream data,
        long dataLength,
        string contentType,
        CancellationToken ct = default)
    {
        var meta = new Dictionary<string, string>
        {
            [nameof(doiId)] = doiId.ToString(),
        };
        return Store(
            objectName,
            data,
            contentType,
            dataLength,
            meta,
            ct);
    }
}
