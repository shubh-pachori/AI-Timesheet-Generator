-- AI Timesheet Generator — PostgreSQL schema
-- This mirrors what `dotnet ef migrations` will generate from the EF Core model.
-- Provided for reference / manual setup if you prefer raw SQL over migrations.

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    full_name VARCHAR(200) NOT NULL,
    email VARCHAR(200) NOT NULL UNIQUE,
    azure_ad_object_id VARCHAR(200),
    role VARCHAR(20) NOT NULL DEFAULT 'Employee',
    manager_id UUID REFERENCES users(id),
    created_at TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE connections (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    provider VARCHAR(30) NOT NULL, -- GitHub | AzureDevOps | Jira | OutlookCalendar | TeamsCalendar
    access_token TEXT NOT NULL,
    refresh_token TEXT,
    external_account_id VARCHAR(200),
    connected_at TIMESTAMP NOT NULL DEFAULT now(),
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE activities (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    source VARCHAR(30) NOT NULL, -- GitCommit | PullRequest | JiraTicket | Meeting | CodeReview
    title VARCHAR(500) NOT NULL,
    description TEXT,
    external_reference VARCHAR(200),
    status VARCHAR(50),
    activity_date DATE NOT NULL,
    estimated_hours DOUBLE PRECISION,
    created_at TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE timesheets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    week_start_date DATE NOT NULL,
    week_end_date DATE NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Draft', -- Draft | Generated | Submitted | Approved | Rejected
    ai_weekly_summary TEXT,
    generated_at TIMESTAMP NOT NULL DEFAULT now(),
    submitted_at TIMESTAMP
);

CREATE TABLE timesheet_entries (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    timesheet_id UUID NOT NULL REFERENCES timesheets(id) ON DELETE CASCADE,
    entry_date DATE NOT NULL,
    activity_description TEXT NOT NULL,
    hours DOUBLE PRECISION NOT NULL DEFAULT 0,
    development_hours DOUBLE PRECISION NOT NULL DEFAULT 0,
    meeting_hours DOUBLE PRECISION NOT NULL DEFAULT 0,
    review_hours DOUBLE PRECISION NOT NULL DEFAULT 0,
    is_edited BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE approvals (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    timesheet_id UUID NOT NULL UNIQUE REFERENCES timesheets(id) ON DELETE CASCADE,
    manager_id UUID REFERENCES users(id),
    status VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending | Approved | Rejected
    comments TEXT,
    decided_at TIMESTAMP
);

CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id),
    action VARCHAR(200) NOT NULL,
    details TEXT,
    timestamp TIMESTAMP NOT NULL DEFAULT now()
);

CREATE INDEX idx_activities_user_date ON activities(user_id, activity_date);
CREATE INDEX idx_timesheets_user ON timesheets(user_id);
CREATE INDEX idx_timesheets_status ON timesheets(status);
CREATE INDEX idx_connections_user ON connections(user_id);
