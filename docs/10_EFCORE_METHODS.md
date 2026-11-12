Cheat sheet of the most necessary and frequently used Entity Framework Core methods, broken down by what they do, their parameters, and exactly when to use them.

---

### 1. Fetching a Single Entity

* **`FindAsync(params object[] keyValues)`**
* **Operation:** Looks for an entity with the specified Primary Key. It first checks the local memory (DbContext cache). If it finds it there, it returns it without querying the database. Otherwise, it queries the database.
* **Params:** The primary key value(s) (e.g., a `Guid` or `int`).
* **When to use:** Whenever you are looking up a record by its exact Primary Key. It is the fastest, most efficient way to get a single entity by ID.


* **`FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)`**
* **Operation:** Returns the first record that matches the condition. If nothing matches, it returns `null`.
* **Params:** A lambda expression for the condition (e.g., `p => p.Phone == "123456"`).
* **When to use:** When searching by a non-primary key (like an email or phone number), or when you expect multiple results but only care about the first one.


* **`SingleOrDefaultAsync(Expression<Func<T, bool>> predicate)`**
* **Operation:** Returns the *only* record that matches the condition. If it finds *more than one* matching record, it throws an exception. If it finds none, it returns `null`.
* **Params:** A lambda expression for the condition.
* **When to use:** When you are querying a column that is supposed to be unique (like an Email) and you want the application to crash/warn you if duplicate data somehow entered the database.



---

### 2. Filtering and Fetching Collections

* **`Where(Expression<Func<T, bool>> predicate)`**
* **Operation:** Filters the query based on a condition. *Note: This does not execute the query.* It just builds the SQL statement.
* **Params:** A lambda expression (e.g., `p => p.Gender == Gender.Male`).
* **When to use:** Any time you need to filter a list of records.


* **`ToListAsync()`**
* **Operation:** Executes the SQL query built by `Where()`, `Select()`, etc., and materializes the results into a C# `List<T>`.
* **Params:** None.
* **When to use:** Put this at the very end of your LINQ chain when you actually want to execute the query and pull the data into memory.


* **`Select(Expression<Func<T, TResult>> selector)`**
* **Operation:** Maps the database columns to a specific DTO or anonymous object. This translates to the `SELECT col1, col2` part of a SQL query.
* **Params:** A lambda expression mapping the properties (e.g., `p => new { p.FirstName, p.LastName }`).
* **When to use:** When you only need 2 columns from a table with 50 columns. This drastically reduces memory usage and network traffic.



---

### 3. Checks and Aggregates

* **`AnyAsync(Expression<Func<T, bool>> predicate)`**
* **Operation:** Checks if *at least one* record matches the condition. Returns a `bool`. It translates to an `EXISTS` SQL query, meaning it stops searching the database the millisecond it finds the first match.
* **Params:** A lambda expression condition.
* **When to use:** When validating uniqueness (e.g., checking if an email is already taken). Never use `CountAsync() > 0` or `.Where(...).ToList().Any()` for this; `AnyAsync()` is vastly faster.


* **`CountAsync(Expression<Func<T, bool>> predicate)`**
* **Operation:** Counts how many records match a condition. Returns an `int`.
* **Params:** A lambda expression condition (optional).
* **When to use:** For pagination (getting total pages) or displaying dashboard metrics.



---

### 4. Performance & Relational Data

* **`Include(Expression<Func<T, TProperty>> navigationPropertyPath)`**
* **Operation:** Tells EF Core to execute a SQL `JOIN` to bring back related data.
* **Params:** A lambda expression pointing to a navigation property (e.g., `u => u.Profile`).
* **When to use:** When you are fetching a User and you *also* need their Profile data in the exact same query.


* **`AsNoTracking()`**
* **Operation:** Tells EF Core *not* to track changes to the returned entities.
* **Params:** None.
* **When to use:** Put this on **every single read-only query** (e.g., fetching data to return to the frontend). It makes queries roughly 30% faster and uses significantly less memory because EF Core doesn't have to set up internal snapshot trackers.
* *Example:* `await _dbContext.Users.AsNoTracking().ToListAsync();`



---

### 5. Modifying Data

* **`Add(T entity)` / `Update(T entity)` / `Remove(T entity)**`
* **Operation:** Changes the state of an entity in EF Core's memory to `Added`, `Modified`, or `Deleted`. *Note: These are synchronous methods and they do NOT hit the database yet.*
* **Params:** The entity object.
* **When to use:** Right before you want to save changes. (You rarely need `Update()` if you fetched the entity first, because EF Core automatically tracks changes to fetched entities).


* **`SaveChangesAsync()`**
* **Operation:** Looks at all the tracked entities, generates the necessary `INSERT`, `UPDATE`, and `DELETE` SQL commands, wraps them in a single database transaction, and executes them.
* **Params:** CancellationToken (optional).
* **When to use:** Call this exactly once at the end of your service method to commit all your changes at the same time.