using System;
using System.Collections.Generic;
using System.Linq;
using Excel = Aspose.Cells;
using Publisher_Data_Operations.Extensions;

namespace Publisher_Data_Operations.Helper
{
    /// <summary>
    /// Enum of Validation Types - used in template to indicate which type of validation to perform
    /// </summary>
    public enum ValidationType
    {
        DateFormat,
        MaxTwoDecimals,
        MaxOneDecimal,
        MaxFourDecimals,
        WholeNumbers,
        RequiredValues,
        UnacceptableElements,
        AcceptableElements,
        RowSequenceCheck,
        RowWithDateSequenceCheck,
        FundAndInvestmentCheck,
        InvestmentMix17,
        InvestmentMix40,
        FlagValueValidation,
        AtLeastOneRequired,
        TwelveMonthValidation,
        CalendarYearValidation,
        UniqueColumnContents,
        XMLValidation,
        ScenarioValidation,
        AllOrNone,
        FieldAttributeAllCheck,
        FieldAttributeCheck,
        SheetNameCheck,
        ActiveFilter,
        MatchingDataSheetRequired
    }

    /// <summary>
    /// Identifies the type of sheet in each WorksheetValidation objects
    /// </summary>
    public enum SheetType
    {
        wsData,
        ws16,
        ws17,
        ws40,
        wsAllocation,
        wsDistribution,
        wsNavPU,
        wsNumInvest,
        ws10K,
        wsTheFund,
        wsTerminated,
        wsNewSeries,
        wsManagmentFees,
        wsFixedAdminFees,
        wsSoftDollarComissions,
        wsBrokerageCommissions,
        wsInvestmentsFund,
        wsSeriesRedesignation,
        wsSeriesMerger,
        wsRemainingYears,
        wsIncomeTaxes,
        wsFundMerger,
        wsFairValue,
        wsSeedMoney,
        wsSubsidiarySummary,
        wsSubsidiaryDetail,
        wsCommitments,
        wsFullName,
        wsFundTransfer,
        wsNewlyOffered,
        wsChangeFundName,
        wsManagementPersonnel,
        wsKeyManagementPersonnelBrokerageCommissions,
        wsFundMergers,
        wsNewFunds,
        wsNoLongerOffered,
        wsUnfundedLoanCommitments,
        wsComparisonOfNetAsset,
        wsYearByYear,
        wsAnnualCompoundReturns,

        wsBookFundMap,
        wsFundtoBookTableMap,

        wsFieldUpdate,
        wsTableUpdate,
        wsClientTranslation,
        wsStaticLanguage,

        wsBNY,

        wsUnknown
    }

    /// <summary>
    /// Holds an Aspose Excel worksheet and it's associated template as well as other details
    /// </summary>
    public class WorksheetValidation
    {
        public Excel.Worksheet InputSheet { get; set; }
        public Excel.Worksheet TemplateSheet { get; set; }
        public int FirstDataRow { get; set; }
        public int DataRows { get => InputSheet.Cells.MaxDataRow - (FirstDataRow - 1); } //MaxdataRows is 0 based and FirstDataRow should be part of the Datarows so remove 1 so it's included
        public SheetType TypeOfSheet { get; set; }

        /// <summary>
        /// Set the initial variables and determine the type of worksheet
        /// </summary>
        /// <param name="inputSheet">The data source sheet</param>
        /// <param name="templateSheet">The template sheet</param>
        /// <param name="firstDataRow">The first data row of the input sheet</param>
        public WorksheetValidation(Excel.Worksheet inputSheet, Excel.Worksheet templateSheet, int firstDataRow)
        {
            InputSheet = inputSheet;
            TemplateSheet = templateSheet;
            FirstDataRow = firstDataRow;

            TypeOfSheet = WorksheetValidation.GetSheetType(InputSheet.Name);
        }

