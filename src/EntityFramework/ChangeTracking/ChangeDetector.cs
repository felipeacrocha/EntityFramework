// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.ChangeTracking
{
    public class ChangeDetector
    {
        private readonly DbContextConfiguration _configuration;
        private readonly StateEntryNotifier _notifier;

        /// <summary>
        ///     This constructor is intended only for use when creating test doubles that will override members
        ///     with mocked or faked behavior. Use of this constructor for other purposes may result in unexpected
        ///     behavior including but not limited to throwing <see cref="NullReferenceException" />.
        /// </summary>
        protected ChangeDetector()
        {
        }

        public ChangeDetector([NotNull] DbContextConfiguration configuration, [NotNull] StateEntryNotifier notifier)
        {
            Check.NotNull(configuration, "configuration");
            Check.NotNull(notifier, "notifier");

            _configuration = configuration;
            _notifier = notifier;
        }

        public virtual void SidecarPropertyChanged([NotNull] StateEntry entry, [NotNull] IPropertyBase propertyBase)
        {
            Check.NotNull(entry, "entry");
            Check.NotNull(propertyBase, "propertyBase");

            var property = propertyBase as IProperty;
            if (property != null)
            {
                DetectPrincipalKeyChange(entry, property);
            }
        }

        public virtual void SidecarPropertyChanging([NotNull] StateEntry entry, [NotNull] IPropertyBase propertyBase)
        {
            PropertyChanging(entry, propertyBase);
        }

        public virtual void PropertyChanged([NotNull] StateEntry entry, [NotNull] IPropertyBase propertyBase)
        {
            Check.NotNull(entry, "entry");
            Check.NotNull(propertyBase, "propertyBase");

            var property = propertyBase as IProperty;

            if (property != null)
            {
                entry.SetPropertyModified(property);

                // Note: Make sure DetectPrincipalKeyChange is called even if DetectForeignKeyChange has returned true
                var foreignKeyChange = DetectForeignKeyChange(entry, property);
                var principalKeyChange = DetectPrincipalKeyChange(entry, property);
                if (foreignKeyChange
                    || principalKeyChange)
                {
                    entry.RelationshipsSnapshot.TakeSnapshot(property);
                }
            }
            else
            {
                var navigation = propertyBase as INavigation;

                if (navigation != null)
                {
                    if (DetectNavigationChange(entry, navigation))
                    {
                        entry.RelationshipsSnapshot.TakeSnapshot(navigation);
                    }
                }
            }
        }

        public virtual void PropertyChanging([NotNull] StateEntry entry, [NotNull] IPropertyBase propertyBase)
        {
            Check.NotNull(entry, "entry");
            Check.NotNull(propertyBase, "propertyBase");

            if (entry.EntityType.UseLazyOriginalValues)
            {
                entry.OriginalValues.EnsureSnapshot(propertyBase);

                // TODO: Consider making snapshot temporary here since it is no longer required after PropertyChanged is called
                entry.RelationshipsSnapshot.TakeSnapshot(propertyBase);
            }
        }

        public virtual bool DetectChanges([NotNull] StateManager stateManager)
        {
            Check.NotNull(stateManager, "stateManager");

            var foundChanges = false;
            foreach (var entry in stateManager.StateEntries.ToArray())
            {
                if (DetectChanges(entry))
                {
                    foundChanges = true;
                }
            }

            return foundChanges;
        }

        public virtual bool DetectChanges([NotNull] StateEntry entry)
        {
            Check.NotNull(entry, "entry");

            var entityType = entry.EntityType;
            var originalValues = entry.TryGetSidecar(Sidecar.WellKnownNames.OriginalValues);

            // TODO: Consider more efficient/higher-level/abstract mechanism for checking if DetectChanges is needed
            if (entityType.Type == null
                || originalValues == null
                || typeof(INotifyPropertyChanged).GetTypeInfo().IsAssignableFrom(entityType.Type.GetTypeInfo()))
            {
                return false;
            }

            var changedFkProperties = new List<IProperty>();
            var foundChanges = false;
            foreach (var property in entityType.Properties)
            {
                // TODO: Perf: don't lookup accessor twice
                if (!Equals(entry[property], originalValues[property]))
                {
                    entry.SetPropertyModified(property);
                    foundChanges = true;
                }

                if (DetectForeignKeyChange(entry, property))
                {
                    changedFkProperties.Add(property);
                }
            }

            foreach (var property in changedFkProperties)
            {
                entry.RelationshipsSnapshot.TakeSnapshot(property);
            }

            foreach (var navigation in entityType.Navigations)
            {
                if (DetectNavigationChange(entry, navigation))
                {
                    entry.RelationshipsSnapshot.TakeSnapshot(navigation);
                }
            }

            return foundChanges;
        }

        private bool DetectForeignKeyChange(StateEntry entry, IProperty property)
        {
            if (property.IsForeignKey())
            {
                var snapshotValue = entry.RelationshipsSnapshot[property];
                var currentValue = entry[property];

                // Note that mutation of a byte[] key is not supported or detected, but two different instances
                // of byte[] with the same content must be detected as equal.
                if (!StructuralComparisons.StructuralEqualityComparer.Equals(currentValue, snapshotValue))
                {
                    _notifier.ForeignKeyPropertyChanged(entry, property, snapshotValue, currentValue);

                    return true;
                }
            }

            return false;
        }

        private bool DetectNavigationChange(StateEntry entry, INavigation navigation)
        {
            var snapshotValue = entry.RelationshipsSnapshot[navigation];
            var currentValue = entry[navigation];

            if (navigation.IsCollection())
            {
                var snapshotCollection = (IEnumerable)snapshotValue;
                var currentCollection = (IEnumerable)currentValue;

                var added = new HashSet<object>(ReferenceEqualityComparer.Instance);

                var removed = new HashSet<object>(ReferenceEqualityComparer.Instance);
                if (snapshotCollection != null)
                {
                    foreach (var entity in snapshotCollection)
                    {
                        removed.Add(entity);
                    }
                }

                if (currentCollection != null)
                {
                    foreach (var entity in currentCollection)
                    {
                        if (!removed.Remove(entity))
                        {
                            added.Add(entity);
                        }
                    }
                }

                if (added.Any()
                    || removed.Any())
                {
                    _notifier.NavigationCollectionChanged(entry, navigation, added, removed);

                    return true;
                }
            }
            else if (!ReferenceEquals(currentValue, snapshotValue))
            {
                _notifier.NavigationReferenceChanged(entry, navigation, snapshotValue, currentValue);

                return true;
            }

            return false;
        }

        private bool DetectPrincipalKeyChange(StateEntry entry, IProperty property)
        {
            // TODO: Perf: make it fast to check if a property is part of the primary key
            var isPrimaryKey = property.IsPrimaryKey();

            // TODO: Perf: make it faster to check if the property is at the principal end or not
            var foreignKeys = _configuration.Model.GetReferencingForeignKeys(property).ToArray();

            if (isPrimaryKey || foreignKeys.Length > 0)
            {
                var snapshotValue = entry.RelationshipsSnapshot[property];
                var currentValue = entry[property];

                if (!StructuralComparisons.StructuralEqualityComparer.Equals(currentValue, snapshotValue))
                {
                    if (isPrimaryKey)
                    {
                        _configuration.StateManager.UpdateIdentityMap(entry, entry.RelationshipsSnapshot.GetPrimaryKeyValue());
                    }

                    _notifier.PrincipalKeyPropertyChanged(entry, property, snapshotValue, currentValue);

                    entry.RelationshipsSnapshot.TakeSnapshot(property);

                    return true;
                }
            }

            return false;
        }
    }
}
