// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Relational.Update;
using Microsoft.Data.Entity.Relational.Utilities;

namespace Microsoft.Data.Entity.Relational
{
    public abstract class SqlGenerator
    {
        public virtual void AppendInsertOperation(
            [NotNull] StringBuilder commandStringBuilder,
            [NotNull] ModificationCommand command)
        {
            Check.NotNull(command, "command");

            var schemaQualifiedName = command.SchemaQualifiedName;
            var operations = command.ColumnModifications;

            var writeOperations = operations.Where(o => o.IsWrite).ToArray();
            var readOperations = operations.Where(o => o.IsRead).ToArray();

            AppendInsertCommand(commandStringBuilder, schemaQualifiedName, writeOperations);

            if (readOperations.Length > 0)
            {
                var keyOperations = operations.Where(o => o.IsKey).ToArray();

                AppendSelectAffectedCommand(commandStringBuilder, schemaQualifiedName, readOperations, keyOperations);
            }
            else
            {
                AppendSelectAffectedCountCommand(commandStringBuilder, schemaQualifiedName);
            }
        }

        public virtual void AppendUpdateOperation(
            [NotNull] StringBuilder commandStringBuilder,
            [NotNull] ModificationCommand command)
        {
            Check.NotNull(command, "command");

            var schemaQualifiedName = command.SchemaQualifiedName;
            var operations = command.ColumnModifications;

            var writeOperations = operations.Where(o => o.IsWrite).ToArray();
            var conditionOperations = operations.Where(o => o.IsCondition).ToArray();
            var readOperations = operations.Where(o => o.IsRead).ToArray();

            AppendUpdateCommand(commandStringBuilder, schemaQualifiedName, writeOperations, conditionOperations);

            if (readOperations.Length > 0)
            {
                var keyOperations = operations.Where(o => o.IsKey).ToArray();

                AppendSelectAffectedCommand(commandStringBuilder, schemaQualifiedName, readOperations, keyOperations);
            }
            else
            {
                AppendSelectAffectedCountCommand(commandStringBuilder, schemaQualifiedName);
            }
        }

        public virtual void AppendDeleteOperation(
            [NotNull] StringBuilder commandStringBuilder,
            [NotNull] ModificationCommand command)
        {
            Check.NotNull(command, "command");
            
            var schemaQualifiedName = command.SchemaQualifiedName;
            var conditionOperations = command.ColumnModifications.Where(o => o.IsCondition).ToArray();

            AppendDeleteCommand(commandStringBuilder, schemaQualifiedName, conditionOperations);

            AppendSelectAffectedCountCommand(commandStringBuilder, schemaQualifiedName);
        }

        public virtual void AppendInsertCommand(
            [NotNull] StringBuilder commandStringBuilder,
            SchemaQualifiedName schemaQualifiedName,
            [NotNull] IReadOnlyList<ColumnModification> writeOperations)
        {
            Check.NotNull(commandStringBuilder, "commandStringBuilder");
            Check.NotNull(writeOperations, "writeOperations");

            AppendInsertCommandHeader(commandStringBuilder, schemaQualifiedName, writeOperations);
            AppendValuesHeader(commandStringBuilder, writeOperations);
            AppendValues(commandStringBuilder, writeOperations);
            commandStringBuilder.Append(BatchCommandSeparator).AppendLine();
        }

        public virtual void AppendUpdateCommand(
            [NotNull] StringBuilder commandStringBuilder,
            SchemaQualifiedName schemaQualifiedName,
            [NotNull] IReadOnlyList<ColumnModification> writeOperations,
            [NotNull] IReadOnlyList<ColumnModification> conditionOperations)
        {
            Check.NotNull(commandStringBuilder, "commandStringBuilder");
            Check.NotNull(writeOperations, "writeOperations");
            Check.NotNull(conditionOperations, "conditionOperations");

            AppendUpdateCommandHeader(commandStringBuilder, schemaQualifiedName, writeOperations);
            AppendWhereClause(commandStringBuilder, conditionOperations);
            commandStringBuilder.Append(BatchCommandSeparator).AppendLine();
        }

