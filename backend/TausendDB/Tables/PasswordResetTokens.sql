CREATE TABLE [dbo].[PasswordResetTokens]
(
	[PasswordResetTokenId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	ResetToken UniqueIdentifier NOT NULL,
	AccountID BIGINT NOT NULL FOREIGN KEY REFERENCES Accounts(AccountID),
	CreatedDateTime DATETIME NOT NULL,
	ExpirationDateTime DATETIME NOT NULL,
	UsedDateTime DATETIME NULL
)
