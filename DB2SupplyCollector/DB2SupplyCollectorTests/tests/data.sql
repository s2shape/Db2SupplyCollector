connect to testdb;

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


insert into test_data_types(bool_field, char_field, varchar_field, smallint_field, int_field, bigint_field, decfloat_field, real_field, double_field, date_field, time_field, timestamp_field, xml_field)
values(True, 'char!', 'varchar!', 1, 299792458, 9223372036854775807, 6.626, 1.280649, 6.022140761234567, '2019-08-13', '17:40:00', '2019-08-13 17:40:00', '<Hello>world</Hello>');

RUNSTATS ON TABLE test_data_types

create table test_field_names (
   id int PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
   low_case integer,
   UPCASE integer,
   CamelCase integer,
   "Table" integer,
   "SELECT" integer
);

insert into test_field_names(low_case, upcase, camelcase, "Table", "SELECT")
values(0,0,0,0,0);

RUNSTATS ON TABLE test_field_names

create table test_index (
   id int NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
   name varchar(100) NOT NULL UNIQUE
);

insert into test_index(name)
values('Sunday');
insert into test_index(name)
values('Monday');
insert into test_index(name)
values('Tuesday');
insert into test_index(name)
values('Wednesday');
insert into test_index(name)
values('Thursday');
insert into test_index(name)
values('Friday');
insert into test_index(name)
values('Saturday');

RUNSTATS ON TABLE test_index

create table test_index_ref (
   id int NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
   index_id integer REFERENCES test_index(id)
);

insert into test_index_ref(index_id)
values(1);
insert into test_index_ref(index_id)
values(5);

RUNSTATS ON TABLE test_index_ref