        /// <summary>
        /// Static function to return the SheetType based on the sheet name
        /// </summary>
        /// <param name="sheetName">The name of the sheet to get the type for</param>
        /// <returns>The determined SheetType - wsUnknown if not found</returns>
        public static SheetType GetSheetType(string sheetName)
        {
            if (sheetName.IndexOf("Document", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsData;

            if (sheetName.IndexOf("BNY_Data", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsBNY;
            else if (sheetName.IndexOf("Allocation", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsAllocation;
            else if (sheetName.IndexOf("Investments", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsNumInvest;
            else if (sheetName.IndexOf("Distributions", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsDistribution;
            else if (sheetName.IndexOf("10K", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.ws10K;
            else if (sheetName.IndexOf("NAVPU", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsNavPU;
            else if (sheetName.IndexOf("38") >= 0)
                return SheetType.wsTheFund;
            else if (sheetName.IndexOf("SC93", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsNewFunds;
            else if (sheetName.IndexOf("SC94", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsNoLongerOffered;
            else if (sheetName.IndexOf("94") >= 0)
                return SheetType.wsTerminated;
            else if (sheetName.IndexOf("93") >= 0)
                return SheetType.wsNewSeries;
            else if (sheetName.IndexOf("F42", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsManagmentFees; // FS version
            else if (sheetName.IndexOf("M9a", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsManagmentFees; // MRFP version         
            else if (sheetName.IndexOf("39") >= 0)
                return SheetType.wsFixedAdminFees;
            else if (sheetName == "F40") // FS - don't confuse with FF40
                return SheetType.wsBrokerageCommissions;
            else if (sheetName.IndexOf("150") >= 0) // check for 150 first
                return SheetType.wsComparisonOfNetAsset;
            else if (sheetName.IndexOf("50") >= 0)
                return SheetType.wsInvestmentsFund;
            else if (sheetName.IndexOf("27b") >= 0)
                return SheetType.wsSoftDollarComissions;
            else if (sheetName.IndexOf("96") >= 0)
                return SheetType.wsSeriesRedesignation;
            else if (sheetName.IndexOf("95") >= 0)
                return SheetType.wsSeriesMerger;
            else if (sheetName.IndexOf("41") >= 0)
                return SheetType.wsRemainingYears;
            else if (sheetName.IndexOf("118") >= 0)
                return SheetType.wsIncomeTaxes;
            else if (sheetName.IndexOf("91") >= 0)
                return SheetType.wsChangeFundName;
            else if (sheetName.IndexOf("92") >= 0)
                return SheetType.wsFundMerger;

            else if (sheetName.IndexOf("21") >= 0)
                return SheetType.wsFairValue;
            else if (sheetName.IndexOf("163ca", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsSeedMoney;
            else if (sheetName.IndexOf("163da", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsSubsidiarySummary;
            else if (sheetName.IndexOf("120") >= 0)
                return SheetType.wsSubsidiaryDetail;
            else if (sheetName.IndexOf("121") >= 0)
                return SheetType.wsCommitments;
            else if (sheetName.IndexOf("122") >= 0)
                return SheetType.wsFullName;
            else if (sheetName.IndexOf("123") >= 0)
                return SheetType.wsFundTransfer;
            else if (sheetName.IndexOf("147") >= 0)
                return SheetType.wsNewlyOffered;

            //else if (sheetName == "SC40") // Seg FS Book - don't confuse with FF40
            //return SheetType.wsManagementPersonnel;
            else if (sheetName.IndexOf("167") >= 0)
                return SheetType.wsKeyManagementPersonnelBrokerageCommissions;  // handles both SC167 and SC167b
            else if (sheetName.IndexOf("168") >= 0)
                return SheetType.wsFundMergers;
            else if (sheetName.IndexOf("15") >= 0)
                return SheetType.wsYearByYear;
            else if (sheetName.IndexOf("M17", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsAnnualCompoundReturns;
            else if (sheetName.IndexOf("40") >= 0)
                return SheetType.ws40;
            else if (sheetName.IndexOf("16") >= 0)
                return SheetType.ws16;
            else if (sheetName.IndexOf("17") >= 0)
                return SheetType.ws17;
            else if (sheetName.IndexOf("90") >= 0)
                return SheetType.wsUnfundedLoanCommitments;

            else if (sheetName.IndexOf("Field UPDATE", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsFieldUpdate;
            else if (sheetName.IndexOf("Table UPDATE", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsTableUpdate;
            else if (sheetName.IndexOf("Client Translation", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsClientTranslation;
            else if (sheetName.IndexOf("Language", StringComparison.OrdinalIgnoreCase) >= 0 || sheetName.IndexOf("Static Text", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsStaticLanguage;
            else if (sheetName.IndexOf("BookFundMap", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsBookFundMap;
            else if (sheetName.IndexOf("FundtoBookTableMap", StringComparison.OrdinalIgnoreCase) >= 0)
                return SheetType.wsFundtoBookTableMap;
            else
                return SheetType.wsUnknown;
        }

        public static int DefaultHeaderRows(SheetType sheetType)
        {
            switch (sheetType)
            {
                case SheetType.wsData:
                case SheetType.wsNavPU:
                    return 2;
                default:
                    return 1;
            }
        }
    }

    /// <summary>
    /// An extension of List to hold WorksheetValidation and the associated ExcelHelper
    /// </summary>
    public class ValidationList : List<WorksheetValidation>
    {
        public ExcelHelper ExHelper { get; set; }
        public int TotalDocuments
        {
            get
            {
                try
                {
                    WorksheetValidation doc = GetSheetByType(SheetType.wsData);
                    if (doc != null)
                        return doc.DataRows;
                    else
                        return -1;
                }
                catch
                {
                    return -1;
                };
            }
        }
        //public DataTypeID ValidFileType { get => _fileType; }
        //public DocumentTypeID ValidDataType { get => _dataType; }

        //private DataTypeID _fileType;
        //private DocumentTypeID _dataType;

        public PDIStream ProcessStream { get; private set; }

        /// <summary>
        /// On creation of the list require an ExcelHelper
        /// </summary>
        /// <param name="ex">The ExcelHelper instance to use</param>
        public ValidationList(ExcelHelper ex)
        {
            ExHelper = ex;
            ProcessStream = ex.ProcessStream;
        }

        /// <summary>
        /// Add a WorksheetValidation to the list
        /// </summary>
        /// <param name="inputSheet">The Excel worksheet used for input</param>
        /// <param name="templateSheet">The Excel worksheet that is the template for inputSheet</param>
        public void AddWS(Excel.Worksheet inputSheet, Excel.Worksheet templateSheet)
        {
            WorksheetValidation wsValid = new WorksheetValidation(inputSheet, templateSheet, ExHelper.CountHeaderRows(templateSheet));
            this.Add(wsValid);
        }

        public bool ContainsSheetByName(string inputSheetName)
        {
            for (int i = 0; i < this.Count; i++)
                if (this[i].InputSheet.Name == inputSheetName)
                    return true;

            return false;
        }

        /// <summary>
        /// Get the first instance of the requested type of sheet in the list or return null
        /// </summary>
        /// <param name="st">The SheetType requested</param>
        /// <returns></returns>
        public WorksheetValidation GetSheetByType(SheetType st)
        {
            return this.FirstOrDefault(x => x.TypeOfSheet == st);
        }

        /// <summary>
        /// Identify the type of file
        /// </summary>
 /*     public bool ValidateFileName(string fileName)
        {
            string fileType = fileName.Split('_')[3].ToUpper();
            string dataType = fileName.Split('_')[4].ToUpper();

            bool retVal = true;
            if (!Enum.TryParse<DataTypeID>(fileType, out _fileType))
                retVal = false;

            if (!Enum.TryParse<DocumentTypeID>(dataType, out _dataType))
                retVal = false;

            return retVal;
        }
 */
        /// <summary>
        /// Run validation on the list of WorksheetValidations contained in this
        /// </summary>
        public bool Validate()
        {
            if (ExHelper is null)
                throw new System.NullReferenceException("Excel Helper must not be null");

            WorksheetValidation workSheetDD = GetSheetByType(SheetType.wsData);

            if (workSheetDD is null && ProcessStream.PdiFile.GetDataType == DataTypeID.BAU)
                throw new System.NullReferenceException("Unable to locate DocumentData worksheet");

            WorksheetValidation workSheet16 = null, workSheet17 = null, workSheet40 = null;
            // Loop on all worksheets contained in this list
            foreach (WorksheetValidation curValidation in this)
            {
                if (ProcessStream.PdiFile.GetDataType != DataTypeID.BNY) // BNY format is not text and allows formulas
                {
                    ExHelper.LogFormulas(curValidation.InputSheet);
                    ExHelper.CheckCellDataType("@", curValidation.InputSheet); // this is a slow check
                    ExHelper.CheckBlanksAndNAInWorksheet(curValidation);
                }
                
                ExHelper.ValidateWorkSheetHeader(curValidation); //Validate Sequence of columns
                ExHelper.LogHiddenSheetRowsColumns(curValidation.InputSheet); //Log hidden sheet, rows, and columns
                ExHelper.TemplateValidation(curValidation, workSheetDD); //Validate based on template validation requirements

                //sheet specific validations
                switch (curValidation.TypeOfSheet)
                {
                    case SheetType.wsData:
                        ExHelper.ValidateAccountDocumentTypeAndLOB(curValidation); //verify client account, document type and line of business
                        //TotalDocuments = ExHelper.GetDocuemntCountAndValidateDcoumnetNumbersInitialDates(curValidation.InputSheet); //Get count of provided documents and log duplicate entries

                        if (ProcessStream.PdiFile.GetDataType == DataTypeID.BAU && (ProcessStream.PdiFile.GetDocumentType == DocumentTypeID.FF || ProcessStream.PdiFile.GetDocumentType == DocumentTypeID.ETF))
                            ExHelper.MerValidation(curValidation);
                        break;

                    case SheetType.ws16:
                        ExHelper.MatchFundCodeToDocumentDataTab(workSheetDD, curValidation);
                        ExHelper.RequiredFundValidation(workSheetDD, curValidation, ValidationType.FundAndInvestmentCheck);
                        workSheet16 = curValidation;
                        break;

                    case SheetType.ws17:
                        ExHelper.MatchFundCodeToDocumentDataTab(workSheetDD, curValidation);
                        ExHelper.RequiredFundValidation(workSheetDD, curValidation, ValidationType.InvestmentMix17);
                        workSheet17 = curValidation;
                        break;

                    case SheetType.ws40:
                        ExHelper.MatchFundCodeToDocumentDataTab(workSheetDD, curValidation);
                        ExHelper.RequiredFundValidation(workSheetDD, curValidation, ValidationType.InvestmentMix40);
                        workSheet40 = curValidation;
                        break;

                }       
            }
            
            if (!(workSheet16 is null) && !(workSheet17 is null))
            {
                ExHelper.MatchFundCodesOfTwoSheets(workSheet16, workSheet17);
                ExHelper.ValidateIsProforma(this);
            }
            return ExHelper.IsValidData;
        }
    }
}
