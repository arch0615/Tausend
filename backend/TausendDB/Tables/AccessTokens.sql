CREATE TABLE [dbo].[AccessTokens]
(
	[AccessTokenId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	AccessToken UniqueIdentifier NOT NULL,
	AccountID BIGINT NOT NULL FOREIGN KEY REFERENCES Accounts(AccountID),
	CreatedDateTime DATETIME NOT NULL, 
	ExpirationDateTime DATETIME NOT NULL
)
