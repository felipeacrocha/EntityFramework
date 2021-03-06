// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity.Identity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Relational;
using Microsoft.Data.Entity.SqlServer.Utilities;
using Microsoft.Data.Entity.Storage;

namespace Microsoft.Data.Entity.SqlServer
{
    public class SqlServerDataStoreServices : DataStoreServices
    {
        private readonly SqlServerDataStore _store;
        private readonly SqlServerDataStoreCreator _creator;
        private readonly SqlServerConnection _connection;
        private readonly SqlServerValueGeneratorCache _valueGeneratorCache;
        private readonly RelationalDatabase _database;
        private readonly ModelBuilderFactory _modelBuilderFactory;

        public SqlServerDataStoreServices(
            [NotNull] SqlServerDataStore store,
            [NotNull] SqlServerDataStoreCreator creator,
            [NotNull] SqlServerConnection connection,
            [NotNull] SqlServerValueGeneratorCache valueGeneratorCache,
            [NotNull] RelationalDatabase database,
            [NotNull] ModelBuilderFactory modelBuilderFactory)
        {
            Check.NotNull(store, "store");
            Check.NotNull(creator, "creator");
            Check.NotNull(connection, "connection");
            Check.NotNull(valueGeneratorCache, "valueGeneratorCache");
            Check.NotNull(database, "database");
            Check.NotNull(modelBuilderFactory, "modelBuilderFactory");

            _store = store;
            _creator = creator;
            _connection = connection;
            _valueGeneratorCache = valueGeneratorCache;
            _database = database;
            _modelBuilderFactory = modelBuilderFactory;
        }

        public override DataStore Store
        {
            get { return _store; }
        }

        public override DataStoreCreator Creator
        {
            get { return _creator; }
        }

        public override DataStoreConnection Connection
        {
            get { return _connection; }
        }

        public override ValueGeneratorCache ValueGeneratorCache
        {
            get { return _valueGeneratorCache; }
        }

        public override Database Database
        {
            get { return _database; }
        }

        public override IModelBuilderFactory ModelBuilderFactory
        {
            get { return _modelBuilderFactory; }
        }
    }
}
