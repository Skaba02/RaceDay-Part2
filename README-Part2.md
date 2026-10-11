# RaceDay - Part 2: RESTful API Development

## Description

This part builds the RESTful API that powers RaceDay, following the plan from Part 1. It is written in ASP.NET Core Web API (C#) with Entity Framework Core (Code-First), and follows the same six-entity schema from the Part 1 ERD and SQL script - Roles, Users, Events, Categories, Enrolments, and Results.

Two small fields were added to the Event entity that were not in the Part 1 ERD: `DistanceKm` and `EventType`. The Part 2 brief specifically asks each event to capture its own distance and type (Run, Walk, or Cycle), on top of the age/distance Categories already defined under each event in Part 1.

## Roles

- **Organiser** - manages events and categories, views who has enrolled, and records results.
- **Participant** - browses events, enrols in a category, and views their own enrolments and results.

Role-based access is enforced through session management: after logging in, the user's Id and Role are stored in a server-side session tied to their session cookie. Every request after that is checked against the session - some endpoints just need someone logged in, others are restricted to one specific role.

## Project Structure

```
Part2/
  RaceDay.Api/            - the API project
    Models/               - EF Core entities
    Data/                 - the database context
    Dtos/                 - request/response shapes
    Controllers/          - one controller per resource
    Filters/              - session-based auth/role checks
  RaceDay.Api.Tests/      - unit tests (xUnit)
  RaceDay.sln
```

## Setup and Run Instructions

1. **Install prerequisites**: Visual Studio 2022 (with the "ASP.NET and web development" workload) and SQL Server (Express or Developer edition is fine).
2. **Open the solution**: open `Part2/RaceDay.sln` in Visual Studio.
3. **Set your connection string**: open `RaceDay.Api/appsettings.json` and replace `YOUR_SERVER_NAME` with your own SQL Server instance name.
4. **Create the database**: open the Package Manager Console (Tools -> NuGet Package Manager -> Package Manager Console), set the Default Project to `RaceDay.Api`, then run:
   ```
   Add-Migration InitialCreate
   Update-Database
   ```
   This creates the `RaceDayDB` database with the schema above and seeds the two roles.
5. **Run the project**: press F5, or click the green Run arrow. Swagger opens automatically in your browser.
6. **Try it out**: register a user via `POST /api/auth/register`, then log in via `POST /api/auth/login`. Swagger keeps the session cookie automatically, so role-restricted endpoints will work right after logging in, in the same browser tab.

## Running the Tests

- **In Visual Studio**: open Test Explorer (Test -> Test Explorer) and click "Run All Tests". The tests use an in-memory database, so no SQL Server connection is needed to run them.
- **From the command line**: `dotnet test Part2/RaceDay.sln`

## CI/CD

A GitHub Actions workflow (`.github/workflows/build-and-test.yml`) builds the project and runs all unit tests on every push.

**CI/CD screenshot:**

`[Insert screenshot of the successful green build here after your first push]`

## Video Walkthrough

`[Insert your unlisted YouTube link here]`

The video covers:
- The running API and its Swagger UI
- The code structure (Models, Data, Dtos, Controllers, Filters)
- How authentication and role-based access work
- The unit tests and what they check
