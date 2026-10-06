CREATE PROCEDURE DisassociateCentral
    @Identifier VARCHAR(255),
    @AccountId BIGINT
AS
BEGIN
    -- @AccountId = 0 means an internal/system caller (the relay, via SystemAuth), e.g. a panel
    -- factory reset -- unlink every account from the device. Any other caller is an end user and
    -- may only remove their own link, not force other accounts off a shared panel -- see
    -- ../../backend/DAY5_SUMMARY.md.
    DECLARE @RowCount INT

    IF @AccountId = 0 BEGIN
        DELETE ADP
        FROM AccountDevicePins ADP
        INNER JOIN Devices D ON D.DeviceId = ADP.DeviceId
        WHERE D.Identifier = @Identifier
    END
    ELSE BEGIN
        DELETE ADP
        FROM AccountDevicePins ADP
        INNER JOIN Devices D ON D.DeviceId = ADP.DeviceId
        WHERE D.Identifier = @Identifier AND ADP.AccountId = @AccountId
    END

    SET @RowCount = @@ROWCOUNT

    SELECT @RowCount
END;
