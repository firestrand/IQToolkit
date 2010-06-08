using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IQToolkit;

namespace IQToolkitTest
{
    /// <summary>
    /// Summary description for TypeHelperTests
    /// </summary>
    [TestClass]
    public class TypeHelperTests
    {
        public TypeHelperTests()
        {
 
        }

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

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
        public void Convert_Guid_To_String()
        {
            Guid guid = Guid.NewGuid();
            string expected = guid.ToString();
            string actual = TypeHelper.Convert(guid, typeof (string)) as string;
            Assert.AreEqual(expected,actual);
        }
        [TestMethod]
        public void Convert_String_To_Guid()
        {
            Guid expected = Guid.NewGuid();
            string expectedString = expected.ToString();
            Guid actual = (Guid)TypeHelper.Convert(expectedString, typeof(Guid));
            Assert.AreEqual(expected, actual);
        }
        [ExpectedException(typeof(FormatException))]
        [TestMethod]
        public void Convert_Non_Guid_String_To_Guid_Throws_FormatException()
        {
            Guid actual = (Guid)TypeHelper.Convert("000000000000", typeof(Guid));
        }
    }
}
