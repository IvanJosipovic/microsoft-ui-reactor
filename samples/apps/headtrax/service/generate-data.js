#!/usr/bin/env node
// HeadTrax data generator – creates a realistic employee hierarchy in SQLite.
//
// Usage:
//   node generate-data.js                  # default 200,000 employees
//   node generate-data.js --count 250000   # custom count
//   node generate-data.js --count 500      # small dataset for dev
//   node generate-data.js --db ./my.db     # custom db path
//   node generate-data.js --reset          # drop & recreate even if db exists

import { readFileSync } from "fs";
import { join, dirname } from "path";
import { fileURLToPath } from "url";
import Database from "better-sqlite3";
import { faker } from "@faker-js/faker";

const __dirname = dirname(fileURLToPath(import.meta.url));

// ── CLI args ────────────────────────────────────────────────────────────────

function parseArgs() {
  const args = process.argv.slice(2);
  const opts = { count: 200_000, db: join(__dirname, "headtrax.db"), reset: false };
  for (let i = 0; i < args.length; i++) {
    if (args[i] === "--count" && args[i + 1]) opts.count = parseInt(args[++i], 10);
    if (args[i] === "--db" && args[i + 1]) opts.db = args[++i];
    if (args[i] === "--reset") opts.reset = true;
  }
  return opts;
}

// ── Reference data ──────────────────────────────────────────────────────────

const DEPARTMENTS = [
  "Engineering",
  "Product",
  "Design",
  "Sales",
  "Marketing",
  "Human Resources",
  "Finance",
  "Legal",
  "Operations",
  "Customer Success",
  "Data Science",
  "Security",
  "IT",
  "Facilities",
];

const LOCATIONS = [
  "Seattle, WA",
  "San Francisco, CA",
  "New York, NY",
  "Austin, TX",
  "Boston, MA",
  "London, UK",
  "Dublin, IE",
  "Tokyo, JP",
  "Singapore, SG",
  "Bangalore, IN",
  "Toronto, CA",
  "Berlin, DE",
  "Sydney, AU",
  "Denver, CO",
  "Chicago, IL",
];

const COST_CENTERS = [
  "CC-1000", "CC-1100", "CC-1200", "CC-1300", "CC-1400",
  "CC-2000", "CC-2100", "CC-2200", "CC-2300", "CC-2400",
  "CC-3000", "CC-3100", "CC-3200", "CC-3300", "CC-3400",
];

// Titles by level (0 = CEO, 7 = IC)
const TITLES_BY_LEVEL = {
  0: ["Chief Executive Officer"],
  1: [
    "Chief Technology Officer",
    "Chief Financial Officer",
    "Chief Marketing Officer",
    "Chief People Officer",
    "Chief Operating Officer",
    "Chief Revenue Officer",
    "SVP Engineering",
    "SVP Product",
  ],
  2: [
    "VP Engineering", "VP Product", "VP Design", "VP Sales",
    "VP Marketing", "VP Finance", "VP Human Resources", "VP Operations",
    "VP Customer Success", "VP Data Science", "VP Security", "VP IT",
  ],
  3: [
    "Director of Engineering", "Director of Product", "Director of Design",
    "Director of Sales", "Director of Marketing", "Director of Finance",
    "Director of HR", "Director of Operations", "Director of Customer Success",
    "Director of Data Science", "Director of Security", "Director of IT",
  ],
  4: [
    "Senior Engineering Manager", "Senior Product Manager", "Senior Design Manager",
    "Senior Sales Manager", "Senior Marketing Manager", "Senior Finance Manager",
    "Senior HR Manager", "Senior Operations Manager",
  ],
  5: [
    "Engineering Manager", "Product Manager", "Design Manager",
    "Sales Manager", "Marketing Manager", "Finance Manager",
    "HR Manager", "Operations Manager", "Customer Success Manager",
  ],
  6: [
    "Tech Lead", "Staff Engineer", "Senior Software Engineer",
    "Senior Product Designer", "Senior Data Scientist", "Senior Sales Executive",
    "Senior Marketing Specialist", "Senior Financial Analyst", "Senior HR Specialist",
  ],
  7: [
    "Software Engineer", "Software Engineer II", "Software Engineer III",
    "Product Designer", "UX Researcher", "Data Analyst", "Data Scientist",
    "Sales Development Rep", "Account Executive", "Marketing Specialist",
    "Financial Analyst", "HR Coordinator", "Operations Analyst",
    "Customer Success Associate", "Security Analyst", "IT Specialist",
    "Technical Writer", "QA Engineer", "DevOps Engineer", "Site Reliability Engineer",
    "Business Analyst", "Recruiter", "Program Manager", "Solutions Architect",
  ],
};

