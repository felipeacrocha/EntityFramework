// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.Data.Entity.ChangeTracking;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Relational.Utilities;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Relational.Update
{
    public class ColumnModification
    {
        private readonly StateEntry _stateEntry;
        private readonly IProperty _property;
        private readonly string _columnName;
        private readonly LazyRef<string> _parameterName;
        private readonly LazyRef<string> _originalParameterName;
        private readonly LazyRef<string> _outputParameterName;
        private readonly bool _isRead;
        private readonly bool _isWrite;
        private readonly bool _isKey;
        private readonly bool _isCondition;

        /// <summary>
        ///     This constructor is intended only for use when creating test doubles that will override members
        ///     with mocked or faked behavior. Use of this constructor for other purposes may result in unexpected
        ///     behavior including but not limited to throwing <see cref="NullReferenceException" />.
        /// </summary>
        protected ColumnModification()
        {
        }

        public ColumnModification(
            [NotNull] StateEntry stateEntry,
            [NotNull] IProperty property,
            [NotNull] ParameterNameGenerator parameterNameGenerator,
            bool isRead,
            bool isWrite,
            bool isKey,
            bool isCondition)
        {
            Check.NotNull(stateEntry, "stateEntry");
            Check.NotNull(property, "property");
            Check.NotNull(parameterNameGenerator, "parameterNameGenerator");

            _stateEntry = stateEntry;
            _property = property;
            _columnName = property.ColumnName();
            _parameterName = isWrite
                ? new LazyRef<string>(parameterNameGenerator.GenerateNext)
                : new LazyRef<string>((string)null);
            _originalParameterName = isCondition
                ? new LazyRef<string>(parameterNameGenerator.GenerateNext)
                : new LazyRef<string>((string)null);
            _outputParameterName = isRead
                ? new LazyRef<string>(parameterNameGenerator.GenerateNext)
                : new LazyRef<string>((string)null);
            _isRead = isRead;
            _isWrite = isWrite;
            _isKey = isKey;
            _isCondition = isCondition;
        }

        public virtual StateEntry StateEntry
        {
            get { return _stateEntry; }
        }

        public virtual IProperty Property
        {
            get { return _property; }
        }

        public virtual bool IsRead
        {
            get { return _isRead; }
        }

        public virtual bool IsWrite
        {
            get { return _isWrite; }
        }

        public virtual bool IsCondition
        {
            get { return _isCondition; }
        }

        public virtual bool IsKey
        {
            get { return _isKey; }
        }

        public virtual string ParameterName
        {
            get { return _parameterName.Value; }
        }

        public virtual string OriginalParameterName
        {
            get { return _originalParameterName.Value; }
        }

        public virtual string OutputParameterName
        {
            get { return _outputParameterName.Value; }
        }

        public virtual string ColumnName
        {
            get { return _columnName; }
        }

        public virtual object OriginalValue
        {
            get { return StateEntry.OriginalValues.CanStoreValue(Property) ? StateEntry.OriginalValues[Property] : Value; }
        }

        public virtual object Value
        {
            get { return StateEntry[Property]; }
            [param: CanBeNull] set { StateEntry[Property] = value; }
        }
    }
}
