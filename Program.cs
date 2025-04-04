using System;
using System.Threading;
using System.Data;
using System.Data.SqlClient;
using NLog;


namespace PYVS_MeltshopDataCollector
{
    partial class Program
    {
        //define members
        private static readonly int _delay = 600000;    //main delay time (milisecond)

        private static string _conHIS = "Data Source = 172.19.71.211; Initial Catalog = L2HIS; Persist Security Info=True;User ID = MS_L2USER; Password=MS_L2USER;MultipleActiveResultSets=True;Pooling=true;Max Pool Size=10;Connection Lifetime = 120; Connect Timeout = 60";
        private static string _conEXT = "Data Source = 172.19.71.211; Initial Catalog = L2EXCH; Persist Security Info=True;User ID = MS_L2USER; Password=MS_L2USER;MultipleActiveResultSets=True;Pooling=true;Max Pool Size=10;Connection Lifetime = 120; Connect Timeout = 60";
        private static bool dbStatus = false; //database status
        private static int lastheat = 0;

        //logging object - NLog package library
        private static Logger _log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            //set console size(width,height)
            Console.SetWindowSize(80, 30);

            _log.Info("==================================================");
            _log.Info("PYVS_MeltshopDataCollector Process start...");
            _log.Info("Location : {0}", Environment.CurrentDirectory);
            _log.Info("==================================================");

            try
            {
                MeltshopData meltshopData = new MeltshopData();

                while (true)
                {
                    //checking database connection
                    if (dbStatus == false) InitializeDataBase();

                    //data initialize
                    meltshopData.InitData();

                    //check last heat information
                    int reportHeatno = GetLastHeat();

                    
                    //for test - old heat "SELECT TOP 100 REPORT_COUNTER FROM REPORTS WHERE AREA_ID = 400 ORDER BY REPORT_COUNTER DESC"
                    //reportHeatno = 125320;

                    //check report number
                    if (reportHeatno == 0)
                    {
                        _log.Warn("Fail to get heat report : {0}", reportHeatno);
                    }

                    //compare last sent heat number
                    if (reportHeatno > 0 && lastheat != reportHeatno)
                    {
                        _log.Info("Found new heat report : {0}", reportHeatno);

                        //Collect data
                        if (GetMeltshopData(reportHeatno, ref meltshopData))
                        {
                            //data send to MES
                            if (SendMessage(ref meltshopData))
                            {
                                lastheat = reportHeatno;
                                _log.Info("Data send complete... {0}/{1}", reportHeatno, meltshopData.heat.ToString());
                                _log.Info("==================================================");
                            }
                        }
                    }
                    Console.Write(".");
                    Thread.Sleep(_delay);
                }//end while                

            }
            catch (Exception e)
            {
                _log.Error(e.Message);
            }
        }

        private static void InitializeDataBase()
        {
            try
            {
                _log.Warn("Database initialize.....");

                // Connection check...
                using (SqlConnection conn = new SqlConnection(_conHIS))
                {
                    conn.Open();
                    dbStatus = true;
                }
                using (SqlConnection conn = new SqlConnection(_conEXT))
                {
                    conn.Open();
                }
                _log.Info("Database initialize success....");
            }
            catch (Exception ex)
            {
                dbStatus = false;
                _log.Error(ex.Message);
            }
        }

