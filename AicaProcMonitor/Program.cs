using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Npgsql;

namespace AicaProcMonitor
{
    class Program
    {
        static void Main(string[] args)
        {

            try
            {
                //ローカルコンピュータ上で実行されているすべてのプロセスを取得
                System.Diagnostics.Process[] ps =
                    System.Diagnostics.Process.GetProcesses();
                double total = 0;
                double postgres = 0;
                int procCount = 0;
                long serverTotal = 0;
                long useTotal = 0;
                long dbTotal = 0;
                Console.WriteLine("プロセス名\tID\t物理メモリ使用量");
                //配列から1つずつ取り出す
                foreach (System.Diagnostics.Process p in ps)
                {
                    try
                    {
                        //プロセス名を出力する
                        Console.WriteLine("{0}\t{1}\t{2}", p.ProcessName, p.Id, p.WorkingSet64);
                        total += p.WorkingSet64;
                        if (p.ProcessName.ToUpper().Contains("POSTGRES"))
                        {
                            postgres += p.WorkingSet64;
                            procCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("エラー: {0}", ex.Message);
                    }
                }
                total /= 1024;
                total /= 1024;
                postgres /= 1024;
                postgres /= 1024;

                serverTotal = GetTotalMemory();
                useTotal = (long)Math.Round(total, 0, MidpointRounding.AwayFromZero);
                dbTotal = (long)Math.Round(postgres, 0, MidpointRounding.AwayFromZero);
                Console.WriteLine("使用メモリ\t{0}\t{1}", "", useTotal);
                Console.WriteLine("DBプロセスメモリ\t{0}\t{1}", "", dbTotal);

                NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder();
                builder.Database = "Aica002";
                builder.Port = 5432;
                builder.UserName = "aica";
                builder.Password = "ai002ca";
                builder.Host = "192.168.210.185";
                using(NpgsqlConnection con = new NpgsqlConnection(builder.ToString()))
                {
                    con.Open();
                    StringBuilder sql = new StringBuilder();
                    sql.Append("CREATE TABLE IF NOT EXISTS IO_RESOURCE_LOG(");
                    sql.Append(" ID SERIAL NOT NULL,");
                    sql.Append(" EXECUTIONTIME TIMESTAMP DEFAULT NOW(),");
                    sql.Append(" TOTALMEMORY INT8 DEFAULT 0,");
                    sql.Append(" USEMEMORY INT8 DEFAULT 0,");
                    sql.Append(" POSTGRESQLPROCESS INT8 DEFAULT 0,");
                    sql.Append(" POSTGRESQLPROCESSMEMORY INT8 DEFAULT 0,");
                    sql.Append(" PRIMARY KEY(ID)");
                    sql.Append(")");
                    using(NpgsqlCommand command = new NpgsqlCommand(sql.ToString(), con))
                    {
                        command.ExecuteNonQuery();
                    }
                    sql = new StringBuilder();
                    sql.Append("INSERT INTO IO_RESOURCE_LOG (");
                    sql.Append("TOTALMEMORY");
                    sql.Append(",USEMEMORY");
                    sql.Append(",POSTGRESQLPROCESS");
                    sql.Append(",POSTGRESQLPROCESSMEMORY");
                    sql.Append(")VALUES(");
                    sql.Append(serverTotal);
                    sql.Append("," + useTotal);
                    sql.Append("," + procCount);
                    sql.Append("," + dbTotal);
                    sql.Append(")");
                    using (NpgsqlCommand command = new NpgsqlCommand(sql.ToString(), con))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("エラー: {0}", ex.Message);
            }
    
        }

        private static long GetTotalMemory()
        {
            using (System.Management.ManagementClass mc =
             new System.Management.ManagementClass("Win32_OperatingSystem"))
            {
                using(System.Management.ManagementObjectCollection moc = mc.GetInstances()){
                    foreach (System.Management.ManagementObject mo in moc)
                    {
                        //合計物理メモリ
                        UInt64 obj = (UInt64)mo["TotalVisibleMemorySize"];
                        Console.WriteLine("合計物理メモリ\t{0}\t{1}", "", obj / 1024);
                        mo.Dispose();
                        return (long)obj / 1024;
                    }
                }
            }
            return 0;
        }
    }
}
