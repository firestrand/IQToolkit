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
    public class IQToolkitExtendedTests
    {
        readonly DbEntityProvider _provider = DbEntityProvider.From("IQToolkit.Data.SqlClient", @"Data Source=(local);Initial Catalog=IQToolkitTest;Integrated Security=True", "IQToolkitTest.IQToolkitTestModel");
        IQToolkitTestModel db;
        [TestInitialize()]
        public void TestsInit()
        {
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
