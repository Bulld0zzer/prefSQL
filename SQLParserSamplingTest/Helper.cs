﻿using System.Configuration;

namespace prefSQL.SQLParserSamplingTest
{
    class Helper
    {
        public static string ConnectionString = ConfigurationManager.ConnectionStrings["localhost"].ConnectionString;
        public static string ProviderName = ConfigurationManager.ConnectionStrings["localhost"].ProviderName;


    }
}
