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
  
Syncing the database:
- Make sure Azure CLI is installed.
- In the Azure portal (quantumsqlserver), search Microsoft Entra ID and make yourself Microsoft Entra admin.
- From Package Manager Console Run 
	- Add-Migration <MigrationName>
	- Update-Database


https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=vs



Offline Database
In csproj comment and uncomment the COnditional COmpilation value OFFLINE_DATA 
to switch between using a singleton as a backup database, and using the live database. 
