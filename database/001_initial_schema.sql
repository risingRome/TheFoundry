CREATE TABLE dbo.Teams (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Teams PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    Description nvarchar(max) NULL,
    CONSTRAINT UX_Teams_Name UNIQUE (Name)
);

CREATE TABLE dbo.CaseTypes (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CaseTypes PRIMARY KEY,
    Code nvarchar(50) NOT NULL,
    Name nvarchar(150) NOT NULL,
    SlaThresholdHours decimal(9,2) NOT NULL CONSTRAINT DF_CaseTypes_Sla DEFAULT 24,
    CONSTRAINT UX_CaseTypes_Code UNIQUE (Code)
);

CREATE TABLE dbo.Agents (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Agents PRIMARY KEY,
    TeamId int NOT NULL,
    ExternalAgentId nvarchar(64) NOT NULL,
    DisplayName nvarchar(150) NOT NULL,
    IdentityUserId nvarchar(450) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Agents_IsActive DEFAULT 1,
    CONSTRAINT FK_Agents_Teams FOREIGN KEY (TeamId) REFERENCES dbo.Teams(Id),
    CONSTRAINT UX_Agents_ExternalAgentId UNIQUE (ExternalAgentId)
);

CREATE TABLE dbo.OperationsCases (
    Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_OperationsCases PRIMARY KEY,
    SourceCaseId nvarchar(100) NOT NULL,
    TeamId int NOT NULL,
    AgentId int NOT NULL,
    CaseTypeId int NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    ResolvedAtUtc datetime2 NULL,
    Status int NOT NULL,
    Escalated bit NOT NULL,
    Reopened bit NOT NULL,
    IngestedAtUtc datetime2 NOT NULL CONSTRAINT DF_OperationsCases_Ingested DEFAULT SYSUTCDATETIME(),
    Notes nvarchar(2000) NULL,
    CONSTRAINT FK_OperationsCases_Teams FOREIGN KEY (TeamId) REFERENCES dbo.Teams(Id),
    CONSTRAINT FK_OperationsCases_Agents FOREIGN KEY (AgentId) REFERENCES dbo.Agents(Id),
    CONSTRAINT FK_OperationsCases_CaseTypes FOREIGN KEY (CaseTypeId) REFERENCES dbo.CaseTypes(Id),
    CONSTRAINT UX_OperationsCases_SourceCaseId UNIQUE (SourceCaseId)
);

CREATE INDEX IX_OperationsCases_Team_CreatedAt ON dbo.OperationsCases (TeamId, CreatedAtUtc);
CREATE INDEX IX_OperationsCases_Agent_ResolvedAt ON dbo.OperationsCases (AgentId, ResolvedAtUtc);

CREATE TABLE dbo.DataFreshness (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DataFreshness PRIMARY KEY,
    SourceName nvarchar(100) NOT NULL,
    RefreshedAtUtc datetime2 NOT NULL,
    RowCount int NOT NULL,
    Status nvarchar(40) NOT NULL,
    CONSTRAINT UX_DataFreshness_SourceName UNIQUE (SourceName)
);

CREATE TABLE dbo.AuditLogs (
    Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
    UserId nvarchar(450) NULL,
    EventType nvarchar(100) NOT NULL,
    EventPayloadJson nvarchar(max) NOT NULL,
    IpAddress nvarchar(64) NULL,
    CreatedAtUtc datetime2 NOT NULL CONSTRAINT DF_AuditLogs_Created DEFAULT SYSUTCDATETIME(),
    PreviousHash nvarchar(128) NOT NULL,
    RowHash nvarchar(128) NOT NULL
);

INSERT dbo.Teams (Name) VALUES ('Alpha'), ('Beta'), ('Gamma');
INSERT dbo.CaseTypes (Code, Name, SlaThresholdHours)
VALUES ('STANDARD', 'Standard Request', 24), ('PRIORITY', 'Priority Request', 12), ('COMPLEX', 'Complex Investigation', 36);
