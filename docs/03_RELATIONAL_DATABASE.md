In Entity Framework Core (EF Core), you define relationships primarily using **Navigation Properties** inside your entity classes. A navigation property is simply a property that holds a reference to the related entity (or a collection of related entities).

Here is a step-by-step guide with examples for defining each type of relationship.

---

### 1. One-to-Many / Many-to-One
This is the most common relationship. 
**Example:** A `Blog` can have **many** `Posts`. A `Post` belongs to **one** `Blog`.

**Step 1:** On the "One" side (`Blog`), add a collection property (like `List` or `ICollection`) for the "Many" side.
**Step 2:** On the "Many" side (`Post`), add a reference property back to the "One" side, and optionally explicitly define the Foreign Key.

```csharp
public class Blog
{
    public int Id { get; set; }
    public string Name { get; set; }

    // Navigation property (One-to-Many)
    // A Blog contains many Posts
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; }

    // Foreign Key (Optional, but highly recommended for clarity)
    public int BlogId { get; set; } 

    // Navigation property (Many-to-One)
    // A Post belongs to one Blog
    public Blog Blog { get; set; } = null!;
}
```
*EF Core automatically sees the `ICollection<Post>` and the `Blog` property and wires up a One-to-Many relationship in the database.*

---

### 2. One-to-One
A One-to-One relationship requires a reference navigation property on **both** sides. One of the entities must be the "Dependent" (it holds the foreign key) and the other is the "Principal".
**Example:** A `User` has **one** `Address`. An `Address` belongs to **one** `User`.

**Step 1:** On the Principal side (`User`), add a single reference property to the Dependent.
**Step 2:** On the Dependent side (`Address`), add a single reference property to the Principal, **and** add a Foreign Key property.

```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }

    // Navigation property (One-to-One)
    public Address? Address { get; set; } 
}

public class Address
{
    public int Id { get; set; }
    public string StreetName { get; set; }

    // Foreign Key mapping back to User
    public int UserId { get; set; }

    // Navigation property (One-to-One)
    public User User { get; set; } = null!;
}
```
*EF Core knows this is One-to-One because neither side uses an `ICollection`, and it knows `Address` is the dependent because it contains the `UserId` foreign key.*

---

### 3. Many-to-Many
A Many-to-Many relationship requires a collection navigation property on **both** sides. (Note: This requires EF Core 5.0 or newer, which you are using).
**Example:** A `Student` can enroll in **many** `Courses`. A `Course` can have **many** `Students`.

**Step 1:** On the first entity (`Student`), add a collection property for the second entity.
**Step 2:** On the second entity (`Course`), add a collection property for the first entity.

```csharp
public class Student
{
    public int Id { get; set; }
    public string Name { get; set; }

    // Navigation property (Many-to-Many)
    // A Student has many Courses
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; }

    // Navigation property (Many-to-Many)
    // A Course has many Students
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
```
*EF Core automatically sees collections on both sides. Behind the scenes, it will automatically generate the hidden "join table" (e.g., `CourseStudent`) in your PostgreSQL database to handle the Many-to-Many link!*