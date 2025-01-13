CREATE TABLE [Application] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [name] NVARCHAR(50) UNIQUE NOT NULL,
    [creation_datetime] DATETIME2 NOT NULL DEFAULT GETDATE()
);


CREATE TABLE [Container] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [name] NVARCHAR(100) UNIQUE NOT NULL ,
    [creation_datetime] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [parent] INT NOT NULL, -- Application ID
    FOREIGN KEY ([parent]) REFERENCES [Application]([id]) ON DELETE CASCADE,
);


CREATE TABLE [Record] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [name] NVARCHAR(50) UNIQUE NOT NULL ,
    [content] NVARCHAR(MAX) NOT NULL,
    [creation_datetime] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [parent] INT NOT NULL, -- Container ID
    FOREIGN KEY ([parent]) REFERENCES [Container]([id]) ON DELETE CASCADE,
);

CREATE TABLE [Notification] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [name] NVARCHAR(50)  UNIQUE NOT NULL,
    [creation_datetime] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [parent] INT NOT NULL,
    [event] INT NOT NULL CHECK ([event] IN (1, 2)), 
    [endpoint] NVARCHAR(500) NOT NULL,
    [enabled] BIT NOT NULL DEFAULT 1,
    FOREIGN KEY ([parent]) REFERENCES [Container]([id]) ON DELETE CASCADE,
);