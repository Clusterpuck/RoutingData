# Routing Data Interface  
Creates end points for the routing data stored in the SQL database  
Uses Entity Framework framework (7.0.20) so that the database is managed through the C# code 

Currently deployed to: https://routingdata.azurewebsites.net/swagger/index.html where API points can be tested. 

Adviced to use Visual Studio for development after cloning repo and testing locally. 

The first time running after long idle period will be delayed due to services needing to be re-activated. 

Adding a new Data Entity:  
- Add a new class to the Model folder. 
- Add a context line to the ApplicationDBContext Class
- Right click controllers folder, add scaffold item - select API with Entity Framework
- From Package Manager Console Run 
	- Add-Migration <MigrationName>
	- Update-Database


https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=vs




