node-datatable
==============

A Node.js implementation of a server-side processor for the jQuery DataTables plug-in.

The node-datatable module provides backend query generation and result parsing to support
[DataTables](https://www.datatables.net/manual/server-side) server-side processing for SQL databases.
This module does not connect to nor query databases, instead leaving those tasks to the calling application.
SQL querying has been separated so that the caller can leverage his or her existing module choices for connection pools,
database interfaces, and the like. This module has been used with [node-mysql](https://github.com/felixge/node-mysql),
[sequelize](http://sequelizejs.com), and [strong-oracle](https://github.com/strongloop/strong-oracle).

An incomplete code example:

```javascript
var async = require('async'),
    QueryBuilder = require('datatable');

var tableDefinition = {
    sTableName: 'Orgs'
};

var queryBuilder = new QueryBuilder(tableDefinition);

// requestQuery is normally provided by the DataTables AJAX call
var requestQuery = {
    iDisplayStart: 0,
    iDisplayLength: 5
};

// Build an object of SQL statements
var queries = queryBuilder.buildQuery(requestQuery);

// Connect to the database
var myDbObject = ...;

// Execute the SQL statements generated by queryBuilder.buildQuery
myDbObject.query(queries.changeDatabaseOrSchema, function(err){
    if (err) { res.error(err); }
    else{
        async.parallel(
            {
                recordsFiltered: function(cb) {
                    myDbObject.query(queries.recordsFiltered, cb);
                },
                recordsTotal: function(cb) {
                    myDbObject.query(queries.recordsTotal, cb);
                },
                select: function(cb) {
                    myDbObject.query(queries.select, cb);
                }
            },
            function(err, results) {
                if (err) { res.error(err); }
                else {
                    res.json(queryBuilder.parseResponse(results));
                }
            }
        );
    }
});
```

## API ##

The source code contains comments that may help you understand this module.

### Constructor ###

Construct a QueryBuilder object.

#### Parameters ####

The node-datatable constructor takes an object parameter containing
the following properties. In the simplest case, only the first
two options will be necessary.

- ```dbType``` - (Default: ```"mysql"```) The database language to use for queries
(```"mysql"```, ```"postgres"```, or ```"oracle"```). The default value is ```mysql```.

- ```sTableName``` - The name of the table in the database where a
JOIN is not used. If a JOIN is used, set ```sSelectSql```.

- ```sCountColumnName``` (Default: ```"id"```) The name of the column on which to
do a SQL COUNT(). This is overridden with ```*``` when ```sSelectSql``` is set.

- ```sDatabaseOrSchema``` - If set, ```buildQuery``` will add a ```changeDatabaseOrSchema```
property to the object returned containing a _USE sDatabaseOrSchema_ statement for
MySQL / Postgres or an _ALTER SESSION SET CURRENT_SCHEMA = sDatabaseOrSchema_ statement for Oracle.

- ```aSearchColumns``` - In database queries where JOIN is used, you may wish to specify an
alternate array of column names that the search string will be applied against. Example:

```javascript
aSearchColumns: ["table3.username", "table1.timestamp", "table1.urlType", "table1.mimeType", "table1.url", "table2.description"],
```

- ```sSelectSql``` - (Default: ```"id"```) A list of columns that will be
selected. This can be used in combination with joins (see ```sFromSql```).

- ```sFromSql``` - If set, this is used as the FROM clause of the SELECT
statement. If not set, ```sTableName``` is used. Use this for more complex
queries, for example when using JOIN. Example when using a double JOIN:

```javascript
"table1 LEFT JOIN table2 ON table1.errorId=table2.errorId LEFT JOIN table3 ON table1.sessionId=table3.sessionId"
```

- ```sWhereAndSql``` - Use this to specify arbitrary SQL that you
wish to append to the generated WHERE clauses with an AND condition.

- ```sDateColumnName``` - If this property and ```dateFrom``` and/or ```dateTo``` is set, a date range WHERE clause
will be added to the SQL query. This should be set to the name of the datetime column that is to be used in the clause.

- ```dateFrom``` - If set, the query will return records greater than or equal to this date.

- ```dateTo``` - If set, the query will return records less than or equal to this date.

#### Returns #####

The query builder object.

Example:

```javascript
var queryBuilder = new QueryBuilder({
    sTableName: 'user'
});
```

### buildQuery ###

Builds an object containing the following properties:

```changeDatabaseOrSchema```: _(Optional, if ```sDatabaseOrSchema``` is set)_ A USE sDatabaseOrSchema_ statement for
MySQL / Postgres or an _ALTER SESSION SET CURRENT_SCHEMA = sDatabaseOrSchema_ statement for Oracle.

```recordsFiltered```: _(Optional, if ```requestQuery.search.value``` is set)_ A SELECT statement that counts
the number of filtered entries in the database. This is used to calculate the ```recordsFiltered``` return value.

```recordsTotal```: A SELECT statement that counts the total number of unfiltered
entries in the database. This is used to calculate the ```recordsTotal``` return value.

```select```: A SELECT statement that returns a page of filtered records from the database.
This will use a LIMIT statement for MySQL / Postgres or a top-n query for Oracle.

Note that #2, #3, and #4 will include date filtering as well as any other filtering specified in ```sWhereAndSql```.

#### Parameters ####

- ```requestQuery```: An object containing the properties set by the client-side DataTables
library as defined in [sent parameters](https://www.datatables.net/manual/server-side#Sent-parameters).
Note that you may build your own ```requestQuery```, omitting certain properties, and achieve a different outcome.
For example, passing an empty ```requestQuery``` object will build a select statement that gets all rows from the
table. Such a scenario could be useful for building a custom file export function.

#### Returns #####

The resultant object of query strings. The ```changeDatabaseOrSchema``` query should be executed first, and the others
can be executed in sequence or (ideally) in parallel. Each database response should be collected into an object property
having a key that matches the query object. The response object can later be passed to the ```parseReponse``` function.

Example:

```javascript
var queries = queryBuilder.buildQuery(oRequestQuery);
```

### parseResponse ###

Parses an object of responses that were received in response to each of the queries generated by ```buildQuery```.

#### Parameters ####

- ```queryResult```: The object of query responses.

#### Returns #####

An object containing the properties defined in [returned data](https://www.datatables.net/manual/server-side#Returned-data).

Example:

```javascript
var result = queryBuilder.parseResponse(queryResponseObject);
res.json(result);
```

### extractResponseVal ###

Extract a value from a database response. This is useful in situations where your
database query returns a primitive value nested inside of an object inside of an array:

#### Parameters ####

- ```res```: A database response.

#### Returns #####

The first enumerable object property of the first element in an array, or undefined

Example:

```javascript
var val = queryBuilder.extractResponseVal([{COUNT(ID): 13}]);
console.log(val) //13
```

## Database queries involving JOIN ##

Example using ```sSelectSql``` and ```sFromSql``` to create a JOIN query:

```javascript
{
  sSelectSql: "table3.username,table1.timestamp,urlType,mimeType,table1.table3Id,url,table2.code,table2.description",
  sFromSql: "table1 LEFT JOIN table2 ON table1.errorId=table2.errorId LEFT JOIN table3 ON table1.sessionId=table3.sessionId",
}
```

### Contributors ###

* [Matthew Hasbach](https://github.com/mjhasbach)
* [Benjamin Flesch](https://github.com/bf)

## TODO ##

1. Add an additional parameter to allow more then the requested number of records
to be returned. This can be used to reduce the number of client-server calls (I think).
2. A more thorough SQL injection security review (volunteers?).
3. Unit tests (the original author is no longer working on the project that uses this module, so need volunteer)

## References ##

1. [Datatables Manual](http://www.datatables.net/manual/server-side)
2. [How to Handle large datasets](http://datatables.net/forums/discussion/4214/solved-how-to-handle-large-datasets/p1)
