// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Services.V1.Models;
using Voting.Lib.Testing.Validation;

namespace Voting.Basis.Test.ProtoValidatorTests.Models;

public class EntityOrdersTest : ProtoValidatorBaseTest<EntityOrders>
{
    public static EntityOrders NewValid(Action<EntityOrders>? action = null)
    {
        var entityOrders = new EntityOrders
        {
            Orders = { EntityOrderTest.NewValid() },
        };

        action?.Invoke(entityOrders);
        return entityOrders;
    }

    protected override IEnumerable<EntityOrders> OkMessages()
    {
        yield return NewValid();
        yield return NewValid(x => x.Orders.Clear());
    }

    protected override IEnumerable<EntityOrders> NotOkMessages()
    {
        yield break;
    }
}
