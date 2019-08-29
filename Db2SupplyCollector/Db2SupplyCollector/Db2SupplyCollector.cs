using IBM.Data.DB2.Core;
using S2.BlackSwan.SupplyCollector;
using S2.BlackSwan.SupplyCollector.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Db2SupplyCollector
{
    public class Db2SupplyCollector : SupplyCollectorBase
    {
        public override List<string> CollectSample(DataEntity dataEntity, int sampleSize)
        {
            var result = new List<string>();
            using (var conn = new DB2Connection(dataEntity.Container.ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT {dataEntity.Name} FROM {dataEntity.Collection.Name} ORDER BY RAND() LIMIT {sampleSize}";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var val = reader[0];
                            if (val is DBNull)
                            {
                                result.Add(null);
                            }
                            else
                            {
                                result.Add(val.ToString());
                            }
                        }
                    }
                }
            }

            return result.Take(sampleSize).ToList();
        }

        public override List<string> DataStoreTypes()
        {
            return (new[] { "DB2" }).ToList();
        }

        public string BuildConnectionString(string user, string password, string database, string host, int port = 3300)
        {
            return $"server={host}:{port}; uid={user}; pwd={password}; database={database}";
        }

        public override List<DataCollectionMetrics> GetDataCollectionMetrics(DataContainer container)
        {
            var metrics = new List<DataCollectionMetrics>();
            using (var conn = new DB2Connection(container.ConnectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        @" select
                              t.tabschema as schemaName
                              ,substr(t.tabname,1,24) as tabname 
                              ,card as rows_per_table 
                              ,decimal(npages*b.pagesize/1024) as used_kb 
                              ,decimal(c.DATA_OBJECT_P_SIZE + c.INDEX_OBJECT_P_SIZE + c.LONG_OBJECT_P_SIZE + c.LOB_OBJECT_P_SIZE + c.XML_OBJECT_P_SIZE) as allocated_kb 
                              ,((decimal(npages*b.pagesize/1024)) - (decimal(float(npages)/ ((b.pagesize/1024)),9,2))) as unused_mb 
                            from 
                              syscat.tables t , syscat.tablespaces b, sysibmadm.admintabinfo c
                            where t.tbspace=b.tbspace and t.tabname=c.tabname and t.tabschema not in ('SYSIBM','SYSTOOLS')
                            order by 5 desc with ur";



                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int column = 0;

                            var schema = reader.GetString(column++);
                            var table = reader.GetString(column++);
                            var liveRows = reader.GetInt64(column++);
                            var size = reader.GetDB2Decimal(column++);
                            var totalSize = reader.GetDB2Decimal(column++);
                            var unused = reader.GetDB2Decimal(column++);

                            metrics.Add(new DataCollectionMetrics()
                            {
                                Schema = schema,
                                Name = table,
                                RowCount = liveRows,
                                TotalSpaceKB = (decimal)((totalSize)),
                                UnUsedSpaceKB = (decimal)(unused),
                                UsedSpaceKB = (decimal)(size)
                            });
                        }
                    }
                }
            }

            return metrics;
        }

        public override (List<DataCollection>, List<DataEntity>) GetSchema(DataContainer container)
        {
            var collections = new List<DataCollection>();
            var entities = new List<DataEntity>();

            using (var conn = new DB2Connection(container.ConnectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                       @"select tab.tabschema, tab.tabname as table_name,c.colname, c.typename,
                            (select count(1) from syscat.columns where default is not null and tabname = t.tabname) as default_value, 
                            c.nulls as nullable ,  c.identity as isIdentity, case when tab.type = 'P' then 'PRIMARY KEY' when tab.type = 'U' then 'UNIQUE' else '' end as constraint_type, 
                            ref.reftabname as Reference from syscat.tabconst tab 
                            join syscat.columns c on tab.tabschema = c.tabschema and tab.tabname = c.tabname 
                            join syscat.constdep dep  on tab.constname = dep.constname and tab.tabschema = dep.tabschema and tab.tabname = dep.tabname 
                            left join syscat.references ref on tab.tabname = ref.reftabname 
                            join syscat.tables t on t.tabschema = c.tabschema and t.tabname = c.tabname 
                            where tab.tabschema not like('SYS%') order by table_name";

                    DataCollection collection = null;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var schema = reader.GetDB2String(0).ToString();
                            var table = reader.GetDB2String(1).ToString();
                            var columnName = reader.GetDB2String(2).ToString();
                            var dataType = reader.GetDB2String(3).ToString();
                            var columnDef = reader.GetDB2String(4).ToString();
                            var isNullable = "Y".Equals(reader.GetDB2String(5).ToString(), StringComparison.InvariantCultureIgnoreCase);
                            var isIdentity = "Y".Equals(reader.GetDB2String(6).ToString(), StringComparison.InvariantCultureIgnoreCase);
                            var isPrimary = "PRIMARY KEY".Equals(reader.GetDB2String(7).ToString(), StringComparison.InvariantCultureIgnoreCase);
                            var isUnique = "UNIQUE".Equals(reader.GetDB2String(7).ToString(), StringComparison.InvariantCultureIgnoreCase);
                            var isRef = !string.IsNullOrEmpty(Convert.ToString(reader.GetDB2String(8)));

                            if (collection == null || !collection.Schema.Equals(schema) ||
                                !collection.Name.Equals(table))
                            {

                                collection = new DataCollection(container, table)
                                {
                                    Schema = schema
                                };
                                collections.Add(collection);
                            }

                            entities.Add(new DataEntity(columnName, ConvertDataType(dataType), dataType, container, collection)
                            {
                                IsAutoNumber = !String.IsNullOrEmpty(columnDef) && columnDef.StartsWith("nextval(", StringComparison.InvariantCultureIgnoreCase),
                                IsComputed = !String.IsNullOrEmpty(columnDef),
                                IsForeignKey = isRef,
                                IsIndexed = isPrimary || isRef,
                                IsPrimaryKey = isPrimary,
                                IsUniqueKey = isUnique
                            });
                        }
                    }
                }
            }

            return (collections, entities);
        }

        public override bool TestConnection(DataContainer container)
        {
            try
            {
                using (var conn = new DB2Connection(container.ConnectionString))
                {
                    conn.Open();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private DataType ConvertDataType(string dbDataType)
        {
            if ("integer".Equals(dbDataType))
            {
                return DataType.Long;
            }
            else if ("smallint".Equals(dbDataType))
            {
                return DataType.Short;
            }
            else if ("boolean".Equals(dbDataType))
            {
                return DataType.Boolean;
            }
            else if ("character".Equals(dbDataType))
            {
                return DataType.Char;
            }
            else if ("character varying".Equals(dbDataType))
            {
                return DataType.String;
            }
            else if ("text".Equals(dbDataType))
            {
                return DataType.String;
            }
            else if ("double precision".Equals(dbDataType))
            {
                return DataType.Double;
            }
            else if ("real".Equals(dbDataType))
            {
                return DataType.Double;
            }
            else if ("numeric".Equals(dbDataType))
            {
                return DataType.Decimal;
            }
            else if ("date".Equals(dbDataType))
            {
                return DataType.DateTime;
            }
            else if ("time without time zone".Equals(dbDataType))
            {
                return DataType.DateTime;
            }
            else if ("time with time zone".Equals(dbDataType))
            {
                return DataType.DateTime;
            }
            else if ("timestamp without time zone".Equals(dbDataType))
            {
                return DataType.DateTime;
            }
            else if ("timestamp with time zone".Equals(dbDataType))
            {
                return DataType.DateTime;
            }
            else if ("json".Equals(dbDataType))
            {
                return DataType.String;
            }
            else if ("uuid".Equals(dbDataType))
            {
                return DataType.Guid;
            }

            return DataType.Unknown;
        }
    }

    internal static class DbDataReaderExtensions
    {
        internal static string GetDbString(this DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
                return null;
            return reader.GetString(ordinal);
        }
    }
}