// Salary bands by level [min, max]
const SALARY_BANDS = {
  0: [450_000, 800_000],
  1: [300_000, 550_000],
  2: [220_000, 400_000],
  3: [170_000, 300_000],
  4: [140_000, 240_000],
  5: [120_000, 200_000],
  6: [100_000, 170_000],
  7: [65_000, 140_000],
};

// Stock option grants by level [min, max]
const STOCK_BANDS = {
  0: [100_000, 500_000],
  1: [50_000, 200_000],
  2: [20_000, 100_000],
  3: [10_000, 50_000],
  4: [5_000, 25_000],
  5: [2_000, 15_000],
  6: [1_000, 8_000],
  7: [0, 5_000],
};

const STATUSES = ["Active", "Active", "Active", "Active", "Active",
                  "Active", "Active", "Active", "On Leave", "Terminated"];
const GENDERS = ["Male", "Female", "Non-binary", "Prefer not to say"];

// ── Hierarchy shape ─────────────────────────────────────────────────────────
// Determines how many people at each level. The bulk are ICs at level 7.

function computeLevelCounts(totalCount) {
  // These ratios produce a realistic org shape.
  // Level 0 is always 1 (CEO). Remaining distributed proportionally.
  const ratios = [0, 0.003, 0.012, 0.03, 0.06, 0.12, 0.15]; // levels 1-6
  const remaining = totalCount - 1;

  const counts = { 0: 1 };
  let allocated = 0;
  for (let lvl = 1; lvl <= 6; lvl++) {
    counts[lvl] = Math.max(1, Math.round(remaining * ratios[lvl]));
    allocated += counts[lvl];
  }
  counts[7] = remaining - allocated;
  return counts;
}

// ── Employee generator ──────────────────────────────────────────────────────

function generateEmployee(id, level, managerId, department) {
  const sex = faker.helpers.arrayElement(["male", "female"]);
  const firstName = faker.person.firstName(sex);
  const lastName = faker.person.lastName(sex);
  const domain = "headtrax.example.com";
  const email = `${firstName.toLowerCase()}.${lastName.toLowerCase()}${id}@${domain}`;
  const employeeNumber = `HT-${String(id).padStart(6, "0")}`;

  const title = faker.helpers.arrayElement(TITLES_BY_LEVEL[level]);
  const location = faker.helpers.arrayElement(LOCATIONS);
  const [salMin, salMax] = SALARY_BANDS[level];
  const salary = Math.round(faker.number.float({ min: salMin, max: salMax }) / 1000) * 1000;
  const [stMin, stMax] = STOCK_BANDS[level];
  const stockOptions = faker.number.int({ min: stMin, max: stMax });

  const hireDate = faker.date.between({
    from: "2005-01-01",
    to: "2025-12-31",
  }).toISOString().slice(0, 10);

  const birthDate = faker.date.between({
    from: "1960-01-01",
    to: "2002-12-31",
  }).toISOString().slice(0, 10);

  const gender = sex === "male" ? "Male" : sex === "female" ? "Female" : faker.helpers.arrayElement(GENDERS);
  const perfRating = Math.round(faker.number.float({ min: 1.0, max: 5.0 }) * 10) / 10;
  const isRemote = faker.datatype.boolean({ probability: 0.3 }) ? 1 : 0;
  const status = faker.helpers.arrayElement(STATUSES);
  const costCenter = faker.helpers.arrayElement(COST_CENTERS);
  const phone = faker.phone.number({ style: "national" });

  return [
    id,                // id
    employeeNumber,    // employee_number
    firstName,         // first_name
    lastName,          // last_name
    email,             // email
    phone,             // phone
    title,             // title
    department,        // department
    location,          // location
    hireDate,          // hire_date
    salary,            // salary
    managerId,         // manager_id
    level,             // level
    status,            // status
    birthDate,         // birth_date
    gender,            // gender
    perfRating,        // performance_rating
    stockOptions,      // stock_options
    isRemote,          // is_remote
    costCenter,        // cost_center
  ];
}

// ── Main ────────────────────────────────────────────────────────────────────

