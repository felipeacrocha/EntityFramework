// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Data.Entity.Metadata;
using Xunit;

namespace Microsoft.Data.Entity.Tests.Metadata
{
    public class PropertyTest
    {
        [Fact]
        public void Members_check_arguments()
        {
            Assert.Equal(
                "name",
                // ReSharper disable once AssignNullToNotNullAttribute
                Assert.Throws<ArgumentNullException>(() => new Property(null, null)).ParamName);
        }

        [Fact]
        public void Can_create_property_from_property_info()
        {
            var property = new Property("Name", typeof(string));

            Assert.Equal("Name", property.Name);
            Assert.Same(typeof(string), property.PropertyType);
        }

        [Fact]
        public void Default_nullability_of_property_is_based_on_nullability_of_CLR_type()
        {
            Assert.True(new Property("Name", typeof(string)).IsNullable);
            Assert.True(new Property("Name", typeof(int?)).IsNullable);
            Assert.False(new Property("Name", typeof(int)).IsNullable);
        }

        [Fact]
        public void Property_nullability_can_be_mutated()
        {
            Assert.False(new Property("Name", typeof(string)) { IsNullable = false }.IsNullable);
            Assert.True(new Property("Name", typeof(int)) { IsNullable = true }.IsNullable);
        }

        [Fact]
        public void UnderlyingType_returns_correct_underlying_type()
        {
            Assert.Equal(typeof(int), new Property("Name", typeof(int?)).UnderlyingType);
            Assert.Equal(typeof(int), new Property("Name", typeof(int)).UnderlyingType);
        }

        [Fact]
        public void HasClrProperty_is_set_appropriately()
        {
            Assert.False(new Property("Kake", typeof(int)).IsShadowProperty);
            Assert.False(new Property("Kake", typeof(int)).IsShadowProperty);
            Assert.True(new Property("Kake", typeof(int), shadowProperty: true).IsShadowProperty);
        }

        [Fact]
        public void Property_is_not_concurrency_token_by_default()
        {
            Assert.False(new Property("Name", typeof(string)).IsConcurrencyToken);
        }

        [Fact]
        public void Can_mark_property_as_concurrency_token()
        {
            var property = new Property("Name", typeof(string));
            Assert.False(property.IsConcurrencyToken);

            property.IsConcurrencyToken = true;
            Assert.True(property.IsConcurrencyToken);
        }

        [Fact]
        public void Property_is_read_write_by_default()
        {
            Assert.False(new Property("Name", typeof(string)).IsReadOnly);
        }

        [Fact]
        public void Property_can_be_marked_as_read_only()
        {
            var property = new Property("Name", typeof(string));
            Assert.False(property.IsReadOnly);

            property.IsReadOnly = true;
            Assert.True(property.IsReadOnly);

            property.IsReadOnly = false;
            Assert.False(property.IsReadOnly);
        }

        [Fact]
        public void Can_get_and_set_property_index_for_normal_property()
        {
            var property = new Property("Kake", typeof(int));

            Assert.Equal(0, property.Index);
            Assert.Equal(-1, property.ShadowIndex);

            property.Index = 1;

            Assert.Equal(1, property.Index);
            Assert.Equal(-1, property.ShadowIndex);

            Assert.Equal(
                "value",
                Assert.Throws<ArgumentOutOfRangeException>(() => property.Index = -1).ParamName);

            Assert.Equal(
                "value",
                Assert.Throws<ArgumentOutOfRangeException>(() => property.ShadowIndex = -1).ParamName);

            Assert.Equal(
                "value",
                Assert.Throws<ArgumentOutOfRangeException>(() => property.ShadowIndex = 1).ParamName);
        }

        [Fact]
        public void Can_get_and_set_property_and_shadow_index_for_shadow_property()
        {
            var property = new Property("Kake", typeof(int), shadowProperty: true);

            Assert.Equal(0, property.Index);
            Assert.Equal(0, property.ShadowIndex);

            property.Index = 1;
            property.ShadowIndex = 2;

            Assert.Equal(1, property.Index);
            Assert.Equal(2, property.ShadowIndex);

            Assert.Equal(
                "value",
                Assert.Throws<ArgumentOutOfRangeException>(() => property.Index = -1).ParamName);

            Assert.Equal(
                "value",
                Assert.Throws<ArgumentOutOfRangeException>(() => property.ShadowIndex = -1).ParamName);
        }
    }
}