        public virtual void AppendDeleteCommand(
            [NotNull] StringBuilder commandStringBuilder,
            SchemaQualifiedName schemaQualifiedName,
            [NotNull] IReadOnlyList<ColumnModification> conditionOperations)
        {
            Check.NotNull(commandStringBuilder, "commandStringBuilder");
            Check.NotNull(conditionOperations, "conditionOperations");

            AppendDeleteCommandHeader(commandStringBuilder, schemaQualifiedName);
            AppendWhereClause(commandStringBuilder, conditionOperations);
            commandStringBuilder.Append(BatchCommandSeparator).AppendLine();
        }

        public abstract void AppendSelectAffectedCountCommand(
            [NotNull] StringBuilder commandStringBuilder,
            SchemaQualifiedName schemaQualifiedName);

        public virtual void AppendSelectAffectedCommand(
            [NotNull] StringBuilder commandStringBuilder,
            SchemaQualifiedName schemaQualifiedName,
            [NotNull] IReadOnlyList<ColumnModification> readOperations,
            [NotNull] IReadOnlyList<ColumnModification> conditionOperations)
        {
            Check.NotNull(commandStringBuilder, "commandStringBuilder");
            Check.NotNull(readOperations, "readOperations");
            Check.NotNull(conditionOperations, "conditionOperations");

            AppendSelectCommandHeader(commandStringBuilder, readOperations);
            AppendFromClause(commandStringBuilder, schemaQualifiedName);
            // TODO: there is no notion of operator - currently all the where conditions check equality
            AppendWhereAffectedClause(commandStringBuilder, conditionOperations);
            commandStringBuilder.Append(BatchCommandSeparator).AppendLine();
        }

        protected virtual void AppendInsertCommandHeader(
            [NotNull] StringBuilder commandStringBuilder,
            SchemaQualifiedName schemaQualifiedName,
            [NotNull] IReadOnlyList<ColumnModification> operations)
        {
            Check.NotNull(commandStringBuilder, "commandStringBuilder");
            Check.NotNull(operations, "operations");

            commandStringBuilder
                .Append("INSERT INTO ")
                .Append(DelimitIdentifier(schemaQualifiedName));

            if (operations.Count > 0)
            {
                commandStringBuilder
                    .Append(" (")
                    .AppendJoin(operations.Select(o => DelimitIdentifier(o.ColumnName)))
                    .Append(")");
            }
        }

        protected virtual void AppendDeleteCommandHeader(
            [NotNull] StringBuilder commandStringBuilder,
            SchemaQualifiedName schemaQualifiedName)
        {
            Check.NotNull(commandStringBuilder, "commandStringBuilder");

            commandStringBuilder
                .Append("DELETE FROM ")
                .Append(DelimitIdentifier(schemaQualifiedName));
        }

        protected virtual void AppendUpdateCommandHeader(
            [NotNull] StringBuilder commandStringBuilder,
            SchemaQualifiedName schemaQualifiedName,
            [NotNull] IReadOnlyList<ColumnModification> operations)
        {
            Check.NotNull(commandStringBuilder, "commandStringBuilder");
            Check.NotNull(operations, "operations");

            commandStringBuilder
                .Append("UPDATE ")
                .Append(DelimitIdentifier(schemaQualifiedName))
                .Append(" SET ")
                .AppendJoin(
                    operations,
                    (sb, v) => sb.Append(DelimitIdentifier(v.ColumnName)).Append(" = ").Append(v.ParameterName), ", ");
        }

        protected virtual void AppendSelectCommandHeader(
            [NotNull] StringBuilder commandStringBuilder,
            [NotNull] IReadOnlyList<ColumnModification> operations)
        {
            Check.NotNull(commandStringBuilder, "commandStringBuilder");
            Check.NotNull(operations, "operations");

            commandStringBuilder
                .Append("SELECT ")
                .AppendJoin(operations.Select(c => DelimitIdentifier(c.ColumnName)));
        }

        protected virtual void AppendFromClause(
            [NotNull] StringBuilder commandStringBuilder,
            SchemaQualifiedName schemaQualifiedName)
        {
            Check.NotNull(commandStringBuilder, "commandStringBuilder");

            commandStringBuilder
                .AppendLine()
                .Append("FROM ")
                .Append(DelimitIdentifier(schemaQualifiedName));
        }

        protected virtual void AppendValuesHeader(
            [NotNull] StringBuilder commandStringBuilder,
            [NotNull] IReadOnlyList<ColumnModification> operations)
        {
            Check.NotNull(commandStringBuilder, "commandStringBuilder");
            Check.NotNull(operations, "operations");

            commandStringBuilder.AppendLine();
            commandStringBuilder.Append(operations.Count > 0 ? "VALUES " : "DEFAULT VALUES");
        }

