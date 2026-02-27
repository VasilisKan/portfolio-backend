CREATE TABLE IF NOT EXISTS "Users" 
(
    "Id" UUID PRIMARY KEY,
    "Email" TEXT NOT NULL,
    "Username" TEXT NOT NULL DEFAULT '',
    "PasswordHash" TEXT NOT NULL,
    "isAdmin" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Add columns if table already existed without them (e.g. existing Neon DB)
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "Username" TEXT NOT NULL DEFAULT '';
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP;

-- Unique username when not empty (allows multiple empty for legacy users)
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Username" ON "Users" ("Username") WHERE "Username" != '';

CREATE TABLE IF NOT EXISTS "Tickets"
(
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Title" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Category" TEXT NOT NULL,
    "IsResolved" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("UserId") REFERENCES "Users"("Id")
);

-- Add CreatedAt/UpdatedAt to Tickets if missing (legacy DBs)
ALTER TABLE "Tickets" ADD COLUMN IF NOT EXISTS "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE "Tickets" ADD COLUMN IF NOT EXISTS "UpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP;

CREATE TABLE IF NOT EXISTS "PasswordResetTokens"
(
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Token" TEXT NOT NULL,
    "ExpiresAt" TIMESTAMP NOT NULL,
    FOREIGN KEY ("UserId") REFERENCES "Users"("Id")
);

CREATE TABLE IF NOT EXISTS "TicketReplies"
(
    "Id" UUID PRIMARY KEY,
    "TicketId" UUID NOT NULL,
    "UserId" UUID NOT NULL,
    "Message" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("TicketId") REFERENCES "Tickets"("Id"),
    FOREIGN KEY ("UserId") REFERENCES "Users"("Id")
);

-- Demos
CREATE TABLE IF NOT EXISTS "Demos"
(
    "Id" UUID PRIMARY KEY,
    "Title" VARCHAR(255) NOT NULL,
    "Slug" VARCHAR(255) NOT NULL,
    "HtmlContent" TEXT NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT now(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT now(),
    "CreatedByUserId" UUID NULL,
    FOREIGN KEY ("CreatedByUserId") REFERENCES "Users"("Id")
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Demos_Slug" ON "Demos" ("Slug");

-- Demo assignments (many-to-many: demo ↔ users)
CREATE TABLE IF NOT EXISTS "DemoAssignments"
(
    "Id" UUID PRIMARY KEY,
    "DemoId" UUID NOT NULL,
    "UserId" UUID NOT NULL,
    "AssignedAt" TIMESTAMPTZ NOT NULL DEFAULT now(),
    FOREIGN KEY ("DemoId") REFERENCES "Demos"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("UserId") REFERENCES "Users"("Id"),
    UNIQUE ("DemoId", "UserId")
);
CREATE INDEX IF NOT EXISTS "IX_DemoAssignments_DemoId" ON "DemoAssignments" ("DemoId");
CREATE INDEX IF NOT EXISTS "IX_DemoAssignments_UserId" ON "DemoAssignments" ("UserId");

-- Showcase (site | photo)
CREATE TABLE IF NOT EXISTS "Showcase"
(
    "Id" UUID PRIMARY KEY,
    "Type" VARCHAR(20) NOT NULL,
    "Title" VARCHAR(255) NOT NULL,
    "Slug" VARCHAR(255) NOT NULL,
    "HtmlContent" TEXT NULL,
    "ImageUrl" VARCHAR(2048) NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT now(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT now(),
    "CreatedByUserId" UUID NULL,
    FOREIGN KEY ("CreatedByUserId") REFERENCES "Users"("Id")
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Showcase_Slug" ON "Showcase" ("Slug");

-- Showcase assignments (who can view)
CREATE TABLE IF NOT EXISTS "ShowcaseAssignments"
(
    "Id" UUID PRIMARY KEY,
    "ShowcaseId" UUID NOT NULL,
    "UserId" UUID NOT NULL,
    "AssignedAt" TIMESTAMPTZ NOT NULL DEFAULT now(),
    FOREIGN KEY ("ShowcaseId") REFERENCES "Showcase"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("UserId") REFERENCES "Users"("Id"),
    UNIQUE ("ShowcaseId", "UserId")
);
CREATE INDEX IF NOT EXISTS "IX_ShowcaseAssignments_ShowcaseId" ON "ShowcaseAssignments" ("ShowcaseId");
CREATE INDEX IF NOT EXISTS "IX_ShowcaseAssignments_UserId" ON "ShowcaseAssignments" ("UserId");