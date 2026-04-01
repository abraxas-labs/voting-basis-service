// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data;
using Xunit;

namespace Voting.Basis.Test;

public class DatabaseSchemaTest : BaseTest
{
    public DatabaseSchemaTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public void EnsureNoPendingModelChanges()
    {
        GetService<DataContext>()
            .Database
            .HasPendingModelChanges()
            .Should()
            .BeFalse("Pending EF Core model changes detected, add a database migration");
    }
}
