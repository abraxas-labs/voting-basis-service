// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Data;

namespace Voting.Basis.Test.MockedData;

public static class DatabaseUtil
{
    private static bool _migrated;

    public static void Truncate(DataContext db)
    {
        // on the first run, we migrate the database to ensure the same structure as the "real" DB
        if (!_migrated)
        {
            db.Database.Migrate();
            _migrated = true;
        }

        // truncating tables is much faster than recreating the database
        var tableNames = db.Model.GetEntityTypes().Select(m => $@"""{m.GetTableName()}""");
        db.Database.ExecuteSqlRaw($"TRUNCATE {string.Join(",", tableNames)} CASCADE");
    }
}
