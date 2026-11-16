-- Initial SQL Server schema. Executed transactionally by DatabaseSetup, without EF migrations.
-- Future schema edits must be applied using an explicit ALTER TABLE script.
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
    THROW 50001, 'This database belongs to an EF version. Choose a new database name; do not overwrite it.', 1;

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUsers (
        Id nvarchar(128) NOT NULL PRIMARY KEY,
        UserName nvarchar(256) NULL,
        NormalizedUserName nvarchar(256) NULL,
        Email nvarchar(256) NULL,
        NormalizedEmail nvarchar(256) NULL,
        EmailConfirmed bit NOT NULL,
        PasswordHash nvarchar(max) NULL,
        SecurityStamp nvarchar(max) NULL,
        ConcurrencyStamp nvarchar(max) NULL,
        PhoneNumber nvarchar(max) NULL,
        PhoneNumberConfirmed bit NOT NULL,
        TwoFactorEnabled bit NOT NULL,
        LockoutEnd datetimeoffset NULL,
        LockoutEnabled bit NOT NULL,
        AccessFailedCount int NOT NULL,
        FullName nvarchar(120) NOT NULL,
        CreatedAt datetime2 NOT NULL
    );
    CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
    CREATE UNIQUE INDEX EmailIndex ON dbo.AspNetUsers(NormalizedEmail) WHERE NormalizedEmail IS NOT NULL;
END;
IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoles (
        Id nvarchar(128) NOT NULL PRIMARY KEY,
        Name nvarchar(256) NULL,
        NormalizedName nvarchar(256) NULL,
        ConcurrencyStamp nvarchar(max) NULL
    );
    CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;
END;
IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
    CREATE TABLE dbo.AspNetUserRoles (
        UserId nvarchar(128) NOT NULL REFERENCES dbo.AspNetUsers(Id),
        RoleId nvarchar(128) NOT NULL REFERENCES dbo.AspNetRoles(Id),
        PRIMARY KEY (UserId, RoleId)
    );
IF OBJECT_ID(N'dbo.Institutions', N'U') IS NULL
    CREATE TABLE dbo.Institutions (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name nvarchar(120) NOT NULL,
        RegistrationNumber nvarchar(80) NOT NULL UNIQUE,
        UserId nvarchar(128) NOT NULL UNIQUE REFERENCES dbo.AspNetUsers(Id),
        Status int NOT NULL CONSTRAINT CK_Institution_Status CHECK (Status IN (0,1,2)),
        AppliedAt datetime2 NOT NULL
    );

-- Existing credential tables/data from older versions are deliberately left untouched.
