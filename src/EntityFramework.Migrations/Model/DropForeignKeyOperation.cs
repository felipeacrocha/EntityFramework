// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity.Migrations.Utilities;
using Microsoft.Data.Entity.Relational;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Migrations.Model
{
    public class DropForeignKeyOperation : MigrationOperation
    {
        private readonly SchemaQualifiedName _tableName;
        private readonly string _foreignKeyName;

        public DropForeignKeyOperation(SchemaQualifiedName tableName, [NotNull] string foreignKeyName)
        {
            Check.NotEmpty(foreignKeyName, "foreignKeyName");

            _tableName = tableName;
            _foreignKeyName = foreignKeyName;
        }

        public virtual SchemaQualifiedName TableName
        {
            get { return _tableName; }
        }

        public virtual string ForeignKeyName
        {
            get { return _foreignKeyName; }
        }

        public override bool IsDestructiveChange
        {
            get { return true; }
        }

        public override void Accept<TVisitor, TContext>(TVisitor visitor, TContext context)
        {
            Check.NotNull(visitor, "visitor");
            Check.NotNull(context, "context");

            visitor.Visit(this, context);
        }

        public override void GenerateSql(MigrationOperationSqlGenerator generator, IndentedStringBuilder stringBuilder)
        {
            Check.NotNull(generator, "generator");
            Check.NotNull(stringBuilder, "stringBuilder");

            generator.Generate(this, stringBuilder);
        }

        public override void GenerateCode(MigrationCodeGenerator generator, IndentedStringBuilder stringBuilder)
        {
            Check.NotNull(generator, "generator");
            Check.NotNull(stringBuilder, "stringBuilder");

            generator.Generate(this, stringBuilder);
        }
    }
}