function main() {
  const opts = parseArgs();
  console.log(`\n🏢 HeadTrax Data Generator`);
  console.log(`   Target: ${opts.count.toLocaleString()} employees`);
  console.log(`   Database: ${opts.db}\n`);

  const startTime = Date.now();

  // Open database & apply schema
  const db = new Database(opts.db);
  const schema = readFileSync(join(__dirname, "schema.sql"), "utf-8");
  db.exec(schema);

  const levelCounts = computeLevelCounts(opts.count);
  console.log("   Level distribution:");
  for (const [lvl, count] of Object.entries(levelCounts)) {
    const label = lvl === "0" ? "CEO" : lvl === "7" ? "IC" : `L${lvl}`;
    console.log(`     ${label}: ${count.toLocaleString()}`);
  }
  console.log();

  // Prepare batched insert
  const insertSQL = `INSERT INTO employees (
    id, employee_number, first_name, last_name, email, phone,
    title, department, location, hire_date, salary, manager_id,
    level, status, birth_date, gender, performance_rating,
    stock_options, is_remote, cost_center
  ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`;

  const insert = db.prepare(insertSQL);
  const BATCH_SIZE = 5000;

  // Track employees by level so children can pick a random parent
  const idsByLevel = {};
  let nextId = 1;

  // We assign departments top-down. The CEO and C-suite span all departments.
  // VPs and below inherit or get assigned a department.
  const deptAssignment = new Map(); // id → department

  const insertBatch = db.transaction((rows) => {
    for (const row of rows) insert.run(...row);
  });

  let totalInserted = 0;
  let batch = [];

  function flushBatch() {
    if (batch.length > 0) {
      insertBatch(batch);
      totalInserted += batch.length;
      batch = [];
      if (totalInserted % 50_000 === 0 || totalInserted === opts.count) {
        const elapsed = ((Date.now() - startTime) / 1000).toFixed(1);
        const pct = ((totalInserted / opts.count) * 100).toFixed(0);
        process.stdout.write(`\r   Progress: ${totalInserted.toLocaleString()} / ${opts.count.toLocaleString()} (${pct}%) – ${elapsed}s`);
      }
    }
  }

  // Generate level by level (top-down so parents exist before children)
  for (let level = 0; level <= 7; level++) {
    const count = levelCounts[level];
    idsByLevel[level] = [];

    for (let i = 0; i < count; i++) {
      const id = nextId++;

      // Pick manager from the level above (CEO has no manager)
      let managerId = null;
      let department;

      if (level === 0) {
        department = "Executive";
      } else {
        const parentLevel = level - 1;
        const parents = idsByLevel[parentLevel];
        // Distribute children roughly evenly across parents with some variance
        const parentIdx = i % parents.length;
        managerId = parents[parentIdx];
        // Inherit department from manager, or assign one
        if (level <= 2) {
          department = DEPARTMENTS[i % DEPARTMENTS.length];
        } else {
          department = deptAssignment.get(managerId) || DEPARTMENTS[i % DEPARTMENTS.length];
        }
      }

      deptAssignment.set(id, department);

      const row = generateEmployee(id, level, managerId, department);
      batch.push(row);
      idsByLevel[level].push(id);

      if (batch.length >= BATCH_SIZE) {
        flushBatch();
      }
    }
  }
  flushBatch();

  console.log("\n\n   Building FTS index...");
  // Populate the FTS index from existing data
  db.exec(`
    INSERT INTO employees_fts(rowid, first_name, last_name, email, title, department, location)
    SELECT id, first_name, last_name, email, title, department, location FROM employees;
  `);

  // Gather some stats
  const stats = db.prepare("SELECT COUNT(*) as total FROM employees").get();
  const deptStats = db.prepare(
    "SELECT department, COUNT(*) as cnt FROM employees GROUP BY department ORDER BY cnt DESC"
  ).all();
  const locStats = db.prepare(
    "SELECT location, COUNT(*) as cnt FROM employees GROUP BY location ORDER BY cnt DESC LIMIT 5"
  ).all();

  const elapsed = ((Date.now() - startTime) / 1000).toFixed(1);

  console.log(`\n   ✅ Done in ${elapsed}s`);
  console.log(`   Total employees: ${stats.total.toLocaleString()}`);
  console.log(`\n   Top departments:`);
  for (const { department, cnt } of deptStats.slice(0, 8)) {
    console.log(`     ${department}: ${cnt.toLocaleString()}`);
  }
  console.log(`\n   Top locations:`);
  for (const { location, cnt } of locStats) {
    console.log(`     ${location}: ${cnt.toLocaleString()}`);
  }

  db.close();
  console.log();
}

main();
