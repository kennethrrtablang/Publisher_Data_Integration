using System;
using Publisher_Data_Operations;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Azure.Storage.Blobs;
using Publisher_Data_Operations.Helper;
using Publisher_Data_Operations.Extensions;
using System.IO;

namespace DLL_Core_Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            // use the local.settings.json file instead of the user secrets to match the PDI_Azure_Function
            // https://blog.hildenco.com/2020/05/configuration-in-net-core-console.html
            /* 
            {
                "PDI_ConnectionString": "[PDI Connection string here]",
                "PUB_ConnectionString": "[Publisher Connection string here]",
                "FileFolder": "[File Directory For Processing]",
                "ValidationFolder": "[File Directory For Validation]"
            }
            */
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"local.settings.json", true, true)
                .AddJsonFile($"appsettings.{env}.json", true, true)
                .AddEnvironmentVariables();

            var config = builder.Build();


            string con = config["PDI_ConnectionString"]; //ConnectionStrings:pdi
            string conPub = config["PUB_ConnectionString"];
            string fileDir = config["FileFolder"];
            string valDir = config["ValidationFolder"];

            string sapdi = config["sapdi"];
            string valContainer = config["TemplateContainer"];

            string smtpAccount = config["SMTP_Account"];
            string smtpPassword = config["SMTP_Password"];
            string smtpFromEmail = config["SMTP_FromEmail"];



            //Generic gn = new Generic(con, null);

            //RowIdentity row = new RowIdentity(2, 1003, 4, "test");
            //gn.ClientID = 1003;
            //gn.LobID = 4;
            //gn.DocTypeID = 2;

            //gn.SearchFrench(row, "INVESTMENT FUNDS", 888, "Test");


            //string sql = "SELECT DISTINCT c.FEED_COMPANY_ID AS Client_ID, t.DOCUMENT_TYPE_ID, LEFT(SUBSTRING(d.DOCUMENT_NUMBER, PATINDEX('%[0-9.-]%', d.DOCUMENT_NUMBER), 8000), PATINDEX('%[^0-9.-]%', SUBSTRING(d.DOCUMENT_NUMBER, PATINDEX('%[0-9.-]%', d.DOCUMENT_NUMBER), 8000) + 'X') - 1) AS FundCode, a.FIELD_NAME, e.[CONTENT] AS english, f.[CONTENT] AS french FROM dbo.DOCUMENT_FIELD_VALUE AS e INNER JOIN dbo.DOCUMENT_FIELD_VALUE AS f ON f.DOCUMENT_ID = e.DOCUMENT_ID AND f.DOCUMENT_FIELD_ID = e.DOCUMENT_FIELD_ID AND e.LANGUAGE_ID = 1 AND f.LANGUAGE_ID = 2 INNER JOIN dbo.DOCUMENT_FIELD_ATTRIBUTE AS a ON a.DOCUMENT_FIELD_ID = e.DOCUMENT_FIELD_ID INNER JOIN dbo.[DOCUMENT] AS d ON d.DOCUMENT_ID = e.DOCUMENT_ID INNER JOIN dbo.LINE_OF_BUSINESS AS l ON l.BUSINESS_ID = d.BUSINESS_ID INNER JOIN dbo.COMPANY AS c ON c.COMPANY_ID = l.COMPANY_ID INNER JOIN dbo.DOCUMENT_TEMPLATE AS t ON t.DOCUMENT_TEMPLATE_ID = d.DOCUMENT_TEMPLATE_ID WHERE(c.FEED_COMPANY_ID >= 1000) AND(t.DOCUMENT_TYPE_ID IN(12, 13, 14)) AND(a.FIELD_NAME LIKE '%P3%h') AND (a.IS_ACTIVE = 1) AND (d.IS_ACTIVE = 1) AND (t.IS_ACTIVE = 1) AND (l.IS_ACTIVE = 1) AND (e.IS_ACTIVE = 1) AND (f.IS_ACTIVE = 1) AND (e.[CONTENT] <> N'' AND e.[CONTENT] <> N'<p></p>') AND c.FEED_COMPANY_ID = @clientID and DOCUMENT_TYPE_ID = @docTypeID;";

            /*
            DBConnection dbConPub = new DBConnection(conPub);
            PDIFile pdiFile = new PDIFile("CIBCM_IAF_NLOB_STATIC_SFS_20221129_132200_2.xlsx", con, true);
            PDIStream ps = new PDIStream(@"C:\Users\Scott\source\Sample Files\Test\Sort Order\CIBCM_IAF_NLOB_STATIC_SFS_20221129_132200_2.xlsx", pdiFile);
            Orchestration orch2 = new Orchestration(con, Environment.GetEnvironmentVariable("PUB_ConnectionString"), ps);

            orch2.LoadTempTable(dbConPub, (int)pdiFile.JobID, true);
            */
            //CustomDataTable dtHistorict = "<table><row><cell>Dec. 31&lt;br/&gt;2012</cell><cell>8.05</cell></row><row><cell>Dec. 31&lt;br/&gt;2013</cell><cell>15.19</cell></row><row><cell>Dec. 31&lt;br/&gt;2014*</cell><cell>Dec. 31&lt;br/&gt;2014*</cell></row><row><cell>Dec. 31&lt;br/&gt;2015*</cell><cell>Dec. 31&lt;br/&gt;2015*</cell></row><row><cell>Dec. 31&lt;br/&gt;2016*</cell><cell>Dec. 31&lt;br/&gt;2016*</cell></row><row><cell>Dec. 31&lt;br/&gt;2017</cell><cell>8.17</cell></row><row><cell>Dec. 31&lt;br/&gt;2018</cell><cell>-4.64</cell></row><row><cell>Dec. 31&lt;br/&gt;2019</cell><cell>13.23</cell></row><row><cell>Dec. 31&lt;br/&gt;2020</cell><cell>4.70</cell></row><row><cell>Dec. 31&lt;br/&gt;2021</cell><cell>13.92</cell></row></table>".XMLtoDataTable();
            /*
                        string dtCurrent = "<table><row><cell>HSBC Canadian Bond Fund - Investor Series - Net Assets per Unit<sup>(1)</sup></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>Year(s) ended December 31</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell></cell><cell>2023</cell><cell>2021</cell><cell>2020</cell><cell>2019</cell><cell>2018</cell><cell>2017</cell></row><row><cell>Net assets per unit, beginning of period(2)</cell><cell>$14.69</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Increase (decrease) from operations:</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>Total revenue</cell><cell>0.16</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total expenses</cell><cell>(0.06)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Realized gains (losses)</cell><cell>(0.16)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Unrealized gains (losses)</cell><cell>(1.44)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total increase (decrease) from operations(2)</cell><cell>$(1.50)</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Distributions to unitholders:</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>From net investment income (excluding dividends)</cell><cell>(0.11)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>From dividends</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>From capital gains</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Return of capital</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total annual distributions(2, 3)</cell><cell>$(0.11)</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Net assets per unit, end of period(2)</cell><cell>$121.12</cell><cell>$13.69</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row></table>";

                        //"<table><row><cell>Dec. 31&lt;br/&gt;2022</cell><cell>8.05</cell></row></table>";

                        // M12a 966636"<table><row><cell>Ratios and Supplemental Data</cell><cell></cell></row><row><cell></cell><cell>2023</cell></row><row><cell>Total net asset value (in 000s)(4)</cell><cell>$113,134</cell></row><row><cell>Number of units outstanding (in 000s)(4)</cell><cell>9,333</cell></row><row><cell>Management expense ratio (&quot;MER&quot;)(5)</cell><cell>1.14%</cell></row><row><cell>MER before waivers or absorptions(5)</cell><cell>1.14%</cell></row><row><cell>Trading expense ratio(6)</cell><cell>n/a</cell></row><row><cell>Portfolio turnover rate(7)</cell><cell>20.88%</cell></row><row><cell>Total net asset value per unit(4)</cell><cell>$12.12</cell></row></table>";//"<table><row><cell>Ratios and Supplemental Data</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell></cell><cell>2022</cell><cell>2021</cell><cell>2020</cell><cell>2019</cell><cell>2018</cell><cell>2017</cell></row><row><cell>Total net asset value (in 000s)(4)</cell><cell>$113,134</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Number of units outstanding (in 000s)(4)</cell><cell>9,333</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Management expense ratio (&quot;MER&quot;)(5)</cell><cell>1.14%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell></row><row><cell>MER before waivers or absorptions(5)</cell><cell>1.14%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell></row><row><cell>Trading expense ratio(6)</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell></row><row><cell>Portfolio turnover rate(7)</cell><cell>20.88%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell></row><row><cell>Total net asset value per unit(4)</cell><cell>$12.12</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row></table>"; //

                        // M11a 966636 

                        // M15 426747 "<table><row><cell>Dec. 31&lt;br/&gt;2022</cell><cell>8.05</cell></row></table>".XMLtoDataTable();
                        string dbConPub = "Server=tcp:publisher.database.windows.net,1433;Initial Catalog=PUBLISHER_STAGING;Persist Security Info=False;User ID=dba;Password=m@plethorpe1;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

                        MergeTables mt = new MergeTables(2, 1, -1, 6, "Sep", 1); // M12 new MergeTables(1, 1, -1, 6); // M11 new MergeTables(2,1,-1,6); // new MergeTables(-1, 0, -1, 0, 11, -1);

                        mt.LoadHistoricalData("M11", new RowIdentity(1, 1005, 1, "966636"), dbConPub);
                        //CustomDataTable dtHistoric = mt.GetHistoricFieldTable("M11a");

                        string temp = mt.GetHistoricFieldString("M11a");

                        //bool isRerun = mt.IsRerun(dtCurrent, dtHistoric);

                        //mt.Merge(dtCurrent.XMLtoDataTable(), mt.GetHistoricFieldTable("M15i"));

                        string temp2 = mt.MergeTableData(dtCurrent, "M11a");

                        Console.WriteLine("Finished");
            */
            //string result = mt.MergeTableData(dtCurrent, "M12a");

            //Console.WriteLine(result);
            //System.Data.DataTable dt = new System.Data.DataTable("Test");

            //dbCon.LoadDataTable(sql, new Dictionary<string, object>(2) { { "@clientID", 1003 }, { "@docTypeID", 13 } }, dt, false, "PUBLISHER_PROD");

            //ZipHandler zip = new ZipHandler(@"C:\Users\Scott\source\Sample Files\FSMRFP\PROD\CIBCM_IAF_NLOB_FSMRFP_MRFP_20211016_225401_1.zip", con, conPub);

            //object rep = new { blank = 3 + 1, nonBlank = 6 + 1, lastRow = 2, rMax = 10, cMax = "D" };
            //string s = ZipHandler.ConvertRange("M12{{i-z}}|Series{{-1}}|{{blank-1}}");
            //orch.ProcessFile(564, fileDir, valDir, true);

            //bool ret = orch.ProcessFile($"{fileDir}CIBCM_CMSB_NLOB_BAU_FF_20210323_122050_30.xlsx", valDir);



            //var p = pdiFile.LoadFileParameters();


            //PDIBatch batch = new PDIBatch(con, 85);



            //bool test = batch.IsComplete();

            //PDISendGrid sg = new PDISendGrid(config["SMTP_Password"]);

            //sg.SendBatchMessage(batch, "skonkle@investorcom.com", "TEST BATCH");
            //sg.SendTemplateMessage(ps, "skonkle@investorcom.com", "TEST TEMPLATE");

            //----------------------------------------------------------------------------------------
            string filePath = @"C:\Users\fsofi\source\Sample Files\Test\CIBCM_IAF_NLOB_50427167_FSMRFP_MRFP_20230212_010101_3.xlsx";

            DownloadTemplate(filePath, valDir, sapdi, valContainer);

            Orchestration orch = new Orchestration(con, conPub); // conPub

            //orch.ClearImportSP();

            System.Diagnostics.Stopwatch sp = new System.Diagnostics.Stopwatch();
            sp.Start();
            orch.ProcessFile(filePath, valDir, 1);
            sp.Stop();

            Console.WriteLine($"Elapsed Time: {sp.Elapsed.ToString(@"m\:ss\.fff")}");
            Console.WriteLine($"File_ID: { orch.FileID}");
            Console.WriteLine($"Job_ID: {orch.GetFile.JobID}" );
            //----------------------------------------------------------------------------------------


            //orch.ProcessFile((int)pdiFile.FileID, @"C:\Users\Scott\source\Sample Files\", @"C:\Users\Scott\source\Sample Files\Validation\", true);
            //orch.ProcessFile(@"C:\Users\Scott\source\Sample Files\ICOM_FTI_NLOB_STATIC_ETF_20210722_093000_4.xlsx", @"C:\Users\Scott\source\Sample Files\Validation\", true);

            //PDIStream ps = new PDIStream(@"C:\Users\Scott\source\Sample Files\ICOM_FTI_NLOB_BAU_ETF_20210810_080000_1.xlsx");
            //ExcelHelper ex = new ExcelHelper(ps, con);

            //ex.IsBrandNewFund("31635FSEM", "31635", 11, "ETFLAUNCH2021JUNE", 1001);



            //PDIFile pdiFile = new PDIFile("ICOM_IGWM_NLOB_BAU_FF_20210521_120000_4.xlsx", con, true);
            //PDIStream process = new PDIStream(@"C:\Users\Scott\source\Sample Files\ICOM_IGWM_NLOB_BAU_FF_20210521_120000_4.xlsx", pdiFile);
            //PDIStream template = new PDIStream(@"C:\Users\Scott\source\Sample Files\Validation\TEMPLATE_BAU_FF.xlsx");

            //FileIntegrityCheck _fileCheck = new FileIntegrityCheck(process, template, con); //ICOM_FTI_NLOB_STATIC_ETF_20210210_101722_1.xlsm"
            //_fileCheck.FileCheck();

            //orch.ImportStoredProcedure((int)pdiFile.JobID, (int)pdiFile.CompanyID, (int)pdiFile.DocumentTypeID);
            //orch.LoadStoredProcedure((int)pdiFile.JobID); 


            //PDIFile pdiFile = new PDIFile(953, con);
            //pdiFile.JobID = 842;

            //PDIStream pdiStream = new PDIStream(Path.Combine(@"C:\Users\Scott\source\Sample Files\Profiles\Jan 26\", pdiFile.OnlyFileName), pdiFile);
            ////PDIMail smtp = new PDIMail(smtpAccount, smtpPassword, "pdi_support@investorcom.com", "TEST", "smtp.sendgrid.net");      //string test = smtp.failedTemplate.ReplaceByDictionary(temp);
            //////smtp.SendTemplateMessage(null, "skonkle@investorcom.com", "Test Subject", null);
            ////smtp.SendErrorMessage(pdiFile, "skonkle@investorcom.com");


            //PDISendGrid sg = new PDISendGrid(smtpPassword, "pdi_support@investorcom.com", "PDI Local DEV");
            //sg.SendTemplateMessage(pdiStream, "skonkle@investorcom.com", "Test Message");

            //sg.SendEmail("konkle@gmail.com", "Scott Konkle");

            //Transform tran = new Transform(pdiFile, con);
            //tran.RunTransform()


        }

        public static bool DownloadTemplate(string sourcePath, string localTemplatePath, string storageConString, string storageContainerName)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(storageConString);
            PDIFile pdiFile = new PDIFile(sourcePath);

            try
            {
                using (Stream file = File.OpenWrite(Path.Combine(localTemplatePath, pdiFile.GetDefaultTemplateName())))
                {
                    BlobContainerClient blobSourceContainer = blobServiceClient.GetBlobContainerClient(storageContainerName);
                    if (blobSourceContainer.Exists())
                    {
                        BlobClient source = blobSourceContainer.GetBlobClient($"templates/{pdiFile.GetDefaultTemplateName()}");
                        if (source.Exists())
                        {
                            Azure.Response res = source.DownloadTo(file);
                            if (res.Status != 206)
                                Console.WriteLine($"Issue downloading BLOB Template {pdiFile.GetDefaultTemplateName()} - response was {res.Status}");
                        }
                        else
                            return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            
            return true;
        }
    }
}
