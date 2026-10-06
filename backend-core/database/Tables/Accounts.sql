CREATE TABLE [dbo].[Accounts]
(
	[AccountId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	Email NVARCHAR(255) NOT NULL,
	FirstName NVARCHAR(100) NOT NULL,
	LastName NVARCHAR(100) NOT NULL,
	-- Legacy unsalted SHA-256 hash. NULL once the account has been migrated to PasswordHash.
	[Password] VARBINARY(MAX) NULL,
	-- Bcrypt hash (salted). Populated for all new accounts; back-filled for existing
	-- accounts the first time they log in successfully (see AccountDao.VerifyPassword).
	PasswordHash NVARCHAR(100) NULL,
	[Enabled] BIT NOT NULL,
	CreatedDateTime DATETIME NOT NULL,
	DeletedDateTime DATETIME NULL,
    UpdatedDateTime DATETIME NOT NULL,
    -- 0 = EndUser, 1 = Installer, 2 = Admin (see Tausend.Core.Enums.AccountRole)
    Role TINYINT NOT NULL CONSTRAINT DF_Accounts_Role DEFAULT (0),
    -- Stamped by CreateLoginSession on every successful login. NULL for an account that has
    -- never logged in (or predates this column).
    LastLoginDateTime DATETIME NULL
)
