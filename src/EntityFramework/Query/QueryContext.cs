// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using JetBrains.Annotations;
using Microsoft.Data.Entity.ChangeTracking;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.Logging;

namespace Microsoft.Data.Entity.Query
{
    public class QueryContext
    {
        private readonly ILogger _logger;
        private readonly IQueryBuffer _queryBuffer;
        private readonly StateManager _stateManager;

        public QueryContext(
            [NotNull] ILogger logger,
            [NotNull] IQueryBuffer queryBuffer,
            [NotNull] StateManager stateManager)
        {
            Check.NotNull(logger, "logger");
            Check.NotNull(queryBuffer, "queryBuffer");
            Check.NotNull(stateManager, "stateManager");

            _logger = logger;
            _queryBuffer = queryBuffer;
            _stateManager = stateManager;
        }

        public virtual ILogger Logger
        {
            get { return _logger; }
        }

        public virtual IQueryBuffer QueryBuffer
        {
            get { return _queryBuffer; }
        }

        public virtual StateManager StateManager
        {
            get { return _stateManager; }
        }

        public virtual CancellationToken CancellationToken { get; set; }
    }
}
