# Staff birthdays in profiles and Manager account creation

The profile API previously accepted `dateOfBirth` but silently ignored it for users
without a Member record. `Users.DateOfBirth` now holds the optional birthday for
staff accounts. `GET /api/profile` and `PUT /api/profile` keep their existing DTOs;
Member birthdays continue to use the Members table.

Manager can also supply the optional birthday to `POST /api/users/staff` when
creating Coach and Receptionist accounts. Staff list, detail and creation
responses return the stored `dateOfBirth`, so the frontend can verify persistence
instead of guessing that the submitted value was saved. Invalid dates are
rejected before an account is created.

Apply the additive `AddStaffBirthDate` migration before serving the updated API:

```sh
dotnet ef database update
```

The existing startup migration path also applies this migration at deployment.
The new column is nullable so existing staff accounts remain valid. Omitted
birthdays preserve their previous value. The current shared birth-date policy
(1950 onwards, minimum age 16) is checked before other profile changes are applied,
including when the service is called directly.

Validation:

```sh
dotnet test tests/SportsCenterAPI.Tests/SportsCenterAPI.Tests.csproj --filter FullyQualifiedName~ProfileServiceTests
```

The tests cover both roles without Member rows, persisted readback, unrelated edits
preserving the birthday and rejected dates leaving the profile unchanged. No
application database or live account was modified during implementation.
