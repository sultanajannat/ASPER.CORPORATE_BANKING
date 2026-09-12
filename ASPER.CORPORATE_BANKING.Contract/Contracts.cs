namespace ASPER.CORPORATE_BANKING.Contract
{
    public record DPSAccountCreatedMessage(
          string CustomerCode,
          string CustomerName,
          string AccountTitle,
          string AccountTitleBn,
          decimal OpeningBalance,
          string CustomerAccountNo,
          string Reason,
          int ProductBuilderId,
          int DpsTenureId,
          string ChannelName,
          string CreatedBy,
          string NidNo,
          string Duration,
          DateTime? AccountCreatedAt,
          string AccountStatus,
          DateTime? LastTransaction,
          string ProductName,
          List<NomineeContract> Nominees

    );
    public record NomineeContract(
    string NomineeName,
    string RelationWithAccount,
    string NomineeNid,
    string NomineeMobile,
    string NomineePhoto,
    decimal Percentage
);
    public record TransactionInfo
    (
        string AccountNo,
        decimal? Amount,
        DateTime? TransactionDate,
        string TransactionType,
        string Description,
        string SourceAccount,
        string Destination,
        string TransactionId
    );

    public record DPSEncashmentMessage
    (
        string DpsAccountNo,     // The DPS Account being closed
        string TargetAccountNo,  // The MFS/Bank account receiving the money
        decimal Amount,
        string TransactionId,
        string CreatedBy,
        DateTime? TransactionDate,
        string Description,
        string cardType
    );


    public record DPSScheduleItem
    (
        int? ScheduleId,         
        int? MonthNumber,
        DateTime? EffectiveDate,
        decimal? MonthlyAmount,
        decimal? InterestRate,
        decimal? ClosingBalance,
        decimal? Interest,
        decimal? TotalDays,
        DateTime? CreatedAt
    );
    
    public record DPSScheduleForPublic
    (
        string AccountNo,       
        string CustomerNidNo,   
        int? TotalDuration,     
        List<DPSScheduleItem> Schedules 
    );

    public record DPSPaymentMessage
    (
        int ProductId,
        int ProductLedgerId,
        int CustomerId,
        int CustomerAccountId,
        string TargetAccountNo, 
        string TargetAccountName,
        string SourceAccountNo, 
        decimal Amount, 
        string TransactionId, 
        string CreatedBy,
        string TransactionType,
        string ProductName,
        string CustomerCode,
        string ProductCategoryName
    );

    public record DPSPaymentCommission
    (
        int? paymentCommissionId,
        string AccountNo,
        string transactionNo,
        string customerName,
        string Remarks,
        string ChannelName,
        string ChannelCode,
        string ProductName,
        string ProductCode,
        decimal? CommissionAmount,
        decimal? CollectionAmount,
        DateTime? CollectionDate,
        bool? IsPercentage,
        decimal? CommissionValue,
        bool? status,
        DateTime? CREATED_AT
    );


    public record DPSchannelCreate
    (
        int? channelId,
        string channelName,
        string channelcode,
        string mobile,
        string author,
        string address,
        DateTime? createdAt,
        bool? IsActive
    );

    public record ProcessAutoDebitMessage
    (
        int ScheduleId,
        string AccountNo,
        decimal Amount,
        string Token,
        DateTime ScheduledDate
    );

    //public record TaxFileUploadMessage
    //(
    //    string MobileNo,
    //    string TinNo,
    //    string TaxDocumentFile
    //);

    public record MonthlyProvisionItem(
        int Id,
        int? ProductId,
        string ProductName,
        string ProductCode,
        decimal? TotalInterestCharged,
        DateTime? ProcessDate,
        DateTime? BusinessDate
    );

    public record MonthlyProvisionVoucherProcessMessage(
        List<MonthlyProvisionItem> Items,
        string EventName,
        string refNo,
        string transactionId,
        string remarks,
        string postingtype
    );

    public record DPSChannelCommissionItem(
        string ChannelName,
        string ChannelCode,
        decimal TotalCommission,
        long TransactionCount,
        decimal TotalTransactionVolume,
        DateTime BusinessDate
    );

    public record ChannelCommissionVoucherProcessMessage(
        List<DPSChannelCommissionItem> Items,
        string RefNo,
        string transactionId,
        string Remarks,
        DateTime BusinessDate,
        string Username
    );

    public record ExternalfundTransferGlProcessMessage(
       
       string VoucherType,
       string TriggeredBy,
       string RefNo,
       DateTime BusinessDate,
       DateTime PublishedAt
      
   );
}

