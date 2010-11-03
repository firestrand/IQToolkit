using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Mapping;

namespace IQToolkitTest
{
    
    public class IQToolkitTestModel
    {
        public class Person
        {
            private Address _address;
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public Address Address
            {
                get { return _address ?? (_address = new Address()); }
                set { _address = value; }
            }
        }
        public class Address
        {
            public int Id { get; set; }
            public string Line1 { get; set; }
            public string Line2 { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string PostalCode { get; set; }
        }

        private readonly DbEntityProvider provider;

        public IQToolkitTestModel(DbEntityProvider provider)
        {
            this.provider = provider;
        }
        [Table(Name = "People", Alias = "P")]
        [Column(Member = "Id", IsPrimaryKey = true, IsGenerated = true, Alias = "P")]
        [Column(Member = "FirstName", Alias = "P")]
        [Column(Member = "LastName", Alias = "P")]
        [ExtensionTable(Name = "Addresses", Alias = "A", KeyColumns = "Id", RelatedAlias = "P", RelatedKeyColumns = "AddressId")]
        [Column(Member = "Address.Id", Alias = "A")]
        [Column(Member = "Address.Line1", Alias = "A")]
        [Column(Member = "Address.Line2", Alias = "A")]
        [Column(Member = "Address.City", Alias = "A")]
        [Column(Member = "Address.State", Alias = "A")]
        [Column(Member = "Address.PostalCode", Alias = "A")]
        public IEntityTable<Person> People
        {
            get { return provider.GetTable<Person>("People"); }
        }
    }
}
