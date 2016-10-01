# CRUDpanel
Microsoft SQL Server database CRUD operations panel. It adjusts the existing database and automatically generates Create, Read, Update and Delete forms for each table.

## Technologies
- ASP.NET MVC 5
- Angularjs 1.4.7
- Bootstrap 3.3.6
- Angular-UI-Bootstrap 2.1.3

## Setup
It's simple:
  1. Download the repository.
  2. Open `CRUDpanel.csproj` in Visual Studio.
  3. Open `Web.config` file and change `connectionString` section to connect your server and database.
  4. Save the Solution as `../CRUDpanel-master/CRUDpanel.sln`
  5. Run the project from saved solution file and enjoy :)
  
## Data Types
CRUDpanel supports the following MS SQL Data Types
#### Numerics
- `int`
- `float`
- `bit`
- `decimal`
- `bigint`
- `smallint`
- `tinyint`
- `numeric`

#### Strings
- `nvarchar`
- `varchar`
- `nchar`
- `char`
- `ntext`
- `text`

`ntext` and `text` types only works when table has identity column.

#### Date and Time
- `datetime`
- `date`
  
## Working Specifications
CRUDpanel can work with databases which hasn't relationships between tables and in this situation it works fine but with limited functionality. With relationships it gives some features.
  
## Features
- `One to Many` relationship: When you edit record's view with update button, CRUDpanel retrives information and shows you table with records which are dependant on this record.
- `Many to One` relationship: When you create or update record, CRUDpanel generates the list of FK tables, so you can choose record from these FK tables.