        protected virtual void AppendValues(
            [NotNull] StringBuilder commandStringBuilder,
            [NotNull] IReadOnlyList<ColumnModification> operations)
        {
            Check.NotNull(commandStringBuilder, "commandStringBuilder");
            Check.NotNull(operations, "operations");

            if (operations.Count > 0)
            {
                commandStringBuilder
                    .Append("(")
                    .AppendJoin(operations.Select(o => o.ParameterName))
                    .Append(")");
            }
        }

        protected virtual void AppendWhereClause(
            [NotNull] StringBuilder commandStringBuilder,
            [NotNull] IReadOnlyList<ColumnModification> operations)
        {
            Check.NotNull(commandStringBuilder, "commandStringBuilder");
            Check.NotNull(operations, "operations");

            if (operations.Count > 0)
            {
                commandStringBuilder
                    .AppendLine()
                    .Append("WHERE ")
                    .AppendJoin(operations, (sb, v) => AppendWhereCondition(sb, v, useOriginalValue: true), " AND ");
            }
        }

        protected virtual void AppendWhereAffectedClause(
            [NotNull] StringBuilder commandStringBuilder,
            [NotNull] IReadOnlyList<ColumnModification> operations)
        {
            Check.NotNull(commandStringBuilder, "commandStringBuilder");
            Check.NotNull(operations, "operations");

            commandStringBuilder
                .AppendLine()
                .Append("WHERE ");

            AppendRowsAffectedWhereCondition(commandStringBuilder, 1);

            if (operations.Count > 0)
            {
                commandStringBuilder
                    .Append(" AND ")
                    .AppendJoin(operations, (sb, v) =>
                        {
                            if (v.IsKey)
                            {
                                if (v.IsRead)
                                {
                                    AppendIdentityWhereCondition(sb, v);
                                }
                                else
                                {
                                    AppendWhereCondition(sb, v, useOriginalValue: !v.IsWrite);
                                }
                            }
                        }, " AND ");
            }
        }

        protected abstract void AppendRowsAffectedWhereCondition([NotNull] StringBuilder commandStringBuilder, int expectedRowsAffected);

        protected virtual void AppendWhereCondition(
            [NotNull] StringBuilder commandStringBuilder,
            [NotNull] ColumnModification columnModification,
            bool useOriginalValue)
        {
            Check.NotNull(commandStringBuilder, "commandStringBuilder");
            Check.NotNull(columnModification, "columnModification");

            commandStringBuilder
                .Append(DelimitIdentifier(columnModification.ColumnName))
                .Append(" = ")
                .Append(useOriginalValue
                    ? columnModification.OriginalParameterName
                    : columnModification.ParameterName);
        }

        protected abstract void AppendIdentityWhereCondition(
            [NotNull] StringBuilder commandStringBuilder,
            [NotNull] ColumnModification columnModification);

        public virtual void AppendBatchHeader([NotNull] StringBuilder commandStringBuilder)
        {
        }

        public virtual string BatchCommandSeparator
        {
            get { return ";"; }
        }

        // TODO: Consider adding a base class for all SQL generators (DDL, DML),
        // to avoid duplicating the five methods below.

        public virtual string DelimitIdentifier(SchemaQualifiedName schemaQualifiedName)
        {
            return
                (schemaQualifiedName.IsSchemaQualified
                    ? DelimitIdentifier(schemaQualifiedName.Schema) + "."
                    : string.Empty)
                + DelimitIdentifier(schemaQualifiedName.Name);
        }

        public virtual string DelimitIdentifier([NotNull] string identifier)
        {
            Check.NotEmpty(identifier, "identifier");

            return "\"" + EscapeIdentifier(identifier) + "\"";
        }

        public virtual string EscapeIdentifier([NotNull] string identifier)
        {
            Check.NotEmpty(identifier, "identifier");

            return identifier.Replace("\"", "\"\"");
        }

        public virtual string GenerateLiteral([NotNull] string literal)
        {
            Check.NotNull(literal, "literal");

            return "'" + EscapeLiteral(literal) + "'";
        }

        public virtual string EscapeLiteral([NotNull] string literal)
        {
            Check.NotNull(literal, "literal");

            return literal.Replace("'", "''");
        }
    }
}
