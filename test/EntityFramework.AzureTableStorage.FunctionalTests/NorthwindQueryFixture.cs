// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Data.Entity.AzureTableStorage.Metadata;
using Microsoft.Data.Entity.FunctionalTests;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Advanced;
using Microsoft.Framework.DependencyInjection.Fallback;
using Northwind;

namespace Microsoft.Data.Entity.AzureTableStorage.FunctionalTests
{
    public class NorthwindQueryFixture : NorthwindQueryFixtureBase
    {
        private readonly DbContextOptions _options;
        private readonly IServiceProvider _serviceProvider;

        public IModel CreateAzureTableStorageModel()
        {
            var model = CreateModel();

            // TODO: Need to do this because we're not using the ATS conventions and so need to redefine keys manually
            var productType = model.GetEntityType(typeof(Product));
            var orderDetailType = model.GetEntityType(typeof(OrderDetail));

            orderDetailType.RemoveNavigation(orderDetailType.Navigations.Single());
            productType.RemoveNavigation(productType.Navigations.Single());
            orderDetailType.RemoveForeignKey(orderDetailType.ForeignKeys.Single());

            const string tableSuffix = "FunctionalTests";
            var builder = new BasicModelBuilder(model);
            builder.Entity<Customer>()
                .PartitionAndRowKey(s => s.City, s => s.CustomerID)
                .Timestamp("Timestamp", true)
                .TableName("Customer" + tableSuffix)
                .Key(e => e.CustomerID); // See issue #632
            builder.Entity<Employee>()
                .PartitionAndRowKey(s => s.City, s => s.EmployeeID)
                .Timestamp("Timestamp", true)
                .TableName("Employee" + tableSuffix)
                .Key(e => e.EmployeeID); // See issue #632
            builder.Entity<Order>()
                .PartitionAndRowKey(s => s.CustomerID, s => s.OrderID)
                .Timestamp("Timestamp", true)
                .TableName("Order" + tableSuffix)
                .Key(e => e.OrderID); // See issue #632
            builder.Entity<Product>()
                .PartitionAndRowKey(s => s.SupplierID, s => s.ProductID)
                .Timestamp("Timestamp", true)
                .TableName("Product" + tableSuffix)
                .Key(e => e.ProductID); // See issue #632
            builder.Entity<OrderDetail>()
                .PartitionAndRowKey(s => s.OrderID, s => s.ProductID)
                .Timestamp("Timestamp", true)
                .TableName("OrderDetail" + tableSuffix);

            var modelBuilder = new BasicModelBuilder(model);
            modelBuilder.Entity<OrderDetail>().ForeignKey<Product>(od => od.ProductID);

            var productIdFk = orderDetailType.ForeignKeys.Single();
            orderDetailType.AddNavigation(new Navigation(productIdFk, "Product", pointsToPrincipal: true));
            productType.AddNavigation(new Navigation(productIdFk, "OrderDetails", pointsToPrincipal: false));

            return builder.Model;
        }

        public NorthwindQueryFixture()
        {
            var model = CreateAzureTableStorageModel();

            var titleProperty
                = model.GetEntityType(typeof(Employee)).GetProperty("Title");

            _options
                = new DbContextOptions()
                    .UseModel(model)
                    .UseAzureTableStorage(TestConfig.Instance.ConnectionString, batchRequests: false);

            var services = new ServiceCollection();
            services.AddEntityFramework().UseLoggerFactory(TestFileLogger.Factory).AddAzureTableStorage();
            _serviceProvider = services.BuildServiceProvider();

            using (var context = new DbContext(_options))
            {
                if (!context.Database.EnsureCreated())
                {
                    return;
                }

                context.Set<Customer>().AddRange(NorthwindData.CreateCustomers());

                foreach (var employee in NorthwindData.CreateEmployees())
                {
                    context.Set<Employee>().Add(employee);
                    context.ChangeTracker.Entry(employee).StateEntry[titleProperty] = employee.Title;
                }

                context.Set<Order>().AddRange(NorthwindData.CreateOrders());
                context.Set<Product>().AddRange(NorthwindData.CreateProducts());
                context.Set<OrderDetail>().AddRange(NorthwindData.CreateOrderDetails());
                context.SaveChanges();
            }
        }

        public DbContext CreateContext()
        {
            return new DbContext(_serviceProvider, _options);
        }
    }
}
