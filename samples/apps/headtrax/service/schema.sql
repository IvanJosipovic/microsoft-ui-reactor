-- HeadTrax Employee Database Schema
-- Designed for 150K-250K employee datasets with manager hierarchy

PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
PRAGMA foreign_keys = ON;

DROP TABLE IF EXISTS employees;

CREATE TABLE employees (
    id                  INTEGER PRIMARY KEY,
    employee_number     TEXT    NOT NULL UNIQUE,
    first_name          TEXT    NOT NULL,
    last_name           TEXT    NOT NULL,
    email               TEXT    NOT NULL,
    phone               TEXT,
    title               TEXT    NOT NULL,
    department          TEXT    NOT NULL,
    location            TEXT    NOT NULL,
    hire_date           TEXT    NOT NULL,          -- ISO 8601 date
    salary              REAL    NOT NULL,
    manager_id          INTEGER REFERENCES employees(id),
    level               INTEGER NOT NULL,          -- 0=CEO .. 7=IC
    status              TEXT    NOT NULL DEFAULT 'Active',
    birth_date          TEXT,                      -- ISO 8601 date
    gender              TEXT,
    performance_rating  REAL,                      -- 1.0 – 5.0
    stock_options       INTEGER NOT NULL DEFAULT 0,
    is_remote           INTEGER NOT NULL DEFAULT 0,
    cost_center         TEXT,
    created_at          TEXT    NOT NULL DEFAULT (datetime('now')),
    updated_at          TEXT    NOT NULL DEFAULT (datetime('now'))
);

-- Indexes tuned for typical grid operations (sort, filter, search)
CREATE INDEX idx_emp_manager     ON employees(manager_id);
CREATE INDEX idx_emp_department  ON employees(department);
CREATE INDEX idx_emp_last_name   ON employees(last_name);
CREATE INDEX idx_emp_title       ON employees(title);
CREATE INDEX idx_emp_location    ON employees(location);
CREATE INDEX idx_emp_hire_date   ON employees(hire_date);
CREATE INDEX idx_emp_salary      ON employees(salary);
CREATE INDEX idx_emp_status      ON employees(status);
CREATE INDEX idx_emp_level       ON employees(level);
CREATE INDEX idx_emp_cost_center ON employees(cost_center);

-- Composite indexes for common multi-column sorts
CREATE INDEX idx_emp_dept_name   ON employees(department, last_name, first_name);
CREATE INDEX idx_emp_name        ON employees(last_name, first_name);

-- Full-text search virtual table for free-text queries
CREATE VIRTUAL TABLE employees_fts USING fts5(
    first_name, last_name, email, title, department, location,
    content='employees',
    content_rowid='id'
);

-- Triggers to keep FTS index in sync
CREATE TRIGGER employees_ai AFTER INSERT ON employees BEGIN
    INSERT INTO employees_fts(rowid, first_name, last_name, email, title, department, location)
    VALUES (new.id, new.first_name, new.last_name, new.email, new.title, new.department, new.location);
END;

CREATE TRIGGER employees_ad AFTER DELETE ON employees BEGIN
    INSERT INTO employees_fts(employees_fts, rowid, first_name, last_name, email, title, department, location)
    VALUES ('delete', old.id, old.first_name, old.last_name, old.email, old.title, old.department, old.location);
END;

CREATE TRIGGER employees_au AFTER UPDATE ON employees BEGIN
    INSERT INTO employees_fts(employees_fts, rowid, first_name, last_name, email, title, department, location)
    VALUES ('delete', old.id, old.first_name, old.last_name, old.email, old.title, old.department, old.location);
    INSERT INTO employees_fts(rowid, first_name, last_name, email, title, department, location)
    VALUES (new.id, new.first_name, new.last_name, new.email, new.title, new.department, new.location);
END;
