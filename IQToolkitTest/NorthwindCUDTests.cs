using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IQToolkit;
using IQToolkit.Data;

namespace IQToolkitTest
{
    /// <summary>
    /// Summary description for NorthwidnCUDTests
    /// </summary>
    [TestClass]
    public class NorthwindCUDTests
    {
        DbEntityProvider provider = DbEntityProvider.From("IQToolkit.Data.SqlClient", @"Data Source=ET1841\ETTSILVERS08;Initial Catalog=Northwind;Integrated Security=True", "IQToolkitTest.NorthwindWithAttributes");
        Northwind db;
        public NorthwindCUDTests()
        {
            //
            // TODO: Add constructor logic here
            //
            provider.Connection.Open();
            //db = new Northwind(provider);
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
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
        [TestInitialize()]
        public void NorthwidnCUDTestsInit()
        {
            db = new Northwind(provider);
        }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        [TestCleanup()]
        public void NorthwidnCUDTestsTeardown()
        {
            try
            {
                this.provider.ExecuteCommand("DELETE FROM Orders WHERE CustomerID LIKE 'XX%'");
                this.provider.ExecuteCommand("DELETE FROM Customers WHERE CustomerID LIKE 'XX%'");
            }
            catch (Exception)
            {
                //Do nothing
            }
        }
        //
        #endregion

        [TestMethod]
        public void TestInsertCustomerNoResult()
        {
            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA"
            };
            var result = db.Customers.Insert(cust);
            Assert.AreEqual<int>(1, result);
        }
        [TestMethod]
        public void TestInsertCustomerWithResult()
        {
            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA"
            };
            var result = db.Customers.Insert(cust, c => c.City);
            Assert.AreEqual<string>(result, "Seattle");  // should be value we asked for
        }
        [TestMethod]
        public void TestBatchInsertCustomersNoResult()
        {
            int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new Customer
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Seattle",
                    Country = "USA"
                });
            var results = db.Customers.Batch(custs, (u, c) => u.Insert(c));
            Assert.AreEqual<int>(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, 1)));
        }
        [TestMethod]
        public void TestBatchInsertCustomersWithResult()
        {
            int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new Customer
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Seattle",
                    Country = "USA"
                });
            var results = db.Customers.Batch(custs, (u, c) => u.Insert(c, d => d.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, "Seattle")));
        }
        [TestMethod]
        public void TestInsertOrderWithNoResult()
        {
            this.TestInsertCustomerNoResult(); // create customer "XX1"
            var order = new Order
            {
                CustomerID = "XX1",
                OrderDate = DateTime.Today,
            };
            var result = db.Orders.Insert(order);
            Assert.AreEqual(1, result);
        }
        [TestMethod]
        public void TestInsertOrderWithGeneratedIDResult()
        {
            this.TestInsertCustomerNoResult(); // create customer "XX1"
            var order = new Order
            {
                CustomerID = "XX1",
                OrderDate = DateTime.Today,
            };
            var result = db.Orders.Insert(order, o => o.OrderID);
            Assert.AreNotEqual(1, result);
        }
        [TestMethod]
        public void TestUpdateCustomerNoResult()
        {
            this.TestInsertCustomerNoResult(); // create customer "XX1"

            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA"
            };

            var result = db.Customers.Update(cust);
            Assert.AreEqual(1, result);
        }
        [TestMethod]
        public void TestUpdateCustomerWithResult()
        {
            this.TestInsertCustomerNoResult(); // create customer "XX1"

            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA"
            };

            var result = db.Customers.Update(cust, null, c => c.City);
            Assert.AreEqual("Portland", result);
        }
        [TestMethod]
        public void TestUpdateCustomerWithUpdateCheckThatDoesNotSucceed()
        {
            this.TestInsertCustomerNoResult(); // create customer "XX1"

            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA"
            };

            var result = db.Customers.Update(cust, d => d.City == "Detroit");
            Assert.AreEqual(0, result); // 0 for failure
        }
        [TestMethod]
        public void TestUpdateCustomerWithUpdateCheckThatSucceeds()
        {
            this.TestInsertCustomerNoResult(); // create customer "XX1"

            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA"
            };

            var result = db.Customers.Update(cust, d => d.City == "Seattle");
            Assert.AreEqual(1, result);
        }
        [TestMethod]
        public void TestBatchUpdateCustomer()
        {
            this.TestBatchInsertCustomersNoResult();

            int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new Customer
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Seattle",
                    Country = "USA"
                });

            var results = db.Customers.Batch(custs, (u, c) => u.Update(c));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, 1)));
        }
        [TestMethod]
        public void TestBatchUpdateCustomerWithUpdateCheck()
        {
            this.TestBatchInsertCustomersNoResult();

            int n = 10;
            var pairs = Enumerable.Range(1, n).Select(
                i => new
                {
                    original = new Customer
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Seattle",
                        Country = "USA"
                    },
                    current = new Customer
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Portland",
                        Country = "USA"
                    }
                });

            var results = db.Customers.Batch(pairs, (u, x) => u.Update(x.current, d => d.City == x.original.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, 1)));
        }
        [TestMethod]
        public void TestBatchUpdateCustomerWithResult()
        {
            this.TestBatchInsertCustomersNoResult();

            int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new Customer
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Portland",
                    Country = "USA"
                });

            var results = db.Customers.Batch(custs, (u, c) => u.Update(c, null, d => d.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, "Portland")));
        }
        [TestMethod]
        public void TestBatchUpdateCustomerWithUpdateCheckAndResult()
        {
            this.TestBatchInsertCustomersNoResult();

            int n = 10;
            var pairs = Enumerable.Range(1, n).Select(
                i => new
                {
                    original = new Customer
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Seattle",
                        Country = "USA"
                    },
                    current = new Customer
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Portland",
                        Country = "USA"
                    }
                });

            var results = db.Customers.Batch(pairs, (u, x) => u.Update(x.current, d => d.City == x.original.City, d => d.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, "Portland")));
        }
        [TestMethod]
        public void TestUpsertNewCustomerNoResult()
        {
            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle", // moved to Portland!
                Country = "USA"
            };

            var result = db.Customers.InsertOrUpdate(cust);
            Assert.AreEqual(1, result);
        }
        [TestMethod]
        public void TestUpsertExistingCustomerNoResult()
        {
            this.TestInsertCustomerNoResult();

            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA"
            };

            var result = db.Customers.InsertOrUpdate(cust);
            Assert.AreEqual(1, result);
        }
        [TestMethod]
        public void TestUpsertNewCustomerWithResult()
        {
            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle", // moved to Portland!
                Country = "USA"
            };

            var result = db.Customers.InsertOrUpdate(cust, null, d => d.City);
            Assert.AreEqual("Seattle", result);
        }
        [TestMethod]
        public void TestUpsertExistingCustomerWithResult()
        {
            this.TestInsertCustomerNoResult();

            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA"
            };

            var result = db.Customers.InsertOrUpdate(cust, null, d => d.City);
            Assert.AreEqual("Portland", result);
        }
        [TestMethod]
        public void TestUpsertNewCustomerWithUpdateCheck()
        {
            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA"
            };

            var result = db.Customers.InsertOrUpdate(cust, d => d.City == "Portland");
            Assert.AreEqual(1, result);
        }
        [TestMethod]
        public void TestUpsertExistingCustomerWithUpdateCheck()
        {
            this.TestInsertCustomerNoResult();

            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Portland", // moved to Portland!
                Country = "USA"
            };

            var result = db.Customers.InsertOrUpdate(cust, d => d.City == "Seattle");
            Assert.AreEqual(1, result);
        }
        [TestMethod]
        public void TestBatchUpsertNewCustomersNoResult()
        {
            int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new Customer
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Portland",
                    Country = "USA"
                });

            var results = db.Customers.Batch(custs, (u, c) => u.InsertOrUpdate(c));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, 1)));
        }
        [TestMethod]
        public void TestBatchUpsertExistingCustomersNoResult()
        {
            this.TestBatchInsertCustomersNoResult();

            int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new Customer
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Portland",
                    Country = "USA"
                });

            var results = db.Customers.Batch(custs, (u, c) => u.InsertOrUpdate(c));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, 1)));
        }
        [TestMethod]
        public void TestBatchUpsertNewCustomersWithResult()
        {
            int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new Customer
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Portland",
                    Country = "USA"
                });

            var results = db.Customers.Batch(custs, (u, c) => u.InsertOrUpdate(c, null, d => d.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, "Portland")));
        }
        [TestMethod]
        public void TestBatchUpsertExistingCustomersWithResult()
        {
            this.TestBatchInsertCustomersNoResult();

            int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new Customer
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Portland",
                    Country = "USA"
                });

            var results = db.Customers.Batch(custs, (u, c) => u.InsertOrUpdate(c, null, d => d.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, "Portland")));
        }
        [TestMethod]
        public void TestBatchUpsertNewCustomersWithUpdateCheck()
        {
            int n = 10;
            var pairs = Enumerable.Range(1, n).Select(
                i => new
                {
                    original = new Customer
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Seattle",
                        Country = "USA"
                    },
                    current = new Customer
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Portland",
                        Country = "USA"
                    }
                });

            var results = db.Customers.Batch(pairs, (u, x) => u.InsertOrUpdate(x.current, d => d.City == x.original.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, 1)));
        }
        [TestMethod]
        public void TestBatchUpsertExistingCustomersWithUpdateCheck()
        {
            this.TestBatchInsertCustomersNoResult();

            int n = 10;
            var pairs = Enumerable.Range(1, n).Select(
                i => new
                {
                    original = new Customer
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Seattle",
                        Country = "USA"
                    },
                    current = new Customer
                    {
                        CustomerID = "XX" + i,
                        CompanyName = "Company" + i,
                        ContactName = "Contact" + i,
                        City = "Portland",
                        Country = "USA"
                    }
                });

            var results = db.Customers.Batch(pairs, (u, x) => u.InsertOrUpdate(x.current, d => d.City == x.original.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, 1)));
        }
        [TestMethod]
        public void TestDeleteCustomer()
        {
            this.TestInsertCustomerNoResult();

            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA"
            };

            var result = db.Customers.Delete(cust);
            Assert.AreEqual(1, result);
        }
        [TestMethod]
        public void TestDeleteCustomerForNonExistingCustomer()
        {
            this.TestInsertCustomerNoResult();

            var cust = new Customer
            {
                CustomerID = "XX2",
                CompanyName = "Company2",
                ContactName = "Contact2",
                City = "Seattle",
                Country = "USA"
            };

            var result = db.Customers.Delete(cust);
            Assert.AreEqual(0, result);
        }
        [TestMethod]
        public void TestDeleteCustomerWithDeleteCheckThatSucceeds()
        {
            this.TestInsertCustomerNoResult();

            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA"
            };

            var result = db.Customers.Delete(cust, d => d.City == "Seattle");
            Assert.AreEqual(1, result);
        }
        [TestMethod]
        public void TestDeleteCustomerWithDeleteCheckThatDoesNotSucceed()
        {
            this.TestInsertCustomerNoResult();

            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA"
            };

            var result = db.Customers.Delete(cust, d => d.City == "Portland");
            Assert.AreEqual(0, result);
        }
        [TestMethod]
        public void TestBatchDeleteCustomers()
        {
            this.TestBatchInsertCustomersNoResult();

            int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new Customer
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Seattle",
                    Country = "USA"
                });

            var results = db.Customers.Batch(custs, (u, c) => u.Delete(c));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, 1)));
        }
        [TestMethod]
        public void TestBatchDeleteCustomersWithDeleteCheck()
        {
            this.TestBatchInsertCustomersNoResult();

            int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new Customer
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Seattle",
                    Country = "USA"
                });

            var results = db.Customers.Batch(custs, (u, c) => u.Delete(c, d => d.City == c.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, 1)));
        }
        [TestMethod]
        public void TestBatchDeleteCustomersWithDeleteCheckThatDoesNotSucceed()
        {
            this.TestBatchInsertCustomersNoResult();

            int n = 10;
            var custs = Enumerable.Range(1, n).Select(
                i => new Customer
                {
                    CustomerID = "XX" + i,
                    CompanyName = "Company" + i,
                    ContactName = "Contact" + i,
                    City = "Portland",
                    Country = "USA"
                });

            var results = db.Customers.Batch(custs, (u, c) => u.Delete(c, d => d.City == c.City));
            Assert.AreEqual(n, results.Count());
            Assert.IsTrue(results.All(r => object.Equals(r, 0)));
        }
        [TestMethod]
        public void TestDeleteWhere()
        {
            this.TestBatchInsertCustomersNoResult();

            var result = db.Customers.Delete(c => c.CustomerID.StartsWith("XX"));
            Assert.AreEqual(10, result);
        }
        [TestMethod]
        public void TestSessionIdentityCache()
        {
            NorthwindSession ns = new NorthwindSession(provider);

            // both objects should be the same instance
            var cust = ns.Customers.Single(c => c.CustomerID == "ALFKI");
            var cust2 = ns.Customers.Single(c => c.CustomerID == "ALFKI");

            Assert.IsNotNull(cust);
            Assert.IsNotNull(cust2);
            Assert.ReferenceEquals(cust, cust2);
        }
        [TestMethod]
        public void TestSessionProviderNotIdentityCached()
        {
            NorthwindSession ns = new NorthwindSession(provider);
            Northwind db2 = new Northwind(ns.Session.Provider);

            // both objects should be different instances
            var cust = ns.Customers.Single(c => c.CustomerID == "ALFKI");
            var cust2 = ns.Customers.ProviderTable.Single(c => c.CustomerID == "ALFKI");

            Assert.AreNotEqual(null, cust);
            Assert.AreNotEqual(null, cust2);
            Assert.AreEqual(cust.CustomerID, cust2.CustomerID);
            Assert.IsFalse(ReferenceEquals(cust, cust2));
        }
        [TestMethod]
        public void TestSessionSubmitActionOnModify()
        {
            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA"
            };

            db.Customers.Insert(cust);

            var ns = new NorthwindSession(provider);
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));

            // fetch the previously inserted customer
            cust = ns.Customers.Single(c => c.CustomerID == "XX1");
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));

            cust.ContactName = "Contact Modified";
            Assert.AreEqual(SubmitAction.Update, ns.Customers.GetSubmitAction(cust));

            ns.SubmitChanges();
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));

            // prove actually modified by fetching through provider
            var cust2 = this.db.Customers.Single(c => c.CustomerID == "XX1");
            Assert.AreEqual("Contact Modified", cust2.ContactName);

            // ready to be submitted again!
            cust.City = "SeattleX";
            Assert.AreEqual(SubmitAction.Update, ns.Customers.GetSubmitAction(cust));
        }
        [TestMethod]
        public void TestSessionSubmitActionOnInsert()
        {
            NorthwindSession ns = new NorthwindSession(this.provider);
            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA"
            };
            Assert.AreEqual<SubmitAction>(SubmitAction.None, ns.Customers.GetSubmitAction(cust));

            ns.Customers.InsertOnSubmit(cust);
            Assert.AreEqual<SubmitAction>(SubmitAction.Insert, ns.Customers.GetSubmitAction(cust));

            ns.SubmitChanges();
            Assert.AreEqual<SubmitAction>(SubmitAction.None, ns.Customers.GetSubmitAction(cust));

            cust.City = "SeattleX";
            Assert.AreEqual<SubmitAction>(SubmitAction.Update, ns.Customers.GetSubmitAction(cust));
        }
        [TestMethod]
        public void TestSessionSubmitActionOnInsertOrUpdate()
        {
            NorthwindSession ns = new NorthwindSession(this.provider);
            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA"
            };
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));

            ns.Customers.InsertOrUpdateOnSubmit(cust);
            Assert.AreEqual(SubmitAction.InsertOrUpdate, ns.Customers.GetSubmitAction(cust));

            ns.SubmitChanges();
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));

            cust.City = "SeattleX";
            Assert.AreEqual(SubmitAction.Update, ns.Customers.GetSubmitAction(cust));
        }
        [TestMethod]
        public void TestSessionSubmitActionOnUpdate()
        {
            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA"
            };
            this.db.Customers.Insert(cust);

            NorthwindSession ns = new NorthwindSession(this.provider);
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));

            ns.Customers.UpdateOnSubmit(cust);
            Assert.AreEqual(SubmitAction.Update, ns.Customers.GetSubmitAction(cust));

            ns.SubmitChanges();
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));

            cust.City = "SeattleX";
            Assert.AreEqual(SubmitAction.Update, ns.Customers.GetSubmitAction(cust));
        }
        [TestMethod]
        public void TestSessionSubmitActionOnDelete()
        {
            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA"
            };
            this.db.Customers.Insert(cust);

            NorthwindSession ns = new NorthwindSession(this.provider);
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));

            ns.Customers.DeleteOnSubmit(cust);
            var custHash1 = cust.GetHashCode();
            var custHash2 = cust.GetHashCode();
            var custSessTable1 = ns.Customers.GetHashCode();
            var custSessTable2 = ns.Customers.GetHashCode();
            var custSessionTable = ns.Customers;
            custSessionTable.DeleteOnSubmit(cust);

            Assert.AreEqual(SubmitAction.Delete, ns.Customers.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.Delete, custSessionTable.GetSubmitAction(cust));//ns.Customers.GetSubmitAction(cust));

            ns.SubmitChanges();
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));

            // modifications after delete don't trigger updates
            cust.City = "SeattleX";
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));
        }
        [TestMethod]
        public void TestDeleteThenInsertSamePK()
        {
            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA"
            };

            var cust2 = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company2",
                ContactName = "Contact2",
                City = "Chicago",
                Country = "USA"
            };

            this.db.Customers.Insert(cust);

            NorthwindSession ns = new NorthwindSession(this.provider);
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust2));

            ns.Customers.DeleteOnSubmit(cust);
            Assert.AreEqual(SubmitAction.Delete, ns.Customers.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust2));

            ns.Customers.InsertOnSubmit(cust2);
            Assert.AreEqual(SubmitAction.Delete, ns.Customers.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.Insert, ns.Customers.GetSubmitAction(cust2));

            ns.SubmitChanges();
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust2));

            // modifications after delete don't trigger updates
            cust.City = "SeattleX";
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));

            // modifications after insert do trigger updates
            cust2.City = "ChicagoX";
            Assert.AreEqual(SubmitAction.Update, ns.Customers.GetSubmitAction(cust2));
        }
        [TestMethod]
        public void TestInsertThenDeleteSamePK()
        {
            var cust = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company1",
                ContactName = "Contact1",
                City = "Seattle",
                Country = "USA"
            };

            var cust2 = new Customer
            {
                CustomerID = "XX1",
                CompanyName = "Company2",
                ContactName = "Contact2",
                City = "Chicago",
                Country = "USA"
            };

            this.db.Customers.Insert(cust);

            NorthwindSession ns = new NorthwindSession(this.provider);
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust2));

            ns.Customers.InsertOnSubmit(cust2);
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.Insert, ns.Customers.GetSubmitAction(cust2));

            ns.Customers.DeleteOnSubmit(cust);
            Assert.AreEqual(SubmitAction.Delete, ns.Customers.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.Insert, ns.Customers.GetSubmitAction(cust2));

            ns.SubmitChanges();
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust2));

            // modifications after delete don't trigger updates
            cust.City = "SeattleX";
            Assert.AreEqual(SubmitAction.None, ns.Customers.GetSubmitAction(cust));

            // modifications after insert do trigger updates
            cust2.City = "ChicagoX";
            Assert.AreEqual(SubmitAction.Update, ns.Customers.GetSubmitAction(cust2));
        }
    }
}
