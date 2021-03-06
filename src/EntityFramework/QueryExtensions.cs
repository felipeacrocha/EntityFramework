﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;

namespace Microsoft.Data.Entity
{
    public static class QueryExtensions
    {
        public static TProperty Property<TProperty>([NotNull] this object entity, [NotNull] string propertyName)
        {
            throw new InvalidOperationException(Strings.PropertyExtensionInvoked);
        }
    }
}
