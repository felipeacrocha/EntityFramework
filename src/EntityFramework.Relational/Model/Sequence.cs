// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity.Relational.Utilities;

namespace Microsoft.Data.Entity.Relational.Model
{
    // TODO: Consider adding more validation.
    public class Sequence
    {
        private readonly SchemaQualifiedName _name;
        private int _incrementBy = 1;
        private string _dataType = "BIGINT";

        public Sequence(SchemaQualifiedName name)
        {
            _name = name;
        }

        public Sequence(SchemaQualifiedName name, [NotNull] string dataType, int startWith, int incrementBy)
        {
            Check.NotEmpty(dataType, "dataType");

            _name = name;
            _dataType = dataType;
            StartWith = startWith;
            _incrementBy = incrementBy;
        }

        public virtual SchemaQualifiedName Name
        {
            get { return _name; }
        }

        public virtual int StartWith { get; set; }

        public virtual int IncrementBy
        {
            get { return _incrementBy; }
            set { _incrementBy = value; }
        }

        public virtual string DataType
        {
            get { return _dataType; }

            [param: NotNull]
            set
            {
                Check.NotEmpty(value, "value");

                _dataType = value;
            }
        }

        public virtual Sequence Clone([NotNull] CloneContext cloneContext)
        {
            Check.NotNull(cloneContext, "cloneContext");

            return new Sequence(Name, DataType, StartWith, IncrementBy);
        }
    }
}