        private static int GetLastHeat()
        {
            int reportno = 0;

            try
            {
                #region SQL
                //Get last report heat number
                string sql = "SELECT REPORT_COUNTER FROM REPORTS WHERE REPORT_COUNTER = (SELECT TOP 1 REPORT_COUNTER FROM REPORTS WHERE AREA_ID = 400 ORDER BY REPORT_COUNTER DESC)";
                #endregion

                //
                using (SqlConnection con = new SqlConnection(_conHIS))
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.CommandType = CommandType.Text;
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            reportno = Convert.ToInt32(reader["REPORT_COUNTER"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                dbStatus = false;
                _log.Error(ex.Message);
            }

            return reportno;
        }

        private static bool GetMeltshopData(int reportno, ref MeltshopData mdata)
        {
            bool flag = false;
            try
            {

                #region SQL
                //Get last report heat number
                string sql = "SELECT HEAT_ID, PO_ID, " +
                    "ISNULL((SELECT ELECTRIC_ENERGY FROM REP_EAF_STEPS WHERE REPORT_COUNTER = " + reportno + " AND STEP_NAME = 'PREPARATION'),0) AS PRE_EE," +
                    "ISNULL((SELECT ELECTRIC_ENERGY FROM REP_EAF_STEPS WHERE REPORT_COUNTER = " + reportno + " AND STEP_NAME = 'MELTING1'),0) AS MELT1_EE," +
                    "ISNULL((SELECT ELECTRIC_ENERGY FROM REP_EAF_STEPS WHERE REPORT_COUNTER = " + reportno + " AND STEP_NAME = 'MELTING2'),0) AS MELT2_EE," +
                    "ISNULL((SELECT ELECTRIC_ENERGY FROM REP_EAF_STEPS WHERE REPORT_COUNTER = " + reportno + " AND STEP_NAME = 'MELTING3'),0) AS MELT3_EE," +
                    "ISNULL((SELECT ELECTRIC_ENERGY FROM REP_EAF_STEPS WHERE REPORT_COUNTER = " + reportno + " AND STEP_NAME = 'MELTING EXTRA'),0) AS MELTEX_EE," +
                    "ISNULL((SELECT ELECTRIC_ENERGY FROM REP_EAF_STEPS WHERE REPORT_COUNTER = " + reportno + " AND STEP_NAME = 'REFINING'),0) AS REF_EE," +
                    "ISNULL((SELECT TOTAL_MODULE_FUEL FROM REP_EAF_STEPS WHERE REPORT_COUNTER = " + reportno + " AND STEP_NAME = 'PREPARATION'),0) AS PRE_FUEL," +
                    "ISNULL((SELECT TOTAL_MODULE_FUEL FROM REP_EAF_STEPS WHERE REPORT_COUNTER = " + reportno + " AND STEP_NAME = 'MELTING1'),0) AS MELT1_FUEL," +
                    "ISNULL((SELECT TOTAL_MODULE_FUEL FROM REP_EAF_STEPS WHERE REPORT_COUNTER = " + reportno + " AND STEP_NAME = 'MELTING2'),0) AS MELT2_FUEL," +
                    "ISNULL((SELECT TOTAL_MODULE_FUEL FROM REP_EAF_STEPS WHERE REPORT_COUNTER = " + reportno + " AND STEP_NAME = 'MELTING3'),0) AS MELT3_FUEL," +
                    "ISNULL((SELECT TOTAL_MODULE_FUEL FROM REP_EAF_STEPS WHERE REPORT_COUNTER = " + reportno + " AND STEP_NAME = 'MELTING EXTRA'),0) AS MELTEX_FUEL," +
                    "ISNULL((SELECT TOTAL_MODULE_FUEL FROM REP_EAF_STEPS WHERE REPORT_COUNTER = " + reportno + " AND STEP_NAME = 'REFINING'),0) AS REF_FUEL " +
                    "FROM REPORTS WHERE REPORT_COUNTER = " + reportno;
                #endregion

                //_log.Debug(sql);

                using (SqlConnection con = new SqlConnection(_conHIS))
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.CommandType = CommandType.Text;
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            mdata.heat = reader["HEAT_ID"].ToString().Trim();
                            mdata.porder = reader["PO_ID"].ToString().Trim();

                            mdata.eafEE_pre = Convert.ToInt32(reader["PRE_EE"]);
                            mdata.eafEE_melt1 = Convert.ToInt32(reader["MELT1_EE"]);
                            mdata.eafEE_melt2 = Convert.ToInt32(reader["MELT2_EE"]);
                            mdata.eafEE_melt3 = Convert.ToInt32(reader["MELT3_EE"]);
                            mdata.eafEE_meltExtra = Convert.ToInt32(reader["MELTEX_EE"]);
                            mdata.eafEE_ref = Convert.ToInt32(reader["REF_EE"]);

                            mdata.eafModuleFuel_pre = Convert.ToInt32(reader["PRE_FUEL"]);
                            mdata.eafModuleFuel_melt1 = Convert.ToInt32(reader["MELT1_FUEL"]);
                            mdata.eafModuleFuel_melt2 = Convert.ToInt32(reader["MELT2_FUEL"]);
                            mdata.eafModuleFuel_melt3 = Convert.ToInt32(reader["MELT3_FUEL"]);
                            mdata.eafModuleFuel_meltExtra = Convert.ToInt32(reader["MELTEX_FUEL"]);
                            mdata.eafModuleFuel_ref = Convert.ToInt32(reader["REF_FUEL"]);

                            _log.Info("Data collecting finished ...");
                            flag = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                dbStatus = false;
                _log.Error(ex.Message);
            }
            return flag;
        }

        private static bool SendMessage(ref MeltshopData msg)
        {
            string msgHead = string.Empty;
            string msgData = string.Empty;
            string sql = string.Empty;
            bool flag = false;

            try
            {
                // Set message - Header
                msgHead += "M20RL029";                              //TC Code
                msgHead += "2000";                                  //send factory code
                msgHead += "L02";                                   //sending process code
                msgHead += "2000";                                  //receive factory code
                msgHead += "M20";                                   //receive process code
                msgHead += DateTime.Now.ToString("yyyyMMddHHmmss"); //sending date time
                msgHead += "L3SentryCCMMAN";                        //send program ID
                msgHead += "RM20L02_01".PadRight(19);               //EAI IF ID (Queue name)
                msgHead += "000186".PadRight(31);                   //message length & spare 

                // Set message - Data
                msgData += msg.heat.ToString().PadRight(6);         //heat id
                msgData += msg.porder.ToString().PadRight(6);       //Production order ID
                msgData += "0".ToString();                          //strand number (not use)
                msgData += "1".ToString();                          //Repeating count (default 1)

                msgData += msg.eafEE_pre.ToString("D8");            //EAF Preparation step electric energy
                msgData += msg.eafEE_melt1.ToString("D8");          //EAF Melting 1 step electric energy
                msgData += msg.eafEE_melt2.ToString("D8");          //EAF Melting 2 step electric energy
                msgData += msg.eafEE_melt3.ToString("D8");          //EAF Melting 3 step electric energy
                msgData += msg.eafEE_meltExtra.ToString("D8");      //EAF Melting Extra step electric energy
                msgData += msg.eafEE_ref.ToString("D8");            //EAF Refining step electric energy

                msgData += msg.eafModuleFuel_pre.ToString("D4");    //EAF Preparation step total module fuel(NG)
                msgData += msg.eafModuleFuel_melt1.ToString("D4");  //EAF Melting 1 step total module fuel(NG)
                msgData += msg.eafModuleFuel_melt2.ToString("D4");  //EAF Melting 2 step total module fuel(NG)
                msgData += msg.eafModuleFuel_melt3.ToString("D4");  //EAF Melting 3 step total module fuel(NG)
                msgData += msg.eafModuleFuel_meltExtra.ToString("D4");//EAF Melting Extra steptotal module fuel(NG)
                msgData += msg.eafModuleFuel_ref.ToString("D4");    //EAF Refining step total module fuel(NG)

                // Insert database
                sql = "INSERT INTO TT_L2_L3_NEW (HEADER, DATA, MSG_CODE, INTERFACE_ID) VALUES (" +
                       "'" + msgHead + "'," +
                       "'" + msgData + "'," +
                       "'M20RL029'," +
                       "'RM20L02_01')";

                using (SqlConnection con = new SqlConnection(_conEXT))
                {
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.CommandType = CommandType.Text;
                    con.Open();

                    int rowsAffected = cmd.ExecuteNonQuery();
                    flag = true;
                }
                _log.Debug(" --- MES sent ==> sql[{0}]", sql);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            return flag;
        }

    }

}
