﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Data.Common;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.SQLite;
using Microsoft.Data.Entity.SQLite.Utilities;

// ReSharper disable once CheckNamespace

namespace Microsoft.Data.Entity
{
    public static class SQLiteDbContextOptionsExtensions
    {
        public static DbContextOptions UseSQLite([NotNull] this DbContextOptions options, [NotNull] string connectionString)
        {
            Check.NotNull(options, "options");
            Check.NotEmpty(connectionString, "connectionString");

            ((IDbContextOptionsExtensions)options)
                .AddOrUpdateExtension<SQLiteOptionsExtension>(x => x.ConnectionString = connectionString);

            return options;
        }

        public static DbContextOptions<T> UseSQLite<T>([NotNull] this DbContextOptions<T> options, [NotNull] string connectionString)
        {
            return (DbContextOptions<T>)UseSQLite((DbContextOptions)options, connectionString);
        }

        // TODO: Use SQLiteConnection instead of DbConnection?
        public static DbContextOptions UseSQLite([NotNull] this DbContextOptions options, [NotNull] DbConnection connection)
        {
            Check.NotNull(options, "options");
            Check.NotNull(connection, "connection");

            ((IDbContextOptionsExtensions)options)
                .AddOrUpdateExtension<SQLiteOptionsExtension>(x => x.Connection = connection);

            return options;
        }

        // TODO: Use SQLiteConnection instead of DbConnection?
        public static DbContextOptions<T> UseSQLite<T>([NotNull] this DbContextOptions<T> options, [NotNull] DbConnection connection)
        {
            return (DbContextOptions<T>)UseSQLite((DbContextOptions)options, connection);
        }
    }
}
