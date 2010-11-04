SET NOCOUNT ON
GO

USE [master]
GO

IF  EXISTS (SELECT name FROM sys.databases WHERE name = N'IQToolkitTest')
DROP DATABASE [IQToolkitTest]
GO

DECLARE @device_directory NVARCHAR(520)
SELECT @device_directory = SUBSTRING(filename, 1, CHARINDEX(N'master.mdf', LOWER(filename)) - 1)
FROM master.dbo.sysaltfiles WHERE dbid = 1 AND fileid = 1

EXECUTE (N'CREATE DATABASE IQToolkitTest
  ON PRIMARY (NAME = N''IQToolkitTest'', FILENAME = N''' + @device_directory + N'IQToolkitTest.mdf'', SIZE = 2048KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB)
  LOG ON (NAME = N''IQToolkitTest_log'',  FILENAME = N''' + @device_directory + N'IQToolkitTest.ldf'', SIZE = 1024KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)')
go

ALTER DATABASE [IQToolkitTest] SET COMPATIBILITY_LEVEL = 100
GO

set quoted_identifier on
GO
SET DATEFORMAT mdy
GO

USE [IQToolkitTest]
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[People]') AND type in (N'U'))
DROP TABLE [dbo].[People]
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Addresses]') AND type in (N'U'))
DROP TABLE [dbo].[Addresses]
GO
USE [IQToolkitTest]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[People](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [nchar](20) NOT NULL,
	[LastName] [nchar](20) NOT NULL,
	[AddressId] [int] NOT NULL,
	CONSTRAINT [PK_People] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY]

CREATE TABLE [dbo].[Addresses](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Line1] [nchar](40) NOT NULL,
	[Line2] [nchar](40) NULL,
	[City] [nchar](20) NOT NULL,
	[State] [nchar](2) NULL,
	[PostalCode] [nchar](10) NOT NULL,
	CONSTRAINT [PK_Addresses] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[People]  WITH CHECK ADD  CONSTRAINT [FK_People_Addresses] FOREIGN KEY([AddressId])
REFERENCES [dbo].[Addresses] ([Id])
GO

ALTER TABLE [dbo].[People] CHECK CONSTRAINT [FK_People_Addresses]
GO








