using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest
{
    /// <summary>
    /// Summary description for NorthwindFunctionalTests
    /// </summary>
    [TestClass]
    public class NorthwindExtensionTableCUDTests
    {
        readonly DbEntityProvider provider = DbEntityProvider.From("IQToolkit.Data.SqlClient", @"Data Source=(local);Initial Catalog=Northwind;Integrated Security=True", "IQToolkitTest.NorthwindWithAttributes");
        Northwind db;

        public NorthwindExtensionTableCUDTests()
        {
            
            TSqlLanguage language = new TSqlLanguage(); //Reference to get assembly into test context
        }

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        [TestInitialize()]
        public void NorthwindInit()
        {
            provider.Connection.Open();
            db = new Northwind(provider);
            ClearOutTestData();
            
        }
        [TestCleanup()]
        public void NorthwindTeardown()
        {
            ClearOutTestData();
            provider.Connection.Close();
        }
        private void SetupTablesToTest()
        {
            string command = 
                "USE [Northwind]" + Environment.NewLine +
                "GO" + Environment.NewLine +
                "SET ANSI_NULLS ON" + Environment.NewLine +
                "GO" + Environment.NewLine +
                "SET QUOTED_IDENTIFIER ON" + Environment.NewLine +
                "GO" + Environment.NewLine +
                "SET ANSI_PADDING ON" + Environment.NewLine +
                "GO" + Environment.NewLine +
                "IF  NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CustomerComments]') AND type in (N'U'))" + Environment.NewLine +
                "CREATE TABLE [dbo].[CustomerComments](" + Environment.NewLine +
                "	[CustomerID] [nchar](5) NOT NULL," + Environment.NewLine +
                "	[Comment] [varchar](max) NOT NULL," + Environment.NewLine +
                " CONSTRAINT [PK_CustomerComments] PRIMARY KEY CLUSTERED " + Environment.NewLine +
                "(" + Environment.NewLine +
                "	[CustomerID] ASC" + Environment.NewLine +
                ")WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]" + Environment.NewLine +
                ") ON [PRIMARY]" + Environment.NewLine +
                "GO" + Environment.NewLine +
                "SET ANSI_PADDING OFF" + Environment.NewLine +
                "GO" + Environment.NewLine;
                                
            try
            {
                //Create 0-1 relationship tables
                provider.ExecuteCommand(command);

            }
            catch (Exception)
            {
                
                throw;
            }
        }
        private void ClearOutTestData()
        {
            try
            {
                provider.ExecuteCommand("DELETE FROM Orders WHERE CustomerID LIKE 'XX%'");
                provider.ExecuteCommand("DELETE FROM Customers WHERE CustomerID LIKE 'XX%'");
                provider.ExecuteCommand("DELETE FROM CustomerComments WHERE CustomerID LIKE 'XX%'");
            }
            catch (Exception)
            {
                //Do nothing
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void Extension_Table_Compiled_Query()
        {
            var fn = QueryCompiler.Compile((string id) => db.CustomersWithComments.Where(c => c.CustomerID == id));
            var items = fn("ALKFI").ToList();
        }
        [TestMethod]
        public void Extension_Table_InsertCustomerNoResult()
        {
            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA",
                Comment = "New Comment"
            };
            var result = db.CustomersWithComments.Insert(cust);
            Assert.AreEqual(2, result);
        }
        [TestMethod]
        public void Extension_Table_InsertCustomerWithResult()
        {
            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA",
                Comment = "New Comment"
            };
            var result = db.CustomersWithComments.Insert(cust, c => c.City);
            Assert.AreEqual(result, "Seattle");  // should be value we asked for
        }
        [TestMethod]
        public void Extension_Table_BatchInsertCustomersNoResult()
        {
            const int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new CustomerWithComments
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Seattle",
                    Country = "USA",
                    Comment = "New Comment"
                });
            var results = db.CustomersWithComments.Batch(custs, (u, c) => u.Insert(c));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, 1)));
        }
        [TestMethod]
        public void Extension_Table_InsertCustomersWithResult()
        {
            const int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new CustomerWithComments
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Seattle",
                    Country = "USA",
					Comment = "New Comment"
                });
            var results = db.CustomersWithComments.Batch(custs, (u, c) => u.Insert(c, d => d.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, "Seattle")));
        }
        [TestMethod]
        public void Extension_Table_InsertOrderWithNoResult()
        {
            Extension_Table_InsertCustomerNoResult(); // create customer "XX1"
            var order = new Order
            {
                CustomerID = "XX1",
                OrderDate = DateTime.Today,
            };
            var result = db.Orders.Insert(order);
            Assert.AreEqual(1, result);
        }
        [TestMethod]
        public void Extension_Table_InsertOrderWithGeneratedIDResult()
        {
            Extension_Table_InsertCustomerNoResult(); // create customer "XX1"
            var order = new Order
            {
                CustomerID = "XX1",
                OrderDate = DateTime.Today,
            };
            var result = db.Orders.Insert(order, o => o.OrderID);
            Assert.AreNotEqual(2, result);
        }
        [TestMethod]
        public void Extension_Table_UpdateCustomerNoResult()
        {
            Extension_Table_InsertCustomerNoResult(); // create customer "XX1"

            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA",
				Comment = "New Comment: Moved to Portland!"
            };

            var result = db.CustomersWithComments.Update(cust);
            Assert.AreEqual(2, result);
        }
        [TestMethod]
        public void Extension_Table_UpdateCustomerWithResult()
        {
            Extension_Table_InsertCustomerNoResult(); // create customer "XX1"

            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA",
				Comment = "New Comment"
            };

            var result = db.CustomersWithComments.Update(cust, null, c => c.City);
            Assert.AreEqual("Portland", result);
        }
        [TestMethod]
        public void Extension_Table_UpdateCustomerWithUpdateCheckThatDoesNotSucceed()
        {
            Extension_Table_InsertCustomerNoResult(); // create customer "XX1"

            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA",
				Comment = "New Comment"
            };

            var result = db.CustomersWithComments.Update(cust, d => d.City == "Detroit");
            Assert.AreEqual(-1, result); //Returns -1 due to existance check
            var customer = db.CustomersWithComments.Single(c => c.CustomerID == "XX1");
            Assert.AreEqual("Seattle",customer.City); //City should be unchanged
        }
        [TestMethod]
        public void Extension_Table_UpdateCustomerWithUpdateCheckThatSucceeds()
        {
            Extension_Table_InsertCustomerNoResult(); // create customer "XX1"

            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA",
				Comment = "New Comment"
            };

            var result = db.CustomersWithComments.Update(cust, d => d.City == "Seattle");
            Assert.AreEqual(2, result);
        }
        [TestMethod]
        public void Extension_Table_BatchUpdateCustomer()
        {
            Extension_Table_BatchInsertCustomersNoResult();

            const int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new CustomerWithComments
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Seattle",
                    Country = "USA",
				Comment = "New Comment"
                });

            var results = db.CustomersWithComments.Batch(custs, (u, c) => u.Update(c));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, 1)));
        }
        [TestMethod]
        public void Extension_Table_BatchUpdateCustomerWithUpdateCheck()
        {
            Extension_Table_BatchInsertCustomersNoResult();

            const int n = 10;
            var pairs = Enumerable.Range(1, n).Select(
                i => new
                {
                    original = new CustomerWithComments
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Seattle",
                        Country = "USA",
				Comment = "New Comment"
                    },
                    current = new CustomerWithComments
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Portland",
                        Country = "USA",
				Comment = "New Comment"
                    }
                });

            var results = db.CustomersWithComments.Batch(pairs, (u, x) => u.Update(x.current, d => d.City == x.original.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, 1)));
        }
        [TestMethod]
        public void Extension_Table_BatchUpdateCustomerWithResult()
        {
            Extension_Table_BatchInsertCustomersNoResult();

            const int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new CustomerWithComments
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Portland",
                    Country = "USA",
				Comment = "New Comment"
                });

            var results = db.CustomersWithComments.Batch(custs, (u, c) => u.Update(c, null, d => d.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, "Portland")));
        }
        [TestMethod]
        public void Extension_Table_BatchUpdateCustomerWithUpdateCheckAndResult()
        {
            Extension_Table_BatchInsertCustomersNoResult();

            const int n = 10;
            var pairs = Enumerable.Range(1, n).Select(
                i => new
                {
                    original = new CustomerWithComments
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Seattle",
                        Country = "USA",
				Comment = "New Comment"
                    },
                    current = new CustomerWithComments
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Portland",
                        Country = "USA",
				Comment = "New Comment"
                    }
                });

            var results = db.CustomersWithComments.Batch(pairs, (u, x) => u.Update(x.current, d => d.City == x.original.City, d => d.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, "Portland")));
        }
        [TestMethod]
        public void Extension_Table_UpsertNewCustomerNoResult()
        {
            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle", // moved to Portland!
                Country = "USA",
				Comment = "New Comment"
            };

            var result = db.CustomersWithComments.InsertOrUpdate(cust);
            Assert.AreEqual(2, result);
        }
        [TestMethod]
        public void Extension_Table_UpsertExistingCustomerNoResult()
        {
            Extension_Table_InsertCustomerNoResult();

            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA",
				Comment = "New Comment"
            };

            var result = db.CustomersWithComments.InsertOrUpdate(cust);
            Assert.AreEqual(2, result);
        }
        [TestMethod]
        public void Extension_Table_UpsertNewCustomerWithResult()
        {
            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle", // moved to Portland!
                Country = "USA",
				Comment = "New Comment"
            };

            var result = db.CustomersWithComments.InsertOrUpdate(cust, null, d => d.City);
            Assert.AreEqual("Seattle", result);
        }
        [TestMethod]
        public void Extension_Table_UpsertExistingCustomerWithResult()
        {
            Extension_Table_InsertCustomerNoResult();

            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA",
				Comment = "New Comment"
            };

            var result = db.CustomersWithComments.InsertOrUpdate(cust, null, d => d.City);
            Assert.AreEqual("Portland", result);
        }
        [TestMethod]
        public void Extension_Table_UpsertNewCustomerWithUpdateCheck()
        {
            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA",
				Comment = "New Comment"
            };

            var result = db.CustomersWithComments.InsertOrUpdate(cust, d => d.City == "Portland");
            Assert.AreEqual(2, result);
        }
        [TestMethod]
        public void Extension_Table_UpsertExistingCustomerWithUpdateCheck()
        {
            Extension_Table_InsertCustomerNoResult();

            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA",
				Comment = "New Comment"
            };

            var result = db.CustomersWithComments.InsertOrUpdate(cust, d => d.City == "Seattle");
            Assert.AreEqual(2, result);
        }
        [TestMethod]
        public void Extension_Table_BatchUpsertNewCustomersNoResult()
        {
            const int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new CustomerWithComments
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Portland",
                    Country = "USA",
				Comment = "New Comment"
                });

            var results = db.CustomersWithComments.Batch(custs, (u, c) => u.InsertOrUpdate(c));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, 1)));
        }
        [TestMethod]
        public void Extension_Table_BatchUpsertExistingCustomersNoResult()
        {
            Extension_Table_BatchInsertCustomersNoResult();

            const int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new CustomerWithComments
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Portland",
                    Country = "USA",
				Comment = "New Comment"
                });

            var results = db.CustomersWithComments.Batch(custs, (u, c) => u.InsertOrUpdate(c));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, 1)));
        }
        [TestMethod]
        public void Extension_Table_BatchUpsertNewCustomersWithResult()
        {
            const int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new CustomerWithComments
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Portland",
                    Country = "USA",
				Comment = "New Comment"
                });

            var results = db.CustomersWithComments.Batch(custs, (u, c) => u.InsertOrUpdate(c, null, d => d.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, "Portland")));
        }
        [TestMethod]
        public void Extension_Table_BatchUpsertExistingCustomersWithResult()
        {
            Extension_Table_BatchInsertCustomersNoResult();

            const int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new CustomerWithComments
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Portland",
                    Country = "USA",
				Comment = "New Comment"
                });

            var results = db.CustomersWithComments.Batch(custs, (u, c) => u.InsertOrUpdate(c, null, d => d.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, "Portland")));
        }
        [TestMethod]
        public void Extension_Table_BatchUpsertNewCustomersWithUpdateCheck()
        {
            const int n = 10;
            var pairs = Enumerable.Range(1, n).Select(
                i => new
                {
                    original = new CustomerWithComments
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Seattle",
                        Country = "USA",
				Comment = "New Comment"
                    },
                    current = new CustomerWithComments
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Portland",
                        Country = "USA",
				Comment = "New Comment"
                    }
                });

            var results = db.CustomersWithComments.Batch(pairs, (u, x) => u.InsertOrUpdate(x.current, d => d.City == x.original.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, 1)));
        }
        [TestMethod]
        public void Extension_Table_BatchUpsertExistingCustomersWithUpdateCheck()
        {
            Extension_Table_BatchInsertCustomersNoResult();

            const int n = 10;
            var pairs = Enumerable.Range(1, n).Select(
                i => new
                {
                    original = new CustomerWithComments
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Seattle",
                        Country = "USA",
				Comment = "New Comment"
                    },
                    current = new CustomerWithComments
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Portland",
                        Country = "USA",
				Comment = "New Comment"
                    }
                });

            var results = db.CustomersWithComments.Batch(pairs, (u, x) => u.InsertOrUpdate(x.current, d => d.City == x.original.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, 1)));
        }
        [TestMethod]
        public void Extension_Table_DeleteCustomer()
        {
            Extension_Table_InsertCustomerNoResult();

            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA",
				Comment = "New Comment"
            };

            var result = db.CustomersWithComments.Delete(cust);
            Assert.AreEqual(2, result);
        }
        [TestMethod]
        public void Extension_Table_DeleteCustomerForNonExistingCustomer()
        {
            Extension_Table_InsertCustomerNoResult();

            var cust = new CustomerWithComments
            {
                CustomerID = "XX2",
                CompanyName = "Company2",
                ContactName = "Contact2",
                City = "Seattle",
                Country = "USA",
				Comment = "New Comment"
            };

            var result = db.CustomersWithComments.Delete(cust);
            Assert.AreEqual(0, result);
        }
        [TestMethod]
        public void Extension_Table_DeleteCustomerWithDeleteCheckThatSucceeds()
        {
            Extension_Table_InsertCustomerNoResult();

            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA",
				Comment = "New Comment"
            };

            var result = db.CustomersWithComments.Delete(cust, d => d.City == "Seattle");
            Assert.AreEqual(2, result);
        }
        [TestMethod]
        public void Extension_Table_DeleteCustomerWithDeleteCheckThatDoesNotSucceed()
        {
            Extension_Table_InsertCustomerNoResult();

            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA",
				Comment = "New Comment"
            };

            var result = db.CustomersWithComments.Delete(cust, d => d.City == "Portland");
            Assert.AreEqual(-1, result); //Expected result due to Exists Check
            var check = db.CustomersWithComments.Single(c => c.CustomerID == "XX1");
            Assert.IsNotNull(check); //Verify that the customer has not been deleted
        }
        [TestMethod]
        public void Extension_Table_DeleteCustomerWithDeleteCheckOnExtensionDataThatDoesNotSucceed()
        {
            Extension_Table_InsertCustomerNoResult();

            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA",
                Comment = "Custom comment"
            };

            var result = db.CustomersWithComments.Delete(cust, d => d.Comment == "Comment does not exist");
            Assert.AreEqual(-1, result); //Expected result due to Exists Check
            var check = db.CustomersWithComments.Single(c => c.CustomerID == "XX1");
            Assert.IsNotNull(check); //Verify that the customer has not been deleted
        }
        [TestMethod]
        public void Extension_Table_BatchDeleteCustomers()
        {
            Extension_Table_BatchInsertCustomersNoResult();

            const int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new CustomerWithComments
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Seattle",
                    Country = "USA",
				Comment = "New Comment"
                });

            var results = db.CustomersWithComments.Batch(custs, (u, c) => u.Delete(c));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, 1)));
        }
        [TestMethod]
        public void Extension_Table_BatchDeleteCustomersWithDeleteCheck()
        {
            Extension_Table_BatchInsertCustomersNoResult();

            const int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new CustomerWithComments
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Seattle",
                    Country = "USA",
				Comment = "New Comment"
                });

            var results = db.CustomersWithComments.Batch(custs, (u, c) => u.Delete(c, d => d.City == c.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, 1)));
        }
        [TestMethod]
        public void Extension_Table_BatchDeleteCustomersWithDeleteCheckThatDoesNotSucceed()
        {
            Extension_Table_BatchInsertCustomersNoResult();

            const int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new CustomerWithComments
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Portland",
                    Country = "USA",
				    Comment = "New Comment"
                });

            var results = db.CustomersWithComments.Batch(custs, (u, c) => u.Delete(c, d => d.City == c.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => Equals(r, 1))); //Each batch command will complete with a 1 result due to the existance check
            var customers = db.CustomersWithComments.Count(c => c.CustomerID.Substring(0,2) == "XX");//Get all customers with XX and count
            Assert.AreEqual(n,customers); //Ensure no customers were deleted
        }
        [TestMethod]
        public void Extension_Table_DeleteWhere()
        {
            Extension_Table_BatchInsertCustomersNoResult();

            var result = db.CustomersWithComments.Delete(c => c.CustomerID.StartsWith("XX"));
            Assert.AreEqual(10, result);
        }
        [TestMethod]
        public void Extension_Table_SessionIdentityCache()
        {
            NorthwindSession ns = new NorthwindSession(provider);

            // both objects should be the same instance
            var cust = ns.CustomersWithComments.Single(c => c.CustomerID == "ALFKI");
            var cust2 = ns.CustomersWithComments.Single(c => c.CustomerID == "ALFKI");

            Assert.IsNotNull(cust);
            Assert.IsNotNull(cust2);
            Assert.ReferenceEquals(cust, cust2);
        }
        [TestMethod]
        public void Extension_Table_SessionProviderNotIdentityCached()
        {
            NorthwindSession ns = new NorthwindSession(provider);
            Northwind db2 = new Northwind(ns.Session.Provider);

            // both objects should be different instances
            var cust = ns.CustomersWithComments.Single(c => c.CustomerID == "ALFKI");
            var cust2 = ns.CustomersWithComments.ProviderTable.Single(c => c.CustomerID == "ALFKI");

            Assert.AreNotEqual(null, cust);
            Assert.AreNotEqual(null, cust2);
            Assert.AreEqual(cust.CustomerID, cust2.CustomerID);
            Assert.IsFalse(Assert.ReferenceEquals(cust, cust2));
        }
        [TestMethod]
        public void Extension_Table_SessionSubmitActionOnModify()
        {
            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA",
                Comment = "New Comment"
            };

            this.db.CustomersWithComments.Insert(cust);

            var ns = new NorthwindSession(this.provider);
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));

            // fetch the previously inserted customer
            cust = ns.CustomersWithComments.Single(c => c.CustomerID == "XX1");
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));

            cust.ContactName = "Contact Modified";
            Assert.AreEqual(SubmitAction.Update, ns.CustomersWithComments.GetSubmitAction(cust));

            ns.SubmitChanges();
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));

            // prove actually modified by fetching through provider
            var cust2 = this.db.CustomersWithComments.Single(c => c.CustomerID == "XX1");
            Assert.AreEqual("Contact Modified", cust2.ContactName);

            // ready to be submitted again!
            cust.City = "SeattleX";
            Assert.AreEqual(SubmitAction.Update, ns.CustomersWithComments.GetSubmitAction(cust));
        }
        [TestMethod]
        public void Extension_Table_SessionSubmitActionOnInsert()
        {
            NorthwindSession ns = new NorthwindSession(provider);
            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA",
				Comment = "New Comment"
            };
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));

            ns.CustomersWithComments.InsertOnSubmit(cust);
            Assert.AreEqual(SubmitAction.Insert, ns.CustomersWithComments.GetSubmitAction(cust));

            ns.SubmitChanges();
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));

            cust.City = "SeattleX";
            Assert.AreEqual(SubmitAction.Update, ns.CustomersWithComments.GetSubmitAction(cust));
        }
        [TestMethod]
        public void Extension_Table_SessionSubmitActionOnInsertOrUpdate()
        {
            NorthwindSession ns = new NorthwindSession(provider);
            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA",
				Comment = "New Comment"
            };
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));

            ns.CustomersWithComments.InsertOrUpdateOnSubmit(cust);
            Assert.AreEqual(SubmitAction.InsertOrUpdate, ns.CustomersWithComments.GetSubmitAction(cust));

            ns.SubmitChanges();
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));

            cust.City = "SeattleX";
            Assert.AreEqual(SubmitAction.Update, ns.CustomersWithComments.GetSubmitAction(cust));
        }
        [TestMethod]
        public void Extension_Table_SessionSubmitActionOnUpdate()
        {
            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA",
				Comment = "New Comment"
            };
            db.CustomersWithComments.Insert(cust);

            NorthwindSession ns = new NorthwindSession(provider);
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));

            ns.CustomersWithComments.UpdateOnSubmit(cust);
            Assert.AreEqual(SubmitAction.Update, ns.CustomersWithComments.GetSubmitAction(cust));

            ns.SubmitChanges();
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));

            cust.City = "SeattleX";
            Assert.AreEqual(SubmitAction.Update, ns.CustomersWithComments.GetSubmitAction(cust));
        }
        [TestMethod]
        public void Extension_Table_SessionSubmitActionOnDelete()
        {
            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA",
				Comment = "New Comment"
            };
            db.CustomersWithComments.Insert(cust);

            NorthwindSession ns = new NorthwindSession(provider);
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));

            ns.CustomersWithComments.DeleteOnSubmit(cust);
            var custHash1 = cust.GetHashCode();
            var custHash2 = cust.GetHashCode();
            var custSessTable1 = ns.CustomersWithComments.GetHashCode();
            var custSessTable2 = ns.CustomersWithComments.GetHashCode();
            var custSessionTable = ns.CustomersWithComments;
            custSessionTable.DeleteOnSubmit(cust);

            Assert.AreEqual(SubmitAction.Delete, ns.CustomersWithComments.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.Delete, custSessionTable.GetSubmitAction(cust));//ns.CustomersWithComments.GetSubmitAction(cust));

            ns.SubmitChanges();
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));

            // modifications after delete don't trigger updates
            cust.City = "SeattleX";
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));
        }
        [TestMethod]
        public void Extension_Table_DeleteThenInsertSamePK()
        {
            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA",
				Comment = "New Comment"
            };

            var cust2 = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company2",
                ContactName = "Contact2",
                City = "Chicago",
                Country = "USA",
				Comment = "New Comment"
            };

            db.CustomersWithComments.Insert(cust);

            NorthwindSession ns = new NorthwindSession(provider);
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust2));

            ns.CustomersWithComments.DeleteOnSubmit(cust);
            Assert.AreEqual(SubmitAction.Delete, ns.CustomersWithComments.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust2));

            ns.CustomersWithComments.InsertOnSubmit(cust2);
            Assert.AreEqual(SubmitAction.Delete, ns.CustomersWithComments.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.Insert, ns.CustomersWithComments.GetSubmitAction(cust2));

            ns.SubmitChanges();
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust2));

            // modifications after delete don't trigger updates
            cust.City = "SeattleX";
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));

            // modifications after insert do trigger updates
            cust2.City = "ChicagoX";
            Assert.AreEqual(SubmitAction.Update, ns.CustomersWithComments.GetSubmitAction(cust2));
        }
        [TestMethod]
        public void Extension_Table_InsertThenDeleteSamePK()
        {
            var cust = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA",
				Comment = "New Comment"
            };

            var cust2 = new CustomerWithComments
            {
                CustomerID = "XX1",
                CompanyName = "Company2",
                ContactName = "Contact2",
                City = "Chicago",
                Country = "USA",
				Comment = "New Comment"
            };

            db.CustomersWithComments.Insert(cust);

            NorthwindSession ns = new NorthwindSession(provider);
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust2));
            
            ns.CustomersWithComments.InsertOnSubmit(cust2);
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.Insert, ns.CustomersWithComments.GetSubmitAction(cust2));

            ns.CustomersWithComments.DeleteOnSubmit(cust);
            Assert.AreEqual(SubmitAction.Delete, ns.CustomersWithComments.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.Insert, ns.CustomersWithComments.GetSubmitAction(cust2));

            ns.SubmitChanges();
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust2));

            // modifications after delete don't trigger updates
            cust.City = "SeattleX";
            Assert.AreEqual(SubmitAction.None, ns.CustomersWithComments.GetSubmitAction(cust));

            // modifications after insert do trigger updates
            cust2.City = "ChicagoX";
            Assert.AreEqual(SubmitAction.Update, ns.CustomersWithComments.GetSubmitAction(cust2));
        }
    }
}
