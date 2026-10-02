using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using IQToolkit;
using IQToolkit.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest
{
    [TestClass]
    [TestCategory("SqlServerIntegration")]
    public class IQToolkitExtendedTests
    {
        readonly DbEntityProvider _provider = DbEntityProvider.From("IQToolkit.Data.SqlClient", Environment.GetEnvironmentVariable("IQTOOLKIT_TEST_CONNECTION") ?? "Server=localhost;Database=unused", "IQToolkitTest.IQToolkitTestModel");
        IQToolkitTestModel db;
        [TestInitialize()]
        public void TestsInit()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("IQTOOLKIT_TEST_CONNECTION")))
                Assert.Inconclusive("Set IQTOOLKIT_TEST_CONNECTION to run this SQL Server integration suite.");
            _provider.Connection.Open();
            db = new IQToolkitTestModel(_provider);
        }
        [TestMethod]
        public void CanInsertPersonWithAssociatedAddressUsingExtensionTableCapability()
        {
            var person = new IQToolkitTestModel.Person
                             {
                                 Address =
                                     new IQToolkitTestModel.Address
                                         {
                                             Line1 = "123 Fake St.",
                                             City = "Indianapolis",
                                             State = "IN",
                                             PostalCode = "46201"
                                         },
                                 FirstName = "John",
                                 LastName = "Smith"
                             };
            var result = db.People.Insert(person, p=>p.Id);
            Assert.IsTrue(result > 0);

        }
    }
}
