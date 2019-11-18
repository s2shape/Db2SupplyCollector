using System;
using System.Runtime.InteropServices;
using System.Text;
using IBM.Data.DB2.Core;
using S2.BlackSwan.SupplyCollector.Models;
using SupplyCollectorDataLoader;

namespace Db2SupplyCollectorLoader
{
    public class Db2SupplyCollectorLoader : SupplyCollectorDataLoaderBase
    {
        public override void InitializeDatabase(DataContainer dataContainer) {
            // Do nothing
        }

        private DB2Type ConvertDbType(DataType dataType) {
            switch (dataType) {
                case DataType.String:
                    return DB2Type.VarChar;
                case DataType.Int:
                    return DB2Type.Integer;
                case DataType.Double:
                    return DB2Type.Double;
                case DataType.Boolean:
                    return DB2Type.Boolean;
                case DataType.DateTime:
                    return DB2Type.Timestamp;
                default:
                    return DB2Type.Integer;
            }
        }

        public override void LoadSamples(DataEntity[] dataEntities, long count) {
            using (var conn = new DB2Connection(dataEntities[0].Container.ConnectionString)) {
                conn.Open();

                var sb = new StringBuilder();
                sb.Append("CREATE TABLE ");
                sb.Append(dataEntities[0].Collection.Name);
                sb.Append("(\n");
                sb.Append("id_field int PRIMARY KEY GENERATED ALWAYS AS IDENTITY");

                foreach (var dataEntity in dataEntities) {
                    sb.Append(",\n");
                    sb.Append(dataEntity.Name);
                    sb.Append(" ");

                    switch (dataEntity.DataType) {
                        case DataType.String:
                            sb.Append("varchar(1024)");
                            break;
                        case DataType.Int:
                            sb.Append("int");
                            break;
                        case DataType.Double:
                            sb.Append("double");
                            break;
                        case DataType.Boolean:
                            sb.Append("boolean");
                            break;
                        case DataType.DateTime:
                            sb.Append("timestamp");
                            break;
                        default:
                            sb.Append("int");
                            break;
                    }
                }

                sb.Append(");");

                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = sb.ToString();
                    cmd.ExecuteNonQuery();
                }

                sb = new StringBuilder();
                sb.Append("INSERT INTO ");
                sb.Append(dataEntities[0].Collection.Name);
                sb.Append("(");

                bool first = true;
                foreach (var dataEntity in dataEntities) {
                    if (!first) {
                        sb.Append(", ");
                    }
                    sb.Append(dataEntity.Name);
                    first = false;
                }
                sb.Append(") VALUES (");

                first = true;
                foreach (var dataEntity in dataEntities)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }

                    sb.Append("@");
                    sb.Append(dataEntity.Name);
                    first = false;
                }

                sb.Append(");");

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sb.ToString();
                    foreach (var dataEntity in dataEntities) {
                        cmd.Parameters.Add(new DB2Parameter($"@{dataEntity.Name}", ConvertDbType(dataEntity.DataType)));
                    }
                    long rows = 0;

                    var r = new Random();

                    while (rows < count) {
                        foreach (var dataEntity in dataEntities) {
                            object val;

                            switch (dataEntity.DataType) {
                                case DataType.String:
                                    val = new Guid().ToString();
                                    break;
                                case DataType.Int:
                                    val = r.Next();
                                    break;
                                case DataType.Double:
                                    val = r.NextDouble();
                                    break;
                                case DataType.Boolean:
                                    val = r.Next(100) > 50;
                                    break;
                                case DataType.DateTime:
                                    val = DateTimeOffset
                                        .FromUnixTimeMilliseconds(
                                            DateTimeOffset.Now.ToUnixTimeMilliseconds() + r.Next()).DateTime;
                                    break;
                                default:
                                    val = r.Next();
                                    break;
                            }

                            cmd.Parameters[$"@{dataEntity.Name}"].Value = val;
                        }

                        if (rows % 1000 == 0) {
                            Console.Write(".");
                        }

                        cmd.ExecuteNonQuery();

                        rows++;
                    }
                }
            }
        }

        public override void LoadUnitTestData(DataContainer dataContainer) {
            using (var conn = new DB2Connection(dataContainer.ConnectionString)) {
                conn.Open();

                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        create table test_data_types (
                           id_field int PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                           bool_field boolean,
                           char_field char(40),
                           varchar_field varchar(100),
                           smallint_field smallint,
                           int_field integer,
                           bigint_field bigint,
                           decfloat_field decfloat,
                           real_field real,
                           double_field double,
                           date_field date,
                           time_field time,
                           timestamp_field timestamp,
                           xml_field xml
                        );
                    ";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        insert into test_data_types(bool_field, char_field, varchar_field, smallint_field, int_field, bigint_field, decfloat_field, real_field, double_field, date_field, time_field, timestamp_field, xml_field)
                        values(True, 'char!', 'varchar!', 1, 299792458, 9223372036854775807, 6.626, 1.280649, 6.022140761234567, '2019-08-13', '17:40:00', '2019-08-13 17:40:00', '<Hello>world</Hello>');
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        RUNSTATS ON TABLE test_data_types;
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        create table test_field_names (
                           id int PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                           low_case integer,
                           UPCASE integer,
                           CamelCase integer,
                           ""Table"" integer,
                           ""SELECT"" integer
                        );
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        insert into test_field_names(low_case, upcase, camelcase, ""Table"", ""SELECT"")
                            values(0, 0, 0, 0, 0);
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        RUNSTATS ON TABLE test_field_names;
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        create table test_index (
                           id int NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                           name varchar(100) NOT NULL UNIQUE
                        );
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        insert into test_index(name)
                        values('Sunday');
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        insert into test_index(name)
                        values('Monday');
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        insert into test_index(name)
                        values('Tuesday');
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        insert into test_index(name)
                        values('Wednesday');
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        insert into test_index(name)
                        values('Thursday');
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        insert into test_index(name)
                        values('Friday');
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        insert into test_index(name)
                        values('Saturday');
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        RUNSTATS ON TABLE test_index;
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        create table test_index_ref (
                           id int NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                           index_id integer REFERENCES test_index(id)
                        );
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        insert into test_index_ref(index_id)
                        values(1);
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        insert into test_index_ref(index_id)
                        values(5);
                    ";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand()) {
                    cmd.CommandText = @"
                        RUNSTATS ON TABLE test_index_ref;
                    ";
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
