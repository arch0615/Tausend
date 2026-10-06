CREATE TABLE [dbo].[RefreshTokens]
(
	[RefreshTokenId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	RefreshToken UniqueIdentifier NOT NULL,
	AccountID BIGINT NOT NULL FOREIGN KEY REFERENCES Accounts(AccountID),
	CreatedDateTime DATETIME NOT NULL,
	ExpirationDateTime DATETIME NOT NULL,
	RevokedDateTime DATETIME NULL
)
