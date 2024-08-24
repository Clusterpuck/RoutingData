using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoutingData.DTO;

namespace TestRoutingData
{
    public class TestOfflineDatabase : OfflineDatabase
    {

    }
    public static class TestServiceProvider
    {
        public static readonly TestOfflineDatabase OfflineDatabaseInstance = new TestOfflineDatabase();
    }

}
