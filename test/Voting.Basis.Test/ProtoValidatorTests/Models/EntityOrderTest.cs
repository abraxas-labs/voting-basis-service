// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class EntityOrderTest : ProtoValidatorBaseTest<EntityOrder>
{
    public static EntityOrder NewValid(Action<EntityOrder>? action = null)
    {
        var entityOrder = new EntityOrder
        {
            Id = "da36912c-7eaf-43fe-86d4-70c816f17c5a",
            Position = 27,
        };

        action?.Invoke(entityOrder);
        return entityOrder;
    }

    protected override IEnumerable<EntityOrder> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Position = 1);
        yield return NewValid(x => x.Position = 100);
    }

    protected override IEnumerable<EntityOrder> NotOkMessages()
    {
        yield return NewValid(x => x.Id = "invalid-guid");
        yield return NewValid(x => x.Id = string.Empty);
        yield return NewValid(x => x.Position = 0);
        yield return NewValid(x => x.Position = 101);
    }
}
