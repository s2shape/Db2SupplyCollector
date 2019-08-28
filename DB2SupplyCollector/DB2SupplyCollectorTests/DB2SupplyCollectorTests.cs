using System;
using Xunit;
using DB2SupplyCollector;
using S2.BlackSwan.SupplyCollector.Models;
using System.Collections.Generic;
using System.Linq;

namespace DB2SupplyCollectorTests
{
    public class DB2SupplyCollectorTests
    {
        private readonly DB2SupplyCollector.DB2SupplyCollector _instance;
        public readonly DataContainer _container;

        public DB2SupplyCollectorTests()
        {
            _instance = new DB2SupplyCollector.DB2SupplyCollector();
            _container = new DataContainer()
            {
                ConnectionString = _instance.BuildConnectionString("db2inst1", "mydb2container123", "testdb", "localhost", 50000)
            };
        }

        [Fact]
        public void DataStoreTypesTest()
        {
            var result = _instance.DataStoreTypes();
            Assert.Contains("DB2", result);
        }

        [Fact]
        public void TestConnectionTest()
        {
            var result = _instance.TestConnection(_container);
            Assert.True(result);
        }

        [Fact]
        public void CollectSampleTest()
        {
            var entity = new DataEntity("name", DataType.String, "character varying", _container,
                new DataCollection(_container, "test_index"));

            var samples = _instance.CollectSample(entity, 7);
            Assert.Equal(7, samples.Count);
            Assert.Contains("Wednesday", samples);
        }

        [Fact]
        public void GetDataCollectionMetricsTest()
        {
            var metrics = new DataCollectionMetrics[] {
                new DataCollectionMetrics()
                    {Name = "test_data_types", RowCount = 1, TotalSpaceKB = 1024},
                new DataCollectionMetrics()
                    {Name = "test_field_names", RowCount = 1, TotalSpaceKB = 512},
                new DataCollectionMetrics()
                    {Name = "test_index", RowCount = 7, TotalSpaceKB = 512},
                new DataCollectionMetrics()
                    {Name = "test_index_ref", RowCount = 2, TotalSpaceKB = 512}
            };

            var result = _instance.GetDataCollectionMetrics(_container);
            Assert.Equal(4, result.Count);

            foreach (var metric in metrics)
            {
                var resultMetric = result.First<DataCollectionMetrics>(x => x.Name.Trim().ToLower().Equals(metric.Name));
                Assert.NotNull(resultMetric);

                Assert.Equal(metric.RowCount, resultMetric.RowCount);
                Assert.Equal(metric.TotalSpaceKB, resultMetric.TotalSpaceKB);
            }
        }

        [Fact]
        public void GetTableNamesTest()
        {
            var (tables, elements) = _instance.GetSchema(_container);
            Assert.Equal(4, tables.Count);
            Assert.Equal(26, elements.Count);

            var tableNames = new string[] { "test_data_types", "test_field_names", "test_index", "test_index_ref" };
            foreach (var tableName in tableNames)
            {
                var table = tables.Find(x => x.Name.Trim().ToLower().Equals(tableName));
                Assert.NotNull(table);
            }
        }

        [Fact]
        public void DataTypesTest()
        {
            var (tables, elements) = _instance.GetSchema(_container);

            var dataTypes = new Dictionary<string, string>() {
                {"id_field", "integer"},
                {"bool_field", "boolean"},
                {"char_field", "character"},
                {"varchar_field", "varchar"},
                {"smallint_field", "smallint"},
                {"int_field", "integer"},
                {"bigint_field", "bigint"},
                {"decfloat_field", "decfloat"},
                {"real_field", "real"},
                {"double_field", "double"},
                {"date_field", "date"},
                {"time_field", "time"},
                {"timestamp_field", "timestamp"},
                {"xml_field", "xml"}
            };

            var columns = elements.Where(x => x.Collection.Name.ToLower().Trim().Equals("test_data_types")).ToArray();
            Assert.Equal(dataTypes.Count, columns.Length);

            foreach (var column in columns)
            {
                var colname = column.Name.ToLower().Trim();
                Assert.Contains(colname, (IDictionary<string, string>)dataTypes);
                Assert.Equal(column.DbDataType.Trim().ToLower(), dataTypes[colname]);
            }
        }

        [Fact]
        public void SpecialFieldNamesTest()
        {
            var (tables, elements) = _instance.GetSchema(_container);

            var fieldNames = new string[] { "id", "low_case", "upcase", "camelcase", "table", "select" }; // first 4 without quotes are converted to lower case

            var columns = elements.Where(x => x.Collection.Name.Trim().ToLower().Equals("test_field_names")).ToArray();
            Assert.Equal(fieldNames.Length, columns.Length);

            foreach (var column in columns)
            {
                Assert.Contains(column.Name.Trim().ToLower(), fieldNames);
            }
        }

        [Fact]
        public void AttributesTest()
        {
            var (tables, elements) = _instance.GetSchema(_container);

            var idFields = elements.Where(x => x.Name.Trim().ToLower().Equals("id")).ToArray();
            Assert.Equal(4, idFields.Length);

            foreach (var idField in idFields)
            {
                Assert.Equal(DataType.Unknown, idField.DataType);
                Assert.True(idField.IsPrimaryKey || idField.IsUniqueKey);
            }

            var uniqueField = elements.Find(x => x.Name.Trim().ToLower().Equals("name") && x.IsUniqueKey);
            Assert.True(uniqueField.IsUniqueKey);

            var refField = elements.Find(x => x.Name.Trim().ToLower().Equals("index_id"));
            Assert.True(refField.IsForeignKey);

            foreach (var column in elements)
            {

                if (string.IsNullOrEmpty(column.Schema) || !column.Schema.Contains("db2") || column.Name.Equals("id") || column.Name.Equals("name") || column.Name.Equals("index_id") || column.Name.Equals("serial_field"))
                {
                    continue;
                }

                Assert.False(column.IsPrimaryKey);
                Assert.False(column.IsAutoNumber);
                Assert.False(column.IsForeignKey);
                Assert.False(column.IsIndexed);
            }
        }
    }
}
