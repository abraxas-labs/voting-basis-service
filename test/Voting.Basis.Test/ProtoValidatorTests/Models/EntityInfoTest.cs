// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Google.Protobuf.WellKnownTypes;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class EntityInfoTest : ProtoValidatorBaseTest<EntityInfo>
{
    public static EntityInfo NewValid(Action<EntityInfo>? action = null)
    {
        var entityInfo = new EntityInfo
        {
            CreatedOn = new DateTime(2020, 12, 22, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ModifiedOn = new DateTime(2020, 12, 22, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
        };

        action?.Invoke(entityInfo);
        return entityInfo;
    }

    protected override IEnumerable<EntityInfo> OkMessages()
    {
        yield return NewValid();
    }

    protected override IEnumerable<EntityInfo> NotOkMessages()
    {
        yield return NewValid(x => x.CreatedOn = null);
        yield return NewValid(x => x.ModifiedOn = null);
    }
}
