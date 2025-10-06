
namespace Publisher_Data_Operations.Extensions
{
 
    // Reference Enum for pdi_Document_Life_Cycle_Status
    public enum FFDocAge
    {
        BrandNewFund = 0,
        BrandNewSeries = 1,
        NewFund = 2,
        NewSeries = 3,
        TwelveConsecutiveMonths = 4,
        NoSecuritiesIssued = 5

    }

    // Reference Enum for pdi_Document_Type
    public enum DocumentTypeID
    {
        MRFP = 1,
        FS = 2,
        FSMRFP = 3, //this is a fake type only used for zip files containing both FS and MRFP 
        QPD = 4,
        FF = 5,
        FSBOOK = 6,
        ETF = 11,
        SF = 12,
        FP = 13,
        EP = 14,
        SP = 15,
        SFSBOOK = 16,
        SFS = 17,
        QPDBOOK = 19
    }

    // Reference Enum for pdi_Data_Type
    public enum DataTypeID
    {
        BAU = 1,
        STATIC = 2,
        FSMRFP = 3,
        QPD = 4,
        BNY = 5

    }

    // Enum for inserting header row location
    public enum DataRowInsert
    {
        FirstRow,               // default adds the header as the first row in the table
        AfterDescRepeat,
        AfterColumnChange,
        ClearExtraColumns      // Use the header (Scenario) to determine the number of columns in the table and remove any extra columns from the data when merging otherwise same as FirstRow
    }
}
